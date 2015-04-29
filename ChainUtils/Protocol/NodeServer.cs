#if !NOSOCKET
#if !NOUPNP
using System;
using System.Linq;
#endif
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ChainUtils.BitcoinCore;

namespace ChainUtils.Protocol
{
	public delegate void NodeServerNodeEventHandler(NodeServer sender, Node node);
	public delegate void NodeServerMessageEventHandler(NodeServer sender, IncomingMessage message);
	public class NodeServer : IDisposable
	{
		private readonly Network _network;
		public Network Network
		{
			get
			{
				return _network;
			}
		}
		private readonly ProtocolVersion _version;
		public ProtocolVersion Version
		{
			get
			{
				return _version;
			}
		}
		public bool AdvertizeMyself
		{
			get;
			set;
		}

		public NodeServer(Network network, ProtocolVersion version = ProtocolVersion.PROTOCOL_VERSION,
			int internalPort = -1)
		{
			AdvertizeMyself = false;
			internalPort = internalPort == -1 ? network.DefaultPort : internalPort;
			_localEndpoint = new IPEndPoint(IPAddress.Parse("0.0.0.0").MapToIPv6(), internalPort);
			_network = network;
			_externalEndpoint = new IPEndPoint(_localEndpoint.Address, Network.DefaultPort);
			_version = version;
			var listener = new EventLoopMessageListener<IncomingMessage>(ProcessMessage);
			MessageProducer.AddMessageListener(listener);
			OwnResource(listener);
			RegisterPeerTableRepository(_peerTable);
			_nodes = new NodeSet();
			_nodes.NodeAdded += _Nodes_NodeAdded;
			_nodes.NodeRemoved += _Nodes_NodeRemoved;
			_nodes.MessageProducer.AddMessageListener(listener);
			_trace = new TraceCorrelation(NodeServerTrace.Trace, "Node server listening on " + LocalEndpoint);
		}


		public event NodeServerNodeEventHandler NodeRemoved;
		public event NodeServerNodeEventHandler NodeAdded;
		public event NodeServerMessageEventHandler MessageReceived;

		void _Nodes_NodeRemoved(NodeSet sender, Node node)
		{
			var removed = NodeRemoved;
			if(removed != null)
				removed(this, node);
		}

		void _Nodes_NodeAdded(NodeSet sender, Node node)
		{
			var added = NodeAdded;
			if(added != null)
				added(this, node);
		}


		int[] _bitcoinPorts;
		int[] BitcoinPorts
		{
			get
			{
				if(_bitcoinPorts == null)
				{
					_bitcoinPorts = Enumerable.Range(Network.DefaultPort, 10).ToArray();
				}
				return _bitcoinPorts;
			}
		}

		TimeSpan _natLeasePeriod = TimeSpan.FromMinutes(10.0);
		/// <summary>
		/// When using DetectExternalEndpoint, UPNP will open ports on the gateway for a fixed amount of time before renewing
		/// </summary>
		public TimeSpan NatLeasePeriod
		{
			get
			{
				return _natLeasePeriod;
			}
			set
			{
				_natLeasePeriod = value;
			}
		}


		string _natRuleName = "ChainUtils Node Server";
		public string NatRuleName
		{
			get
			{
				return _natRuleName;
			}
			set
			{
				_natRuleName = value;
			}
		}
#if !NOUPNP
		UpnPLease _upnPLease;
		public UpnPLease DetectExternalEndpoint(CancellationToken cancellation = default(CancellationToken))
		{
			if(_upnPLease != null)
			{
				_upnPLease.Dispose();
				_upnPLease = null;
			}
			var lease = new UpnPLease(BitcoinPorts, LocalEndpoint.Port, NatRuleName);
			lease.LeasePeriod = NatLeasePeriod;
			if(lease.DetectExternalEndpoint(cancellation))
			{
				_upnPLease = lease;
				ExternalEndpoint = _upnPLease.ExternalEndpoint;
				return lease;
			}
			else
			{
				using(lease.Trace.Open())
				{
					NodeServerTrace.Information("No UPNP device found, try to use external web services to deduce external address");
					try
					{
						var ip = GetMyExternalIp(cancellation);
						if(ip != null)
							ExternalEndpoint = new IPEndPoint(ip, ExternalEndpoint.Port);
					}
					catch(Exception ex)
					{
						NodeServerTrace.Error("Could not use web service to deduce external address", ex);
					}
				}
				return null;
			}
		}
#endif
		public bool AllowLocalPeers
		{
			get;
			set;
		}


