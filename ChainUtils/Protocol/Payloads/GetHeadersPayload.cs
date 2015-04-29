namespace ChainUtils.Protocol
{
	[Payload("getheaders")]
	public class GetHeadersPayload : Payload
	{
		uint _version = (uint)ProtocolVersion.PROTOCOL_VERSION;
		public ProtocolVersion Version
		{
			get
			{
				return (ProtocolVersion)_version;
			}
			set
			{
				_version = (uint)value;
			}
		}

		BlockLocator _blockLocators;

		public BlockLocator BlockLocators
		{
			get
			{
				return _blockLocators;
			}
			set
			{
				_blockLocators = value;
			}
		}

		Uint256 _hashStop = new Uint256(0);
		public Uint256 HashStop
		{
			get
			{
				return _hashStop;
			}
			set
			{
				_hashStop = value;
			}
		}

		public override void ReadWriteCore(BitcoinStream stream)
		{
			stream.ReadWrite(ref _version);
			stream.ReadWrite(ref _blockLocators);
			stream.ReadWrite(ref _hashStop);
		}
	}
}
