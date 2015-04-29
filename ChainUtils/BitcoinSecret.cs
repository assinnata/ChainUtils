using System;
using System.Linq;
using ChainUtils.DataEncoders;

namespace ChainUtils
{
	public class BitcoinSecret : Base58Data, IDestination, ISecret
	{
		public BitcoinSecret(Key key, Network network)
			: base(ToBytes(key), network)
		{
		}

		private static byte[] ToBytes(Key key)
		{
			var keyBytes = key.ToBytes();
			if(!key.IsCompressed)
				return keyBytes;
			else
				return keyBytes.Concat(new byte[] { 0x01 }).ToArray();
		}
		public BitcoinSecret(string base58, Network expectedAddress = null)
			: base(base58, expectedAddress)
		{
		}

		private BitcoinAddress _address;

		public BitcoinAddress GetAddress()
		{
			if(_address == null)
				_address = PrivateKey.PubKey.GetAddress(Network);

			return _address;
		}

		[Obsolete("Use PubKeyHash instead")]
		public virtual KeyId Id
		{
			get
			{
				return PrivateKey.PubKey.Hash;
			}
		}
		public virtual KeyId PubKeyHash
		{
			get
			{
				return PrivateKey.PubKey.Hash;
			}
		}

		[Obsolete("Use PrivateKey instead")]
		public Key Key
		{
			get
			{
				return PrivateKey;
			}
		}

		public PubKey PubKey
		{
			get
			{
				return PrivateKey.PubKey;
			}
		}

		#region ISecret Members
		Key _key;
		public Key PrivateKey
		{
			get
			{
				if(_key == null)
					_key = new Key(VchData, 32, IsCompressed);
				return _key;
			}
		}
		#endregion

		protected override bool IsValid
		{
			get
			{
				if(VchData.Length != 33 && VchData.Length != 32)
					return false;

				if(VchData.Length == 33 && IsCompressed)
					return true;
				if(VchData.Length == 32 && !IsCompressed)
					return true;
				return false;
			}
		}

		public BitcoinEncryptedSecret Encrypt(string password)
		{
			return PrivateKey.GetEncryptedBitcoinSecret(password, Network);
		}


		public BitcoinSecret Copy(bool? compressed)
		{
			if(compressed == null)
				compressed = IsCompressed;

			if(compressed.Value && IsCompressed)
			{
				return new BitcoinSecret(WifData, Network);
			}
			else
			{
				var result = Encoders.Base58Check.DecodeData(WifData);
				var resultList = result.ToList();

				if(compressed.Value)
				{
					resultList.Insert(resultList.Count, 0x1);
				}
				else
				{
					resultList.RemoveAt(resultList.Count - 1);
				}
				return new BitcoinSecret(Encoders.Base58Check.EncodeData(resultList.ToArray()), Network);
			}
		}

		public bool IsCompressed
		{
			get
			{
				return VchData.Length > 32 && VchData[32] == 1;
			}
		}

		public override Base58Type Type
		{
			get
			{
				return Base58Type.SecretKey;
			}
		}

		#region IDestination Members

		public Script ScriptPubKey
		{
			get
			{
				return GetAddress().ScriptPubKey;
			}
		}

		#endregion


	}
}
