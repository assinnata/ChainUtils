using System;

namespace ChainUtils.DataEncoders
{
	public class Base64Encoder : DataEncoder
	{
		public override byte[] DecodeData(string encoded)
		{
			return Convert.FromBase64String(encoded);
		}
		public override string EncodeData(byte[] data, int length)
		{
			return Convert.ToBase64String(data, 0, length);
		}
	}
}
