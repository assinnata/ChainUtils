using System;
using System.Threading.Tasks;

namespace ChainUtils
{
	public class NoSqlTransactionRepository : ITransactionRepository
	{
		private readonly NoSqlRepository _repository;
		public NoSqlRepository Repository
		{
			get
			{
				return _repository;
			}
		}

		public NoSqlTransactionRepository():this(new InMemoryNoSqlRepository())
		{

		}
		public NoSqlTransactionRepository(NoSqlRepository repository)
		{
			if(repository == null)
				throw new ArgumentNullException("repository");
			_repository = repository;
		}
		#region ITransactionRepository Members

		public Task<Transaction> GetAsync(Uint256 txId)
		{
			return _repository.GetAsync<Transaction>(GetId(txId));
		}

		private string GetId(Uint256 txId)
		{
			return "tx-" + txId.ToString();
		}

		public Task PutAsync(Uint256 txId, Transaction tx)
		{
			return _repository.PutAsync(GetId(txId), tx);
		}

		#endregion
	}
}
