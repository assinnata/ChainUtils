#if !NOFILEIO
using System;
using System.Collections.Generic;

namespace ChainUtils.BitcoinCore
{
	public class IndexedBlockUndoStore : IndexedStore<StoredItem<BlockUndo>, BlockUndo>
	{
		public IndexedBlockUndoStore(NoSqlRepository index, BlockUndoStore store)
			: base(index, store)
		{
			_store = store;
			IndexedLimit = "BlockUndo Index";
		}

		private readonly BlockUndoStore _store;
		public new BlockUndoStore Store
		{
			get
			{
				return _store;
			}
		}

		public void Put(Uint256 blockId, BlockUndo undo)
		{
			undo.BlockId = blockId;
			Put(undo);
		}

		public BlockUndo Get(Uint256 blockId)
		{
			if(blockId == null)
				return null;
			var undo = Get(blockId.ToString());
			undo.BlockId = blockId;
			return undo;
		}

		protected override string GetKey(BlockUndo item)
		{
			if(item.BlockId == null)
			{
				throw new NotSupportedException("BlockUndo.BlockId unknow");
			}
			return item.BlockId.ToString();
		}

		protected override IEnumerable<StoredItem<BlockUndo>> EnumerateForIndex(DiskBlockPosRange range)
		{
			return Store.Enumerate(range);
		}

		protected override IEnumerable<StoredItem<BlockUndo>> EnumerateForGet(DiskBlockPosRange range)
		{
			return Store.Enumerate(range);
		}
	}
}
#endif