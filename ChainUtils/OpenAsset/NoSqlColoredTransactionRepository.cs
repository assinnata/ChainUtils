using System.Threading.Tasks;

namespace ChainUtils.OpenAsset
{
	public class NoSqlColoredTransactionRepository : IColoredTransactionRepository
	{
		public NoSqlColoredTransactionRepository()
			: this(null, null)
		{

		}
		public NoSqlColoredTransactionRepository(ITransactionRepository transactionRepository)
			: this(transactionRepository, null)
		{

		}
		public NoSqlColoredTransactionRepository(ITransactionRepository transactionRepository, NoSqlRepository repository)
		{
			if(transactionRepository == null)
				transactionRepository = new NoSqlTransactionRepository();
			if(repository == null)
				repository = new InMemoryNoSqlRepository();
			_transactions = transactionRepository;
			_repository = repository;
		}

		private readonly NoSqlRepository _repository;
		public NoSqlRepository Repository
		{
			get
			{
				return _repository;
			}
		}

		ITransactionRepository _transactions;
		#region IColoredTransactionRepository Members

		public ITransactionRepository Transactions
		{
			get
			{
				return _transactions;
			}
		}

		public Task<ColoredTransaction> GetAsync(Uint256 txId)
		{
			return _repository.GetAsync<ColoredTransaction>(GetId(txId));
		}

		private string GetId(Uint256 txId)
		{
			return "ctx-" + txId;
		}

		public Task PutAsync(Uint256 txId, ColoredTransaction tx)
		{
			return _repository.PutAsync(GetId(txId), tx);
		}

		#endregion
	}
}
