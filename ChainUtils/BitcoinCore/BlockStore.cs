#if !NOFILEIO
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ChainUtils.BitcoinCore
{
	public class BlockStore : Store<StoredBlock, Block>
	{
		public const int MaxBlockfileSize = 0x8000000; // 128 MiB



		public BlockStore(string folder, Network network)
			: base(folder, network)
		{
			MaxFileSize = MaxBlockfileSize;
			FilePrefix = "blk";
		}


		public ConcurrentChain GetChain()
		{
			var chain = new ConcurrentChain(Network);
			SynchronizeChain(chain);
			return chain;
		}

		public void SynchronizeChain(ChainBase chain)
		{
			var headers = new Dictionary<Uint256, BlockHeader>();
			var inChain = new HashSet<Uint256>();
			inChain.Add(chain.GetBlock(0).HashBlock);
			foreach(var header in Enumerate(true).Select(b => b.Item.Header))
			{
				var hash = header.GetHash();
				headers.Add(hash, header);
			}
			var toRemove = new List<Uint256>();
			while(headers.Count != 0)
			{
				foreach(var header in headers)
				{
					if(inChain.Contains(header.Value.HashPrevBlock))
					{
						toRemove.Add(header.Key);
						chain.SetTip(header.Value);
						inChain.Add(header.Key);
					}
				}
				foreach(var item in toRemove)
					headers.Remove(item);
				if(toRemove.Count == 0)
					break;
				toRemove.Clear();
			}
		}


		[ThreadStatic]
		bool _headerOnly;
		public IEnumerable<StoredBlock> Enumerate(Stream stream, uint fileIndex = 0, DiskBlockPosRange range = null, bool headersOnly = false)
		{
			using(HeaderOnlyScope(headersOnly))
			{
				foreach(var r in Enumerate(stream, fileIndex, range))
				{
					yield return r;
				}
			}
		}


		private IDisposable HeaderOnlyScope(bool headersOnly)
		{
			var old = headersOnly;
			var oldBuff = BufferSize;
			return new Scope(() =>
			{
				_headerOnly = headersOnly;
				if(!_headerOnly)
					BufferSize = 1024 * 1024;
			}, () =>
			{
				_headerOnly = old;
				BufferSize = oldBuff;
			});
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="headersOnly"></param>
		/// <param name="blockStart">Inclusive block count</param>
		/// <param name="blockEnd">Exclusive block count</param>
		/// <returns></returns>
		public IEnumerable<StoredBlock> Enumerate(bool headersOnly, int blockCountStart, int count = 999999999)
		{
			var blockCount = 0;
			DiskBlockPos start = null;
			foreach(var block in Enumerate(true, null))
			{
				if(blockCount == blockCountStart)
				{
					start = block.BlockPosition;
				}
				blockCount++;
			}
			if(start == null)
				yield break;


			var i = 0;
			foreach(var result in Enumerate(headersOnly, new DiskBlockPosRange(start)))
			{
				if(i >= count)
					break;
				yield return result;
				i++;
			}

		}

		public IEnumerable<StoredBlock> Enumerate(bool headersOnly, DiskBlockPosRange range = null)
		{
			using(HeaderOnlyScope(headersOnly))
			{
				foreach(var result in Enumerate(range))
				{
					yield return result;
				}
			}
		}


		protected override StoredBlock ReadStoredItem(Stream stream, DiskBlockPos pos)
		{
			var block = new StoredBlock(Network, pos);
			block.ParseSkipBlockContent = _headerOnly;
			block.ReadWrite(stream, false);
			return block;
		}

		protected override StoredBlock CreateStoredItem(Block item, DiskBlockPos position)
		{
			return new StoredBlock(Network.Magic, item, position);
		}
	}
}
#endif