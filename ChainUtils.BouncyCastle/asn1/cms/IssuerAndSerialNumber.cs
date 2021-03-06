using ChainUtils.BouncyCastle.Asn1.X509;
using ChainUtils.BouncyCastle.Math;

namespace ChainUtils.BouncyCastle.Asn1.Cms
{
    public class IssuerAndSerialNumber
        : Asn1Encodable
    {
        private X509Name	name;
        private DerInteger	serialNumber;

        public static IssuerAndSerialNumber GetInstance(object obj)
        {
            if (obj == null)
                return null;
            var existing = obj as IssuerAndSerialNumber;
            if (existing != null)
                return existing;
            return new IssuerAndSerialNumber(Asn1Sequence.GetInstance(obj));
        }

        public IssuerAndSerialNumber(
            Asn1Sequence seq)
        {
            name = X509Name.GetInstance(seq[0]);
            serialNumber = (DerInteger) seq[1];
        }

        public IssuerAndSerialNumber(
            X509Name	name,
            BigInteger	serialNumber)
        {
            this.name = name;
            this.serialNumber = new DerInteger(serialNumber);
        }

        public IssuerAndSerialNumber(
            X509Name	name,
            DerInteger	serialNumber)
        {
            this.name = name;
            this.serialNumber = serialNumber;
        }

        public X509Name Name
        {
            get { return name; }
        }

        public DerInteger SerialNumber
        {
            get { return serialNumber; }
        }

        public override Asn1Object ToAsn1Object()
        {
            return new DerSequence(name, serialNumber);
        }
    }
}
