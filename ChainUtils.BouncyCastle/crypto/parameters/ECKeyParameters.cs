using System;
using ChainUtils.BouncyCastle.Asn1;
using ChainUtils.BouncyCastle.Asn1.CryptoPro;
using ChainUtils.BouncyCastle.Crypto.Generators;
using ChainUtils.BouncyCastle.Security;
using ChainUtils.BouncyCastle.Utilities;

namespace ChainUtils.BouncyCastle.Crypto.Parameters
{
    public abstract class ECKeyParameters
        : AsymmetricKeyParameter
    {
        private static readonly string[] algorithms = { "EC", "ECDSA", "ECDH", "ECDHC", "ECGOST3410", "ECMQV" };

        private readonly string algorithm;
        private readonly ECDomainParameters parameters;
        private readonly DerObjectIdentifier publicKeyParamSet;

        protected ECKeyParameters(
            string				algorithm,
            bool				isPrivate,
            ECDomainParameters	parameters)
            : base(isPrivate)
        {
            if (algorithm == null)
                throw new ArgumentNullException("algorithm");
            if (parameters == null)
                throw new ArgumentNullException("parameters");

            this.algorithm = VerifyAlgorithmName(algorithm);
            this.parameters = parameters;
        }

        protected ECKeyParameters(
            string				algorithm,
            bool				isPrivate,
            DerObjectIdentifier	publicKeyParamSet)
            : base(isPrivate)
        {
            if (algorithm == null)
                throw new ArgumentNullException("algorithm");
            if (publicKeyParamSet == null)
                throw new ArgumentNullException("publicKeyParamSet");

            this.algorithm = VerifyAlgorithmName(algorithm);
            parameters = LookupParameters(publicKeyParamSet);
            this.publicKeyParamSet = publicKeyParamSet;
        }

        public string AlgorithmName
        {
            get { return algorithm; }
        }

        public ECDomainParameters Parameters
        {
            get { return parameters; }
        }

        public DerObjectIdentifier PublicKeyParamSet
        {
            get { return publicKeyParamSet; }
        }

        public override bool Equals(
            object obj)
        {
            if (obj == this)
                return true;

            var other = obj as ECDomainParameters;

            if (other == null)
                return false;

            return Equals(other);
        }

        protected bool Equals(
            ECKeyParameters other)
        {
            return parameters.Equals(other.parameters) && base.Equals(other);
        }

        public override int GetHashCode()
        {
            return parameters.GetHashCode() ^ base.GetHashCode();
        }

        internal ECKeyGenerationParameters CreateKeyGenerationParameters(
            SecureRandom random)
        {
            if (publicKeyParamSet != null)
            {
                return new ECKeyGenerationParameters(publicKeyParamSet, random);
            }

            return new ECKeyGenerationParameters(parameters, random);
        }

        internal static string VerifyAlgorithmName(string algorithm)
        {
            var upper = Platform.ToUpperInvariant(algorithm);
            if (Array.IndexOf(algorithms, algorithm, 0, algorithms.Length) < 0)
                throw new ArgumentException("unrecognised algorithm: " + algorithm, "algorithm");
            return upper;
        }

        internal static ECDomainParameters LookupParameters(
            DerObjectIdentifier publicKeyParamSet)
        {
            if (publicKeyParamSet == null)
                throw new ArgumentNullException("publicKeyParamSet");

            var p = ECGost3410NamedCurves.GetByOid(publicKeyParamSet);

            if (p == null)
            {
                var x9 = ECKeyPairGenerator.FindECCurveByOid(publicKeyParamSet);

                if (x9 == null)
                {
                    throw new ArgumentException("OID is not a valid public key parameter set", "publicKeyParamSet");
                }

                p = new ECDomainParameters(x9.Curve, x9.G, x9.N, x9.H, x9.GetSeed());
            }

            return p;
        }
    }
}
