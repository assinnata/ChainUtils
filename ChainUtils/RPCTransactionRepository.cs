using System;
using System.Threading.Tasks;
using ChainUtils.RPC;

namespace ChainUtils
{
	public class RpcTransactionRepository : ITransactionRepository
	{
		RpcClient _client;
		public RpcTransactionRepository(RpcClient client)
		{
			if(client == null)
				throw new ArgumentNullException("client");
			_client = client;
		}
		#region ITransactionRepository Members

		public Task<Transaction> GetAsync(Uint256 txId)
		{
			return _client.GetRawTransactionAsync(txId, false);
		}

		public Task PutAsync(Uint256 txId, Transaction tx)
		{
			return Task.FromResult(false);
		}

		#endregion
	}
}