		private void PopulateTableWithHardNodes()
		{
			InternalMessageProducer.PushMessages(Network.SeedNodes.Select(n => new Peer(PeerOrigin.HardSeed, n)).ToArray());
		}
		private void PopulateTableWithDnsNodes()
		{
			var peers = Network.DnsSeeds
							.SelectMany(s =>
							{
								try
								{
									return s.GetAddressNodes();
								}
								catch(Exception ex)
								{
									NodeServerTrace.ErrorWhileRetrievingDnsSeedIp(s.Name, ex);
									return new IPAddress[0];
								}
							})
							.Select(s => new Peer(PeerOrigin.DnsSeed, new NetworkAddress()
							{
								Endpoint = new IPEndPoint(s, Network.DefaultPort),
								Time = Utils.UnixTimeToDateTime(0)
							})).ToArray();

			InternalMessageProducer.PushMessages(peers);
		}

		readonly PeerTable _peerTable = new PeerTable();
		public PeerTable PeerTable
		{
			get
			{
				return _peerTable;
			}
		}

		private IPEndPoint _localEndpoint;
		public IPEndPoint LocalEndpoint
		{
			get
			{
				return _localEndpoint;
			}
		}

		Socket _socket;
		TraceCorrelation _trace;

		public bool IsListening
		{
			get
			{
				return _socket != null;
			}
		}

		public void Listen()
		{
			if(_socket != null)
				throw new InvalidOperationException("Already listening");
			using(_trace.Open())
			{
				try
				{
					_socket = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);
#if !NOIPDUALMODE
					_socket.DualMode = true;
#endif

					_socket.Bind(LocalEndpoint);
					_socket.Listen(8);
					NodeServerTrace.Information("Listening...");
					BeginAccept();
				}
				catch(Exception ex)
				{
					NodeServerTrace.Error("Error while opening the Protocol server", ex);
					throw;
				}
			}
		}

		private void BeginAccept()
		{
			if(_isDisposed)
				return;
			NodeServerTrace.Information("Accepting connection...");
			_socket.BeginAccept(EndAccept, null);
		}
		private void EndAccept(IAsyncResult ar)
		{
			using(_trace.Open())
			{
				Socket client = null;
				try
				{
					client = _socket.EndAccept(ar);
					if(_isDisposed)
						return;
					NodeServerTrace.Information("Client connection accepted : " + client.RemoteEndPoint);
					var cancel = new CancellationTokenSource();
					cancel.CancelAfter(TimeSpan.FromSeconds(10));
					var message = Message.ReadNext(client, Network, Version, cancel.Token);
					MessageProducer.PushMessage(new IncomingMessage()
					{
						Socket = client,
						Message = message,
						Node = null,
					});
				}
				catch(OperationCanceledException ex)
				{
					NodeServerTrace.Error("The remote connecting failed to send a message within 10 seconds, dropping connection", ex);
				}
				catch(Exception ex)
				{
					if(_isDisposed)
						return;
					if(client == null)
					{
						NodeServerTrace.Error("Error while accepting connection ", ex);
						Thread.Sleep(3000);
					}
					else
					{
						NodeServerTrace.Error("Invalid message received from the remote connecting node", ex);
					}
				}
				BeginAccept();
			}
		}

