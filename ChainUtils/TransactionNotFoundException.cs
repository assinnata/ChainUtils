﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChainUtils
{
	public class TransactionNotFoundException : Exception
	{
		public TransactionNotFoundException()
		{
		}
		public TransactionNotFoundException(uint256 txId)
			: this(null, txId, null)
		{

		}
		public TransactionNotFoundException(string message, uint256 txId)
			: this(message, txId, null)
		{
		}
		public TransactionNotFoundException(string message, uint256 txId, Exception inner)
			: base(message, inner)
		{
			if(message == null)
				message = "Transaction " + txId + " not found";
			TxId = txId;
		}
		public uint256 TxId
		{
			get;
			set;
		}
	}
}