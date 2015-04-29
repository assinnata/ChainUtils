using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ChainUtils.OpenAsset
{
	public class CachedColoredTransactionRepository : IColoredTransactionRepository
	{
		IColoredTransactionRepository _inner;
		CachedTransactionRepository _innerTransactionRepository;
		Dictionary<Uint256, ColoredTransaction> _coloredTransactions = new Dictionary<Uint256, ColoredTransaction>();
		ReaderWriterLock _lock = new ReaderWriterLock();

		public ColoredTransaction GetFromCache(Uint256 txId)
		{
			using(_lock.LockRead())
			{
				return _coloredTransactions.TryGet(txId);
			}
		}

		public CachedColoredTransactionRepository(IColoredTransactionRepository inner)
		{
			if(inner == null)
				throw new ArgumentNullException("inner");
			_inner = inner;
			_innerTransactionRepository = new CachedTransactionRepository(inner.Transactions);
		}
		#region IColoredTransactionRepository Members

		public CachedTransactionRepository Transactions
		{
			get
			{
				return _innerTransactionRepository;
			}
		}

		ITransactionRepository IColoredTransactionRepository.Transactions
		{
			get
			{
				return _innerTransactionRepository;
			}
		}

		public async Task<ColoredTransaction> GetAsync(Uint256 txId)
		{
			ColoredTransaction result = null;
			bool found;
			using(_lock.LockRead())
			{
				found = _coloredTransactions.TryGetValue(txId, out result);
			}
			if(!found)
			{
				result = await _inner.GetAsync(txId).ConfigureAwait(false);
				using(_lock.LockWrite())
				{
					_coloredTransactions.AddOrReplace(txId, result);
				}
			}
			return result;
		}

		public Task PutAsync(Uint256 txId, ColoredTransaction tx)
		{
			using(_lock.LockWrite())
			{
				if(!_coloredTransactions.ContainsKey(txId))
					_coloredTransactions.AddOrReplace(txId, tx);
				else
					_coloredTransactions[txId] = tx;
				return _inner.PutAsync(txId, tx);
			}
		}

		#endregion
	}
}
