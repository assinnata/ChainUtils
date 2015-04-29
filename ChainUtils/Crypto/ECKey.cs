using System;
using System.IO;
using System.Text;
using ChainUtils.BouncyCastle.Asn1;
using ChainUtils.BouncyCastle.Asn1.Sec;
using ChainUtils.BouncyCastle.Asn1.X9;
using ChainUtils.BouncyCastle.Crypto.Parameters;
using ChainUtils.BouncyCastle.Crypto.Signers;
using ChainUtils.BouncyCastle.Math;
using ChainUtils.BouncyCastle.Math.EC;
using ChainUtils.DataEncoders;

namespace ChainUtils.Crypto
{
	public class EcKey
	{
		public ECPrivateKeyParameters PrivateKey
		{
			get
			{
				return _key as ECPrivateKeyParameters;
			}
		}
		ECKeyParameters _key;


		public static BigInteger HalfCurveOrder = null;
		public static ECDomainParameters Curve = null;
		static EcKey()
		{
			var @params = CreateCurve();
			Curve = new ECDomainParameters(@params.Curve, @params.G, @params.N, @params.H);
			HalfCurveOrder = @params.N.ShiftRight(1);
		}

		public EcKey(byte[] vch, bool isPrivate)
		{
			if(isPrivate)
				_key = new ECPrivateKeyParameters(new BigInteger(1, vch), DomainParameter);
			else
			{
				var q = Secp256K1.Curve.DecodePoint(vch);
				_key = new ECPublicKeyParameters("EC", q, DomainParameter);
			}
		}


		X9ECParameters _secp256K1;
		public X9ECParameters Secp256K1
		{
			get
			{
				if(_secp256K1 == null)
					_secp256K1 = CreateCurve();
				return _secp256K1;
			}
		}

		public static X9ECParameters CreateCurve()
		{
			return SecNamedCurves.GetByName("secp256k1");
		}
		ECDomainParameters _domainParameter;
		public ECDomainParameters DomainParameter
		{
			get
			{
				if(_domainParameter == null)
					_domainParameter = new ECDomainParameters(Secp256K1.Curve, Secp256K1.G, Secp256K1.N, Secp256K1.H);
				return _domainParameter;
			}
		}


		public EcdsaSignature Sign(Uint256 hash)
		{
			AssertPrivateKey();
			var signer = new DeterministicEcdsa();
			signer.setPrivateKey(PrivateKey);
			var sig = EcdsaSignature.FromDer(signer.SignHash(hash.ToBytes()));
			return sig.MakeCanonical();
		}

		private void AssertPrivateKey()
		{
			if(PrivateKey == null)
				throw new InvalidOperationException("This key should be a private key for such operation");
		}



		internal bool Verify(Uint256 hash, EcdsaSignature sig)
		{
			var signer = new ECDsaSigner();
			signer.Init(false, GetPublicKeyParameters());
			return signer.VerifySignature(hash.ToBytes(), sig.R, sig.S);
		}


		public PubKey GetPubKey(bool isCompressed)
		{
			var q = GetPublicKeyParameters().Q;
			//Pub key (q) is composed into X and Y, the compressed form only include X, which can derive Y along with 02 or 03 prepent depending on whether Y in even or odd.
			var result = Secp256K1.Curve.CreatePoint(q.X.ToBigInteger(), q.Y.ToBigInteger(), isCompressed).GetEncoded();
			return new PubKey(result);
		}



		public ECPublicKeyParameters GetPublicKeyParameters()
		{
			if(_key is ECPublicKeyParameters)
				return (ECPublicKeyParameters)_key;
			else
			{
				var q = Secp256K1.G.Multiply(PrivateKey.D);
				return new ECPublicKeyParameters("EC", q, DomainParameter);
			}
		}