		public IPAddress GetMyExternalIp(CancellationToken cancellation = default(CancellationToken))
		{

			var tasks = new[]{
						new {IP = "91.198.22.70", DNS ="checkip.dyndns.org"}, 
						new {IP = "209.68.27.16", DNS = "www.ipchicken.com"}
			 }.Select(site =>
			 {
				 return Task.Run(() =>
					 {
						 var ip = IPAddress.Parse(site.IP);
						 try
						 {
							 ip = Dns.GetHostAddresses(site.DNS).First();
						 }
						 catch(Exception ex)
						 {
							 NodeServerTrace.Warning("can't resolve ip of " + site.DNS + " using hardcoded one " + site.IP, ex);
						 }
						 var client = new WebClient();
						 var page = client.DownloadString("http://" + ip);
						 var match = Regex.Match(page, "[0-9]{1,3}\\.[0-9]{1,3}\\.[0-9]{1,3}\\.[0-9]{1,3}");
						 return match.Value;
					 });
			 }).ToArray();


			Task.WaitAny(tasks, cancellation);
			try
			{
				var result = tasks.First(t => t.IsCompleted && !t.IsFaulted).Result;
				NodeServerTrace.ExternalIpReceived(result);
				return IPAddress.Parse(result);
			}
			catch(InvalidOperationException)
			{
				NodeServerTrace.ExternalIpFailed(tasks.Select(t => t.Exception).FirstOrDefault());
				throw new WebException("Impossible to detect extenal ip");
			}
		}

		internal readonly MessageProducer<IncomingMessage> MessageProducer = new MessageProducer<IncomingMessage>();
		internal readonly MessageProducer<object> InternalMessageProducer = new MessageProducer<object>();

		MessageProducer<IncomingMessage> _allMessages = new MessageProducer<IncomingMessage>();
		public MessageProducer<IncomingMessage> AllMessages
		{
			get
			{
				return _allMessages;
			}
		}

		volatile IPEndPoint _externalEndpoint;
		public IPEndPoint ExternalEndpoint
		{
			get
			{
				return _externalEndpoint;
			}
			set
			{
				_externalEndpoint = Utils.EnsureIPv6(value);
			}
		}


		internal void ExternalAddressDetected(IPAddress iPAddress)
		{
			if(!ExternalEndpoint.Address.IsRoutable(AllowLocalPeers) && iPAddress.IsRoutable(AllowLocalPeers))
			{
				NodeServerTrace.Information("New externalAddress detected " + iPAddress);
				ExternalEndpoint = new IPEndPoint(iPAddress, ExternalEndpoint.Port);
			}
		}

		public Node GetNodeByHostName(string hostname, int port = -1, CancellationToken cancellation = default(CancellationToken))
		{
			if(port == -1)
				port = Network.DefaultPort;
			var ip = Dns.GetHostAddresses(hostname).First();
			var endpoint = new IPEndPoint(ip, port);
			return GetNodeByEndpoint(endpoint, cancellation);
		}

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		readonly NodeSet _nodes;

		public Node GetNodeByEndpoint(IPEndPoint endpoint, CancellationToken cancellation = default(CancellationToken))
		{
			lock(_nodes)
			{
				endpoint = Utils.EnsureIPv6(endpoint);

				var node = _nodes.GetNodeByEndpoint(endpoint);
				if(node != null)
					return node;
				var peer = PeerTable.GetPeer(endpoint);
				if(peer == null)
					peer = new Peer(PeerOrigin.Manual, new NetworkAddress()
					{

						Endpoint = endpoint,
						Time = Utils.UnixTimeToDateTime(0)
					});

				return AddNode(peer, cancellation);
			}
		}

		public Node GetNodeByEndpoint(string endpoint)
		{
			var ip = Utils.ParseIpEndpoint(endpoint, Network.DefaultPort);
			return GetNodeByEndpoint(ip);
		}

		public Node GetNodeByPeer(Peer peer, CancellationToken cancellation = default(CancellationToken))
		{
			var node = _nodes.GetNodeByPeer(peer);
			if(node != null)
				return node;
			return AddNode(peer, cancellation);

		}

