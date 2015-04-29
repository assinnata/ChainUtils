using System;
using System.Collections.Generic;
using System.Linq;

namespace ChainUtils.BitcoinCore
{
	public class Coins : IBitcoinSerializable
	{
		// whether transaction is a coinbase
		bool _fCoinBase;
		public bool Coinbase
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

		// unspent transaction outputs; spent outputs are .IsNull(); spent outputs at the end of the array are dropped
		List<TxOut> _vout = new List<TxOut>();

		// at which height this transaction was included in the active block chain
		uint _nHeight;
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

		// version of the CTransaction; accesses to this value should probably check for nHeight as well,
		// as new tx version will probably only be introduced at certain heights
		uint _nVersion;
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

		public List<TxOut> Outputs
		{
			get
			{
				return _vout;
			}
		}

		Money _value;
		public Money Value
		{
			get
			{
				return _value;
			}
		}

		public Coins()
		{

		}
		public Coins(Transaction tx, int height)
			: this(tx, null, height)
		{
		}
		public Coins(Transaction tx, Func<TxOut, bool> belongsToCoins, int height)
		{
			if(belongsToCoins == null)
				belongsToCoins = o => !o.ScriptPubKey.IsUnspendable;
			_fCoinBase = tx.IsCoinBase;
			_vout = tx.Outputs.ToList();
			_nVersion = tx.Version;
			_nHeight = (uint)height;
			ClearUnused(belongsToCoins);
			UpdateValue();
		}

		private void UpdateValue()
		{
			_value = Outputs.Where(o => !o.IsNull).Select(o => o.Value).Sum();
		}

		public bool IsEmpty
		{
			get
			{
				return _vout.Count == 0;
			}
		}

		private void ClearUnused(Func<TxOut, bool> belongsToCoins)
		{
			for(var i = 0 ; i < _vout.Count ; i++)
			{
				var o = _vout[i];
				if(o.ScriptPubKey.IsUnspendable || !belongsToCoins(o))
				{
					_vout[i] = new TxOut();
				}
			}
			Cleanup();
		}

		private void Cleanup()
		{
			var count = _vout.Count;
			// remove spent outputs at the end of vout
			for(var i = count - 1 ; i >= 0 ; i--)
			{
				if(_vout[i].IsNull)
					_vout.RemoveAt(i);
				else
					break;
			}
		}

#if !PORTABLE
		public bool Spend(int position, out TxInUndo undo)
		{
			undo = null;
			if(position >= _vout.Count)
				return false;
			if(_vout[position].IsNull)
				return false;
			undo = new TxInUndo(_vout[position].Clone());
			_vout[position].SetNull();
			Cleanup();
			if(_vout.Count == 0)
			{
				undo.Height = _nHeight;
				undo.CoinBase = _fCoinBase;
				undo.Version = _nVersion;
			}
			return true;
		}

		public bool Spend(int position)
		{
			TxInUndo undo;
			return Spend(position, out undo);
		}
#endif
		#region IBitcoinSerializable Members

