namespace ChainUtils.Protocol
{
	[Payload("pong")]
	public class PongPayload : Payload
	{
		private ulong _nonce;
		public ulong Nonce
		{
			get
			{
				return _nonce;
			}
			set
			{
				_nonce = value;
			}
		}

		public override void ReadWriteCore(BitcoinStream stream)
		{
			stream.ReadWrite(ref _nonce);
		}
	}
}
