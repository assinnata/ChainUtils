using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace ChainUtils
{
	public class ConcurrentChain : ChainBase
	{
		Dictionary<Uint256, ChainedBlock> _blocksById = new Dictionary<Uint256, ChainedBlock>();
		Dictionary<int, ChainedBlock> _blocksByHeight = new Dictionary<int, ChainedBlock>();
		ReaderWriterLock _lock = new ReaderWriterLock();

		public ConcurrentChain()
		{

		}
		public ConcurrentChain(BlockHeader genesis)
		{
			SetTip(new ChainedBlock(genesis, 0));
		}
		public ConcurrentChain(Network network)
		{
			if(network != null)
			{
				var genesis = network.GetGenesis();
				SetTip(new ChainedBlock(genesis.Header, 0));
			}
		}

		public ConcurrentChain(byte[] bytes)
		{
			Load(bytes);
		}

		public void Load(byte[] chain)
		{
			Load(new MemoryStream(chain));
		}

		public void Load(Stream stream)
		{
			Load(new BitcoinStream(stream, false));
		}

		public void Load(BitcoinStream stream)
		{
			using(_lock.LockWrite())
			{
				try
				{
					var height = 0;
					while(true)
					{
						Uint256 id = null;
						stream.ReadWrite<Uint256>(ref id);
						BlockHeader header = null;
						stream.ReadWrite<BlockHeader>(ref header);
						if(height == 0)
						{
							_blocksByHeight.Clear();
							_blocksById.Clear();
							_tip = null;
							SetTipNoLock(new ChainedBlock(header, 0));
						}
						else
							SetTipNoLock(new ChainedBlock(header, id, Tip));
						height++;
					}
				}
				catch(EndOfStreamException)
				{
				}
			}
		}

		public byte[] ToBytes()
		{
			var ms = new MemoryStream();
			WriteTo(ms);
			return ms.ToArray();
		}

		public void WriteTo(Stream stream)
		{
			WriteTo(new BitcoinStream(stream, true));
		}

		public void WriteTo(BitcoinStream stream)
		{
			using(_lock.LockRead())
			{
				for(var i = 0 ; i < Tip.Height + 1 ; i++)
				{
					var block = GetBlockNoLock(i);
					stream.ReadWrite(block.HashBlock);
					stream.ReadWrite(block.Header);
				}
			}
		}

		public ConcurrentChain Clone()
		{
			var chain = new ConcurrentChain();
			chain._tip = _tip;
			using(_lock.LockRead())
			{
				foreach(var kv in _blocksById)
				{
					chain._blocksById.Add(kv.Key, kv.Value);
				}
				foreach(var kv in _blocksByHeight)
				{
					chain._blocksByHeight.Add(kv.Key, kv.Value);
				}
			}
			return chain;
		}

		/// <summary>
		/// Force a new tip for the chain
		/// </summary>
		/// <param name="pindex"></param>
		/// <returns>forking point</returns>
		public override ChainedBlock SetTip(ChainedBlock block)
		{
			using(_lock.LockWrite())
			{
				return SetTipNoLock(block);
			}
		}

		private ChainedBlock SetTipNoLock(ChainedBlock block)
		{
			var height = Tip == null ? -1 : Tip.Height;
			foreach(var orphaned in EnumerateThisToFork(block))
			{
				_blocksById.Remove(orphaned.HashBlock);
				_blocksByHeight.Remove(orphaned.Height);
				height--;
			}
			var fork = GetBlockNoLock(height);
			foreach(var newBlock in block.EnumerateToGenesis()
				.TakeWhile(c => c != Tip))
			{
				_blocksById.AddOrReplace(newBlock.HashBlock, newBlock);
				_blocksByHeight.AddOrReplace(newBlock.Height, newBlock);
			}
			_tip = block;
			return fork;
		}



		private IEnumerable<ChainedBlock> EnumerateThisToFork(ChainedBlock block)
		{
			if(_tip == null)
				yield break;
			var tip = _tip;
			while(true)
			{
				if(ReferenceEquals(null, block) || ReferenceEquals(null, tip))
					throw new InvalidOperationException("No fork found between the two chains");
				if(tip.Height > block.Height)
				{
					yield return tip;
					tip = tip.Previous;
				}
				else if(tip.Height < block.Height)
				{
					block = block.Previous;
				}
				else if(tip.Height == block.Height)
				{
					if(tip.HashBlock == block.HashBlock)
						break;
					yield return tip;
					block = block.Previous;
					tip = tip.Previous;
				}
			}
		}

		#region IChain Members

		public override ChainedBlock GetBlock(Uint256 id)
		{
			using(_lock.LockRead())
			{
				ChainedBlock result;
				_blocksById.TryGetValue(id, out result);
				return result;
			}
		}

		private ChainedBlock GetBlockNoLock(int height)
		{
			ChainedBlock result;
			_blocksByHeight.TryGetValue(height, out result);
			return result;
		}

		public override ChainedBlock GetBlock(int height)
		{
			using(_lock.LockRead())
			{
				return GetBlockNoLock(height);
			}
		}


		volatile ChainedBlock _tip;
		public override ChainedBlock Tip
		{
			get
			{
				return _tip;
			}
		}

		public override int Height
		{
			get
			{
				return Tip.Height;
			}
		}

		#endregion

		protected override IEnumerable<ChainedBlock> EnumerateFromStart()
		{
			using(_lock.LockRead())
			{
				var i = 0;
				while(true)
				{
					var block = GetBlockNoLock(i);
					if(block == null)
						yield break;
					yield return block;
					i++;
				}
			}
		}

		public override string ToString()
		{
			return Tip == null ? "no tip" : Tip.Height.ToString();
		}


		
	}

	internal class ReaderWriterLock
	{
		class FuncDisposable : IDisposable
		{
			Action _onEnter, _onLeave;
			public FuncDisposable(Action onEnter, Action onLeave)
			{
				this._onEnter = onEnter;
				this._onLeave = onLeave;
				onEnter();
			}

			#region IDisposable Members

			public void Dispose()
			{
				_onLeave();
			}

			#endregion
		}
		ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();

		public IDisposable LockRead()
		{
			return new FuncDisposable(() => _lock.EnterReadLock(), () => _lock.ExitReadLock());
		}
		public IDisposable LockWrite()
		{
			return new FuncDisposable(() => _lock.EnterWriteLock(), () => _lock.ExitWriteLock());
		}		
	}
}
