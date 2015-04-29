namespace ChainUtils.Protocol
{
	[Payload("ping")]
	public class PingPayload : Payload
	{
		
		public PingPayload()
		{
			_nonce = RandomUtils.GetUInt64();
		}
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

		public PongPayload CreatePong()
		{
			return new PongPayload()
			{
				Nonce = Nonce
			};
		}
	}
}
