using System;

namespace ChainUtils
{
	public class TransactionNotFoundException : Exception
	{
		public TransactionNotFoundException()
		{
		}
		public TransactionNotFoundException(Uint256 txId)
			: this(null, txId, null)
		{

		}
		public TransactionNotFoundException(string message, Uint256 txId)
			: this(message, txId, null)
		{
		}
		public TransactionNotFoundException(string message, Uint256 txId, Exception inner)
			: base(message, inner)
		{
			if(message == null)
				message = "Transaction " + txId + " not found";
			TxId = txId;
		}
		public Uint256 TxId
		{
			get;
			set;
		}
	}
}
