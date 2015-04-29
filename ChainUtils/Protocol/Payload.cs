namespace ChainUtils.Protocol
{
	public class Payload : IBitcoinSerializable
	{
		public string Command
		{
			get
			{
				return PayloadAttribute.GetCommandName(GetType());
			}
		}

		#region IBitcoinSerializable Members

		public void ReadWrite(BitcoinStream stream)
		{
			using(stream.NetworkFormatScope(true))
			{
				ReadWriteCore(stream);
			}
		}
		public virtual void ReadWriteCore(BitcoinStream stream)
		{

		}

		#endregion

		public override string ToString()
		{
			return GetType().Name;
		}
	}
}
