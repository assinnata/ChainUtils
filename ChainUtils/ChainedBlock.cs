﻿using System;
using System.Collections.Generic;

namespace ChainUtils
{
	//public enum BlockStatus : byte
	//{
	//	VALID_UNKNOWN = 0,
	//	VALID_HEADER = 1, // parsed, version ok, hash satisfies claimed PoW, 1 <= vtx count <= max, timestamp not in future
	//	VALID_TREE = 2, // parent found, difficulty matches, timestamp >= median previous, checkpoint
	//	VALID_TRANSACTIONS = 3, // only first tx is coinbase, 2 <= coinbase input script length <= 100, transactions valid, no duplicate txids, sigops, size, merkle root
	//	VALID_CHAIN = 4, // outputs do not overspend inputs, no double spends, coinbase output ok, immature coinbase spends, BIP30
	//	VALID_SCRIPTS = 5, // scripts/signatures ok
	//	VALID_MASK = 7,

	//	HAVE_DATA = 8, // full block available in blk*.dat
	//	HAVE_UNDO = 16, // undo data available in rev*.dat
	//	HAVE_MASK = 24,

	//	FAILED_VALID = 32, // stage after last reached validness failed
	//	FAILED_CHILD = 64, // descends from failed block
	//	FAILED_MASK = 96
	//}


	/** The block chain is a tree shaped structure starting with the
 * genesis block at the root, with each block potentially having multiple
 * candidates to be the next block. A blockindex may have multiple pprev pointing
 * to it, but at most one of them can be part of the currently active branch.
 */
	public class ChainedBlock
	{
		// pointer to the hash of the block, if any. memory is owned by this CBlockIndex
		Uint256 _phashBlock;

		public Uint256 HashBlock
		{
			get
			{
				return _phashBlock;
			}
		}


		// pointer to the index of the predecessor of this block
		ChainedBlock _pprev;

		public ChainedBlock Previous
		{
			get
			{
				return _pprev;
			}
		}

		// height of the entry in the chain. The genesis block has height 0
		int _nHeight;

		public int Height
		{
			get
			{
				return _nHeight;
			}
		}

		//DiskBlockPos nDataPos;

		//public DiskBlockPos BlockPosition
		//{
		//	get
		//	{
		//		return nDataPos;
		//	}
		//}

		// Byte offset within rev?????.dat where this block's undo data is stored
		//uint nUndoPos;

		// (memory only) Total amount of work (expected number of hashes) in the chain up to and including this block
		//uint256 nChainWork;

		// Number of transactions in this block.
		// Note: in a potential headers-first mode, this number cannot be relied upon
		//uint nTx;

		// (memory only) Number of transactions in the chain up to and including this block
		//ulong nChainTx; // change to 64-bit type when necessary; won't happen before 2030

		//// Verification status of this block. See enum BlockStatus
		//BlockStatus nStatus;

		//public BlockStatus Status
		//{
		//	get
		//	{
		//		return nStatus;
		//	}
		//}

		BlockHeader _header;

		public BlockHeader Header
		{
			get
			{
				return _header;
			}
		}




		// (memory only) Sequencial id assigned to distinguish order in which blocks are received.
		//uint nSequenceId;

		public ChainedBlock(BlockHeader header,Uint256 headerHash, ChainedBlock previous)
		{
			if(previous != null)
			{
				_nHeight = previous.Height + 1;
			}
			_pprev = previous;
			//this.nDataPos = pos;
			this._header = header;
			_phashBlock = headerHash ?? header.GetHash();

			if(previous == null)
			{
				if(header.HashPrevBlock != 0)
					throw new ArgumentException("Only the genesis block can have no previous block");
			}
			else
			{
				if(previous.HashBlock != header.HashPrevBlock)
					throw new ArgumentException("The previous block has not the expected hash");
			}
		}

		public ChainedBlock(BlockHeader header, int height)
		{
			_nHeight = height;
			//this.nDataPos = pos;
			this._header = header;
			_phashBlock = header.GetHash();
		}

		public BlockLocator GetLocator()
		{
			var nStep = 1;
			var vHave = new List<Uint256>();

			var pindex = this;
			while(pindex != null)
			{
				vHave.Add(pindex.HashBlock);
				// Stop when we have added the genesis block.
				if(pindex.Height == 0)
					break;
				// Exponentially larger steps back, plus the genesis block.
				var nHeight = Math.Max(pindex.Height - nStep, 0);
				while(pindex.Height > nHeight)
					pindex = pindex.Previous;
				if(vHave.Count > 10)
					nStep *= 2;
			}

			return new BlockLocator(vHave);
		}

		public override bool Equals(object obj)
		{
			var item = obj as ChainedBlock;
			if(item == null)
				return false;
			return _phashBlock.Equals(item._phashBlock);
		}
		public static bool operator ==(ChainedBlock a, ChainedBlock b)
		{
			if(ReferenceEquals(a, b))
				return true;
			if(((object)a == null) || ((object)b == null))
				return false;
			return a._phashBlock == b._phashBlock;
		}

		public static bool operator !=(ChainedBlock a, ChainedBlock b)
		{
			return !(a == b);
		}

		public override int GetHashCode()
		{
			return _phashBlock.GetHashCode();
		}



		public IEnumerable<ChainedBlock> EnumerateToGenesis()
		{
			var current = this;
			while(current != null)
			{
				yield return current;
				current = current.Previous;
			}
		}

		public override string ToString()
		{
			return Height + " - " + HashBlock;
		}

		public ChainedBlock FindAncestorOrSelf(int height)
		{
			if(height > Height)
				throw new InvalidOperationException("Can only find blocks below or equals to current height");
			if(height < 0)
				throw new ArgumentOutOfRangeException("height");
			var currentBlock = this;
			while(height != currentBlock.Height)
			{
				currentBlock = currentBlock.Previous;
			}
			return currentBlock;
		}
		public ChainedBlock FindAncestorOrSelf(Uint256 blockHash)
		{
			var currentBlock = this;
			while(currentBlock != null && currentBlock.HashBlock != blockHash)
			{
				currentBlock = currentBlock.Previous;
			}
			return currentBlock;
		}
	}
}
