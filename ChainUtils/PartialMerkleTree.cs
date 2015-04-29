using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace ChainUtils
{
	public class PartialMerkleTree : IBitcoinSerializable
	{
		uint _transactionCount;
		public uint TransactionCount
		{
			get
			{
				return _transactionCount;
			}
			set
			{
				_transactionCount = value;
			}
		}

		List<Uint256> _hashes = new List<Uint256>();
		public List<Uint256> Hashes
		{
			get
			{
				return _hashes;
			}
		}

		BitArray _flags = new BitArray(0);
		public BitArray Flags
		{
			get
			{
				return _flags;
			}
			set
			{
				_flags = value;
			}
		}

		// serialization implementation
		#region IBitcoinSerializable Members

		public void ReadWrite(BitcoinStream stream)
		{
			stream.ReadWrite(ref _transactionCount);
			stream.ReadWrite(ref _hashes);
			byte[] vBytes = null;
			if(!stream.Serializing)
			{
				stream.ReadWriteAsVarString(ref vBytes);
				var writer = new BitWriter();
				for(var p = 0 ; p < vBytes.Length * 8 ; p++)
					writer.Write((vBytes[p / 8] & (1 << (p % 8))) != 0);


			}
			else
			{
				vBytes = new byte[(_flags.Length + 7) / 8];
				for(var p = 0 ; p < _flags.Length ; p++)
					vBytes[p / 8] |= (byte)(ToByte(_flags.Get(p)) << (p % 8));
				stream.ReadWriteAsVarString(ref vBytes);
			}
		}

		private byte ToByte(bool v)
		{
			return (byte)(v ? 1 : 0);
		}

		#endregion

		public PartialMerkleTree(Uint256[] vTxid, bool[] vMatch)
		{
			if(vMatch.Length != vTxid.Length)
				throw new ArgumentException("The size of the array of txid and matches is different");
			TransactionCount = (uint)vTxid.Length;

			var root = MerkleNode.GetRoot(vTxid);
			var flags = new BitWriter();

			MarkNodes(root, vMatch);
			BuildCore(root, flags);

			Flags = flags.ToBitArray();
		}

		private static void MarkNodes(MerkleNode root, bool[] vMatch)
		{
			var matches = new BitReader(new BitArray(vMatch));
			foreach(var leaf in root.GetLeafs())
			{
				if(matches.Read())
				{
					leaf.IsMarked = true;
					foreach(var ancestor in leaf.Ancestors())
					{
						ancestor.IsMarked = true;
					}
				}
			}
		}

		public MerkleNode GetMerkleRoot()
		{
			var node = MerkleNode.GetRoot((int)TransactionCount);
			var flags = new BitReader(Flags);
			var hashes = Hashes.GetEnumerator();
			GetMatchedTransactionsCore(node, flags, hashes, true).AsEnumerable();
			return node;
		}
		public bool Check(Uint256 expectedMerkleRootHash = null)
		{
			try
			{
				var hash = GetMerkleRoot().Hash;
				return expectedMerkleRootHash == null || hash == expectedMerkleRootHash;
			}
			catch(Exception)
			{
				return false;
			}
		}
		


		private void BuildCore(MerkleNode node, BitWriter flags)
		{
			if(node == null)
				return;
			flags.Write(node.IsMarked);
			if(node.IsLeaf || !node.IsMarked)
				Hashes.Add(node.Hash);

			if(node.IsMarked)
			{
				BuildCore(node.Left, flags);
				BuildCore(node.Right, flags);
			}
		}

		public IEnumerable<Uint256> GetMatchedTransactions()
		{
			var flags = new BitReader(Flags);
			var root = MerkleNode.GetRoot((int)TransactionCount);
			var hashes = Hashes.GetEnumerator();
			return GetMatchedTransactionsCore(root, flags, hashes, false);
		}

		private IEnumerable<Uint256> GetMatchedTransactionsCore(MerkleNode node, BitReader flags, IEnumerator<Uint256> hashes, bool calculateHash)
		{
			if(node == null)
				return new Uint256[0];
			node.IsMarked = flags.Read();

			if(node.IsLeaf || !node.IsMarked)
			{
				hashes.MoveNext();
				node.Hash = hashes.Current;
			}
			if(!node.IsMarked)
				return new Uint256[0];
			if(node.IsLeaf)
				return new[] { node.Hash };
			var left = GetMatchedTransactionsCore(node.Left, flags, hashes, calculateHash);
			var right = GetMatchedTransactionsCore(node.Right, flags, hashes, calculateHash);
			if(calculateHash)
				node.UpdateHash();
			return left.Concat(right);
		}

		public MerkleNode TryGetMerkleRoot()
		{
			try
			{
				return GetMerkleRoot();
			}
			catch(Exception)
			{
				return null;
			}
		}
	}
}
