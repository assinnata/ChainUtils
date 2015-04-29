using System.Collections.Generic;
using System.Linq;

namespace ChainUtils
{
	public class MerkleBlock : IBitcoinSerializable
	{
		// Public only for unit testing
		BlockHeader _header;

		public BlockHeader Header
		{
			get
			{
				return _header;
			}
			set
			{
				_header = value;
			}
		}
		PartialMerkleTree _partialMerkleTree;

		public PartialMerkleTree PartialMerkleTree
		{
			get
			{
				return _partialMerkleTree;
			}
			set
			{
				_partialMerkleTree = value;
			}
		}

		// Create from a CBlock, filtering transactions according to filter
		// Note that this will call IsRelevantAndUpdate on the filter for each transaction,
		// thus the filter will likely be modified.
		public MerkleBlock(Block block, BloomFilter filter)
		{
			_header = block.Header;

			var vMatch = new List<bool>();
			var vHashes = new List<Uint256>();


			for(uint i = 0 ; i < block.Transactions.Count ; i++)
			{
				var hash = block.Transactions[(int)i].GetHash();
				vMatch.Add(filter.IsRelevantAndUpdate(block.Transactions[(int)i]));
				vHashes.Add(hash);
			}

			_partialMerkleTree = new PartialMerkleTree(vHashes.ToArray(), vMatch.ToArray());
		}

		public MerkleBlock(Block block, Uint256[] txIds)
		{
			_header = block.Header;

			var vMatch = new List<bool>();
			var vHashes = new List<Uint256>();
			for(var i = 0 ; i < block.Transactions.Count ; i++)
			{
				var hash = block.Transactions[i].GetHash();
				vHashes.Add(hash);
				vMatch.Add(txIds.Contains(hash));
			}
			_partialMerkleTree = new PartialMerkleTree(vHashes.ToArray(), vMatch.ToArray());
		}

		#region IBitcoinSerializable Members

		public void ReadWrite(BitcoinStream stream)
		{
			stream.ReadWrite(ref _header);
			stream.ReadWrite(ref _partialMerkleTree);
		}

		#endregion
	}
}
