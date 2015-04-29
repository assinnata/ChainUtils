using System.Collections;
using ChainUtils.BouncyCastle.Utilities;

namespace ChainUtils.BouncyCastle.Asn1.X509
{
    public class AttributeTable
    {
        private readonly IDictionary attributes;

        public AttributeTable(
            IDictionary attrs)
        {
            attributes = Platform.CreateHashtable(attrs);
        }

#if !SILVERLIGHT
        [Obsolete]
        public AttributeTable(
            Hashtable attrs)
        {
            this.attributes = Platform.CreateHashtable(attrs);
        }
#endif

		public AttributeTable(
            Asn1EncodableVector v)
        {
            attributes = Platform.CreateHashtable(v.Count);

			for (var i = 0; i != v.Count; i++)
            {
                var a = AttributeX509.GetInstance(v[i]);

				attributes.Add(a.AttrType, a);
            }
        }

		public AttributeTable(
            Asn1Set s)
        {
            attributes = Platform.CreateHashtable(s.Count);

			for (var i = 0; i != s.Count; i++)
            {
                var a = AttributeX509.GetInstance(s[i]);

				attributes.Add(a.AttrType, a);
            }
        }

		public AttributeX509 Get(
            DerObjectIdentifier oid)
        {
            return (AttributeX509) attributes[oid];
        }

#if !SILVERLIGHT
        [Obsolete("Use 'ToDictionary' instead")]
		public Hashtable ToHashtable()
        {
            return new Hashtable(attributes);
        }
#endif

        public IDictionary ToDictionary()
        {
            return Platform.CreateHashtable(attributes);
        }
    }
}
