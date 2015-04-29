#if !NOFILEIO
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;

namespace ChainUtils.BitcoinCore
{
	public class IndexedBlockStore : IndexedStore<StoredBlock, Block>, IBlockProvider
	{
		private readonly BlockStore _store;

		public new BlockStore Store
		{
			get
			{
				return _store;
			}
		}
		public IndexedBlockStore(NoSqlRepository index, BlockStore store)
			: base(index, store)
		{
			_store = store;
			IndexedLimit = "Last Index Position";
		}

		public BlockHeader GetHeader(Uint256 hash)
		{
			try
			{
				return GetHeaderAsync(hash).Result;
			}
			catch(AggregateException aex)
			{
				ExceptionDispatchInfo.Capture(aex.InnerException).Throw();
				return null; //Can't happen
			}
		}

		public async Task<BlockHeader> GetHeaderAsync(Uint256 hash)
		{
			var pos = await Index.GetAsync<DiskBlockPos>(hash.ToString()).ConfigureAwait(false);
			if(pos == null)
				return null;
			var stored = _store.Enumerate(false, new DiskBlockPosRange(pos)).FirstOrDefault();
			if(stored == null)
				return null;
			return stored.Item.Header;
		}

		public Block Get(Uint256 id)
		{
			try
			{
				return GetAsync(id).Result;
			}
			catch(AggregateException aex)
			{
				ExceptionDispatchInfo.Capture(aex.InnerException).Throw();
				return null; //Can't happen
			}
		}
		public Task<Block> GetAsync(Uint256 id)
		{
			return GetAsync(id.ToString());
		}

#region IBlockProvider Members

		public Block GetBlock(Uint256 id, List<byte[]> searchedData)
		{
			var block = Get(id.ToString());
			if(block == null)
				throw new Exception("Block " + id + " not present in the index");
			return block;
		}

		#endregion

		protected override string GetKey(Block item)
		{
			return item.GetHash().ToString();
		}

		protected override IEnumerable<StoredBlock> EnumerateForIndex(DiskBlockPosRange range)
		{
			return Store.Enumerate(true, range);
		}

		protected override IEnumerable<StoredBlock> EnumerateForGet(DiskBlockPosRange range)
		{
			return Store.Enumerate(false, range);
		}
	}
}
#endif