		private Node AddNode(Peer peer, CancellationToken cancellationToken)
		{
			try
			{
				var node = new Node(peer, Network, CreateVersionPayload(peer, ExternalEndpoint, Version), cancellationToken);
				return AddNode(node);
			}
			catch(Exception)
			{
				return null;
			}
		}

		private Node AddNode(Node node)
		{
			if(node.State < NodeState.Connected)
				return null;
			return _nodes.AddNode(node);
		}

		internal void RemoveNode(Node node)
		{
			_nodes.RemoveNode(node);
		}
		void ProcessMessage(IncomingMessage message)
		{
			AllMessages.PushMessage(message);
			TraceCorrelation trace = null;
			if(message.Node != null)
			{
				trace = message.Node.TraceCorrelation;
			}
			else
			{
				trace = new TraceCorrelation(NodeServerTrace.Trace, "Processing inbound message " + message.Message);
			}
			using(trace.Open(false))
			{
				ProcessMessageCore(message);
			}
		}

		private void ProcessMessageCore(IncomingMessage message)
		{
			if(message.Message.Payload is VersionPayload)
			{
				var version = message.AssertPayload<VersionPayload>();
				var connectedToSelf = version.Nonce == Nonce;
				if(message.Node != null && connectedToSelf)
				{
					NodeServerTrace.ConnectionToSelfDetected();
					message.Node.Disconnect();
					return;
				}

				if(message.Node == null)
				{
					var remoteEndpoint = version.AddressFrom;
					if(!remoteEndpoint.Address.IsRoutable(AllowLocalPeers))
					{
						//Send his own endpoint
						remoteEndpoint = new IPEndPoint(((IPEndPoint)message.Socket.RemoteEndPoint).Address, Network.DefaultPort);
					}

					var peer = new Peer(PeerOrigin.Advertised, new NetworkAddress()
					{
						Endpoint = remoteEndpoint,
						Time = DateTimeOffset.UtcNow
					});
					var node = new Node(peer, Network, CreateVersionPayload(peer, ExternalEndpoint, Version), message.Socket, version);

					if(connectedToSelf)
					{
						node.SendMessage(CreateVersionPayload(node.Peer, ExternalEndpoint, Version));
						NodeServerTrace.ConnectionToSelfDetected();
						node.Disconnect();
						return;
					}

					var cancel = new CancellationTokenSource();
					cancel.CancelAfter(TimeSpan.FromSeconds(10.0));
					try
					{
						AddNode(node);
						node.RespondToHandShake(cancel.Token);
					}
					catch(OperationCanceledException ex)
					{
						NodeServerTrace.Error("The remote node did not respond fast enough (10 seconds) to the handshake completion, dropping connection", ex);
						node.Disconnect();
						throw;
					}
					catch(Exception)
					{
						node.Disconnect();
						throw;
					}
				}
			}

			var messageReceived = MessageReceived;
			if(messageReceived != null)
				messageReceived(this, message);
		}


		public bool IsConnectedTo(IPEndPoint endpoint)
		{
			return _nodes.Contains(endpoint);
		}

		ConcurrentDictionary<Node, Node> _connectedNodes = new ConcurrentDictionary<Node, Node>();

		public bool AdvertiseMyself()
		{
			if(IsListening && ExternalEndpoint.Address.IsRoutable(AllowLocalPeers))
			{
				NodeServerTrace.Information("Advertizing myself");
				foreach(var node in _connectedNodes)
				{
					node.Value.SendMessage(new AddrPayload(new NetworkAddress()
					{
						Ago = TimeSpan.FromSeconds(0),
						Endpoint = ExternalEndpoint
					}));
				}
				return true;
			}
			else
				return false;

		}

		List<IDisposable> _resources = new List<IDisposable>();
		IDisposable OwnResource(IDisposable resource)
		{
			if(_isDisposed)
			{
				resource.Dispose();
				return Scope.Nothing;
			}
			return new Scope(() =>
			{
				lock(_resources)
				{
					_resources.Add(resource);
				}
			}, () =>
			{
				lock(_resources)
				{
					_resources.Remove(resource);
				}
			});
		}
		#region IDisposable Members

