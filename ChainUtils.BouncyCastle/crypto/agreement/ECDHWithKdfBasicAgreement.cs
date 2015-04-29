using System;
using ChainUtils.BouncyCastle.Asn1;
using ChainUtils.BouncyCastle.Asn1.X9;
using ChainUtils.BouncyCastle.Crypto.Agreement.Kdf;
using ChainUtils.BouncyCastle.Math;
using ChainUtils.BouncyCastle.Security;

namespace ChainUtils.BouncyCastle.Crypto.Agreement
{
	public class ECDHWithKdfBasicAgreement
		: ECDHBasicAgreement
	{
		private readonly string algorithm;
		private readonly IDerivationFunction kdf;

		public ECDHWithKdfBasicAgreement(
			string				algorithm,
			IDerivationFunction	kdf)
		{
			if (algorithm == null)
				throw new ArgumentNullException("algorithm");
			if (kdf == null)
				throw new ArgumentNullException("kdf");

			this.algorithm = algorithm;
			this.kdf = kdf;
		}

		public override BigInteger CalculateAgreement(
			ICipherParameters pubKey)
		{
			// Note that the ec.KeyAgreement class in JCE only uses kdf in one
			// of the engineGenerateSecret methods.

			var result = base.CalculateAgreement(pubKey);

			var keySize = GeneratorUtilities.GetDefaultKeySize(algorithm);

			var dhKdfParams = new DHKdfParameters(
				new DerObjectIdentifier(algorithm),
				keySize,
				BigIntToBytes(result));

			kdf.Init(dhKdfParams);

			var keyBytes = new byte[keySize / 8];
			kdf.GenerateBytes(keyBytes, 0, keyBytes.Length);

			return new BigInteger(1, keyBytes);
		}

		private byte[] BigIntToBytes(BigInteger r)
		{
			var byteLength = X9IntegerConverter.GetByteLength(privKey.Parameters.Curve);
			return X9IntegerConverter.IntegerToBytes(r, byteLength);
		}
	}
}
