namespace ChainUtils.Protocol
{
	[Payload("tx")]
	public class TxPayload : BitcoinSerializablePayload<Transaction> 
	{
		public TxPayload()
		{

		}
		public TxPayload(Transaction transaction): base(transaction)
		{
			
		}
	}
}
