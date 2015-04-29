namespace ChainUtils.Protocol
{
	[Payload("block")]
	public class BlockPayload : BitcoinSerializablePayload<Block>
	{
		public BlockPayload()
		{

		}
		public BlockPayload(Block block)
			: base(block)
		{

		}
	}
}
