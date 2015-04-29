using System;
using System.Collections.Generic;
using System.Linq;

namespace ChainUtils
{
	public abstract class ChainBase
	{
		public virtual ChainedBlock Genesis
		{
			get
			{
				return GetBlock(0);
			}
		}
		public abstract ChainedBlock GetBlock(Uint256 id);
		public abstract ChainedBlock GetBlock(int height);
		public abstract ChainedBlock Tip
		{
			get;
		}
		public abstract int Height
		{
			get;
		}

		public bool Contains(Uint256 hash)
		{
			var pindex = GetBlock(hash);
			return pindex != null;
		}


		public IEnumerable<ChainedBlock> ToEnumerable(bool fromTip)
		{
			if(fromTip)
			{
				var b = Tip;
				while(b != null)
				{
					yield return b;
					b = b.Previous;
				}
			}
			else
			{
				foreach(var b in EnumerateFromStart())
					yield return b;
			}
		}

		public ChainedBlock SetTip(ChainBase otherChain)
		{
			return SetTip(otherChain.Tip);
		}
		public bool SetTip(BlockHeader header)
		{
			ChainedBlock chainedHeader;
			return TrySetTip(header, out chainedHeader);
		}

		public bool TrySetTip(BlockHeader header, out ChainedBlock chainedHeader)
		{
			chainedHeader = null;
			var prev = GetBlock(header.HashPrevBlock);
			if(prev == null)
				return false;
			chainedHeader = new ChainedBlock(header, header.GetHash(), GetBlock(header.HashPrevBlock));
			SetTip(chainedHeader);
			return true;
		}

		protected abstract IEnumerable<ChainedBlock> EnumerateFromStart();

		public bool Contains(ChainedBlock blockIndex)
		{
			return GetBlock(blockIndex.Height) != null;
		}

		public bool SameTip(ChainBase chain)
		{
			return Tip.HashBlock == chain.Tip.HashBlock;
		}

		static readonly TimeSpan NTargetTimespan = TimeSpan.FromSeconds(14 * 24 * 60 * 60); // two weeks
		static readonly TimeSpan NTargetSpacing = TimeSpan.FromSeconds(10 * 60);
		static readonly long NInterval = NTargetTimespan.Ticks / NTargetSpacing.Ticks;

		private void Assert(object obj)
		{
			if(obj == null)
				throw new NotSupportedException("Can only calculate work of a full chain");
		}
		public Target GetWorkRequired(Network network, int height)
		{
			var nProofOfWorkLimit = new Target(network.ProofOfWorkLimit);
			var pindexLast = height == 0 ? null : GetBlock(height - 1);

			// Genesis block
			if(pindexLast == null)
				return nProofOfWorkLimit;

			// Only change once per interval
			if((height) % NInterval != 0)
			{
				if(network == Network.TestNet)
				{
					// Special difficulty rule for testnet:
					// If the new block's timestamp is more than 2* 10 minutes
					// then allow mining of a min-difficulty block.
					if(DateTimeOffset.UtcNow > pindexLast.Header.BlockTime + TimeSpan.FromTicks(NTargetSpacing.Ticks * 2))
						return nProofOfWorkLimit;
					else
					{
						// Return the last non-special-min-difficulty-rules-block
						var pindex = pindexLast;
						while(pindex.Previous != null && (pindex.Height % NInterval) != 0 && pindex.Header.Bits == nProofOfWorkLimit)
							pindex = pindex.Previous;
						return pindex.Header.Bits;
					}
				}
				return pindexLast.Header.Bits;
			}

			// Go back by what we want to be 14 days worth of blocks
			var pastHeight = pindexLast.Height - NInterval + 1;
			var pindexFirst = GetBlock((int)pastHeight);
			Assert(pindexFirst);

			// Limit adjustment step
			var nActualTimespan = pindexLast.Header.BlockTime - pindexFirst.Header.BlockTime;
			if(nActualTimespan < TimeSpan.FromTicks(NTargetTimespan.Ticks / 4))
				nActualTimespan = TimeSpan.FromTicks(NTargetTimespan.Ticks / 4);
			if(nActualTimespan > TimeSpan.FromTicks(NTargetTimespan.Ticks * 4))
				nActualTimespan = TimeSpan.FromTicks(NTargetTimespan.Ticks * 4);

			// Retarget
			var bnNew = pindexLast.Header.Bits.ToBigInteger();
			bnNew *= (ulong)nActualTimespan.TotalSeconds;
			bnNew /= (ulong)NTargetTimespan.TotalSeconds;
			var newTarget = new Target(bnNew);
			if(newTarget > nProofOfWorkLimit)
				newTarget = nProofOfWorkLimit;

			return newTarget;
		}



		public ChainedBlock FindFork(ChainBase chain)
		{
			return FindFork(chain.ToEnumerable(true).Select(o => o.HashBlock));
		}

		public ChainedBlock FindFork(IEnumerable<Uint256> hashes)
		{
			// Find the first block the caller has in the main chain
			foreach(var hash in hashes)
			{
				var mi = GetBlock(hash);
				if(mi != null)
				{
					return mi;
				}
			}
			return Genesis;
		}
#if !PORTABLE
		public ChainedBlock FindFork(BlockLocator locator)
		{
			return FindFork(locator.Blocks);
		}
#endif

		public IEnumerable<ChainedBlock> EnumerateAfter(Uint256 blockHash)
		{
			var block = GetBlock(blockHash);
			if(block == null)
				return new ChainedBlock[0];
			return EnumerateAfter(block);
		}

		public virtual IEnumerable<ChainedBlock> EnumerateAfter(ChainedBlock block)
		{
			var i = block.Height + 1;
			var prev = block;
			while(true)
			{
				var b = GetBlock(i);
				if(b == null || b.Previous != prev)
					yield break;
				yield return b;
				i++;
				prev = b;
			}
		}

		/// <summary>
		/// Force a new tip for the chain
		/// </summary>
		/// <param name="pindex"></param>
		/// <returns>forking point</returns>
		public abstract ChainedBlock SetTip(ChainedBlock pindex);
	}
}
