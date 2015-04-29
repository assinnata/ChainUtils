using ChainUtils.BouncyCastle.Asn1;
using ChainUtils.BouncyCastle.Asn1.X509;
using ChainUtils.BouncyCastle.Crypto.Generators;
using ChainUtils.BouncyCastle.Crypto.Parameters;
using ChainUtils.BouncyCastle.Crypto.Utilities;

namespace ChainUtils.BouncyCastle.Crypto.Agreement.Kdf
{
    /**
    * X9.63 based key derivation function for ECDH CMS.
    */
    public class ECDHKekGenerator
        : IDerivationFunction
    {
        private readonly IDerivationFunction kdf;

        private DerObjectIdentifier	algorithm;
        private int					keySize;
        private byte[]				z;

        public ECDHKekGenerator(IDigest digest)
        {
            kdf = new Kdf2BytesGenerator(digest);
        }

        public virtual void Init(IDerivationParameters param)
        {
            var parameters = (DHKdfParameters)param;

            algorithm = parameters.Algorithm;
            keySize = parameters.KeySize;
            z = parameters.GetZ(); // TODO Clone?
        }

        public virtual IDigest Digest
        {
            get { return kdf.Digest; }
        }

        public virtual int GenerateBytes(byte[]	outBytes, int outOff, int len)
        {
            // TODO Create an ASN.1 class for this (RFC3278)
            // ECC-CMS-SharedInfo
            var s = new DerSequence(
                new AlgorithmIdentifier(algorithm, DerNull.Instance),
                new DerTaggedObject(true, 2, new DerOctetString(Pack.UInt32_To_BE((uint)keySize))));

            kdf.Init(new KdfParameters(z, s.GetDerEncoded()));

            return kdf.GenerateBytes(outBytes, outOff, len);
        }
    }
}
