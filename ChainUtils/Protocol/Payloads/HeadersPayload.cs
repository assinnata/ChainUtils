using System.Collections.Generic;

namespace ChainUtils.Protocol
{
	[Payload("headers")]
	public class HeadersPayload : Payload
	{
		List<BlockHeader> _headers = new List<BlockHeader>();

		public List<BlockHeader> Headers
		{
			get
			{
				return _headers;
			}
		}

		public override void ReadWriteCore(BitcoinStream stream)
		{
			stream.ReadWrite(ref _headers);
		}
	}
}
