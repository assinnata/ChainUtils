namespace ChainUtils.Protocol
{
	public class UnknowPayload : Payload
	{
		private byte[] _data = new byte[0];
		public byte[] Data
		{
			get
			{
				return _data;
			}
			set
			{
				_data = value;
			}
		}
		public override void ReadWriteCore(BitcoinStream stream)
		{
			stream.ReadWrite(ref _data);
		}
	}
}
