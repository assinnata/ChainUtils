using System;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;

namespace ChainUtils.BitcoinCore
{
	public class CoinsView
	{
		public CoinsView(NoSqlRepository index)
		{
			if(index == null)
				throw new ArgumentNullException("index");
			_index = index;
		}

		public CoinsView()
			: this(new InMemoryNoSqlRepository())
		{

		}
		private readonly NoSqlRepository _index;
		public NoSqlRepository Index
		{
			get
			{
				return _index;
			}
		}

		public Coins GetCoins(Uint256 txId)
		{
			try
			{
				return Index.GetAsync<Coins>(txId.ToString()).Result;
			}
			catch(AggregateException aex)
			{
				ExceptionDispatchInfo.Capture(aex.InnerException).Throw();
				return null; //Can't happen
			}
		}

		public Task<Coins> GetCoinsAsync(Uint256 txId)
		{
			return Index.GetAsync<Coins>(txId.ToString());
		}


		public void SetCoins(Uint256 txId, Coins coins)
		{
			Index.PutAsync(txId.ToString(), coins);
		}

		public bool HaveCoins(Uint256 txId)
		{
			return GetCoins(txId) != null;
		}

		public Uint256 GetBestBlock()
		{
			try
			{
				return GetBestBlockAsync().Result;
			}
			catch(AggregateException aex)
			{
				ExceptionDispatchInfo.Capture(aex.InnerException).Throw();
				return null; //Can't happen
			}
		}
		public async Task<Uint256> GetBestBlockAsync()
		{
			var block = await Index.GetAsync<Uint256>("B").ConfigureAwait(false);
			return block ?? new Uint256(0);
		}

		public void SetBestBlock(Uint256 blockId)
		{
			Index.PutAsync("B", blockId);
		}

		public bool HaveInputs(Transaction tx)
		{
			if(!tx.IsCoinBase)
			{
				// first check whether information about the prevout hash is available
				for(var i = 0 ; i < tx.Inputs.Count ; i++)
				{
					var prevout = tx.Inputs[i].PrevOut;
					if(!HaveCoins(prevout.Hash))
						return false;
				}

				// then check whether the actual outputs are available
				for(var i = 0 ; i < tx.Inputs.Count ; i++)
				{
					var prevout = tx.Inputs[i].PrevOut;
					var coins = GetCoins(prevout.Hash);
					if(!coins.IsAvailable(prevout.N))
						return false;
				}
			}
			return true;
		}

		public TxOut GetOutputFor(TxIn input)
		{
			var coins = GetCoins(input.PrevOut.Hash);
			if(!coins.IsAvailable(input.PrevOut.N))
			{
				return null;
			}
			return coins.Outputs[(int)input.PrevOut.N];
		}

		public Money GetValueIn(Transaction tx)
		{
			if(tx.IsCoinBase)
				return 0;
			return tx.Inputs.Select(i => GetOutputFor(i).Value).Sum();
		}

		public CoinsView CreateCached()
		{
			return new CoinsView(new CachedNoSqlRepository(Index));
		}

		public void AddTransaction(Transaction tx, int height)
		{
			SetCoins(tx.GetHash(), new Coins(tx, height));
		}
	}
}
