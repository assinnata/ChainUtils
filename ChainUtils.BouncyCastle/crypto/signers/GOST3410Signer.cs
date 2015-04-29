using ChainUtils.BouncyCastle.Crypto.Parameters;
using ChainUtils.BouncyCastle.Math;
using ChainUtils.BouncyCastle.Security;

namespace ChainUtils.BouncyCastle.Crypto.Signers
{
	/**
	 * Gost R 34.10-94 Signature Algorithm
	 */
	public class Gost3410Signer
		: IDsa
	{
		private Gost3410KeyParameters key;
		private SecureRandom random;

		public string AlgorithmName
		{
			get { return "GOST3410"; }
		}

		public void Init(
			bool				forSigning,
			ICipherParameters	parameters)
		{
			if (forSigning)
			{
				if (parameters is ParametersWithRandom)
				{
					var rParam = (ParametersWithRandom)parameters;

					random = rParam.Random;
					parameters = rParam.Parameters;
				}
				else
				{
					random = new SecureRandom();
				}

				if (!(parameters is Gost3410PrivateKeyParameters))
					throw new InvalidKeyException("GOST3410 private key required for signing");

				key = (Gost3410PrivateKeyParameters) parameters;
			}
			else
			{
				if (!(parameters is Gost3410PublicKeyParameters))
					throw new InvalidKeyException("GOST3410 public key required for signing");

				key = (Gost3410PublicKeyParameters) parameters;
			}
		}

		/**
		 * generate a signature for the given message using the key we were
		 * initialised with. For conventional Gost3410 the message should be a Gost3411
		 * hash of the message of interest.
		 *
		 * @param message the message that will be verified later.
		 */
		public BigInteger[] GenerateSignature(
			byte[] message)
		{
			var mRev = new byte[message.Length]; // conversion is little-endian
			for (var i = 0; i != mRev.Length; i++)
			{
				mRev[i] = message[mRev.Length - 1 - i];
			}

			var m = new BigInteger(1, mRev);
			var parameters = key.Parameters;
			BigInteger k;

			do
			{
				k = new BigInteger(parameters.Q.BitLength, random);
			}
			while (k.CompareTo(parameters.Q) >= 0);

			var r = parameters.A.ModPow(k, parameters.P).Mod(parameters.Q);

			var s = k.Multiply(m).
				Add(((Gost3410PrivateKeyParameters)key).X.Multiply(r)).
				Mod(parameters.Q);

			return new BigInteger[]{ r, s };
		}

		/**
		 * return true if the value r and s represent a Gost3410 signature for
		 * the passed in message for standard Gost3410 the message should be a
		 * Gost3411 hash of the real message to be verified.
		 */
		public bool VerifySignature(
			byte[]		message,
			BigInteger	r,
			BigInteger	s)
		{
			var mRev = new byte[message.Length]; // conversion is little-endian
			for (var i = 0; i != mRev.Length; i++)
			{
				mRev[i] = message[mRev.Length - 1 - i];
			}

			var m = new BigInteger(1, mRev);
			var parameters = key.Parameters;

			if (r.SignValue < 0 || parameters.Q.CompareTo(r) <= 0)
			{
				return false;
			}

			if (s.SignValue < 0 || parameters.Q.CompareTo(s) <= 0)
			{
				return false;
			}

			var v = m.ModPow(parameters.Q.Subtract(BigInteger.Two), parameters.Q);

			var z1 = s.Multiply(v).Mod(parameters.Q);
			var z2 = (parameters.Q.Subtract(r)).Multiply(v).Mod(parameters.Q);

			z1 = parameters.A.ModPow(z1, parameters.P);
			z2 = ((Gost3410PublicKeyParameters)key).Y.ModPow(z2, parameters.P);

			var u = z1.Multiply(z2).Mod(parameters.P).Mod(parameters.Q);

			return u.Equals(r);
		}
	}
}