		bool _isDisposed;
		public void Dispose()
		{
			if(!_isDisposed)
			{
				_isDisposed = true;

				lock(_resources)
				{
					foreach(var resource in _resources)
						resource.Dispose();
				}
				try
				{
					_nodes.DisconnectAll();
				}
				finally
				{
#if !NOUPNP
					if(_upnPLease != null)
					{
						_upnPLease.Dispose();
					}
#endif
					if(_socket != null)
					{
						Utils.SafeCloseSocket(_socket);
						_socket = null;
					}
				}
			}
		}

		#endregion

		public VersionPayload CreateVersionPayload(Peer peer, IPEndPoint myExternal, ProtocolVersion? version)
		{
			myExternal = Utils.EnsureIPv6(myExternal);
			return new VersionPayload()
					{
						Nonce = Nonce,
						UserAgent = UserAgent,
						Version = version == null ? Version : version.Value,
						StartHeight = 0,
						Timestamp = DateTimeOffset.UtcNow,
						AddressReceiver = peer.NetworkAddress.Endpoint,
						AddressFrom = myExternal,
						Relay = IsRelay
					};
		}

		public bool IsRelay
		{
			get;
			set;
		}

		string _userAgent;
		public string UserAgent
		{
			get
			{
				if(_userAgent == null)
				{
					_userAgent = VersionPayload.GetChainUtilsUserAgent();
				}
				return _userAgent;
			}
		}

		ulong _nonce;
		public ulong Nonce
		{
			get
			{
				if(_nonce == 0)
				{
					_nonce = RandomUtils.GetUInt64();
				}
				return _nonce;
			}
			set
			{
				_nonce = value;
			}
		}





