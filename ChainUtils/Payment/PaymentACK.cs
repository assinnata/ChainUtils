#if !NOPROTOBUF
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace ChainUtils.Payment
{
	public class PaymentAck
	{
		public const int MaxLength = 60000;
		public static PaymentAck Load(byte[] data)
		{
			return Load(new MemoryStream(data));
		}

		public static PaymentAck Load(Stream source)
		{
			if(source.CanSeek && source.Length > MaxLength)
				throw new ArgumentException("PaymentACK messages larger than " + MaxLength + " bytes should be rejected", "source");
			var data = PaymentRequest.Serializer.Deserialize<Proto.PaymentAck>(source);
			return new PaymentAck(data);
		}
		public PaymentAck()
		{

		}
		public PaymentAck(PaymentMessage payment)
		{
			_payment = payment;
		}
		internal PaymentAck(Proto.PaymentAck data)
		{
			_payment = new PaymentMessage(data.Payment);
			Memo = data.MemoSpecified ? data.Memo : null;
			OriginalData = data;
		}

		private readonly PaymentMessage _payment = new PaymentMessage();
		public readonly static string MediaType = "application/bitcoin-paymentack";
		public PaymentMessage Payment
		{
			get
			{
				return _payment;
			}
		}

		public string Memo
		{
			get;
			set;
		}

		internal Proto.PaymentAck OriginalData
		{
			get;
			set;
		}

		public byte[] ToBytes()
		{
			var ms = new MemoryStream();
			WriteTo(ms);
			return ms.ToArray();
		}

		public void WriteTo(Stream output)
		{
			var data = OriginalData == null ? new Proto.PaymentAck() : (Proto.PaymentAck)PaymentRequest.Serializer.DeepClone(OriginalData);
			data.Memo = Memo;
			data.Payment = Payment.ToData();
			PaymentRequest.Serializer.Serialize(output, data);
		}
	}
	public class PaymentMessage
	{
		public const int MaxLength = 50000;
		public static PaymentMessage Load(byte[] data)
		{
			return Load(new MemoryStream(data));
		}

		public PaymentAck CreateAck(string memo = null)
		{
			return new PaymentAck(this)
			{
				Memo = memo
			};
		}

		public static PaymentMessage Load(Stream source)
		{
			if(source.CanSeek && source.Length > MaxLength)
				throw new ArgumentException("Payment messages larger than " + MaxLength + " bytes should be rejected by the merchant's server", "source");
			var data = PaymentRequest.Serializer.Deserialize<Proto.Payment>(source);
			return new PaymentMessage(data);
		}

		public string Memo
		{
			get;
			set;
		}
		public byte[] MerchantData
		{
			get;
			set;
		}

		private readonly List<PaymentOutput> _refundTo = new List<PaymentOutput>();
		public List<PaymentOutput> RefundTo
		{
			get
			{
				return _refundTo;
			}
		}

		private readonly List<Transaction> _transactions = new List<Transaction>();
		public readonly static string MediaType = "application/bitcoin-payment";
		public PaymentMessage()
		{

		}
		internal PaymentMessage(Proto.Payment data)
		{
			Memo = data.MemoSpecified ? data.Memo : null;
			MerchantData = data.MerchantData;
			foreach(var tx in data.Transactions)
			{
				Transactions.Add(new Transaction(tx));
			}
			foreach(var refund in data.RefundTo)
			{
				RefundTo.Add(new PaymentOutput(refund));
			}
			OriginalData = data;
		}

		public PaymentMessage(PaymentRequest request)
		{
			MerchantData = request.Details.MerchantData;
		}
		public List<Transaction> Transactions
		{
			get
			{
				return _transactions;
			}
		}

		public Uri ImplicitPaymentUrl
		{
			get;
			set;
		}

		internal Proto.Payment OriginalData
		{
			get;
			set;
		}

		public byte[] ToBytes()
		{
			var ms = new MemoryStream();
			WriteTo(ms);
			return ms.ToArray();
		}

		public void WriteTo(Stream output)
		{
			PaymentRequest.Serializer.Serialize(output, ToData());
		}

		internal Proto.Payment ToData()
		{
			var data = OriginalData == null ? new Proto.Payment() : (Proto.Payment)PaymentRequest.Serializer.DeepClone(OriginalData);
			data.Memo = Memo;
			data.MerchantData = MerchantData;

			foreach(var refund in RefundTo)
			{
				data.RefundTo.Add(refund.ToData());
			}
			foreach(var transaction in Transactions)
			{
				data.Transactions.Add(transaction.ToBytes());
			}

			return data;
		}

		/// <summary>
		/// Send the payment to given address
		/// </summary>
		/// <param name="paymentUrl">ImplicitPaymentUrl if null</param>
		/// <returns>The PaymentACK</returns>
		public PaymentAck SubmitPayment(Uri paymentUrl = null)
		{
			if(paymentUrl == null)
				paymentUrl = ImplicitPaymentUrl;
			if(paymentUrl == null)
				throw new ArgumentNullException("paymentUrl");
			try
			{
				return SubmitPaymentAsync(paymentUrl, null).Result;
			}
			catch(AggregateException ex)
			{
				throw ex.InnerException;
			}
		}

		public async Task<PaymentAck> SubmitPaymentAsync(Uri paymentUrl, HttpClient httpClient)
		{
			var own = false;
			if(paymentUrl == null)
				paymentUrl = ImplicitPaymentUrl;
			if(paymentUrl == null)
				throw new ArgumentNullException("paymentUrl");
			if(httpClient == null)
			{
				httpClient = new HttpClient();
				own = true;
			}

			try
			{
				var request = new HttpRequestMessage(HttpMethod.Post, paymentUrl.OriginalString);
				request.Headers.Clear();
				request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(PaymentAck.MediaType));
				request.Content = new ByteArrayContent(ToBytes());
				request.Content.Headers.ContentType = new MediaTypeHeaderValue(MediaType);

				var result = await httpClient.SendAsync(request).ConfigureAwait(false);
				if(!result.IsSuccessStatusCode)
					throw new WebException(result.StatusCode + "(" + (int)result.StatusCode + ")");

				if(result.Content.Headers.ContentType == null || !result.Content.Headers.ContentType.MediaType.Equals(PaymentAck.MediaType, StringComparison.InvariantCultureIgnoreCase))
				{
					throw new WebException("Invalid contenttype received, expecting " + PaymentAck.MediaType + ", but got " + result.Content.Headers.ContentType);
				}
				var response = await result.Content.ReadAsStreamAsync().ConfigureAwait(false);
				return PaymentAck.Load(response);
			}
			finally
			{
				if(own)
					httpClient.Dispose();
			}
		}
	}
}
#endif