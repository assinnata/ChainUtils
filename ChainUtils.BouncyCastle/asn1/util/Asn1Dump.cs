using System;
using System.IO;
using System.Text;
using ChainUtils.BouncyCastle.Utilities;
using ChainUtils.BouncyCastle.Utilities.Encoders;

namespace ChainUtils.BouncyCastle.Asn1.Utilities
{
    public sealed class Asn1Dump
    {
        private static readonly string NewLine = Platform.NewLine;

        private Asn1Dump()
        {
        }

        private const string Tab = "    ";
        private const int SampleSize = 32;

        /**
         * dump a Der object as a formatted string with indentation
         *
         * @param obj the Asn1Object to be dumped out.
         */
        private static void AsString(
            string			indent,
            bool			verbose,
            Asn1Object		obj,
            StringBuilder	buf)
        {
            if (obj is Asn1Sequence)
            {
                var tab = indent + Tab;
                buf.Append(indent);
                if (obj is BerSequence)
                {
                    buf.Append("BER Sequence");
                }
                else if (obj is DerSequence)
                {
                    buf.Append("DER Sequence");
                }
                else
                {
                    buf.Append("Sequence");
                }

                buf.Append(NewLine);

                foreach (Asn1Encodable o in ((Asn1Sequence)obj))
                {
                    if (o == null || o is Asn1Null)
                    {
                        buf.Append(tab);
                        buf.Append("NULL");
                        buf.Append(NewLine);
                    }
                    else
                    {
                        AsString(tab, verbose, o.ToAsn1Object(), buf);
                    }
                }
            }
            else if (obj is DerTaggedObject)
            {
                var tab = indent + Tab;
                buf.Append(indent);
                if (obj is BerTaggedObject)
                {
                    buf.Append("BER Tagged [");
                }
                else
                {
                    buf.Append("Tagged [");
                }

                var o = (DerTaggedObject)obj;

                buf.Append(((int)o.TagNo).ToString());
                buf.Append(']');

                if (!o.IsExplicit())
                {
                    buf.Append(" IMPLICIT ");
                }

                buf.Append(NewLine);

                if (o.IsEmpty())
                {
                    buf.Append(tab);
                    buf.Append("EMPTY");
                    buf.Append(NewLine);
                }
                else
                {
                    AsString(tab, verbose, o.GetObject(), buf);
                }
            }
            else if (obj is BerSet)
            {
                var tab = indent + Tab;

                buf.Append(indent);
                buf.Append("BER Set");
                buf.Append(NewLine);

                foreach (Asn1Encodable o in ((Asn1Set)obj))
                {
                    if (o == null)
                    {
                        buf.Append(tab);
                        buf.Append("NULL");
                        buf.Append(NewLine);
                    }
                    else
                    {
                        AsString(tab, verbose, o.ToAsn1Object(), buf);
                    }
                }
            }
            else if (obj is DerSet)
            {
                var tab = indent + Tab;

                buf.Append(indent);
                buf.Append("DER Set");
                buf.Append(NewLine);

                foreach (Asn1Encodable o in ((Asn1Set)obj))
                {
                    if (o == null)
                    {
                        buf.Append(tab);
                        buf.Append("NULL");
                        buf.Append(NewLine);
                    }
                    else
                    {
                        AsString(tab, verbose, o.ToAsn1Object(), buf);
                    }
                }
            }
            else if (obj is DerObjectIdentifier)
            {
                buf.Append(indent + "ObjectIdentifier(" + ((DerObjectIdentifier)obj).Id + ")" + NewLine);
            }
            else if (obj is DerBoolean)
            {
                buf.Append(indent + "Boolean(" + ((DerBoolean)obj).IsTrue + ")" + NewLine);
            }
            else if (obj is DerInteger)
            {
                buf.Append(indent + "Integer(" + ((DerInteger)obj).Value + ")" + NewLine);
            }
            else if (obj is BerOctetString)
            {
                var octets = ((Asn1OctetString)obj).GetOctets();
                var extra = verbose ? dumpBinaryDataAsString(indent, octets) : "";
                buf.Append(indent + "BER Octet String" + "[" + octets.Length + "] " + extra + NewLine);
            }
            else if (obj is DerOctetString)
            {
                var octets = ((Asn1OctetString)obj).GetOctets();
                var extra = verbose ? dumpBinaryDataAsString(indent, octets) : "";
                buf.Append(indent + "DER Octet String" + "[" + octets.Length + "] " + extra + NewLine);
            }
            else if (obj is DerBitString)
            {
                var bt = (DerBitString)obj; 
                var bytes = bt.GetBytes();
                var extra = verbose ? dumpBinaryDataAsString(indent, bytes) : "";
                buf.Append(indent + "DER Bit String" + "[" + bytes.Length + ", " + bt.PadBits + "] " + extra + NewLine);
            }
            else if (obj is DerIA5String)
            {
                buf.Append(indent + "IA5String(" + ((DerIA5String)obj).GetString() + ") " + NewLine);
            }
            else if (obj is DerUtf8String)
            {
                buf.Append(indent + "UTF8String(" + ((DerUtf8String)obj).GetString() + ") " + NewLine);
            }
            else if (obj is DerPrintableString)
            {
                buf.Append(indent + "PrintableString(" + ((DerPrintableString)obj).GetString() + ") " + NewLine);
            }
            else if (obj is DerVisibleString)
            {
                buf.Append(indent + "VisibleString(" + ((DerVisibleString)obj).GetString() + ") " + NewLine);
            }
            else if (obj is DerBmpString)
            {
                buf.Append(indent + "BMPString(" + ((DerBmpString)obj).GetString() + ") " + NewLine);
            }
            else if (obj is DerT61String)
            {
                buf.Append(indent + "T61String(" + ((DerT61String)obj).GetString() + ") " + NewLine);
            }
            else if (obj is DerUtcTime)
            {
                buf.Append(indent + "UTCTime(" + ((DerUtcTime)obj).TimeString + ") " + NewLine);
            }
            else if (obj is DerGeneralizedTime)
            {
                buf.Append(indent + "GeneralizedTime(" + ((DerGeneralizedTime)obj).GetTime() + ") " + NewLine);
            }
            else if (obj is BerApplicationSpecific)
            {
                buf.Append(outputApplicationSpecific("BER", indent, verbose, (BerApplicationSpecific)obj));
            }
            else if (obj is DerApplicationSpecific)
            {
                buf.Append(outputApplicationSpecific("DER", indent, verbose, (DerApplicationSpecific)obj));
            }
            else if (obj is DerEnumerated)
            {
                var en = (DerEnumerated)obj;
                buf.Append(indent + "DER Enumerated(" + en.Value + ")" + NewLine);
            }
            else if (obj is DerExternal)
            {
                var ext = (DerExternal)obj;
                buf.Append(indent + "External " + NewLine);
                var tab = indent + Tab;

                if (ext.DirectReference != null)
                {
                    buf.Append(tab + "Direct Reference: " + ext.DirectReference.Id + NewLine);
                }
                if (ext.IndirectReference != null)
                {
                    buf.Append(tab + "Indirect Reference: " + ext.IndirectReference.ToString() + NewLine);
                }
                if (ext.DataValueDescriptor != null)
                {
                    AsString(tab, verbose, ext.DataValueDescriptor, buf);
                }
                buf.Append(tab + "Encoding: " + ext.Encoding + NewLine);
                AsString(tab, verbose, ext.ExternalContent, buf);
            }
            else
            {
                buf.Append(indent + obj.ToString() + NewLine);
            }
        }

