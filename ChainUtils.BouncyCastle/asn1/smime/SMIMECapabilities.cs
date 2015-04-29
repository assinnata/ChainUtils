using System;
using System.Collections;
using ChainUtils.BouncyCastle.Asn1.Pkcs;
using ChainUtils.BouncyCastle.Asn1.X509;
using ChainUtils.BouncyCastle.Utilities;

namespace ChainUtils.BouncyCastle.Asn1.Smime
{
    /**
     * Handler class for dealing with S/MIME Capabilities
     */
    public class SmimeCapabilities
        : Asn1Encodable
    {
        /**
         * general preferences
         */
        public static readonly DerObjectIdentifier PreferSignedData = PkcsObjectIdentifiers.PreferSignedData;
        public static readonly DerObjectIdentifier CannotDecryptAny = PkcsObjectIdentifiers.CannotDecryptAny;
        public static readonly DerObjectIdentifier SmimeCapabilitesVersions = PkcsObjectIdentifiers.SmimeCapabilitiesVersions;

		/**
         * encryption algorithms preferences
         */
        public static readonly DerObjectIdentifier DesCbc = new DerObjectIdentifier("1.3.14.3.2.7");
        public static readonly DerObjectIdentifier DesEde3Cbc = PkcsObjectIdentifiers.DesEde3Cbc;
        public static readonly DerObjectIdentifier RC2Cbc = PkcsObjectIdentifiers.RC2Cbc;

		private Asn1Sequence capabilities;

		/**
         * return an Attr object from the given object.
         *
         * @param o the object we want converted.
         * @exception ArgumentException if the object cannot be converted.
         */
        public static SmimeCapabilities GetInstance(
            object obj)
        {
            if (obj == null || obj is SmimeCapabilities)
            {
                return (SmimeCapabilities) obj;
            }

			if (obj is Asn1Sequence)
            {
                return new SmimeCapabilities((Asn1Sequence) obj);
            }

			if (obj is AttributeX509)
            {
                return new SmimeCapabilities(
                    (Asn1Sequence)(((AttributeX509) obj).AttrValues[0]));
            }

			throw new ArgumentException("unknown object in factory: " + obj.GetType().Name, "obj");
        }

		public SmimeCapabilities(
            Asn1Sequence seq)
        {
            capabilities = seq;
        }

#if !SILVERLIGHT
        [Obsolete("Use 'GetCapabilitiesForOid' instead")]
        public ArrayList GetCapabilities(
            DerObjectIdentifier capability)
        {
            ArrayList list = new ArrayList();
            DoGetCapabilitiesForOid(capability, list);
			return list;
        }
#endif

        /**
         * returns an ArrayList with 0 or more objects of all the capabilities
         * matching the passed in capability Oid. If the Oid passed is null the
         * entire set is returned.
         */
        public IList GetCapabilitiesForOid(
            DerObjectIdentifier capability)
        {
            var list = Platform.CreateArrayList();
            DoGetCapabilitiesForOid(capability, list);
			return list;
        }

        private void DoGetCapabilitiesForOid(DerObjectIdentifier capability, IList list)
        {
			if (capability == null)
            {
				foreach (var o in capabilities)
				{
                    var cap = SmimeCapability.GetInstance(o);

					list.Add(cap);
                }
            }
            else
            {
				foreach (var o in capabilities)
				{
                    var cap = SmimeCapability.GetInstance(o);

					if (capability.Equals(cap.CapabilityID))
                    {
                        list.Add(cap);
                    }
                }
            }
        }

        /**
         * Produce an object suitable for an Asn1OutputStream.
         * <pre>
         * SMIMECapabilities ::= Sequence OF SMIMECapability
         * </pre>
         */
        public override Asn1Object ToAsn1Object()
        {
            return capabilities;
        }
    }
}
