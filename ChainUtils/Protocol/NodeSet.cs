#if !NOSOCKET
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace ChainUtils.Protocol
{
	public delegate void NodeSetEventHandler(NodeSet sender, Node node);
	public class NodeSet : IDisposable
	{
		class NodeListener : IMessageListener<IncomingMessage>
		{
			NodeSet _parent;
			public NodeListener(NodeSet parent)
			{
				_parent = parent;
			}
			#region MessageListener<IncomingMessage> Members

			public void PushMessage(IncomingMessage message)
			{
				_parent.MessageProducer.PushMessage(message);
			}

			#endregion
		}

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		Dictionary<IPEndPoint, List<Node>> _nodes = new Dictionary<IPEndPoint, List<Node>>();
		readonly NodeListener _messageListener;
		public NodeSet()
		{
			_messageListener = new NodeListener(this);
		}


		private readonly MessageProducer<IncomingMessage> _messageProducer = new MessageProducer<IncomingMessage>();
		public MessageProducer<IncomingMessage> MessageProducer
		{
			get
			{
				return _messageProducer;
			}
		}

		public Node GetNodeByEndpoint(IPEndPoint endpoint)
		{
			lock(_nodes)
			{
				endpoint = Utils.EnsureIPv6(endpoint);
				return GetNodesList(endpoint).FirstOrDefault();
			}
		}

		public Node GetNodeByPeer(Peer peer)
		{
			lock(_nodes)
			{
				return GetNodesList(peer.NetworkAddress.Endpoint).FirstOrDefault();
			}
		}

		List<Node> GetNodesList(IPEndPoint endpoint)
		{
			List<Node> result = null;
			if(!_nodes.TryGetValue(endpoint, out result))
				result = new List<Node>();
			return result;
		}

		public Node AddNode(Node node)
		{
			var added = false;
			lock(_nodes)
			{
				if(node.State < NodeState.Connected)
					return null;
				node.StateChanged += node_StateChanged;
				node.MessageProducer.AddMessageListener(_messageListener);
				var list = GetNodesList(node.Peer.NetworkAddress.Endpoint);
				list.Add(node);
				added = true;
				if(list.Count == 1)
					_nodes.Add(node.Peer.NetworkAddress.Endpoint, list);
			}
			if(added)
			{
				var nodeAdded = NodeAdded;
				if(nodeAdded != null)
					nodeAdded(this, node);
			}
			return node;
		}

		public event NodeSetEventHandler NodeAdded;
		public event NodeSetEventHandler NodeRemoved;
		void node_StateChanged(Node node, NodeState oldState)
		{
			if(node.State == NodeState.Offline || node.State == NodeState.Disconnecting || node.State == NodeState.Failed)
			{
				RemoveNode(node);
			}
		}

		public void RemoveNode(Node node)
		{
			var removed = false;
			lock(_nodes)
			{
				var endpoint = node.Peer.NetworkAddress.Endpoint;
				var nodes = GetNodesList(endpoint);
				if(nodes.Remove(node))
				{
					removed = true;
					node.MessageProducer.RemoveMessageListener(_messageListener);
					node.StateChanged -= node_StateChanged;
					if(nodes.Count == 0)
						_nodes.Remove(endpoint);
				}
			}
			if(removed)
			{
				var nodeRemoved = NodeRemoved;
				if(nodeRemoved != null)
					nodeRemoved(this, node);
			}
		}

		public Node[] GetNodes()
		{
			lock(_nodes)
			{
				return _nodes.Values.SelectMany(s => s).ToArray();
			}
		}

		public void DisconnectAll(CancellationToken cancellation = default(CancellationToken))
		{
			DisconnectNodes(GetNodes());
		}



		public void DisconnectNodes(Node[] nodes, CancellationToken cancellation = default(CancellationToken))
		{
			var tasks = nodes.Select(n => Task.Factory.StartNew(() => n.Disconnect())).ToArray();
			Task.WaitAll(tasks, cancellation);
		}

		public bool Contains(IPEndPoint endpoint)
		{
			lock(_nodes)
			{
				return _nodes.ContainsKey(endpoint);
			}
		}

		public void AddNodes(Node[] nodes)
		{
			lock(_nodes)
			{
				foreach(var node in nodes)
				{
					AddNode(node);
				}
			}
		}

		public int Count()
		{
			lock(_nodes)
			{
				return _nodes.Count;
			}
		}



		public void RemoveNodes(Node[] nodes)
		{
			lock(_nodes)
			{
				foreach(var node in nodes)
				{
					RemoveNode(node);
				}
			}
		}

		#region IDisposable Members

		public void Dispose()
		{
			Task.Factory.StartNew(() =>
			{
				try
				{
					DisconnectAll();
				}
				catch(Exception)
				{

				}
			}, TaskCreationOptions.LongRunning);
		}

		#endregion

		public void SendMessage(Payload payload)
		{
			var nodes = GetNodes();

			var tasks =
				nodes
				.Select(n => Task.Factory.StartNew(() =>
				{
					n.SendMessage(payload);
				}, TaskCreationOptions.LongRunning))
				.ToArray();

			Task.WaitAll(tasks);
		}
	}
}
#endif