		/// <summary>
		/// Fill the PeerTable with fresh addresses
		/// </summary>
		public void DiscoverPeers(int peerToFind = 990)
		{
			var traceCorrelation = new TraceCorrelation(NodeServerTrace.Trace, "Discovering nodes");
			var tasks = new List<Task>();
			using(traceCorrelation.Open())
			{
				while(CountPeerRequired(peerToFind) != 0)
				{
					NodeServerTrace.PeerTableRemainingPeerToGet(CountPeerRequired(peerToFind));
					var peers = PeerTable.GetActivePeers(1000);
					if(peers.Length == 0)
					{
						PopulateTableWithDnsNodes();
						PopulateTableWithHardNodes();
						peers = PeerTable.GetActivePeers(1000);
					}


					var peerTableFull = new CancellationTokenSource();
					var connected = new NodeSet();
					try
					{
						Parallel.ForEach(peers, new ParallelOptions()
						{
							MaxDegreeOfParallelism = 5,
							CancellationToken = peerTableFull.Token,
						}, p =>
						{
							var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(40));
							Node n = null;
							try
							{
								n = GetNodeByPeer(p, cancellation.Token);
								if(n.State < NodeState.HandShaked)
								{
									connected.AddNode(n);
									n.VersionHandshake(cancellation.Token);
								}
								n.SendMessage(new GetAddrPayload());
								Thread.Sleep(2000);
							}
							catch(Exception)
							{
								if(n != null)
									n.Disconnect();
							}
							if(CountPeerRequired(peerToFind) == 0)
								peerTableFull.Cancel();
							else
								NodeServerTrace.Information("Need " + CountPeerRequired(peerToFind) + " more peers");
						});
					}
					catch(OperationCanceledException)
					{
					}
					finally
					{
						connected.DisconnectAll();
					}
				}
				NodeServerTrace.Trace.TraceInformation("Peer table is now full");
			}
		}

		int CountPeerRequired(int peerToFind)
		{
			return Math.Max(0, peerToFind - PeerTable.CountUsed(true));
		}

		public NodeSet CreateNodeSet(int size)
		{
			if(size > 1000)
				throw new ArgumentOutOfRangeException("size", "size should be less than 1000");
			var trace = new TraceCorrelation(NodeServerTrace.Trace, "Creating node set of size " + size);
			var set = new NodeSet();
			using(trace.Open())
			{
				while(set.Count() < size)
				{
					var peerToGet = size - set.Count();
					var activePeers = PeerTable.GetActivePeers(1000);
					activePeers = activePeers.Where(p => !set.Contains(p.NetworkAddress.Endpoint)).ToArray();
					if(activePeers.Length < peerToGet)
					{
						DiscoverPeers(size);
						continue;
					}
					NodeServerTrace.Information("Need " + peerToGet + " more nodes");

					var handshakedNodes = new BlockingCollection<Node>(peerToGet);
					var handshakedFull = new CancellationTokenSource();

					try
					{
						Parallel.ForEach(activePeers,
							new ParallelOptions()
							{
								MaxDegreeOfParallelism = 10,
								CancellationToken = handshakedFull.Token
							}, p =>
						{
							if(set.Contains(p.NetworkAddress.Endpoint))
								return;
							Node node = null;
							try
							{
								node = GetNodeByPeer(p, handshakedFull.Token);
								node.VersionHandshake(handshakedFull.Token);
								if(node != null && node.State != NodeState.HandShaked)
									node.Disconnect();
								if(!handshakedNodes.TryAdd(node))
								{
									handshakedFull.Cancel();
									node.Disconnect();
								}
								else
								{
									var remaining = (size - set.Count() - handshakedNodes.Count);
									if(remaining == 0)
									{
										handshakedFull.Cancel();
									}
									else
										NodeServerTrace.Information("Need " + remaining + " more nodes");
								}
							}
							catch(Exception)
							{
								if(node != null)
									node.Disconnect();
							}
						});
					}
					catch(OperationCanceledException)
					{
					}
					set.AddNodes(handshakedNodes.ToArray());
				}
			}
			return set;
		}

		public IDisposable RegisterPeerTableRepository(PeerTableRepository peerTableRepository)
		{
			var poll = new EventLoopMessageListener<object>(o =>
			{
				var message = o as IncomingMessage;
				if(message != null)
				{
					if(message.Message.Payload is AddrPayload)
					{
						peerTableRepository.WritePeers(((AddrPayload)message.Message.Payload).Addresses
														.Where(a => a.Endpoint.Address.IsRoutable(AllowLocalPeers))
														.Select(a => new Peer(PeerOrigin.Addr, a)));
					}
				}
				var peer = o as Peer;
				if(peer != null)
				{
					if(peer.NetworkAddress.Endpoint.Address.IsRoutable(AllowLocalPeers))
						peerTableRepository.WritePeer(peer);
				}
			});

			if(peerTableRepository != _peerTable)
			{
				InternalMessageProducer.PushMessages(peerTableRepository.GetPeers());
			}
			return new CompositeDisposable(AllMessages.AddMessageListener(poll), InternalMessageProducer.AddMessageListener(poll), OwnResource(poll));
		}

#if !NOFILEIO
		public IDisposable RegisterBlockRepository(BlockRepository repository)
		{
			var listener = new EventLoopMessageListener<IncomingMessage>((m) =>
			{
				if(m.Node != null)
				{
					if(m.Message.Payload is HeadersPayload)
					{
						foreach(var header in ((HeadersPayload)m.Message.Payload).Headers)
						{
							repository.WriteBlockHeader(header);
						}
					}
					if(m.Message.Payload is BlockPayload)
					{
						repository.WriteBlock(((BlockPayload)m.Message.Payload).Object);
					}
				}
			});
			return new CompositeDisposable(AllMessages.AddMessageListener(listener), OwnResource(listener));
		}
#endif
		public Node GetLocalNode()
		{
			return GetNodeByEndpoint(new IPEndPoint(IPAddress.Loopback, ExternalEndpoint.Port));
		}
	}
}
#endif