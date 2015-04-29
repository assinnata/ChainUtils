using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ChainUtils
{
	public class CachedTransactionRepository : ITransactionRepository
	{
		ITransactionRepository _inner;
		Dictionary<Uint256, Transaction> _transactions = new Dictionary<Uint256, Transaction>();
		ReaderWriterLock _lock = new ReaderWriterLock();
		public CachedTransactionRepository(ITransactionRepository inner)
		{
			if(inner == null)
				throw new ArgumentNullException("inner");
			_inner = inner;
		}

		public Transaction GetFromCache(Uint256 txId)
		{
			using(_lock.LockRead())
			{
				return _transactions.TryGet(txId);
			}
		}

		#region ITransactionRepository Members

		public async Task<Transaction> GetAsync(Uint256 txId)
		{
			var found = false;
			Transaction result = null;
			using(_lock.LockRead())
			{
				found = _transactions.TryGetValue(txId, out result);
			}
			if(!found)
			{
				result = await _inner.GetAsync(txId).ConfigureAwait(false);
				using(_lock.LockWrite())
				{
					_transactions.AddOrReplace(txId, result);
				}
			}
			return result;

		}

		public Task PutAsync(Uint256 txId, Transaction tx)
		{
			using(_lock.LockWrite())
			{
				if(!_transactions.ContainsKey(txId))
					_transactions.AddOrReplace(txId, tx);
				else
					_transactions[txId] = tx;
			}
			return _inner.PutAsync(txId, tx);
		}

		#endregion
	}
}
