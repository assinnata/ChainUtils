using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChainUtils.MicroPayment
{
	public class MicroPaymentException : Exception
	{
		public MicroPaymentException(string message):base(message)
		{

		}
	}
}
