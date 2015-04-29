using ChainUtils.BouncyCastle.Math;

namespace ChainUtils.BouncyCastle.Asn1.Pkcs
{
    public class DHParameter
        : Asn1Encodable
    {
        internal DerInteger p, g, l;

		public DHParameter(
            BigInteger	p,
            BigInteger	g,
            int			l)
        {
            this.p = new DerInteger(p);
            this.g = new DerInteger(g);

			if (l != 0)
            {
                this.l = new DerInteger(l);
            }
        }

		public DHParameter(
            Asn1Sequence seq)
        {
            var e = seq.GetEnumerator();

			e.MoveNext();
            p = (DerInteger)e.Current;

			e.MoveNext();
            g = (DerInteger)e.Current;

			if (e.MoveNext())
            {
                l = (DerInteger) e.Current;
            }
        }

		public BigInteger P
		{
			get { return p.PositiveValue; }
		}

		public BigInteger G
		{
			get { return g.PositiveValue; }
		}

		public BigInteger L
		{
            get { return l == null ? null : l.PositiveValue; }
        }

		public override Asn1Object ToAsn1Object()
        {
            var v = new Asn1EncodableVector(p, g);

			if (l != null)
            {
                v.Add(l);
            }

			return new DerSequence(v);
        }
    }
}
