using System;
using System.IO;

namespace ChainUtils.BouncyCastle.Asn1
{
	// TODO Make Obsolete in favour of Asn1OutputStream?
    public class BerOutputStream
        : DerOutputStream
    {
        public BerOutputStream(Stream os) : base(os)
        {
        }

		[Obsolete("Use version taking an Asn1Encodable arg instead")]
        public override void WriteObject(
            object    obj)
        {
            if (obj == null)
            {
                WriteNull();
            }
            else
            {
                var o = obj as Asn1Object;
                if (o != null)
                {
                    o.Encode(this);
                }
                else
                {
                    var encodable = obj as Asn1Encodable;
                    if (encodable != null)
                    {
                        encodable.ToAsn1Object().Encode(this);
                    }
                    else
                    {
                        throw new IOException("object not BerEncodable");
                    }
                }
            }
        }
    }
}
