using System;
using System.Linq;
using ChainUtils.BouncyCastle.Math;
using ChainUtils.BouncyCastle.Math.EC;
using ChainUtils.Crypto;
using ChainUtils.DataEncoders;

namespace ChainUtils
{
	public class BitcoinConfirmationCode : Base58Data
	{

		public BitcoinConfirmationCode(string wif, Network expectedNetwork = null)
			: base(wif, expectedNetwork)
		{
		}
		public BitcoinConfirmationCode(byte[] rawBytes, Network network)
			: base(rawBytes, network)
		{
		}

		byte[] _addressHash;
		public byte[] AddressHash
		{
			get { return _addressHash ?? (_addressHash = VchData.Skip(1).Take(4).ToArray()); }
		}
		public bool IsCompressed
		{
			get
			{
				return (VchData[0] & 0x20) != 0;
			}
		}
		byte[] _ownerEntropy;
		public byte[] OwnerEntropy
		{
			get { return _ownerEntropy ?? (_ownerEntropy = VchData.Skip(1).Skip(4).Take(8).ToArray()); }
		}
		LotSequence _lotSequence;
		public LotSequence LotSequence
		{
			get
			{
				var hasLotSequence = (VchData[0] & 0x04) != 0;
				if(!hasLotSequence)
					return null;
				if(_lotSequence == null)
				{
					_lotSequence = new LotSequence(OwnerEntropy.Skip(4).Take(4).ToArray());
				}
				return _lotSequence;
			}
		}

		byte[] _encryptedPointB;
		byte[] EncryptedPointB
		{
			get { return _encryptedPointB ?? (_encryptedPointB = VchData.Skip(1).Skip(4).Skip(8).ToArray()); }
		}

		public override Base58Type Type
		{
			get
			{
				return Base58Type.ConfirmationCode;
			}
		}

		protected override bool IsValid
		{
			get
			{
				return VchData.Length == 1 + 4 + 8 + 33;
			}
		}


		public bool Check(string passphrase, BitcoinAddress expectedAddress)
		{
			//Derive passfactor using scrypt with ownerentropy and the user's passphrase and use it to recompute passpoint 
			var passfactor = BitcoinEncryptedSecretEc.CalculatePassFactor(passphrase, LotSequence, OwnerEntropy);
			//Derive decryption key for pointb using scrypt with passpoint, addresshash, and ownerentropy
			var passpoint = BitcoinEncryptedSecretEc.CalculatePassPoint(passfactor);
			var derived = BitcoinEncryptedSecretEc.CalculateDecryptionKey(passpoint, AddressHash, OwnerEntropy);

			//Decrypt encryptedpointb to yield pointb
			var pointbprefix = EncryptedPointB[0];
			pointbprefix = (byte)(pointbprefix ^ (byte)(derived[63] & (byte)0x01));

			//Optional since ArithmeticException will catch it, but it saves some times
			if(pointbprefix != 0x02 && pointbprefix != 0x03)
				return false;
			var pointb = BitcoinEncryptedSecret.DecryptKey(EncryptedPointB.Skip(1).ToArray(), derived);
			pointb = new[] { pointbprefix }.Concat(pointb).ToArray();

			var param1 = Encoders.Hex.EncodeData(EncryptedPointB.Skip(1).ToArray());
			var param2 = Encoders.Hex.EncodeData(derived);

			//4.ECMultiply pointb by passfactor. Use the resulting EC point as a public key
			var curve = EcKey.CreateCurve();
			ECPoint pointbec;
			try
			{
				pointbec = curve.Curve.DecodePoint(pointb);
			}
			catch(ArgumentException)
			{
				return false;
			}
			catch(ArithmeticException)
			{
				return false;
			}
			var pubkey = new PubKey(pointbec.Multiply(new BigInteger(1, passfactor)).GetEncoded());

			//and hash it into address using either compressed or uncompressed public key methodology as specifid in flagbyte.
			pubkey = IsCompressed ? pubkey.Compress() : pubkey.Decompress();

			var actualhash = BitcoinEncryptedSecretEc.HashAddress(pubkey.GetAddress(Network));
			var expectedhash = BitcoinEncryptedSecretEc.HashAddress(expectedAddress);

			return Utils.ArrayEqual(actualhash, expectedhash);
		}
	}
}