using ChainUtils.BouncyCastle.Crypto.Parameters;
using ChainUtils.BouncyCastle.Math;
using ChainUtils.BouncyCastle.Math.EC;
using ChainUtils.BouncyCastle.Math.EC.Multiplier;
using ChainUtils.BouncyCastle.Security;

namespace ChainUtils.BouncyCastle.Crypto.Signers
{
    /**
     * GOST R 34.10-2001 Signature Algorithm
     */
    public class ECGost3410Signer
        : IDsa
    {
        private ECKeyParameters key;
        private SecureRandom random;

        public string AlgorithmName
        {
            get { return "ECGOST3410"; }
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

                if (!(parameters is ECPrivateKeyParameters))
                    throw new InvalidKeyException("EC private key required for signing");

                key = (ECPrivateKeyParameters) parameters;
            }
            else
            {
                if (!(parameters is ECPublicKeyParameters))
                    throw new InvalidKeyException("EC public key required for verification");

                key = (ECPublicKeyParameters)parameters;
            }
        }

        /**
         * generate a signature for the given message using the key we were
         * initialised with. For conventional GOST3410 the message should be a GOST3411
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

            var e = new BigInteger(1, mRev);

            var ec = key.Parameters;
            var n = ec.N;
            var d = ((ECPrivateKeyParameters)key).D;

            BigInteger r, s = null;

            var basePointMultiplier = CreateBasePointMultiplier();

            do // generate s
            {
                BigInteger k;
                do // generate r
                {
                    do
                    {
                        k = new BigInteger(n.BitLength, random);
                    }
                    while (k.SignValue == 0);

                    var p = basePointMultiplier.Multiply(ec.G, k).Normalize();

                    r = p.AffineXCoord.ToBigInteger().Mod(n);
                }
                while (r.SignValue == 0);

                s = (k.Multiply(e)).Add(d.Multiply(r)).Mod(n);
            }
            while (s.SignValue == 0);

            return new BigInteger[]{ r, s };
        }

        /**
         * return true if the value r and s represent a GOST3410 signature for
         * the passed in message (for standard GOST3410 the message should be
         * a GOST3411 hash of the real message to be verified).
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

            var e = new BigInteger(1, mRev);
            var n = key.Parameters.N;

            // r in the range [1,n-1]
            if (r.CompareTo(BigInteger.One) < 0 || r.CompareTo(n) >= 0)
            {
                return false;
            }

            // s in the range [1,n-1]
            if (s.CompareTo(BigInteger.One) < 0 || s.CompareTo(n) >= 0)
            {
                return false;
            }

            var v = e.ModInverse(n);

            var z1 = s.Multiply(v).Mod(n);
            var z2 = (n.Subtract(r)).Multiply(v).Mod(n);

            var G = key.Parameters.G; // P
            var Q = ((ECPublicKeyParameters)key).Q;

            var point = ECAlgorithms.SumOfTwoMultiplies(G, z1, Q, z2).Normalize();

            if (point.IsInfinity)
                return false;

            var R = point.AffineXCoord.ToBigInteger().Mod(n);

            return R.Equals(r);
        }

        protected virtual ECMultiplier CreateBasePointMultiplier()
        {
            return new FixedPointCombMultiplier();
        }
    }
}
