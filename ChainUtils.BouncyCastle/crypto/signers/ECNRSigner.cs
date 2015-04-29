using System;
using ChainUtils.BouncyCastle.Crypto.Generators;
using ChainUtils.BouncyCastle.Crypto.Parameters;
using ChainUtils.BouncyCastle.Math;
using ChainUtils.BouncyCastle.Math.EC;
using ChainUtils.BouncyCastle.Security;

namespace ChainUtils.BouncyCastle.Crypto.Signers
{
    /**
     * EC-NR as described in IEEE 1363-2000
     */
    public class ECNRSigner
        : IDsa
    {
        private bool			forSigning;
        private ECKeyParameters	key;
        private SecureRandom	random;

        public string AlgorithmName
        {
            get { return "ECNR"; }
        }

        public void Init(
            bool				forSigning,
            ICipherParameters	parameters)
        {
            this.forSigning = forSigning;

            if (forSigning)
            {
                if (parameters is ParametersWithRandom)
                {
                    var rParam = (ParametersWithRandom) parameters;

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

                key = (ECPublicKeyParameters) parameters;
            }
        }

        // Section 7.2.5 ECSP-NR, pg 34
        /**
         * generate a signature for the given message using the key we were
         * initialised with.  Generally, the order of the curve should be at
         * least as long as the hash of the message of interest, and with
         * ECNR it *must* be at least as long.
         *
         * @param digest  the digest to be signed.
         * @exception DataLengthException if the digest is longer than the key allows
         */
        public BigInteger[] GenerateSignature(
            byte[] message)
        {
            if (!forSigning)
            {
                // not properly initilaized... deal with it
                throw new InvalidOperationException("not initialised for signing");
            }

            var n = ((ECPrivateKeyParameters) key).Parameters.N;
            var nBitLength = n.BitLength;

            var e = new BigInteger(1, message);
            var eBitLength = e.BitLength;

            var  privKey = (ECPrivateKeyParameters)key;

            if (eBitLength > nBitLength)
            {
                throw new DataLengthException("input too large for ECNR key.");
            }

            BigInteger r = null;
            BigInteger s = null;

            AsymmetricCipherKeyPair tempPair;
            do // generate r
            {
                // generate another, but very temporary, key pair using
                // the same EC parameters
                var keyGen = new ECKeyPairGenerator();

                keyGen.Init(new ECKeyGenerationParameters(privKey.Parameters, random));

                tempPair = keyGen.GenerateKeyPair();

                //    BigInteger Vx = tempPair.getPublic().getW().getAffineX();
                var V = (ECPublicKeyParameters) tempPair.Public; // get temp's public key
                var Vx = V.Q.AffineXCoord.ToBigInteger(); // get the point's x coordinate

                r = Vx.Add(e).Mod(n);
            }
            while (r.SignValue == 0);

            // generate s
            var x = privKey.D;                // private key value
            var u = ((ECPrivateKeyParameters) tempPair.Private).D; // temp's private key value
            s = u.Subtract(r.Multiply(x)).Mod(n);

            return new BigInteger[]{ r, s };
        }

        // Section 7.2.6 ECVP-NR, pg 35
        /**
         * return true if the value r and s represent a signature for the
         * message passed in. Generally, the order of the curve should be at
         * least as long as the hash of the message of interest, and with
         * ECNR, it *must* be at least as long.  But just in case the signer
         * applied mod(n) to the longer digest, this implementation will
         * apply mod(n) during verification.
         *
         * @param digest  the digest to be verified.
         * @param r       the r value of the signature.
         * @param s       the s value of the signature.
         * @exception DataLengthException if the digest is longer than the key allows
         */
        public bool VerifySignature(
            byte[]		message,
            BigInteger	r,
            BigInteger	s)
        {
            if (forSigning)
            {
                // not properly initilaized... deal with it
                throw new InvalidOperationException("not initialised for verifying");
            }

            var pubKey = (ECPublicKeyParameters)key;
            var n = pubKey.Parameters.N;
            var nBitLength = n.BitLength;

            var e = new BigInteger(1, message);
            var eBitLength = e.BitLength;

            if (eBitLength > nBitLength)
            {
                throw new DataLengthException("input too large for ECNR key.");
            }

            // r in the range [1,n-1]
            if (r.CompareTo(BigInteger.One) < 0 || r.CompareTo(n) >= 0)
            {
                return false;
            }

            // s in the range [0,n-1]           NB: ECNR spec says 0
            if (s.CompareTo(BigInteger.Zero) < 0 || s.CompareTo(n) >= 0)
            {
                return false;
            }

            // compute P = sG + rW

            var G = pubKey.Parameters.G;
            var W = pubKey.Q;
            // calculate P using Bouncy math
            var P = ECAlgorithms.SumOfTwoMultiplies(G, s, W, r).Normalize();

            if (P.IsInfinity)
                return false;

            var x = P.AffineXCoord.ToBigInteger();
            var t = r.Subtract(x).Mod(n);

            return t.Equals(e);
        }
    }
}
