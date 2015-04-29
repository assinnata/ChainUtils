using System;
using System.Linq;
using ChainUtils.BouncyCastle.Math;
using ChainUtils.BouncyCastle.Math.EC;
using ChainUtils.Crypto;
using ChainUtils.DataEncoders;
using ChainUtils.Stealth;

namespace ChainUtils
{
	public class PubKey : IBitcoinSerializable, IDestination
	{
		/// <summary>
		/// Create a new Public key from string
		/// </summary>
		public PubKey(string hex)
			: this(Encoders.Hex.DecodeData(hex))
		{

		}

		/// <summary>
		/// Create a new Public key from byte array
		/// </summary>
		public PubKey(byte[] bytes)
			: this(bytes, false)
		{
		}

		/// <summary>
		/// Create a new Public key from byte array
		/// </summary>
		/// <param name="bytes">byte array</param>
		/// <param name="unsafe">If false, make internal copy of bytes and does perform only a costly check for PubKey format. If true, the bytes array is used as is and only PubKey.QuickCheck is used for validating the format. </param>	 
		public PubKey(byte[] bytes, bool @unsafe)
		{
			if(bytes == null)
				throw new ArgumentNullException("bytes");
			if(!Check(bytes, false))
			{
				throw new FormatException("Invalid public key");
			}
			if(@unsafe)
				_vch = bytes;
			else
			{
				_vch = bytes.ToArray();
				try
				{
					_ecKey = new EcKey(bytes, false);
				}
				catch(Exception ex)
				{
					throw new FormatException("Invalid public key", ex);
				}
			}
		}

		EcKey _ecKey;
		private EcKey EcKey
		{
			get { return _ecKey ?? (_ecKey = new EcKey(_vch, false)); }
		}

		public PubKey Compress()
		{
			if(IsCompressed)
				return this;
			return EcKey.GetPubKey(true);
		}
		public PubKey Decompress()
		{
			if(!IsCompressed)
				return this;
			return EcKey.GetPubKey(false);
		}

		/// <summary>
		/// Check on public key format.
		/// </summary>
		/// <param name="data">bytes array</param>
		/// <param name="deep">If false, will only check the first byte and length of the array. If true, will also check that the ECC coordinates are correct.</param>
		/// <returns>true if byte array is valid</returns>
		public static bool Check(byte[] data, bool deep)
		{
			var quick = data != null &&
					(
						(data.Length == 33 && (data[0] == 0x02 || data[0] == 0x03)) ||
						(data.Length == 65 && (data[0] == 0x04 || data[0] == 0x06 || data[0] == 0x07))
					);
			if(!deep || !quick)
				return quick;
			try
			{
				new EcKey(data, false);
				return true;
			}
			catch
			{
				return false;
			}
		}

		byte[] _vch;
		KeyId _id;

		[Obsolete("Use Hash instead")]
		public KeyId Id
		{
			get { return _id ?? (_id = new KeyId(Hashes.Hash160(_vch, _vch.Length))); }
		}

		public KeyId Hash
		{
			get { return _id ?? (_id = new KeyId(Hashes.Hash160(_vch, _vch.Length))); }
		}

		public bool IsCompressed
		{
			get
			{
				if(_vch.Length == 65)
					return false;
				if(_vch.Length == 33)
					return true;
				throw new NotSupportedException("Invalid public key size");
			}
		}

		public BitcoinAddress GetAddress(Network network)
		{
			return network.CreateBitcoinAddress(Hash);
		}

		public BitcoinScriptAddress GetScriptAddress(Network network)
		{
			var redeem = PayToPubkeyTemplate.Instance.GenerateScriptPubKey(this);
			return new BitcoinScriptAddress(redeem.Hash, network);
		}


		public bool Verify(Uint256 hash, EcdsaSignature sig)
		{
			return EcKey.Verify(hash, sig);
		}
		public bool Verify(Uint256 hash, byte[] sig)
		{
			return Verify(hash, EcdsaSignature.FromDer(sig));
		}

		[Obsolete("Use ScriptPubKey instead")]
		public Script PaymentScript
		{
			get
			{
				return ScriptPubKey;
			}
		}

		public string ToHex()
		{
			return Encoders.Hex.EncodeData(_vch);
		}

		#region IBitcoinSerializable Members

		public void ReadWrite(BitcoinStream stream)
		{
			stream.ReadWrite(ref _vch);
			if(!stream.Serializing)
				_ecKey = new EcKey(_vch, false);
		}

		#endregion

		public byte[] ToBytes()
		{
			return _vch.ToArray();
		}
		public byte[] ToBytes(bool @unsafe)
		{
			if(@unsafe)
				return _vch;
			else
				return _vch.ToArray();
		}
		public override string ToString()
		{
			return ToHex();
		}


		public bool VerifyMessage(string message, string signature)
		{
			var key = RecoverFromMessage(message, signature);
			return key.Hash == Hash;
		}