		public static EcKey RecoverFromSignature(int recId, EcdsaSignature sig, Uint256 message, bool compressed)
		{
			if(recId < 0)
				throw new ArgumentException("recId should be positive");
			if(sig.R.SignValue < 0)
				throw new ArgumentException("r should be positive");
			if(sig.S.SignValue < 0)
				throw new ArgumentException("s should be positive");
			if(message == null)
				throw new ArgumentNullException("message");


			var curve = CreateCurve();

			// 1.0 For j from 0 to h   (h == recId here and the loop is outside this function)
			//   1.1 Let x = r + jn

			var n = curve.N;
			var i = BigInteger.ValueOf((long)recId / 2);
			var x = sig.R.Add(i.Multiply(n));

			//   1.2. Convert the integer x to an octet string X of length mlen using the conversion routine
			//        specified in Section 2.3.7, where mlen = ⌈(log2 p)/8⌉ or mlen = ⌈m/8⌉.
			//   1.3. Convert the octet string (16 set binary digits)||X to an elliptic curve point R using the
			//        conversion routine specified in Section 2.3.4. If this conversion routine outputs “invalid”, then
			//        do another iteration of Step 1.
			//
			// More concisely, what these points mean is to use X as a compressed public key.
			var prime = ((FpCurve)curve.Curve).Q;
			if(x.CompareTo(prime) >= 0)
			{
				return null;
			}

			// Compressed keys require you to know an extra bit of data about the y-coord as there are two possibilities.
			// So it's encoded in the recId.
			var r = DecompressKey(x, (recId & 1) == 1);
			//   1.4. If nR != point at infinity, then do another iteration of Step 1 (callers responsibility).

			if(!r.Multiply(n).IsInfinity)
				return null;

			//   1.5. Compute e from M using Steps 2 and 3 of ECDSA signature verification.
			var e = new BigInteger(1, message.ToBytes());
			//   1.6. For k from 1 to 2 do the following.   (loop is outside this function via iterating recId)
			//   1.6.1. Compute a candidate public key as:
			//               Q = mi(r) * (sR - eG)
			//
			// Where mi(x) is the modular multiplicative inverse. We transform this into the following:
			//               Q = (mi(r) * s ** R) + (mi(r) * -e ** G)
			// Where -e is the modular additive inverse of e, that is z such that z + e = 0 (mod n). In the above equation
			// ** is point multiplication and + is point addition (the EC group operator).
			//
			// We can find the additive inverse by subtracting e from zero then taking the mod. For example the additive
			// inverse of 3 modulo 11 is 8 because 3 + 8 mod 11 = 0, and -3 mod 11 = 8.

			var eInv = BigInteger.Zero.Subtract(e).Mod(n);
			var rInv = sig.R.ModInverse(n);
			var srInv = rInv.Multiply(sig.S).Mod(n);
			var eInvrInv = rInv.Multiply(eInv).Mod(n);
			var q = (FpPoint)ECAlgorithms.SumOfTwoMultiplies(curve.G, eInvrInv, r, srInv);
			if(compressed)
			{
				q = new FpPoint(curve.Curve, q.X, q.Y, true);
			}
			return new EcKey(q.GetEncoded(), false);
		}

		private static ECPoint DecompressKey(BigInteger xBn, bool yBit)
		{
			var curve = CreateCurve().Curve;
			var compEnc = X9IntegerConverter.IntegerToBytes(xBn, 1 + X9IntegerConverter.GetByteLength(curve));
			compEnc[0] = (byte)(yBit ? 0x03 : 0x02);
			return curve.DecodePoint(compEnc);
		}



		public static EcKey FromDer(byte[] der)
		{

			// To understand this code, see the definition of the ASN.1 format for EC private keys in the OpenSSL source
			// code in ec_asn1.c:
			//
			// ASN1_SEQUENCE(EC_PRIVATEKEY) = {
			//   ASN1_SIMPLE(EC_PRIVATEKEY, version, LONG),
			//   ASN1_SIMPLE(EC_PRIVATEKEY, privateKey, ASN1_OCTET_STRING),
			//   ASN1_EXP_OPT(EC_PRIVATEKEY, parameters, ECPKPARAMETERS, 0),
			//   ASN1_EXP_OPT(EC_PRIVATEKEY, publicKey, ASN1_BIT_STRING, 1)
			// } ASN1_SEQUENCE_END(EC_PRIVATEKEY)
			//

			var decoder = new Asn1InputStream(der);
			var seq = (DerSequence)decoder.ReadObject();
			CheckArgument(seq.Count == 4, "Input does not appear to be an ASN.1 OpenSSL EC private key");
			CheckArgument(((DerInteger)seq[0]).Value.Equals(BigInteger.One),
					"Input is of wrong version");
			var bits = ((DerOctetString)seq[1]).GetOctets();
#if !PORTABLE
			decoder.Close();
#else
			decoder.Dispose();
#endif
			return new EcKey(bits, true);
		}

