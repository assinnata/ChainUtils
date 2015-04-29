using System;
using System.Linq;
using ChainUtils.BouncyCastle.Math;
using ChainUtils.Crypto;
using ChainUtils.DataEncoders;

namespace ChainUtils
{
	//https://github.com/bitcoin/bips/blob/master/bip-0032.mediawiki
	public class ExtKey : IBitcoinSerializable, IDestination, ISecret
	{
		public static ExtKey Parse(string wif, Network expectedNetwork = null)
		{
			return Network.CreateFromBase58Data<BitcoinExtKey>(wif, expectedNetwork).ExtKey;
		}

		Key _key;
		byte[] _vchChainCode = new byte[32];
		uint _nChild;
		byte _nDepth;
		byte[] _vchFingerprint = new byte[4];

		static readonly byte[] Hashkey = new[] { 'B', 'i', 't', 'c', 'o', 'i', 'n', ' ', 's', 'e', 'e', 'd' }.Select(o => (byte)o).ToArray();

		public byte Depth
		{
			get
			{
				return _nDepth;
			}
		}
		public uint Child
		{
			get
			{
				return _nChild;
			}
		}

		public ExtKey(BitcoinExtPubKey extPubKey, BitcoinSecret key)
			: this(extPubKey.ExtPubKey, key.PrivateKey)
		{
		}
		public ExtKey(ExtPubKey extPubKey, Key privateKey)
		{
			if(extPubKey == null)
				throw new ArgumentNullException("extPubKey");
			if(privateKey == null)
				throw new ArgumentNullException("privateKey");
			_nChild = extPubKey.NChild;
			_nDepth = extPubKey.NDepth;
			_vchChainCode = extPubKey.VchChainCode;
			_vchFingerprint = extPubKey.VchFingerprint;
			_key = privateKey;
		}
		public ExtKey()
		{
			var seed = RandomUtils.GetBytes(64);
			SetMaster(seed);
		}
		public Key PrivateKey
		{
			get
			{
				return _key;
			}
		}
		[Obsolete("Use PrivateKey instead")]
		public Key Key
		{
			get
			{
				return _key;
			}
		}
		public ExtKey(string seedHex)
		{
			SetMaster(Encoders.Hex.DecodeData(seedHex));
		}
		public ExtKey(byte[] seed)
		{
			SetMaster(seed.ToArray());
		}
		private void SetMaster(byte[] seed)
		{
			var hashMac = Hashes.Hmacsha512(Hashkey, seed);
			_key = new Key(hashMac.Take(32).ToArray());
			Array.Copy(hashMac.Skip(32).Take(32).ToArray(), 0, _vchChainCode, 0, _vchChainCode.Length);
		}

		public ExtPubKey Neuter()
		{
			var ret = new ExtPubKey
			{
				NDepth = _nDepth,
				VchFingerprint = _vchFingerprint.ToArray(),
				NChild = _nChild,
				Pubkey = _key.PubKey,
				VchChainCode = _vchChainCode.ToArray()
			};
			return ret;
		}

		public bool IsChildOf(ExtKey parentKey)
		{
			if(Depth != parentKey.Depth + 1)
				return false;
			return parentKey.CalculateChildFingerprint().SequenceEqual(Fingerprint);
		}
		public bool IsParentOf(ExtKey childKey)
		{
			return childKey.IsChildOf(this);
		}
		private byte[] CalculateChildFingerprint()
		{
			return _key.PubKey.Hash.ToBytes().Take(_vchFingerprint.Length).ToArray();
		}

		public byte[] Fingerprint
		{
			get
			{
				return _vchFingerprint;
			}
		}
		public ExtKey Derive(uint index)
		{
			var result = new ExtKey
			{
				_nDepth = (byte)(_nDepth + 1),
				_vchFingerprint = CalculateChildFingerprint(),
				_nChild = index
			};
			result._key = _key.Derivate(_vchChainCode, index, out result._vchChainCode);
			return result;
		}

		public ExtKey Derive(int index, bool hardened)
		{
			if(index < 0)
				throw new ArgumentOutOfRangeException("index", "the index can't be negative");
			var realIndex = (uint)index;
			realIndex = hardened ? realIndex | 0x80000000u : realIndex;
			return Derive(realIndex);
		}

		public BitcoinExtKey GetWif(Network network)
		{
			return new BitcoinExtKey(this, network);
		}

		#region IBitcoinSerializable Members

		public void ReadWrite(BitcoinStream stream)
		{
			using(stream.BigEndianScope())
			{
				stream.ReadWrite(ref _nDepth);
				stream.ReadWrite(ref _vchFingerprint);
				stream.ReadWrite(ref _nChild);
				stream.ReadWrite(ref _vchChainCode);
				byte b = 0;
				stream.ReadWrite(ref b);
				stream.ReadWrite(ref _key);
			}
		}

		#endregion

		public ExtKey Derive(KeyPath derivation)
		{
			var result = this;
			return derivation.Indexes.Aggregate(result, (current, index) => current.Derive(index));
		}

		public string ToString(Network network)
		{
			return new BitcoinExtKey(this, network).ToString();
		}

		#region IDestination Members

		public Script ScriptPubKey
		{
			get
			{
				return PrivateKey.PubKey.Hash.ScriptPubKey;
			}
		}

		#endregion

		public bool IsHardened
		{
			get
			{
				return (_nChild & 0x80000000u) != 0;
			}
		}

		public ExtKey GetParentExtKey(ExtPubKey parent)
		{
			if(parent == null)
				throw new ArgumentNullException("parent");
			if(Depth == 0)
				throw new InvalidOperationException("This ExtKey is the root key of the HD tree");
			if(IsHardened)
				throw new InvalidOperationException("This private key is hardened, so you can't get its parent");
			var expectedFingerPrint = parent.CalculateChildFingerprint();
			if(parent.Depth != Depth - 1 || !expectedFingerPrint.SequenceEqual(_vchFingerprint))
				throw new ArgumentException("The parent ExtPubKey is not the immediate parent of this ExtKey", "parent");

			byte[] l = null;
			var ll = new byte[32];
			var lr = new byte[32];

			var pubKey = parent.PubKey.ToBytes();
			l = Hashes.Bip32Hash(parent.VchChainCode, _nChild, pubKey[0], pubKey.Skip(1).ToArray());
			Array.Copy(l, ll, 32);
			Array.Copy(l, 32, lr, 0, 32);
			var ccChild = lr;

			var parse256Ll = new BigInteger(1, ll);
			var n = EcKey.Curve.N;

			if(!ccChild.SequenceEqual(_vchChainCode))
				throw new InvalidOperationException("The derived chain code of the parent is not equal to this child chain code");

			var keyBytes = PrivateKey.ToBytes();
			var key = new BigInteger(1, keyBytes);

			var kPar = key.Add(parse256Ll.Negate()).Mod(n);
			var keyParentBytes = kPar.ToByteArrayUnsigned();
			if(keyParentBytes.Length < 32)
				keyParentBytes = new byte[32 - keyParentBytes.Length].Concat(keyParentBytes).ToArray();

			var parentExtKey = new ExtKey
			{
				_vchChainCode = parent.VchChainCode,
				_nDepth = parent.Depth,
				_vchFingerprint = parent.Fingerprint,
				_nChild = parent.NChild,
				_key = new Key(keyParentBytes)
			};
			return parentExtKey;
		}

	}
}
