using System;
using System.IO;

namespace ChainUtils.BouncyCastle.Asn1
{
	public class DerOctetStringParser
		: Asn1OctetStringParser
	{
		private readonly DefiniteLengthInputStream _stream;

		internal DerOctetStringParser(
			DefiniteLengthInputStream stream)
		{
			_stream = stream;
		}

		public Stream GetOctetStream()
		{
			return _stream;
		}

		public Asn1Object ToAsn1Object()
		{
			try
			{
				return new DerOctetString(_stream.ToArray());
			}
			catch (IOException e)
			{
				throw new InvalidOperationException("IOException converting stream to byte array: " + e.Message, e);
			}
		}
	}
}