        private static string outputApplicationSpecific(
            string					type,
            string					indent,
            bool					verbose,
            DerApplicationSpecific	app)
        {
            var buf = new StringBuilder();

            if (app.IsConstructed())
            {
                try
                {
                    var s = Asn1Sequence.GetInstance(app.GetObject(Asn1Tags.Sequence));
                    buf.Append(indent + type + " ApplicationSpecific[" + app.ApplicationTag + "]" + NewLine);
                    foreach (Asn1Encodable ae in s)
                    {
                        AsString(indent + Tab, verbose, ae.ToAsn1Object(), buf);
                    }
                }
                catch (IOException e)
                {
                    buf.Append(e);
                }
                return buf.ToString();
            }

            return indent + type + " ApplicationSpecific[" + app.ApplicationTag + "] ("
                + Hex.ToHexString(app.GetContents()) + ")" + NewLine;
        }

        [Obsolete("Use version accepting Asn1Encodable")]
        public static string DumpAsString(
            object   obj)
        {
            if (obj is Asn1Encodable)
            {
                var buf = new StringBuilder();
                AsString("", false, ((Asn1Encodable)obj).ToAsn1Object(), buf);
                return buf.ToString();
            }

            return "unknown object type " + obj.ToString();
        }

        /**
         * dump out a DER object as a formatted string, in non-verbose mode
         *
         * @param obj the Asn1Encodable to be dumped out.
         * @return  the resulting string.
         */
        public static string DumpAsString(
            Asn1Encodable obj)
        {
            return DumpAsString(obj, false);
        }

        /**
         * Dump out the object as a string
         *
         * @param obj the Asn1Encodable to be dumped out.
         * @param verbose  if true, dump out the contents of octet and bit strings.
         * @return  the resulting string.
         */
        public static string DumpAsString(
            Asn1Encodable	obj,
            bool			verbose)
        {
            var buf = new StringBuilder();
            AsString("", verbose, obj.ToAsn1Object(), buf);
            return buf.ToString();
        }

        private static string dumpBinaryDataAsString(string indent, byte[] bytes)
        {
            indent += Tab;

            var buf = new StringBuilder(NewLine);

            for (var i = 0; i < bytes.Length; i += SampleSize)
            {
                if (bytes.Length - i > SampleSize)
                {
                    buf.Append(indent);
                    buf.Append(Hex.ToHexString(bytes, i, SampleSize));
                    buf.Append(Tab);
                    buf.Append(calculateAscString(bytes, i, SampleSize));
                    buf.Append(NewLine);
                }
                else
                {
                    buf.Append(indent);
                    buf.Append(Hex.ToHexString(bytes, i, bytes.Length - i));
                    for (var j = bytes.Length - i; j != SampleSize; j++)
                    {
                        buf.Append("  ");
                    }
                    buf.Append(Tab);
                    buf.Append(calculateAscString(bytes, i, bytes.Length - i));
                    buf.Append(NewLine);
                }
            }

            return buf.ToString();
        }

        private static string calculateAscString(
            byte[]	bytes,
            int		off,
            int		len)
        {
            var buf = new StringBuilder();

            for (var i = off; i != off + len; i++)
            {
                var c = (char)bytes[i]; 
                if (c >= ' ' && c <= '~')
                {
                    buf.Append(c);
                }
            }

            return buf.ToString();
        }
    }
}
