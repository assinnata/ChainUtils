using System;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;

namespace ChainUtils.OpenAsset
{
	public interface IColoredTransactionRepository
	{
		ITransactionRepository Transactions
		{
			get;
		}
		Task<ColoredTransaction> GetAsync(Uint256 txId);
		Task PutAsync(Uint256 txId, ColoredTransaction tx);
	}

	public static class ColoredTxRepoExtensions
	{
		public static Task<ColoredTransaction> GetAsync(this IColoredTransactionRepository repo, string txId)
		{
			return repo.GetAsync(new Uint256(txId));
		}

		public static ColoredTransaction Get(this IColoredTransactionRepository repo, string txId)
		{
			return repo.Get(new Uint256(txId));
		}

		public static ColoredTransaction Get(this IColoredTransactionRepository repo, Uint256 txId)
		{
			try
			{
				return repo.GetAsync(txId).Result;
			}
			catch(AggregateException aex)
			{
				ExceptionDispatchInfo.Capture(aex.InnerException).Throw();
				return null;
			}
		}

		public static void Put(this IColoredTransactionRepository repo, Uint256 txId, ColoredTransaction tx)
		{
			try
			{
				repo.PutAsync(txId, tx).Wait();
			}
			catch(AggregateException aex)
			{
				ExceptionDispatchInfo.Capture(aex.InnerException).Throw();
			}
		}
	}
}
