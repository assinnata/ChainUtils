using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace ChainUtils.Protocol
{

	public interface IMessageListener<in T>
	{
		void PushMessage(T message);
	}

	public class NullMessageListener<T> : IMessageListener<T>
	{
		#region MessageListener<T> Members

		public void PushMessage(T message)
		{
		}

		#endregion
	}

	public class NewThreadMessageListener<T> : IMessageListener<T>
	{
		readonly Action<T> _process;
		public NewThreadMessageListener(Action<T> process)
		{
			if(process == null)
				throw new ArgumentNullException("process");
			_process = process;
		}
		#region MessageListener<T> Members

		public void PushMessage(T message)
		{
			if(message != null)
				Task.Factory.StartNew(() =>
				{
					try
					{
						_process(message);
					}
					catch(Exception ex)
					{
						NodeServerTrace.Error("Unexpected expected during message loop", ex);
					}
				});
		}

		#endregion
	}

#if !PORTABLE
	public class EventLoopMessageListener<T> : IMessageListener<T>, IDisposable
	{
		public EventLoopMessageListener(Action<T> processMessage)
		{
			new Thread(new ThreadStart(() =>
			{
				try
				{
					while(!_cancellationSource.IsCancellationRequested)
					{
						var message = _messageQueue.Take(_cancellationSource.Token);
						if(message != null)
						{
							try
							{
								processMessage(message);
							}
							catch(Exception ex)
							{
								NodeServerTrace.Error("Unexpected expected during message loop", ex);
							}
						}
					}
				}
				catch(OperationCanceledException)
				{
				}
			})).Start();
		}
		BlockingCollection<T> _messageQueue = new BlockingCollection<T>(new ConcurrentQueue<T>());
		public BlockingCollection<T> MessageQueue
		{
			get
			{
				return _messageQueue;
			}
		}


		#region MessageListener Members

		public void PushMessage(T message)
		{
			_messageQueue.Add(message);
		}

		#endregion

		#region IDisposable Members

		CancellationTokenSource _cancellationSource = new CancellationTokenSource();
		public void Dispose()
		{
			if(_cancellationSource.IsCancellationRequested)
				return;
			_cancellationSource.Cancel();
		}

		#endregion

	}

	public class PollMessageListener<T> : IMessageListener<T>
	{

		BlockingCollection<T> _messageQueue = new BlockingCollection<T>(new ConcurrentQueue<T>());
		public BlockingCollection<T> MessageQueue
		{
			get
			{
				return _messageQueue;
			}
		}

		public virtual T ReceiveMessage(CancellationToken cancellationToken = default(CancellationToken))
		{
			return MessageQueue.Take(cancellationToken);
		}

		#region MessageListener Members

		public virtual void PushMessage(T message)
		{
			_messageQueue.Add(message);
		}

		#endregion
	}
#endif
}