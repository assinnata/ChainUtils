using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ChainUtils.Crypto;
using ChainUtils.DataEncoders;
using ChainUtils.Protocol;
using ChainUtils.RPC;
using Newtonsoft.Json.Linq;

namespace ChainUtils
{
	/** Nodes collect new transactions into a block, hash them into a hash tree,
	 * and scan through nonce values to make the block's hash satisfy proof-of-work
	 * requirements.  When they solve the proof-of-work, they broadcast the block
	 * to everyone and the block is added to the block chain.  The first transaction
	 * in the block is a special one that creates a new coin owned by the creator
	 * of the block.
	 */
	public class BlockHeader : IBitcoinSerializable
	{
		public BlockHeader(string hex)
			: this(Encoders.Hex.DecodeData(hex))
		{

		}

		public BlockHeader(byte[] bytes)
		{
			this.ReadWrite(bytes);
		}


		// header
		const int CurrentVersion = 2;

		Uint256 _hashPrevBlock;

		public Uint256 HashPrevBlock
		{
			get
			{
				return _hashPrevBlock;
			}
			set
			{
				_hashPrevBlock = value;
			}
		}
		Uint256 _hashMerkleRoot;

		uint _nTime;
		uint _nBits;

		public Target Bits
		{
			get
			{
				return _nBits;
			}
			set
			{
				_nBits = value;
			}
		}

		int _nVersion;

		public int Version
		{
			get
			{
				return _nVersion;
			}
			set
			{
				_nVersion = value;
			}
		}

		uint _nNonce;

		public uint Nonce
		{
			get
			{
				return _nNonce;
			}
			set
			{
				_nNonce = value;
			}
		}
		public Uint256 HashMerkleRoot
		{
			get
			{
				return _hashMerkleRoot;
			}
			set
			{
				_hashMerkleRoot = value;
			}
		}

		public BlockHeader()
		{
			SetNull();
		}


		internal void SetNull()
		{
			_nVersion = CurrentVersion;
			_hashPrevBlock = 0;
			_hashMerkleRoot = 0;
			_nTime = 0;
			_nBits = 0;
			_nNonce = 0;
		}

		public bool IsNull
		{
			get
			{
				return (_nBits == 0);
			}
		}
		#region IBitcoinSerializable Members

		public void ReadWrite(BitcoinStream stream)
		{
			stream.ReadWrite(ref _nVersion);
			stream.ReadWrite(ref _hashPrevBlock);
			stream.ReadWrite(ref _hashMerkleRoot);
			stream.ReadWrite(ref _nTime);
			stream.ReadWrite(ref _nBits);
			stream.ReadWrite(ref _nNonce);
			if(stream.NetworkFormat)
			{
				var txCount = new VarInt(0);
				stream.ReadWrite(ref txCount);
			}
		}

		#endregion

		public Uint256 GetHash()
		{
			return Hashes.Hash256(this.ToBytes());
		}

		public DateTimeOffset BlockTime
		{
			get
			{
				return Utils.UnixTimeToDateTime(_nTime);
			}
			set
			{
				_nTime = Utils.DateTimeToUnixTime(value);
			}
		}

		public bool CheckProofOfWork()
		{
			// Check proof of work matches claimed amount
			if(GetHash() > Bits.ToUInt256())
				return false;
			return true;
		}

		public override string ToString()
		{
			return GetHash().ToString();
		}
	}


	public class Block : IBitcoinSerializable
	{
		public const uint MaxBlockSize = 1000000;
		BlockHeader _header = new BlockHeader();
		// network and disk
		List<Transaction> _vtx = new List<Transaction>();

		public List<Transaction> Transactions
		{
			get
			{
				return _vtx;
			}
			set
			{
				_vtx = value;
			}
		}

		public MerkleNode GetMerkleRoot()
		{
			return MerkleNode.GetRoot(Transactions.Select(t => t.GetHash()));
		}


		public Block()
		{
			SetNull();
		}

		public Block(BlockHeader blockHeader)
		{
			SetNull();
			_header = blockHeader;
		}
		public Block(byte[] bytes)
		{
			this.ReadWrite(bytes);
		}


		public void ReadWrite(BitcoinStream stream)
		{
			using(stream.NetworkFormatScope(false))
			{
				stream.ReadWrite(ref _header);
			}
			stream.ReadWrite(ref _vtx);
		}

		public bool HeaderOnly
		{
			get
			{
				return _vtx == null || _vtx.Count == 0;
			}
		}


		void SetNull()
		{
			_header.SetNull();
			_vtx.Clear();
		}

		public BlockHeader Header
		{
			get
			{
				return _header;
			}
		}


		//public MerkleBranch GetMerkleBranch(int txIndex)
		//{
		//	if(vMerkleTree.Count == 0)
		//		ComputeMerkleRoot();
		//	List<uint256> vMerkleBranch = new List<uint256>();
		//	int j = 0;
		//	for(int nSize = vtx.Count ; nSize > 1 ; nSize = (nSize + 1) / 2)
		//	{
		//		int i = Math.Min(txIndex, nSize - 1);
		//		vMerkleBranch.Add(vMerkleTree[j + i]);
		//		txIndex >>= 1;
		//		j += nSize;
		//	}
		//	return new MerkleBranch(vMerkleBranch);
		//}