		public void ReadWrite(BitcoinStream stream)
		{
			if(stream.Serializing)
			{
				uint nMaskSize = 0, nMaskCode = 0;
				CalcMaskSize(ref nMaskSize, ref nMaskCode);
				var fFirst = _vout.Count > 0 && !_vout[0].IsNull;
				var fSecond = _vout.Count > 1 && !_vout[1].IsNull;
				var nCode = unchecked((uint)(8 * (nMaskCode - (fFirst || fSecond ? 0 : 1)) + (_fCoinBase ? 1 : 0) + (fFirst ? 2 : 0) + (fSecond ? 4 : 0)));
				// version
				stream.ReadWriteAsVarInt(ref _nVersion);
				// size of header code
				stream.ReadWriteAsVarInt(ref nCode);
				// spentness bitmask
				for(uint b = 0 ; b < nMaskSize ; b++)
				{
					byte chAvail = 0;
					for(uint i = 0 ; i < 8 && 2 + b * 8 + i < _vout.Count ; i++)
						if(!_vout[2 + (int)b * 8 + (int)i].IsNull)
							chAvail |= (byte)(1 << (int)i);
					stream.ReadWrite(ref chAvail);
				}

				// txouts themself
				for(uint i = 0 ; i < _vout.Count ; i++)
				{
					if(!_vout[(int)i].IsNull)
					{
						var compressedTx = new TxOutCompressor(_vout[(int)i]);
						stream.ReadWrite(ref compressedTx);
					}
				}
				// coinbase height
				stream.ReadWriteAsVarInt(ref _nHeight);
			}
			else
			{
				uint nCode = 0;
				// version
				stream.ReadWriteAsVarInt(ref _nVersion);
				//// header code
				stream.ReadWriteAsVarInt(ref nCode);
				_fCoinBase = (nCode & 1) != 0;
				var vAvail = new List<bool>() { false, false };
				vAvail[0] = (nCode & 2) != 0;
				vAvail[1] = (nCode & 4) != 0;
				var nMaskCode = unchecked((uint)((nCode / 8) + ((nCode & 6) != 0 ? 0 : 1)));
				//// spentness bitmask
				while(nMaskCode > 0)
				{
					byte chAvail = 0;
					stream.ReadWrite(ref chAvail);
					for(uint p = 0 ; p < 8 ; p++)
					{
						var f = (chAvail & (1 << (int)p)) != 0;
						vAvail.Add(f);
					}
					if(chAvail != 0)
						nMaskCode--;
				}
				// txouts themself
				_vout = Enumerable.Range(0, vAvail.Count).Select(_ => new TxOut()).ToList();
				for(uint i = 0 ; i < vAvail.Count ; i++)
				{
					if(vAvail[(int)i])
					{
						var compressed = new TxOutCompressor();
						stream.ReadWrite(ref compressed);
						_vout[(int)i] = compressed.TxOut;
					}
				}
				//// coinbase height
				stream.ReadWriteAsVarInt(ref _nHeight);
				Cleanup();
				UpdateValue();
			}
		}

		// calculate number of bytes for the bitmask, and its number of non-zero bytes
		// each bit in the bitmask represents the availability of one output, but the
		// availabilities of the first two outputs are encoded separately
		private void CalcMaskSize(ref uint nBytes, ref uint nNonzeroBytes)
		{
			uint nLastUsedByte = 0;
			for(uint b = 0 ; 2 + b * 8 < _vout.Count ; b++)
			{
				var fZero = true;
				for(uint i = 0 ; i < 8 && 2 + b * 8 + i < _vout.Count ; i++)
				{
					if(!_vout[2 + (int)b * 8 + (int)i].IsNull)
					{
						fZero = false;
						continue;
					}
				}
				if(!fZero)
				{
					nLastUsedByte = b + 1;
					nNonzeroBytes++;
				}
			}
			nBytes += nLastUsedByte;
		}

		// check whether a particular output is still available
		public bool IsAvailable(uint position)
		{
			return (position < _vout.Count && !_vout[(int)position].IsNull);
		}

		// check whether the entire CCoins is spent
		// note that only !IsPruned() CCoins can be serialized
		public bool IsPruned
		{
			get
			{
				return _vout.All(v => v.IsNull);
			}
		}

		#endregion

		public void ClearUnspendable()
		{
			ClearUnused(o => !o.ScriptPubKey.IsUnspendable);
		}

		public void MergeFrom(Coins otherCoin)
		{
			var diff = otherCoin.Outputs.Count - Outputs.Count;
			if(diff > 0)
			{
				Outputs.Resize(otherCoin.Outputs.Count);
				for(var i = 0 ; i < Outputs.Count ; i++)
				{
					if(Outputs[i] == null)
						Outputs[i] = new TxOut();
				}
			}
			for(var i = 0 ; i < otherCoin.Outputs.Count ; i++)
			{
				if(!otherCoin.Outputs[i].IsNull)
					Outputs[i] = otherCoin.Outputs[i];
			}
			UpdateValue();
		}
	}
}
