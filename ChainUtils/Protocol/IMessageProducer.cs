using System;
using System.Collections.Generic;

namespace ChainUtils.Protocol
{
	public class MessageProducer<T>
	{
		List<IMessageListener<T>> _listeners = new List<IMessageListener<T>>();
		public IDisposable AddMessageListener(IMessageListener<T> listener)
		{
			lock(_listeners)
			{
				return new Scope(() =>
				{
					_listeners.Add(listener);
				}, () =>
				{
					lock(_listeners)
					{
						_listeners.Remove(listener);
					}
				});
			}
		}

		public void RemoveMessageListener(IMessageListener<T> listener)
		{
			lock(_listeners)
			{
				_listeners.Add(listener);
			}
		}

		public void PushMessage(T message)
		{
			lock(_listeners)
			{
				foreach(var listener in _listeners)
				{
					listener.PushMessage(message);
				}
			}
		}


		public void PushMessages(IEnumerable<T> messages)
		{
			lock(_listeners)
			{
				foreach(var message in messages)
				{
					foreach(var listener in _listeners)
					{
						listener.PushMessage(message);
					}
				}
			}
		}
	}
}
