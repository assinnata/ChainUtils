using System;
using System.IO;
using ChainUtils.BouncyCastle.Asn1.X509;

namespace ChainUtils.BouncyCastle.Asn1.Pkcs
{
    public class PrivateKeyInfo
        : Asn1Encodable
    {
        private readonly Asn1OctetString        privKey;
        private readonly AlgorithmIdentifier	algID;
        private readonly Asn1Set				attributes;

        public static PrivateKeyInfo GetInstance(Asn1TaggedObject obj, bool explicitly)
        {
            return GetInstance(Asn1Sequence.GetInstance(obj, explicitly));
        }

        public static PrivateKeyInfo GetInstance(
            object obj)
        {
            if (obj == null)
                return null;
            if (obj is PrivateKeyInfo)
                return (PrivateKeyInfo) obj;
            return new PrivateKeyInfo(Asn1Sequence.GetInstance(obj));
        }

        public PrivateKeyInfo(AlgorithmIdentifier algID, Asn1Object privateKey)
            : this(algID, privateKey, null)
        {
        }

        public PrivateKeyInfo(
            AlgorithmIdentifier	algID,
            Asn1Object			privateKey,
            Asn1Set				attributes)
        {
            this.algID = algID;
            privKey = new DerOctetString(privateKey.GetEncoded(Der));
            this.attributes = attributes;
        }

        private PrivateKeyInfo(Asn1Sequence seq)
        {
            var e = seq.GetEnumerator();

            e.MoveNext();
            var version = ((DerInteger)e.Current).Value;
            if (version.IntValue != 0)
            {
                throw new ArgumentException("wrong version for private key info: " + version.IntValue);
            }

            e.MoveNext();
            algID = AlgorithmIdentifier.GetInstance(e.Current);
            e.MoveNext();
            privKey = Asn1OctetString.GetInstance(e.Current);

            if (e.MoveNext())
            {
                attributes = Asn1Set.GetInstance((Asn1TaggedObject)e.Current, false);
            }
        }

        public virtual AlgorithmIdentifier PrivateKeyAlgorithm
        {
            get { return algID; }
        }

        [Obsolete("Use 'PrivateKeyAlgorithm' property instead")]
        public virtual AlgorithmIdentifier AlgorithmID
        {
            get { return algID; }
        }

        public virtual Asn1Object ParsePrivateKey()
        {
            return Asn1Object.FromByteArray(privKey.GetOctets());
        }

        [Obsolete("Use 'ParsePrivateKey' instead")]
        public virtual Asn1Object PrivateKey
        {
            get
            {
                try
                {
                    return ParsePrivateKey();
                }
                catch (IOException)
                {
                    throw new InvalidOperationException("unable to parse private key");
                }
            }
        }

        public virtual Asn1Set Attributes
        {
            get { return attributes; }
        }

        /**
         * write out an RSA private key with its associated information
         * as described in Pkcs8.
         * <pre>
         *      PrivateKeyInfo ::= Sequence {
         *                              version Version,
         *                              privateKeyAlgorithm AlgorithmIdentifier {{PrivateKeyAlgorithms}},
         *                              privateKey PrivateKey,
         *                              attributes [0] IMPLICIT Attributes OPTIONAL
         *                          }
         *      Version ::= Integer {v1(0)} (v1,...)
         *
         *      PrivateKey ::= OCTET STRING
         *
         *      Attributes ::= Set OF Attr
         * </pre>
         */
        public override Asn1Object ToAsn1Object()
        {
            var v = new Asn1EncodableVector(new DerInteger(0), algID, privKey);

            if (attributes != null)
            {
                v.Add(new DerTaggedObject(false, 0, attributes));
            }

            return new DerSequence(v);
        }
    }
}