		//Thanks bitcoinj source code
		//http://bitcoinj.googlecode.com/git-history/keychain/core/src/main/java/com/google/bitcoin/core/Utils.java
		public static PubKey RecoverFromMessage(string messageText, string signatureText)
		{
			var signatureEncoded = Convert.FromBase64String(signatureText);
			var message = Utils.FormatMessageForSigning(messageText);
			var hash = Hashes.Hash256(message);
			return RecoverCompact(hash, signatureEncoded);
		}

		public static PubKey RecoverCompact(Uint256 hash, byte[] signatureEncoded)
		{
			if(signatureEncoded.Length < 65)
				throw new ArgumentException("Signature truncated, expected 65 bytes and got " + signatureEncoded.Length);


			int header = signatureEncoded[0];

			// The header byte: 0x1B = first key with even y, 0x1C = first key with odd y,
			//                  0x1D = second key with even y, 0x1E = second key with odd y

			if(header < 27 || header > 34)
				throw new ArgumentException("Header byte out of range: " + header);

			var r = new BigInteger(1, signatureEncoded.Skip(1).Take(32).ToArray());
			var s = new BigInteger(1, signatureEncoded.Skip(33).Take(32).ToArray());
			var sig = new EcdsaSignature(r, s);
			var compressed = false;

			if(header >= 31)
			{
				compressed = true;
				header -= 4;
			}
			var recId = header - 27;

			var key = EcKey.RecoverFromSignature(recId, sig, hash, compressed);
			return key.GetPubKey(compressed);
		}

	    public byte[] Encrypt(string data)
	    {
	        return new byte[] {};
	    }


		public PubKey Derivate(byte[] cc, uint nChild, out byte[] ccChild)
		{
			byte[] lr;
			var l = new byte[32];
			var r = new byte[32];
			if((nChild >> 31) == 0)
			{
				var pubKey = ToBytes();
				lr = Hashes.Bip32Hash(cc, nChild, pubKey[0], pubKey.Skip(1).ToArray());
			}
			else
			{
				throw new InvalidOperationException("A public key can't derivate an hardened child");
			}
			Array.Copy(lr, l, 32);
			Array.Copy(lr, 32, r, 0, 32);
			ccChild = r;


			var n = EcKey.Curve.N;
			var parse256Ll = new BigInteger(1, l);

			if(parse256Ll.CompareTo(n) >= 0)
				throw new InvalidOperationException("You won a prize ! this should happen very rarely. Take a screenshot, and roll the dice again.");

			var q = EcKey.Curve.G.Multiply(parse256Ll).Add(EcKey.GetPublicKeyParameters().Q);
			if(q.IsInfinity)
				throw new InvalidOperationException("You won the big prize ! this would happen only 1 in 2^127. Take a screenshot, and roll the dice again.");

			var p = new FpPoint(EcKey.Curve.Curve, q.X, q.Y, true);
			return new PubKey(p.GetEncoded());
		}

		public override bool Equals(object obj)
		{
			var item = obj as PubKey;
			return item != null && ToHex().Equals(item.ToHex());
		}
		public static bool operator ==(PubKey a, PubKey b)
		{
			if(ReferenceEquals(a, b))
				return true;
			if(((object)a == null) || ((object)b == null))
				return false;
			return a.ToHex() == b.ToHex();
		}

		public static bool operator !=(PubKey a, PubKey b)
		{
			return !(a == b);
		}

		public override int GetHashCode()
		{
			return ToHex().GetHashCode();
		}

		public PubKey UncoverSender(Key ephem, PubKey scan)
		{
			return Uncover(ephem, scan);
		}
		public PubKey UncoverReceiver(Key scan, PubKey ephem)
		{
			return Uncover(scan, ephem);
		}
		public PubKey Uncover(Key priv, PubKey pub)
		{
			var curve = EcKey.CreateCurve();
			var hash = GetStealthSharedSecret(priv, pub);
			//Q' = Q + cG
			var qprim = curve.G.Multiply(new BigInteger(1, hash)).Add(curve.Curve.DecodePoint(ToBytes()));
			return new PubKey(qprim.GetEncoded()).Compress(IsCompressed);
		}

		internal static byte[] GetStealthSharedSecret(Key priv, PubKey pub)
		{
			var curve = EcKey.CreateCurve();
			var pubec = curve.Curve.DecodePoint(pub.ToBytes());
			var p = pubec.Multiply(new BigInteger(1, priv.ToBytes()));
			var pBytes = new PubKey(p.GetEncoded()).Compress().ToBytes();
			var hash = Hashes.SHA256(pBytes);
			return hash;
		}

		public PubKey Compress(bool compression)
		{
		    if(IsCompressed == compression)
				return this;
		    return compression ? Compress() : Decompress();
		}

	    public BitcoinStealthAddress CreateStealthAddress(PubKey scanKey, Network network)
		{
			return new BitcoinStealthAddress(scanKey, new[] { this }, 1, null, network);
		}

		public string ToString(Network network)
		{
			return new BitcoinAddress(Hash, network).ToString();
		}

		#region IDestination Members

		Script _scriptPubKey;
		public Script ScriptPubKey
		{
			get { return _scriptPubKey ?? (_scriptPubKey = PayToPubkeyTemplate.Instance.GenerateScriptPubKey(this)); }
		}

		#endregion

	}
}
