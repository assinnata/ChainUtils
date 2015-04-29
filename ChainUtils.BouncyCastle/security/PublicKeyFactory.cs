using System;
using System.IO;
using ChainUtils.BouncyCastle.Asn1;
using ChainUtils.BouncyCastle.Asn1.CryptoPro;
using ChainUtils.BouncyCastle.Asn1.Oiw;
using ChainUtils.BouncyCastle.Asn1.Pkcs;
using ChainUtils.BouncyCastle.Asn1.X509;
using ChainUtils.BouncyCastle.Asn1.X9;
using ChainUtils.BouncyCastle.Crypto;
using ChainUtils.BouncyCastle.Crypto.Generators;
using ChainUtils.BouncyCastle.Crypto.Parameters;
using ChainUtils.BouncyCastle.Math;

namespace ChainUtils.BouncyCastle.Security
{
    public sealed class PublicKeyFactory
    {
        private PublicKeyFactory()
        {
        }

        public static AsymmetricKeyParameter CreateKey(
            byte[] keyInfoData)
        {
            return CreateKey(
                SubjectPublicKeyInfo.GetInstance(
                    Asn1Object.FromByteArray(keyInfoData)));
        }

        public static AsymmetricKeyParameter CreateKey(
            Stream inStr)
        {
            return CreateKey(
                SubjectPublicKeyInfo.GetInstance(
                    Asn1Object.FromStream(inStr)));
        }

