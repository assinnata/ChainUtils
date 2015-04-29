using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ChainUtils.OpenAsset
{
	internal class NoDuplicateColoredTransactionRepository : IColoredTransactionRepository, ITransactionRepository
	{
		public NoDuplicateColoredTransactionRepository(IColoredTransactionRepository inner)
		{
			if(inner == null)
				throw new ArgumentNullException("inner");
			_inner = inner;
		}
		IColoredTransactionRepository _inner;
		#region IColoredTransactionRepository Members

		public ITransactionRepository Transactions
		{
			get
			{
				return this;
			}
		}

		public Task<ColoredTransaction> GetAsync(Uint256 txId)
		{
			return Request("c" + txId.ToString(), () => _inner.GetAsync(txId));
		}

		public Task PutAsync(Uint256 txId, ColoredTransaction tx)
		{
			return _inner.PutAsync(txId, tx);
		}

		#endregion

		#region ITransactionRepository Members

		Task<Transaction> ITransactionRepository.GetAsync(Uint256 txId)
		{
			return Request("t" + txId.ToString(), () => _inner.Transactions.GetAsync(txId));
		}

		public Task PutAsync(Uint256 txId, Transaction tx)
		{
			return _inner.Transactions.PutAsync(txId, tx);
		}

		#endregion

		Dictionary<string, Task> _tasks = new Dictionary<string, Task>();
		ReaderWriterLock _lock = new ReaderWriterLock();

		Task<T> Request<T>(string key, Func<Task<T>> wrapped)
		{
			Task<T> task = null;
			using(_lock.LockRead())
			{
				task = _tasks.TryGet(key) as Task<T>;
			}
			if(task != null)
				return task;
			using(_lock.LockWrite())
			{
				task = _tasks.TryGet(key) as Task<T>;
				if(task != null)
					return task;
				task = wrapped();
				_tasks.Add(key, task);
			}
			task.ContinueWith((_) =>
			{
				using(_lock.LockWrite())
				{
					_tasks.Remove(key);
				}
			});
			return task;
		}
	}
}
