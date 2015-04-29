#if !NOSOCKET
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace ChainUtils.Protocol
{
	public enum NodeState : int
	{
		Failed,
		Offline,
		Disconnecting,
		Connected,
		HandShaked
	}


	public delegate void NodeEventHandler(Node node);
	public delegate void NodeEventMessageIncoming(Node node, IncomingMessage message);
	public delegate void NodeStateEventHandler(Node node, NodeState oldState);
	public class Node : IDisposable
	{
		public class NodeConnection
		{
			private readonly Node _node;
			public Node Node
			{
				get
				{
					return _node;
				}
			}
			private readonly Socket _socket;
			public Socket Socket
			{
				get
				{
					if(_isDisposed)
					{
						throw new InvalidOperationException("Connection disposed");
					}
					return _socket;
				}
			}
			private readonly ManualResetEvent _disconnected;
			public ManualResetEvent Disconnected
			{
				get
				{
					return _disconnected;
				}
			}
			private readonly CancellationTokenSource _cancel;
			public CancellationTokenSource Cancel
			{
				get
				{
					return _cancel;
				}
			}
#if NOTRACESOURCE
			internal
#else
			public
#endif
 TraceCorrelation TraceCorrelation
			{
				get
				{
					return Node.TraceCorrelation;
				}
			}

			public NodeConnection(Node node, Socket socket)
			{
				_node = node;
				_socket = socket;
				_disconnected = new ManualResetEvent(false);
				_cancel = new CancellationTokenSource();
			}

			EventLoopMessageListener<IncomingMessage> _pingListener;

			bool _isDisposed;
			public void Dispose()
			{
				if(!_isDisposed)
				{
					Utils.SafeCloseSocket(Socket);
					if(_pingListener != null)
					{
						Node.MessageProducer.RemoveMessageListener(_pingListener);
						_pingListener.Dispose();
						_pingListener = null;
					}
					_isDisposed = true;
				}
			}

			void MessageReceived(IncomingMessage message)
			{
				var ping = message.Message.Payload as PingPayload;
				if(ping != null)
				{
					message.Node.SendMessage(ping.CreatePong());
				}

				var version = message.Message.Payload as VersionPayload;
				if(version != null && Node.State == NodeState.HandShaked)
				{
					if((uint)message.Node.Version >= 70002)
						message.Node.SendMessage(new RejectPayload()
						{
							Code = RejectCode.Duplicate
						});
				}
				Node.OnMessageReceived(message);
			}

			public void BeginListen()
			{
				new Thread(() =>
				{
					using(TraceCorrelation.Open(false))
					{
						NodeServerTrace.Information("Listening");
						_pingListener = new EventLoopMessageListener<IncomingMessage>(MessageReceived);
						Node.MessageProducer.AddMessageListener(_pingListener);
						try
						{
							while(!Cancel.Token.IsCancellationRequested)
							{
								PerformanceCounter counter;
								var message = Message.ReadNext(Socket, Node.Network, Node.Version, Cancel.Token, out counter);

								Node.LastSeen = DateTimeOffset.UtcNow;
								Node.MessageProducer.PushMessage(new IncomingMessage()
								{
									Message = message,
									Socket = Socket,
									Node = Node
								});
								Node.Counter.Add(counter);
							}
						}
						catch(OperationCanceledException)
						{
						}
						catch(Exception ex)
						{
							if(Node.State != NodeState.Disconnecting)
							{
								Node.State = NodeState.Failed;
								NodeServerTrace.Error("Connection to server stopped unexpectedly", ex);
							}
						}
						NodeServerTrace.Information("Stop listening");
						if(Node.State != NodeState.Failed)
							Node.State = NodeState.Offline;
						Dispose();

						_cancel.Cancel();
						_disconnected.Set();

					}
				}).Start();
			}

		}


		volatile NodeState _state = NodeState.Offline;
		public NodeState State
		{
			get
			{
				return _state;
			}
			private set
			{
				TraceCorrelation.LogInside(() => NodeServerTrace.Information("State changed from " + _state + " to " + value));
				var previous = _state;
				_state = value;
				if(previous != _state)
				{
					OnStateChanged(previous);
				}

				if(value == NodeState.Failed || value == NodeState.Offline)
				{
					TraceCorrelation.LogInside(() => NodeServerTrace.Trace.TraceEvent(TraceEventType.Stop, 0, "Communication closed"));
				}

				if(value == NodeState.Offline || value == NodeState.Failed)
				{
					OnDisconnected();
				}
			}
		}

		public event NodeStateEventHandler StateChanged;
		private void OnStateChanged(NodeState previous)
		{
			if(StateChanged != null)
			{
				try
				{
					StateChanged(this, previous);
				}
				catch(Exception ex)
				{
					TraceCorrelation.LogInside(() => NodeServerTrace.Error("Error while Disconnected event raised", ex));
				}
			}
		}

		public event NodeEventHandler Disconnected;
		private void OnDisconnected()
		{
			if(Disconnected != null)
			{
				try
				{
					Disconnected(this);
				}
				catch(Exception ex)
				{
					TraceCorrelation.LogInside(() => NodeServerTrace.Error("Error while Disconnected event raised", ex));
				}
			}
		}


		internal readonly NodeConnection Connection;


		public static Node ConnectToLocal(Network network,
								ProtocolVersion myVersion = ProtocolVersion.PROTOCOL_VERSION,
								bool isRelay = true,
								CancellationToken cancellation = default(CancellationToken))
		{
			return Connect(network, Utils.ParseIpEndpoint("localhost", network.DefaultPort), myVersion, isRelay, cancellation);
		}

		public static Node Connect(Network network,
								 string endpoint,
								 ProtocolVersion myVersion = ProtocolVersion.PROTOCOL_VERSION,
								bool isRelay = true,
								CancellationToken cancellation = default(CancellationToken))
		{
			return Connect(network, Utils.ParseIpEndpoint(endpoint, network.DefaultPort), myVersion, isRelay, cancellation);
		}
		public static Node Connect(Network network,
								 IPEndPoint endpoint,
								 ProtocolVersion myVersion = ProtocolVersion.PROTOCOL_VERSION,
								bool isRelay = true,
								CancellationToken cancellation = default(CancellationToken))
		{
			var peer = new Peer(PeerOrigin.Manual, new NetworkAddress()
			{
				Time = DateTimeOffset.UtcNow,
				Endpoint = endpoint
			});

			var version = new VersionPayload()
			{
				Nonce = 12345,
				UserAgent = VersionPayload.GetChainUtilsUserAgent(),
				Version = myVersion,
				StartHeight = 0,
				Timestamp = DateTimeOffset.UtcNow,
				AddressReceiver = peer.NetworkAddress.Endpoint,
				AddressFrom = new IPEndPoint(IPAddress.Parse("0.0.0.0").MapToIPv6(), network.DefaultPort),
				Relay = isRelay
			};
			return new Node(peer, network, version, cancellation);
		}

		internal Node(Peer peer, Network network, VersionPayload myVersion, CancellationToken cancellation)
		{
			_myVersion = myVersion;
			Version = _myVersion.Version;
			Network = network;
			_peer = peer;
			LastSeen = peer.NetworkAddress.Time;

			var socket = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);
#if !NOIPDUALMODE
			socket.DualMode = true;
#endif
			Connection = new NodeConnection(this, socket);
			using(TraceCorrelation.Open())
			{
				try
				{
					var ar = socket.BeginConnect(Peer.NetworkAddress.Endpoint, null, null);
					WaitHandle.WaitAny(new[] { ar.AsyncWaitHandle, cancellation.WaitHandle });
					cancellation.ThrowIfCancellationRequested();
					socket.EndConnect(ar);
					State = NodeState.Connected;
					NodeServerTrace.Information("Outbound connection successfull");
				}
				catch(OperationCanceledException)
				{
					Utils.SafeCloseSocket(socket);
					NodeServerTrace.Information("Connection to node cancelled");
					State = NodeState.Offline;
					return;
				}
				catch(Exception ex)
				{
					Utils.SafeCloseSocket(socket);
					NodeServerTrace.Error("Error connecting to the remote endpoint ", ex);
					State = NodeState.Failed;
					throw;
				}
				Connection.BeginListen();
			}
		}


		public event NodeEventMessageIncoming MessageReceived;
		protected void OnMessageReceived(IncomingMessage message)
		{
			var messageReceived = MessageReceived;
			if(messageReceived != null)
				messageReceived(this, message);
		}

		internal Node(Peer peer, Network network, VersionPayload myVersion, Socket socket, VersionPayload peerVersion)
		{
			_myVersion = myVersion;
			Network = network;
			_peer = peer;
			Connection = new NodeConnection(this, socket);
			_peerVersion = peerVersion;
			Version = peerVersion.Version;
			LastSeen = peer.NetworkAddress.Time;
			TraceCorrelation.LogInside(() =>
			{
				NodeServerTrace.Information("Connected to advertised node " + _peer.NetworkAddress.Endpoint);
				State = NodeState.Connected;
			});
			Connection.BeginListen();
		}

		private readonly Peer _peer;
		public Peer Peer
		{
			get
			{
				return _peer;
			}
		}

		public DateTimeOffset LastSeen
		{
			get;
			private set;
		}

		TraceCorrelation _traceCorrelation = null;
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
#if NOTRACESOURCE
		internal
#else
		public
#endif
 TraceCorrelation TraceCorrelation
		{
			get
			{
				if(_traceCorrelation == null)
				{
					_traceCorrelation = new TraceCorrelation(NodeServerTrace.Trace, "Communication with " + Peer.NetworkAddress.Endpoint.ToString());
				}
				return _traceCorrelation;
			}
		}
		public void SendMessage(Payload payload)
		{
			var message = new Message();
			message.Magic = Network.Magic;
			message.UpdatePayload(payload, Version);
			TraceCorrelation.LogInside(() => NodeServerTrace.Verbose("Sending message " + message));

			var bytes = message.ToBytes();
			Counter.AddWritten(bytes.LongLength);
			var result = Connection.Socket.Send(bytes);
		}

		private PerformanceCounter _counter;
		public PerformanceCounter Counter
		{
			get
			{
				if(_counter == null)
					_counter = new PerformanceCounter();
				return _counter;
			}
		}

		public ProtocolVersion Version
		{
			get;
			private set;
		}

		private readonly MessageProducer<IncomingMessage> _messageProducer = new MessageProducer<IncomingMessage>();
		public MessageProducer<IncomingMessage> MessageProducer
		{
			get
			{
				return _messageProducer;
			}
		}

		public TPayload ReceiveMessage<TPayload>(TimeSpan timeout) where TPayload : Payload
		{
			var source = new CancellationTokenSource();
			source.CancelAfter(timeout);
			return ReceiveMessage<TPayload>(source.Token);
		}



		public TPayload ReceiveMessage<TPayload>(CancellationToken cancellationToken = default(CancellationToken)) where TPayload : Payload
		{
			using(var listener = new NodeListener(this))
			{
				return listener.ReceivePayload<TPayload>(cancellationToken);
			}
		}

		private readonly VersionPayload _myVersion;
		public VersionPayload MyVersion
		{
			get
			{
				return _myVersion;
			}
		}

		VersionPayload _peerVersion;
		public VersionPayload PeerVersion
		{
			get
			{
				return _peerVersion;
			}
		}

		public void VersionHandshake(CancellationToken cancellationToken = default(CancellationToken))
		{
			using(var listener = CreateListener()
									.Where(p => p.Message.Payload is VersionPayload ||
												p.Message.Payload is RejectPayload ||
												p.Message.Payload is VerAckPayload))
			{
				using(TraceCorrelation.Open())
				{
					SendMessage(MyVersion);

					var payload = listener.ReceivePayload<Payload>(cancellationToken);
					if(payload is RejectPayload)
					{
						throw new InvalidOperationException("Handshake rejected : " + ((RejectPayload)payload).Reason);
					}
					var version = (VersionPayload)payload;
					_peerVersion = version;
					Version = version.Version;
					if(!version.AddressReceiver.Address.Equals(MyVersion.AddressFrom.Address))
					{
						NodeServerTrace.Warning("Different external address detected by the node " + version.AddressReceiver.Address + " instead of " + MyVersion.AddressFrom.Address);
					}
					if(version.Version < ProtocolVersion.MinPeerProtoVersion)
					{
						NodeServerTrace.Warning("Outdated version " + version.Version + " disconnecting");
						Disconnect();
						return;
					}
					SendMessage(new VerAckPayload());
					listener.ReceivePayload<VerAckPayload>(cancellationToken);
					State = NodeState.HandShaked;
				}
			}
		}




		public void RespondToHandShake(CancellationToken cancellation = default(CancellationToken))
		{
			using(TraceCorrelation.Open())
			{
				var listener = new PollMessageListener<IncomingMessage>();
				using(MessageProducer.AddMessageListener(listener))
				{
					NodeServerTrace.Information("Responding to handshake");
					SendMessage(MyVersion);
					listener.ReceiveMessage().AssertPayload<VerAckPayload>();
					SendMessage(new VerAckPayload());
					_state = NodeState.HandShaked;
				}
			}
		}


		public void Disconnect()
		{
			if(State == NodeState.Offline)
				return;
			if(State < NodeState.Connected)
			{
				Connection.Disconnected.WaitOne();
				return;
			}
			using(TraceCorrelation.Open())
			{
				NodeServerTrace.Information("Disconnection request");
				State = NodeState.Disconnecting;
				Connection.Cancel.Cancel();
				Connection.Disconnected.WaitOne();
			}
		}


		public override string ToString()
		{
			return State + " (" + Peer.NetworkAddress.Endpoint + ") " + Peer.Origin;
		}



		public Socket Socket
		{
			get
			{
				return Connection.Socket;
			}
		}

		public ConcurrentChain GetChain(Uint256 hashStop = null, CancellationToken cancellationToken = default(CancellationToken))
		{
			var chain = new ConcurrentChain(Network);
			SynchronizeChain(chain, hashStop, cancellationToken);
			return chain;
		}
		public IEnumerable<ChainedBlock> GetHeadersFromFork(ChainedBlock currentTip,
														Uint256 hashStop = null,
														CancellationToken cancellationToken = default(CancellationToken))
		{
			AssertState(NodeState.HandShaked, cancellationToken);
			using(TraceCorrelation.Open())
			{
				NodeServerTrace.Information("Building chain");
				using(var listener = CreateListener().OfType<HeadersPayload>())
				{
					while(true)
					{
						SendMessage(new GetHeadersPayload()
						{
							BlockLocators = currentTip.GetLocator(),
							HashStop = hashStop
						});
						var headers = listener.ReceivePayload<HeadersPayload>(cancellationToken);
						if(headers.Headers.Count == 0)
							break;
						foreach(var header in headers.Headers)
						{
							var prev = currentTip.FindAncestorOrSelf(header.HashPrevBlock);
							if(prev == null)
							{
								NodeServerTrace.Error("Block Header received out of order " + header.GetHash(), null);
								throw new InvalidOperationException("Block Header received out of order");
							}
							currentTip = new ChainedBlock(header, header.GetHash(), prev);
							yield return currentTip;
						}
						if(currentTip.HashBlock == hashStop)
							break;
					}
				}
			}
		}

		public IEnumerable<ChainedBlock> SynchronizeChain(ChainBase chain, Uint256 hashStop = null, CancellationToken cancellationToken = default(CancellationToken))
		{
			var headers = new List<ChainedBlock>();
			foreach(var header in GetHeadersFromFork(chain.Tip, hashStop, cancellationToken))
			{
				chain.SetTip(header);
				headers.Add(header);
			}
			return headers;
		}

		public IEnumerable<Block> GetBlocks(Uint256 hashStop = null, CancellationToken cancellationToken = default(CancellationToken))
		{
			var genesis = new ChainedBlock(Network.GetGenesis().Header, 0);
			return GetBlocksFromFork(genesis, hashStop, cancellationToken);
		}


		public IEnumerable<Block> GetBlocksFromFork(ChainedBlock currentTip, Uint256 hashStop = null, CancellationToken cancellationToken = default(CancellationToken))
		{
			using(var listener = CreateListener())
			{
				SendMessage(new GetBlocksPayload()
				{
					BlockLocators = currentTip.GetLocator(),
				});

				var headers = GetHeadersFromFork(currentTip, hashStop, cancellationToken);

				foreach(var block in GetBlocks(headers.Select(b => b.HashBlock), cancellationToken))
				{
					yield return block;
				}
			}
			//GetBlocks(neededBlocks.ToEnumerable(false).Select(e => e.HashBlock), cancellationToken);
		}
		public IEnumerable<Block> GetBlocks(IEnumerable<Uint256> neededBlocks, CancellationToken cancellationToken = default(CancellationToken))
		{
			AssertState(NodeState.HandShaked, cancellationToken);
			using(TraceCorrelation.Open())
			{
				NodeServerTrace.Information("Downloading blocks");
				var simultaneous = 70;
				PerformanceSnapshot lastSpeed = null;
				using(var listener = CreateListener()
									.OfType<BlockPayload>())
				{
					foreach(var invs in neededBlocks
										.Select(b => new InventoryVector()
											{
												Type = InventoryType.MsgBlock,
												Hash = b
											})
										.Partition(simultaneous))
					{
						NodeServerTrace.Information("Speed " + lastSpeed);
						var begin = Counter.Snapshot();

						var invsByHash = invs.ToDictionary(k => k.Hash);

						SendMessage(new GetDataPayload(invs.ToArray()));

						var downloadedBlocks = new Block[invs.Count];
						while(invsByHash.Count != 0)
						{
							var block = listener.ReceivePayload<BlockPayload>(cancellationToken).Object;
							var thisHash = block.GetHash();
							if(invsByHash.ContainsKey(thisHash))
							{
								downloadedBlocks[invs.IndexOf(invsByHash[thisHash])] = block;
								invsByHash.Remove(thisHash);
							}
						}
						var end = Counter.Snapshot();
						lastSpeed = end - begin;

						foreach(var downloadedBlock in downloadedBlocks)
						{
							yield return downloadedBlock;
						}
					}

				}
			}
		}

		public NodeListener CreateListener()
		{
			return new NodeListener(this);
		}


		private void AssertState(NodeState nodeState, CancellationToken cancellationToken = default(CancellationToken))
		{
			if(nodeState == NodeState.HandShaked && State == NodeState.Connected)
				VersionHandshake(cancellationToken);
			if(nodeState != State)
				throw new InvalidOperationException("Invalid Node state, needed=" + nodeState + ", current= " + State);
		}

		public Uint256[] GetMempool(CancellationToken cancellationToken = default(CancellationToken))
		{
			AssertState(NodeState.HandShaked);
			using(var listener = CreateListener().OfType<InvPayload>())
			{
				SendMessage(new MempoolPayload());
				return listener.ReceivePayload<InvPayload>(cancellationToken).Inventory.Select(i => i.Hash).ToArray();
			}
		}

		public Transaction[] GetMempoolTransactions(CancellationToken cancellationToken = default(CancellationToken))
		{
			return GetMempoolTransactions(GetMempool(), cancellationToken);
		}

		public Transaction[] GetMempoolTransactions(Uint256[] txIds, CancellationToken cancellationToken = default(CancellationToken))
		{
			AssertState(NodeState.HandShaked);
			if(txIds.Length == 0)
				return new Transaction[0];
			var result = new List<Transaction>();
			using(var listener = CreateListener().OfType<TxPayload>())
			{
				SendMessage(new GetDataPayload(txIds.Select(txid => new InventoryVector()
				{
					Type = InventoryType.MsgTx,
					Hash = txid
				}).ToArray()));
				try
				{
					while(result.Count < txIds.Length)
					{
						var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2.0));
						result.Add(listener.ReceivePayload<TxPayload>(
							CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token).Token).Object);

					}
				}
				catch(OperationCanceledException)
				{
					if(cancellationToken.IsCancellationRequested)
					{
						throw;
					}
				}
			}
			return result.ToArray();
		}

		public Network Network
		{
			get;
			set;
		}

		#region IDisposable Members

		public void Dispose()
		{
			Disconnect();
		}

		#endregion

		public void PingPong(CancellationToken cancellation = default(CancellationToken))
		{
			using(var listener = CreateListener().OfType<PongPayload>())
			{
				var ping = new PingPayload()
				{
					Nonce = RandomUtils.GetUInt64()
				};
				SendMessage(ping);

				while(listener.ReceivePayload<PongPayload>(cancellation).Nonce != ping.Nonce)
				{
				}
			}
		}
	}
}
#endif