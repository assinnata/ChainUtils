namespace ChainUtils.MicroPayment
{
	public class OpenChannelMessage
	{
		public Transaction UnsignedRefund
		{
			get;
			set;
		}
	}
	public class OpenChannelAckMessage
	{
		public Transaction SignedRefund
		{
			get;
			set;
		}
	}
	public class OpenedChannelMessage : PayMessage
	{
		public Uint256 FundId
		{
			get;
			set;
		}
	}
	public class PayMessage
	{
		public int Sequence
		{
			get;
			set;
		}
		public Money Amount
		{
			get;
			set;
		}
		public Transaction Payment
		{
			get;
			set;
		}
	}
}
