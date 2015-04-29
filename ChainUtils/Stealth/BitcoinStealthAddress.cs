using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ChainUtils.Stealth
{
	public class BitField
	{
		byte[] _rawform;
		byte[] _mask;
		private readonly int _bitCount;
		public int BitCount
		{
			get
			{
				return _bitCount;
			}
		}
		public int ByteCount
		{
			get
			{
				return _rawform.Length;
			}
		}

		public byte[] Mask
		{
			get
			{
				return _mask;
			}
		}

		public BitField(byte[] rawform, int bitcount)
		{
			_bitCount = bitcount;

			var byteCount = GetPrefixByteLength(bitcount);
			if(rawform.Length == byteCount)
				_rawform = rawform.ToArray();
			if(rawform.Length < byteCount)
				_rawform = rawform.Concat(new byte[byteCount - rawform.Length]).ToArray();
			if(rawform.Length > byteCount)
				_rawform = rawform.Take(byteCount).ToArray();

			_mask = new byte[byteCount];
			var bitleft = bitcount;

			for(var i = 0 ; i < byteCount ; i++)
			{
				var numberBits = Math.Min(8, bitleft);
				_mask[i] = (byte)((1 << numberBits) - 1);
				bitleft -= numberBits;
				if(bitleft == 0)
					break;
			}
		}
		public BitField(uint encodedForm, int bitcount)
			: this(Utils.ToBytes(encodedForm, true), bitcount)
		{

		}

		public static int GetPrefixByteLength(int bitcount)
		{
			if(bitcount > 32)
				throw new ArgumentException("Bitcount should be less or equal to 32", "bitcount");
			if(bitcount == 0)
				return 0;
			return Math.Min(4, bitcount / 8 + 1);
		}

		public byte[] GetRawForm()
		{
			return _rawform.ToArray();
		}

		public uint GetEncodedForm()
		{
			var encoded =
				_rawform.Length == 4 ? _rawform : _rawform.Concat(new byte[4 - _rawform.Length]).ToArray();

			return Utils.ToUInt32(encoded, true);
		}

		public bool Match(uint value)
		{
			var data = Utils.ToBytes(value, true);
			if(data.Length * 8 < _bitCount)
				return false;

			for(var i = 0 ; i < _mask.Length ; i++)
			{
				if((data[i] & _mask[i]) != (_rawform[i] & _mask[i]))
					return false;
			}
			return true;
		}
		public bool Match(StealthMetadata metadata)
		{
			return Match(metadata.BitField);
		}

		public StealthPayment[] GetPayments(Transaction transaction)
		{
			return StealthPayment.GetPayments(transaction, null, null);
		}
	}
	public class BitcoinStealthAddress : Base58Data
	{

		public BitcoinStealthAddress(string base58, Network expectedNetwork = null)
			: base(base58, expectedNetwork)
		{
		}
		public BitcoinStealthAddress(byte[] raw, Network network)
			: base(raw, network)
		{
		}


		public BitcoinStealthAddress(PubKey scanKey, PubKey[] pubKeys, int signatureCount, BitField bitfield, Network network)
			: base(GenerateBytes(scanKey, pubKeys, bitfield, signatureCount), network)
		{
		}





		public byte Options
		{
			get;
			private set;
		}

		public byte SignatureCount
		{
			get;
			set;
		}

		public PubKey ScanPubKey
		{
			get;
			private set;
		}

		public PubKey[] SpendPubKeys
		{
			get;
			private set;
		}

		public BitField Prefix
		{
			get;
			private set;
		}

		protected override bool IsValid
		{
			get
			{
				try
				{
					var ms = new MemoryStream(VchData);
					Options = (byte)ms.ReadByte();
					ScanPubKey = new PubKey(ms.ReadBytes(33));
					var pubkeycount = (byte)ms.ReadByte();
					var pubKeys = new List<PubKey>();
					for(var i = 0 ; i < pubkeycount ; i++)
					{
						pubKeys.Add(new PubKey(ms.ReadBytes(33)));
					}
					SpendPubKeys = pubKeys.ToArray();
					SignatureCount = (byte)ms.ReadByte();

					var bitcount = (byte)ms.ReadByte();
					var byteLength = BitField.GetPrefixByteLength(bitcount);

					var prefix = ms.ReadBytes(byteLength);
					Prefix = new BitField(prefix, bitcount);
				}
				catch(Exception)
				{
					return false;
				}
				return true;
			}
		}
		private static byte[] GenerateBytes(PubKey scanKey, PubKey[] pubKeys, BitField bitField, int signatureCount)
		{
			var ms = new MemoryStream();
			ms.WriteByte(0); //Options
			ms.Write(scanKey.Compress().ToBytes(), 0, 33);
			ms.WriteByte((byte)pubKeys.Length);
			foreach(var key in pubKeys)
			{
				ms.Write(key.Compress().ToBytes(), 0, 33);
			}
			ms.WriteByte((byte)signatureCount);
			if(bitField == null)
				ms.Write(new byte[] { 0 }, 0, 1);
			else
			{
				ms.WriteByte((byte)bitField.BitCount);
				var raw = bitField.GetRawForm();
				ms.Write(raw, 0, raw.Length);
			}
			return ms.ToArray();
		}

		public override Base58Type Type
		{
			get
			{
				return Base58Type.StealthAddress;
			}
		}


		/// <summary>
		/// Scan the Transaction for StealthCoin given address and scan key
		/// </summary>
		/// <param name="tx">The transaction to scan</param>
		/// <param name="address">The stealth address</param>
		/// <param name="scan">The scan private key</param>
		/// <returns></returns>
		public StealthPayment[] GetPayments(Transaction transaction, Key scanKey)
		{
			return StealthPayment.GetPayments(transaction, this, scanKey);
		}

		/// <summary>
		/// Scan the Transaction for StealthCoin given address and scan key
		/// </summary>
		/// <param name="tx">The transaction to scan</param>
		/// <param name="address">The stealth address</param>
		/// <param name="scan">The scan private key</param>
		/// <returns></returns>
		public StealthPayment[] GetPayments(Transaction transaction, ISecret scanKey)
		{
			return GetPayments(transaction, scanKey.PrivateKey);
		}

		/// <summary>
		/// Prepare a stealth payment 
		/// </summary>
		/// <param name="ephemKey">Ephem Key</param>
		/// <returns>Stealth Payment</returns>
		public StealthPayment CreatePayment(Key ephemKey = null)
		{
			if(ephemKey == null)
				ephemKey = new Key();

			var metadata = StealthMetadata.CreateMetadata(ephemKey, Prefix);
			return new StealthPayment(this, ephemKey, metadata);
		}

		/// <summary>
		/// Add a stealth payment to the transaction
		/// </summary>
		/// <param name="transaction">Destination transaction</param>
		/// <param name="value">Money to send</param>
		/// <param name="ephemKey">Ephem Key</param>
		public void SendTo(Transaction transaction, Money value, Key ephemKey = null)
		{
			CreatePayment(ephemKey).AddToTransaction(transaction, value);
		}
	}
}
