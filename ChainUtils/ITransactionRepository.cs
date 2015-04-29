﻿using System;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;

namespace ChainUtils
{
	public interface ITransactionRepository
	{
		Task<Transaction> GetAsync(Uint256 txId);
		Task PutAsync(Uint256 txId, Transaction tx);
	}

	public static class TxRepoExtensions
	{
		public static Task<Transaction> GetAsync(this ITransactionRepository repo, string txId)
		{
			return repo.GetAsync(new Uint256(txId));
		}

		public static Task PutAsync(this ITransactionRepository repo, Transaction tx)
		{
			return repo.PutAsync(tx.GetHash(), tx);
		}

		public static Transaction Get(this ITransactionRepository repo, string txId)
		{
			return repo.Get(new Uint256(txId));
		}

		public static void Put(this ITransactionRepository repo, Transaction tx)
		{
			repo.Put(tx.GetHash(), tx);
		}

		public static Transaction Get(this ITransactionRepository repo, Uint256 txId)
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

		public static void Put(this ITransactionRepository repo, Uint256 txId, Transaction tx)
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
