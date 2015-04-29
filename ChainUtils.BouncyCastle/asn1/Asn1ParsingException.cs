using System;

namespace ChainUtils.BouncyCastle.Asn1
{
#if !(NETCF_1_0 || NETCF_2_0 || SILVERLIGHT)
    [Serializable]
#endif
    public class Asn1ParsingException
		: InvalidOperationException
	{
		public Asn1ParsingException()
		{
		}

		public Asn1ParsingException(
			string message)
			: base(message)
		{
		}

		public Asn1ParsingException(
			string		message,
			Exception	exception)
			: base(message, exception)
		{
		}
	}
}
