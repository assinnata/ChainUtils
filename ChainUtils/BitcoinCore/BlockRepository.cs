#if !NOFILEIO
using System;

namespace ChainUtils.BitcoinCore
{
	public class BlockRepository
	{
		IndexedBlockStore _blockStore;
		IndexedBlockStore _headerStore;
		public BlockRepository(IndexedBlockStore blockStore, 
							   IndexedBlockStore headerStore)
		{
			if(blockStore == null)
				throw new ArgumentNullException("blockStore");
			if(headerStore == null)
				throw new ArgumentNullException("headerStore");
			if(blockStore == headerStore)
				throw new ArgumentException("The two stores should be different");
			_blockStore = blockStore;
			_headerStore = headerStore;
		}


		public void WriteBlock(Block block)
		{
			WriteBlockHeader(block.Header);
			_blockStore.Put(block);
		}
		public void WriteBlockHeader(BlockHeader header)
		{
			var block = new Block(header);
			_headerStore.Put(block);
		}

		public Block GetBlock(Uint256 hash)
		{
			return _blockStore.Get(hash) ?? _headerStore.Get(hash);
		}

		

	}
}
#endif