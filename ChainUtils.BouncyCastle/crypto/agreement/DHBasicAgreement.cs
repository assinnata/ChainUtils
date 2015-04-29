using System;
using ChainUtils.BouncyCastle.Crypto.Parameters;
using ChainUtils.BouncyCastle.Math;

namespace ChainUtils.BouncyCastle.Crypto.Agreement
{
    /**
     * a Diffie-Hellman key agreement class.
     * <p>
     * note: This is only the basic algorithm, it doesn't take advantage of
     * long term public keys if they are available. See the DHAgreement class
     * for a "better" implementation.</p>
     */
    public class DHBasicAgreement
        : IBasicAgreement
    {
        private DHPrivateKeyParameters	key;
        private DHParameters			dhParams;

        public virtual void Init(
            ICipherParameters parameters)
        {
            if (parameters is ParametersWithRandom)
            {
                parameters = ((ParametersWithRandom) parameters).Parameters;
            }

            if (!(parameters is DHPrivateKeyParameters))
            {
                throw new ArgumentException("DHEngine expects DHPrivateKeyParameters");
            }

            key = (DHPrivateKeyParameters) parameters;
            dhParams = key.Parameters;
        }

        public virtual int GetFieldSize()
        {
            return (key.Parameters.P.BitLength + 7) / 8;
        }

        /**
         * given a short term public key from a given party calculate the next
         * message in the agreement sequence.
         */
        public virtual BigInteger CalculateAgreement(
            ICipherParameters pubKey)
        {
            if (key == null)
                throw new InvalidOperationException("Agreement algorithm not initialised");

            var pub = (DHPublicKeyParameters)pubKey;

            if (!pub.Parameters.Equals(dhParams))
            {
                throw new ArgumentException("Diffie-Hellman public key has wrong parameters.");
            }

            return pub.Y.ModPow(key.X, dhParams.P);
        }
    }
}