        public static AsymmetricKeyParameter CreateKey(
            SubjectPublicKeyInfo keyInfo)
        {
            var algID = keyInfo.AlgorithmID;
            var algOid = algID.ObjectID;

            // TODO See RSAUtil.isRsaOid in Java build
            if (algOid.Equals(PkcsObjectIdentifiers.RsaEncryption)
                || algOid.Equals(X509ObjectIdentifiers.IdEARsa)
                || algOid.Equals(PkcsObjectIdentifiers.IdRsassaPss)
                || algOid.Equals(PkcsObjectIdentifiers.IdRsaesOaep))
            {
                var pubKey = RsaPublicKeyStructure.GetInstance(
                    keyInfo.GetPublicKey());

                return new RsaKeyParameters(false, pubKey.Modulus, pubKey.PublicExponent);
            }
            else if (algOid.Equals(X9ObjectIdentifiers.DHPublicNumber))
            {
                var seq = Asn1Sequence.GetInstance(algID.Parameters.ToAsn1Object());

                var dhPublicKey = DHPublicKey.GetInstance(keyInfo.GetPublicKey());

                var y = dhPublicKey.Y.Value;

                if (IsPkcsDHParam(seq))
                    return ReadPkcsDHParam(algOid, y, seq);

                var dhParams = DHDomainParameters.GetInstance(seq);

                var p = dhParams.P.Value;
                var g = dhParams.G.Value;
                var q = dhParams.Q.Value;

                BigInteger j = null;
                if (dhParams.J != null)
                {
                    j = dhParams.J.Value;
                }

                DHValidationParameters validation = null;
                var dhValidationParms = dhParams.ValidationParms;
                if (dhValidationParms != null)
                {
                    var seed = dhValidationParms.Seed.GetBytes();
                    var pgenCounter = dhValidationParms.PgenCounter.Value;

                    // TODO Check pgenCounter size?

                    validation = new DHValidationParameters(seed, pgenCounter.IntValue);
                }

                return new DHPublicKeyParameters(y, new DHParameters(p, g, q, j, validation));
            }
            else if (algOid.Equals(PkcsObjectIdentifiers.DhKeyAgreement))
            {
                var seq = Asn1Sequence.GetInstance(algID.Parameters.ToAsn1Object());

                var derY = (DerInteger) keyInfo.GetPublicKey();

                return ReadPkcsDHParam(algOid, derY.Value, seq);
            }
            else if (algOid.Equals(OiwObjectIdentifiers.ElGamalAlgorithm))
            {
                var para = new ElGamalParameter(
                    Asn1Sequence.GetInstance(algID.Parameters.ToAsn1Object()));
                var derY = (DerInteger) keyInfo.GetPublicKey();

                return new ElGamalPublicKeyParameters(
                    derY.Value,
                    new ElGamalParameters(para.P, para.G));
            }
            else if (algOid.Equals(X9ObjectIdentifiers.IdDsa)
                || algOid.Equals(OiwObjectIdentifiers.DsaWithSha1))
            {
                var derY = (DerInteger) keyInfo.GetPublicKey();
                var ae = algID.Parameters;

                DsaParameters parameters = null;
                if (ae != null)
                {
                    var para = DsaParameter.GetInstance(ae.ToAsn1Object());
                    parameters = new DsaParameters(para.P, para.Q, para.G);
                }

                return new DsaPublicKeyParameters(derY.Value, parameters);
            }
            else if (algOid.Equals(X9ObjectIdentifiers.IdECPublicKey))
            {
                var para = new X962Parameters(algID.Parameters.ToAsn1Object());

                X9ECParameters x9;
                if (para.IsNamedCurve)
                {
                    x9 = ECKeyPairGenerator.FindECCurveByOid((DerObjectIdentifier)para.Parameters);
                }
                else
                {
                    x9 = new X9ECParameters((Asn1Sequence)para.Parameters);
                }

                Asn1OctetString key = new DerOctetString(keyInfo.PublicKeyData.GetBytes());
                var derQ = new X9ECPoint(x9.Curve, key);
                var q = derQ.Point;

                if (para.IsNamedCurve)
                {
                    return new ECPublicKeyParameters("EC", q, (DerObjectIdentifier)para.Parameters);
                }

                var dParams = new ECDomainParameters(x9.Curve, x9.G, x9.N, x9.H, x9.GetSeed());
                return new ECPublicKeyParameters(q, dParams);
            }
            else if (algOid.Equals(CryptoProObjectIdentifiers.GostR3410x2001))
            {
                var gostParams = new Gost3410PublicKeyAlgParameters(
                    (Asn1Sequence) algID.Parameters);

                Asn1OctetString key;
                try
                {
                    key = (Asn1OctetString) keyInfo.GetPublicKey();
                }
                catch (IOException)
                {
                    throw new ArgumentException("invalid info structure in GOST3410 public key");
                }

                var keyEnc = key.GetOctets();
                var x = new byte[32];
                var y = new byte[32];

                for (var i = 0; i != y.Length; i++)
                {
                    x[i] = keyEnc[32 - 1 - i];
                }

                for (var i = 0; i != x.Length; i++)
                {
                    y[i] = keyEnc[64 - 1 - i];
                }

                var ecP = ECGost3410NamedCurves.GetByOid(gostParams.PublicKeyParamSet);

                if (ecP == null)
                    return null;

                var q = ecP.Curve.CreatePoint(new BigInteger(1, x), new BigInteger(1, y));

                return new ECPublicKeyParameters("ECGOST3410", q, gostParams.PublicKeyParamSet);
            }
            else if (algOid.Equals(CryptoProObjectIdentifiers.GostR3410x94))
            {
                var algParams = new Gost3410PublicKeyAlgParameters(
                    (Asn1Sequence) algID.Parameters);

                DerOctetString derY;
                try
                {
                    derY = (DerOctetString) keyInfo.GetPublicKey();
                }
                catch (IOException)
                {
                    throw new ArgumentException("invalid info structure in GOST3410 public key");
                }

                var keyEnc = derY.GetOctets();
                var keyBytes = new byte[keyEnc.Length];

                for (var i = 0; i != keyEnc.Length; i++)
                {
                    keyBytes[i] = keyEnc[keyEnc.Length - 1 - i]; // was little endian
                }

                var y = new BigInteger(1, keyBytes);

                return new Gost3410PublicKeyParameters(y, algParams.PublicKeyParamSet);
            }
            else
            {
                throw new SecurityUtilityException("algorithm identifier in key not recognised: " + algOid);
            }
        }

        private static bool IsPkcsDHParam(Asn1Sequence seq)
        {
            if (seq.Count == 2)
                return true;

            if (seq.Count > 3)
                return false;

            var l = DerInteger.GetInstance(seq[2]);
            var p = DerInteger.GetInstance(seq[0]);

            return l.Value.CompareTo(BigInteger.ValueOf(p.Value.BitLength)) <= 0;
        }

        private static DHPublicKeyParameters ReadPkcsDHParam(DerObjectIdentifier algOid,
            BigInteger y, Asn1Sequence seq)
        {
            var para = new DHParameter(seq);

            var lVal = para.L;
            var l = lVal == null ? 0 : lVal.IntValue;
            var dhParams = new DHParameters(para.P, para.G, null, l);

            return new DHPublicKeyParameters(y, dhParams, algOid);
        }
    }
}
