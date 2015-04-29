using ChainUtils.BouncyCastle.Crypto.Parameters;
using ChainUtils.BouncyCastle.Math;
using ChainUtils.BouncyCastle.Math.EC;
using ChainUtils.BouncyCastle.Math.EC.Multiplier;
using ChainUtils.BouncyCastle.Security;

namespace ChainUtils.BouncyCastle.Crypto.Signers
{
    /**
     * EC-DSA as described in X9.62
     */
    public class ECDsaSigner
        : IDsa
    {
        protected readonly IDsaKCalculator kCalculator;

        protected ECKeyParameters key = null;
        protected SecureRandom random = null;

        /**
         * Default configuration, random K values.
         */
        public ECDsaSigner()
        {
            kCalculator = new RandomDsaKCalculator();
        }

        /**
         * Configuration with an alternate, possibly deterministic calculator of K.
         *
         * @param kCalculator a K value calculator.
         */
        public ECDsaSigner(IDsaKCalculator kCalculator)
        {
            this.kCalculator = kCalculator;
        }

        public virtual string AlgorithmName
        {
            get { return "ECDSA"; }
        }

        public virtual void Init(bool forSigning, ICipherParameters parameters)
        {
            SecureRandom providedRandom = null;

            if (forSigning)
            {
                if (parameters is ParametersWithRandom)
                {
                    var rParam = (ParametersWithRandom)parameters;

                    providedRandom = rParam.Random;
                    parameters = rParam.Parameters;
                }

                if (!(parameters is ECPrivateKeyParameters))
                    throw new InvalidKeyException("EC private key required for signing");

                key = (ECPrivateKeyParameters)parameters;
            }
            else
            {
                if (!(parameters is ECPublicKeyParameters))
                    throw new InvalidKeyException("EC public key required for verification");

                key = (ECPublicKeyParameters)parameters;
            }

            random = InitSecureRandom(forSigning && !kCalculator.IsDeterministic, providedRandom);
        }

        // 5.3 pg 28
        /**
         * Generate a signature for the given message using the key we were
         * initialised with. For conventional DSA the message should be a SHA-1
         * hash of the message of interest.
         *
         * @param message the message that will be verified later.
         */
        public virtual BigInteger[] GenerateSignature(byte[] message)
        {
            var ec = key.Parameters;
            var n = ec.N;
            var e = CalculateE(n, message);
            var d = ((ECPrivateKeyParameters)key).D;

            if (kCalculator.IsDeterministic)
            {
                kCalculator.Init(n, d, message);
            }
            else
            {
                kCalculator.Init(n, random);
            }

            BigInteger r, s;

            var basePointMultiplier = CreateBasePointMultiplier();

            // 5.3.2
            do // Generate s
            {
                BigInteger k;
                do // Generate r
                {
                    k = kCalculator.NextK();

                    var p = basePointMultiplier.Multiply(ec.G, k).Normalize();

                    // 5.3.3
                    r = p.AffineXCoord.ToBigInteger().Mod(n);
                }
                while (r.SignValue == 0);

                s = k.ModInverse(n).Multiply(e.Add(d.Multiply(r))).Mod(n);
            }
            while (s.SignValue == 0);

            return new BigInteger[]{ r, s };
        }

        // 5.4 pg 29
        /**
         * return true if the value r and s represent a DSA signature for
         * the passed in message (for standard DSA the message should be
         * a SHA-1 hash of the real message to be verified).
         */
        public virtual bool VerifySignature(byte[] message, BigInteger r, BigInteger s)
        {
            var n = key.Parameters.N;

            // r and s should both in the range [1,n-1]
            if (r.SignValue < 1 || s.SignValue < 1
                || r.CompareTo(n) >= 0 || s.CompareTo(n) >= 0)
            {
                return false;
            }

            var e = CalculateE(n, message);
            var c = s.ModInverse(n);

            var u1 = e.Multiply(c).Mod(n);
            var u2 = r.Multiply(c).Mod(n);

            var G = key.Parameters.G;
            var Q = ((ECPublicKeyParameters) key).Q;

            var point = ECAlgorithms.SumOfTwoMultiplies(G, u1, Q, u2).Normalize();

            if (point.IsInfinity)
                return false;

            var v = point.AffineXCoord.ToBigInteger().Mod(n);

            return v.Equals(r);
        }

        protected virtual BigInteger CalculateE(BigInteger n, byte[] message)
        {
            var messageBitLength = message.Length * 8;
            var trunc = new BigInteger(1, message);

            if (n.BitLength < messageBitLength)
            {
                trunc = trunc.ShiftRight(messageBitLength - n.BitLength);
            }

            return trunc;
        }

        protected virtual ECMultiplier CreateBasePointMultiplier()
        {
            return new FixedPointCombMultiplier();
        }

        protected virtual SecureRandom InitSecureRandom(bool needed, SecureRandom provided)
        {
            return !needed ? null : (provided != null) ? provided : new SecureRandom();
        }
    }
}
