using System.Collections.Generic;
using System.IO;
using ChainUtils.Crypto;

namespace ChainUtils.BitcoinCore
{
	/** Undo information for a CTxIn
 *
 *  Contains the prevout's CTxOut being spent, and if this was the
 *  last output of the affected transaction, its metadata as well
 *  (coinbase or not, height, transaction version)
 */
	public class TxInUndo : IBitcoinSerializable
	{
		public TxInUndo()
		{

		}
		public TxInUndo(TxOut txOut)
		{
			TxOut = txOut;
		}
		
		TxOut _txout;         // the txout data before being spent

		public TxOut TxOut
		{
			get
			{
				return _txout;
			}
			set
			{
				_txout = value;
			}
		}
		bool _fCoinBase;       // if the outpoint was the last unspent: whether it belonged to a coinbase

		public bool CoinBase
		{
			get
			{
				return _fCoinBase;
			}
			set
			{
				_fCoinBase = value;
			}
		}
		uint _nHeight; // if the outpoint was the last unspent: its height

		public uint Height
		{
			get
			{
				return _nHeight;
			}
			set
			{
				_nHeight = value;
			}
		}
		uint _nVersion;        // if the outpoint was the last unspent: its version

		public uint Version
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


		#region IBitcoinSerializable Members

		public void ReadWrite(BitcoinStream stream)
		{
			if(stream.Serializing)
			{
				var o = (uint)(_nHeight * 2 + (_fCoinBase ? 1 : 0));
				stream.ReadWriteAsCompactVarInt(ref  o);
				if(_nHeight > 0)
					stream.ReadWriteAsCompactVarInt(ref _nVersion);
				var compressor = new TxOutCompressor(_txout);
				stream.ReadWrite(ref compressor);
			}
			else
			{
				uint nCode = 0;
				stream.ReadWriteAsCompactVarInt(ref nCode);
				_nHeight = nCode / 2;
				_fCoinBase = (nCode & 1) != 0;
				if(_nHeight > 0)
					stream.ReadWriteAsCompactVarInt(ref _nVersion);
				var compressor = new TxOutCompressor();
				stream.ReadWrite(ref compressor);
				_txout = compressor.TxOut;
			}
		}

		#endregion
	}
	public class TxUndo : IBitcoinSerializable
	{
		// undo information for all txins
		List<TxInUndo> _vprevout = new List<TxInUndo>();
		public List<TxInUndo> Prevout
		{
			get
			{
				return _vprevout;
			}
		}

		#region IBitcoinSerializable Members

		public void ReadWrite(BitcoinStream stream)
		{
			stream.ReadWrite(ref _vprevout);
		}

		#endregion
	}
	public class BlockUndo : IBitcoinSerializable
	{
		List<TxUndo> _vtxundo = new List<TxUndo>();
		public List<TxUndo> TxUndo
		{
			get
			{
				return _vtxundo;
			}
		}



		#region IBitcoinSerializable Members

		public void ReadWrite(BitcoinStream stream)
		{
			stream.ReadWrite(ref _vtxundo);
		}

		#endregion


		public Uint256 CalculatedChecksum
		{
			get;
			internal set;
		}
		public void ComputeChecksum(Uint256 hashBlock)
		{
			var ms = new MemoryStream();
			hashBlock.ReadWrite(ms, true);
			this.ReadWrite(ms, true);
			CalculatedChecksum = Hashes.Hash256(ms.ToArray());
		}

		public Uint256 BlockId
		{
			get;
			set;
		}
	}
}