		//public static uint256 CheckMerkleBranch(uint256 hash, List<uint256> vMerkleBranch, int nIndex)
		//{
		//	if(nIndex == -1)
		//		return 0;
		//	foreach(var otherside in vMerkleBranch)
		//	{
		//		if((nIndex & 1) != 0)
		//			hash = Hash(otherside, hash);
		//		else
		//			hash = Hash(hash, otherside);
		//		nIndex >>= 1;
		//	}
		//	return hash;
		//}

		//std::vector<uint256> GetMerkleBranch(int nIndex) const;
		//static uint256 CheckMerkleBranch(uint256 hash, const std::vector<uint256>& vMerkleBranch, int nIndex);
		//void print() const;

		public Uint256 GetHash()
		{
			//Block's hash is his header's hash
			return Hashes.Hash256(_header.ToBytes());
		}

		public int Length
		{
			get
			{
				return _header.ToBytes().Length;
			}
		}

		public void ReadWrite(byte[] array, int startIndex)
		{
			var ms = new MemoryStream(array);
			ms.Position += startIndex;
			var bitStream = new BitcoinStream(ms, false);
			ReadWrite(bitStream);
		}

		public Transaction AddTransaction(Transaction tx)
		{
			Transactions.Add(tx);
			return tx;
		}

		public void UpdateMerkleRoot()
		{
			Header.HashMerkleRoot = GetMerkleRoot().Hash;
		}

		/// <summary>
		/// Check proof of work and merkle root
		/// </summary>
		/// <returns></returns>
		public bool Check()
		{
			return CheckMerkleRoot() && Header.CheckProofOfWork();
		}

		public bool CheckProofOfWork()
		{
			return Header.CheckProofOfWork();
		}

		public bool CheckMerkleRoot()
		{
			return Header.HashMerkleRoot == GetMerkleRoot().Hash;
		}

		public Block CreateNextBlockWithCoinbase(BitcoinAddress address, int height)
		{
			return CreateNextBlockWithCoinbase(address, height, DateTimeOffset.UtcNow);
		}
		public Block CreateNextBlockWithCoinbase(BitcoinAddress address, int height, DateTimeOffset now)
		{
			var block = new Block();
			block.Header.Nonce = RandomUtils.GetUInt32();
			block.Header.HashPrevBlock = GetHash();
			block.Header.BlockTime = now;
			var tx = block.AddTransaction(new Transaction());
			tx.AddInput(new TxIn()
			{
				ScriptSig = new Script(Op.GetPushOp(RandomUtils.GetBytes(30)))
			});
			tx.Outputs.Add(new TxOut(address.Network.GetReward(height), address)
			{
				Value = address.Network.GetReward(height)
			});
			return block;
		}

		public Block CreateNextBlockWithCoinbase(PubKey pubkey, Money value)
		{
			return CreateNextBlockWithCoinbase(pubkey, value, DateTimeOffset.UtcNow);
		}
		public Block CreateNextBlockWithCoinbase(PubKey pubkey, Money value, DateTimeOffset now)
		{
			var block = new Block();
			block.Header.Nonce = RandomUtils.GetUInt32();
			block.Header.HashPrevBlock = GetHash();
			block.Header.BlockTime = now;
			var tx = block.AddTransaction(new Transaction());
			tx.AddInput(new TxIn()
			{
				ScriptSig = new Script(Op.GetPushOp(RandomUtils.GetBytes(30)))
			});
			tx.Outputs.Add(new TxOut()
			{
				Value = value,
				ScriptPubKey = PayToPubkeyHashTemplate.Instance.GenerateScriptPubKey(pubkey)
			});
			return block;
		}

		public static Block Parse(string json)
		{
			var formatter = new BlockExplorerFormatter();
			var block = JObject.Parse(json);
			var txs = (JArray)block["tx"];
			var blk = new Block();
			blk.Header.Bits = new Target((uint)block["bits"]);
			blk.Header.BlockTime = Utils.UnixTimeToDateTime((uint)block["time"]);
			blk.Header.Nonce = (uint)block["nonce"];
			blk.Header.Version = (int)block["ver"];
			blk.Header.HashPrevBlock = new Uint256((string)block["prev_block"]);
			blk.Header.HashMerkleRoot = new Uint256((string)block["mrkl_root"]);
			foreach(var tx in txs)
			{
				blk.AddTransaction(formatter.Parse((JObject)tx));
			}
			return blk;
		}

		public MerkleBlock Filter(params Uint256[] txIds)
		{
			return new MerkleBlock(this, txIds);
		}

		public MerkleBlock Filter(BloomFilter filter)
		{
			return new MerkleBlock(this, filter);
		}
	}
}
