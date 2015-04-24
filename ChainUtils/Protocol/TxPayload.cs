using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
