#if !NOSOCKET
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace ChainUtils.Protocol
{
	public class NodeListener : PollMessageListener<IncomingMessage>, IDisposable
	{
		private readonly Node _node;
		public Node Node
		{
			get
			{
				return _node;
			}
		}
		IDisposable _subscription;
		public NodeListener(Node node)
		{
			_subscription = node.MessageProducer.AddMessageListener(this);
			_node = node;
		}

		public NodeListener Where(Func<IncomingMessage, bool> predicate)
		{
			_predicates.Add(predicate);
			return this;
		}
		public NodeListener OfType<TPayload>() where TPayload : Payload
		{
			_predicates.Add(i => i.Message.Payload is TPayload);
			return this;
		}

		public TPayload ReceivePayload<TPayload>(CancellationToken cancellationToken = default(CancellationToken))
			where TPayload : Payload
		{
			var pushedAside = new Queue<IncomingMessage>();
			try
			{
				while(true)
				{
					var message = ReceiveMessage(CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, Node.Connection.Cancel.Token).Token);
					if(_predicates.All(p => p(message)))
					{
						if(message.Message.Payload is TPayload)
							return (TPayload)message.Message.Payload;
						else
						{
							pushedAside.Enqueue(message);
						}
					}
				}
			}
			catch(OperationCanceledException)
			{
				if(Node.Connection.Cancel.IsCancellationRequested)
					throw new InvalidOperationException("Connection dropped");
				throw;
			}
			finally
			{
				while(pushedAside.Count != 0)
					PushMessage(pushedAside.Dequeue());
			}
			throw new InvalidProgramException("Bug in Node.RecieveMessage");
		}

		List<Func<IncomingMessage, bool>> _predicates = new List<Func<IncomingMessage, bool>>();

		#region IDisposable Members

		public void Dispose()
		{
			if(_subscription != null)
				_subscription.Dispose();
		}

		#endregion
	}
}
#endif