		public static string DumpDer(byte[] der)
		{
			var builder = new StringBuilder();
			var decoder = new Asn1InputStream(der);
			var seq = (DerSequence)decoder.ReadObject();
			builder.AppendLine("Version : " + Encoders.Hex.EncodeData(seq[0].GetDerEncoded()));
			builder.AppendLine("Private : " + Encoders.Hex.EncodeData(seq[1].GetDerEncoded()));
			builder.AppendLine("Params : " + Encoders.Hex.EncodeData(((DerTaggedObject)seq[2]).GetObject().GetDerEncoded()));
			builder.AppendLine("Public : " + Encoders.Hex.EncodeData(seq[3].GetDerEncoded()));
#if !PORTABLE
			decoder.Close();
#else
			decoder.Dispose();
#endif
			return builder.ToString();
		}

		static void CheckArgument(bool predicate, string msg)
		{
			if(!predicate)
			{
				throw new FormatException(msg);
			}
		}

		public byte[] ToDer(bool compressed)
		{
			AssertPrivateKey();
			var baos = new MemoryStream();

			// ASN1_SEQUENCE(EC_PRIVATEKEY) = {
			//   ASN1_SIMPLE(EC_PRIVATEKEY, version, LONG),
			//   ASN1_SIMPLE(EC_PRIVATEKEY, privateKey, ASN1_OCTET_STRING),
			//   ASN1_EXP_OPT(EC_PRIVATEKEY, parameters, ECPKPARAMETERS, 0),
			//   ASN1_EXP_OPT(EC_PRIVATEKEY, publicKey, ASN1_BIT_STRING, 1)
			// } ASN1_SEQUENCE_END(EC_PRIVATEKEY)
			var seq = new DerSequenceGenerator(baos);
			seq.AddObject(new DerInteger(1)); // version
			seq.AddObject(new DerOctetString(PrivateKey.D.ToByteArrayUnsigned()));


			//Did not managed to generate the same der as brainwallet by using this
			//seq.AddObject(new DerTaggedObject(0, Secp256k1.ToAsn1Object()));
			Asn1Object secp256K1Der = null;
			if(compressed)
			{
				secp256K1Der = Asn1Object.FromByteArray(Encoders.Hex.DecodeData("308182020101302c06072a8648ce3d0101022100fffffffffffffffffffffffffffffffffffffffffffffffffffffffefffffc2f300604010004010704210279be667ef9dcbbac55a06295ce870b07029bfcdb2dce28d959f2815b16f81798022100fffffffffffffffffffffffffffffffebaaedce6af48a03bbfd25e8cd0364141020101"));
			}
			else
			{
				secp256K1Der = Asn1Object.FromByteArray(Encoders.Hex.DecodeData("3081a2020101302c06072a8648ce3d0101022100fffffffffffffffffffffffffffffffffffffffffffffffffffffffefffffc2f300604010004010704410479be667ef9dcbbac55a06295ce870b07029bfcdb2dce28d959f2815b16f81798483ada7726a3c4655da4fbfc0e1108a8fd17b448a68554199c47d08ffb10d4b8022100fffffffffffffffffffffffffffffffebaaedce6af48a03bbfd25e8cd0364141020101"));
			}
			seq.AddObject(new DerTaggedObject(0, secp256K1Der));
			seq.AddObject(new DerTaggedObject(1, new DerBitString(GetPubKey(compressed).ToBytes())));
			seq.Close();
			return baos.ToArray();
		}


	}
}
