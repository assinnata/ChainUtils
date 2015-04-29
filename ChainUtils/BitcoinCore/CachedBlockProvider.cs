#if !NOFILEIO
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace ChainUtils.BitcoinCore
{
	public class CachedBlockProvider : IBlockProvider
	{
		public CachedBlockProvider(IBlockProvider inner)
		{
			_inner = inner;
			MaxCachedBlock = 50;
		}
		private readonly IBlockProvider _inner;
		public IBlockProvider Inner
		{
			get
			{
				return _inner;
			}
		}

		public int MaxCachedBlock
		{
			get;
			set;
		}
		#region IBlockProvider Members

		ConcurrentDictionary<Uint256, Block> _blocks = new ConcurrentDictionary<Uint256, Block>();

		public Block GetBlock(Uint256 id, List<byte[]> searchedData)
		{
			Block result = null;
			if(_blocks.TryGetValue(id, out result))
				return result;
			result = Inner.GetBlock(id, searchedData);
			_blocks.AddOrUpdate(id, result, (i, b) => b);
			while(_blocks.Count > MaxCachedBlock)
			{
				var removed = TakeRandom(_blocks.Keys.ToList());
				Block ignored = null;
				_blocks.TryRemove(removed, out ignored);
			}
			return result;
		}

		private Uint256 TakeRandom(List<Uint256> id)
		{
			if(id.Count == 0)
				return null;
			var rand = new Random();
			return id[rand.Next(0, id.Count)];
		}

		#endregion
	}
}
#endif