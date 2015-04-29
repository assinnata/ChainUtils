#if !USEBC
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using ChainUtils.Crypto;
using Proto;
using ProtoBuf.Meta;

namespace ChainUtils.Payment
{
	public enum PkiType
	{
		None,
		X509Sha256,
		X509Sha1,
	}

	public class PaymentOutput
	{
		public PaymentOutput()
		{

		}
		public PaymentOutput(Money amount, Script script)
		{
			Amount = amount;
			Script = script;
		}
		public PaymentOutput(Money amount, IDestination destination)
		{
			Amount = amount;
			if(destination != null)
				Script = destination.ScriptPubKey;
		}
		internal PaymentOutput(Output output)
		{
			Amount = new Money(output.Amount);
			Script = output.Script == null ? null : new Script(output.Script);
			OriginalData = output;
		}
		public Money Amount
		{
			get;
			set;
		}
		public Script Script
		{
			get;
			set;
		}
		internal Output OriginalData
		{
			get;
			set;
		}

		internal Output ToData()
		{
			var data = OriginalData == null ? new Output() : (Output)PaymentRequest.Serializer.DeepClone(OriginalData);
			data.Amount = (ulong)Amount.Satoshi;
			data.Script = Script.ToBytes();
			return data;
		}
	}
	public class PaymentDetails
	{
		public PaymentDetails()
		{
			Time = Utils.UnixTimeToDateTime(0);
			Expires = Utils.UnixTimeToDateTime(0);
		}
		public static PaymentDetails Load(byte[] details)
		{
			return Load(new MemoryStream(details));
		}

		private static PaymentDetails Load(Stream source)
		{
			var result = new PaymentDetails();
			var details = PaymentRequest.Serializer.Deserialize<Proto.PaymentDetails>(source);
			result.Network = details.Network == "main" ? Network.Main :
							 details.Network == "test" ? Network.TestNet : null;
			if(result.Network == null)
				throw new NotSupportedException("Invalid network");
			result.Time = Utils.UnixTimeToDateTime(details.Time);
			result.Expires = Utils.UnixTimeToDateTime(details.Expires);
			result.Memo = details.MemoSpecified ? details.Memo : null;
			result.MerchantData = details.MerchantDataSpecified ? details.MerchantData : null;
			result.PaymentUrl = details.PaymentUrlSpecified ? new Uri(details.PaymentUrl, UriKind.Absolute) : null;
			foreach(var output in details.Outputs)
			{
				result.Outputs.Add(new PaymentOutput(output));
			}
			result.OriginalData = details;
			return result;
		}

		public Network Network
		{
			get;
			set;
		}

		/// <summary>
		/// timestamp (seconds since 1-Jan-1970 UTC) when the PaymentRequest was created.
		/// </summary>
		public DateTimeOffset Time
		{
			get;
			set;
		}
		/// <summary>
		/// timestamp (UTC) after which the PaymentRequest should be considered invalid. 
		/// </summary>
		public DateTimeOffset Expires
		{
			get;
			set;
		}
		/// <summary>
		/// plain-text (no formatting) note that should be displayed to the customer, explaining what this PaymentRequest is for. 
		/// </summary>
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

		/// <summary>
		/// Secure (usually https) location where a Payment message (see below) may be sent to obtain a PaymentACK. 
		/// </summary>
		public Uri PaymentUrl
		{
			get;
			set;
		}
		private readonly List<PaymentOutput> _outputs = new List<PaymentOutput>();
		public List<PaymentOutput> Outputs
		{
			get
			{
				return _outputs;
			}
		}

		public byte[] ToBytes()
		{
			var ms = new MemoryStream();
			WriteTo(ms);
			return ms.ToArray();
		}

		static byte[] GetByte<T>(T obj)
		{
			var ms = new MemoryStream();
			PaymentRequest.Serializer.Serialize(ms, obj);
			return ms.ToArray();
		}
		public void WriteTo(Stream output)
		{
			var details = OriginalData == null ? new Proto.PaymentDetails() : (Proto.PaymentDetails)PaymentRequest.Serializer.DeepClone(OriginalData);
			details.Memo = Memo;


			details.MerchantData = MerchantData;

			var network = Network == Network.Main ? "main" :
							  Network == Network.TestNet ? "test" : null;
			if(details.Network != network)
				details.Network = network;

			var time = Utils.DateTimeToUnixTimeLong(Time);
			if(time != details.Time)
				details.Time = time;
			var expires = Utils.DateTimeToUnixTimeLong(Expires);
			if(expires != details.Expires)
				details.Expires = expires;

			details.PaymentUrl = PaymentUrl == null ? null : PaymentUrl.AbsoluteUri;
			details.Outputs.Clear();
			foreach(var o in Outputs)
			{
				details.Outputs.Add(o.ToData());
			}
			PaymentRequest.Serializer.Serialize(output, details);
		}

		public uint Version
		{
			get
			{
				return 1;
			}
		}

		internal Proto.PaymentDetails OriginalData
		{
			get;
			set;
		}
	}

	internal static class RuntimeTypeModelExtensions
	{
		public static T Deserialize<T>(this RuntimeTypeModel seria, Stream source)
		{
			return (T)seria.Deserialize(source, null, typeof(T));
		}
	}
	public class PaymentRequest
	{
		internal static RuntimeTypeModel Serializer;
		static PaymentRequest()
		{
			Serializer = TypeModel.Create();
			Serializer.UseImplicitZeroDefaults = false;
		}
		public static PaymentRequest Load(string file)
		{
			using(var fs = File.OpenRead(file))
			{
				return Load(fs);
			}
		}
		public static PaymentRequest Load(byte[] request)
		{
			return Load(new MemoryStream(request));
		}
		public static PaymentRequest Load(Stream source)
		{
			var result = new PaymentRequest();
			var req = Serializer.Deserialize<Proto.PaymentRequest>(source);
			result.PkiType = ToPkiType(req.PkiType);
			if(req.PkiData != null && req.PkiData.Length != 0)
			{
				var certs = Serializer.Deserialize<X509Certificates>(new MemoryStream(req.PkiData));
				var first = true;
				foreach(var cert in certs.Certificate)
				{
					if(first)
					{
						first = false;
						result.MerchantCertificate = new X509Certificate2(cert);
					}
					else
					{
						result.AdditionalCertificates.Add(new X509Certificate2(cert));
					}
				}
			}
			result._paymentDetails = PaymentDetails.Load(req.SerializedPaymentDetails);
			result.Signature = req.Signature;
			result.OriginalData = req;
			return result;
		}

		public PaymentMessage CreatePayment()
		{
			return new PaymentMessage(this)
			{
				ImplicitPaymentUrl = Details.PaymentUrl
			};
		}
		public void WriteTo(Stream output)
		{
			var req = OriginalData == null ? new Proto.PaymentRequest() : (Proto.PaymentRequest)Serializer.DeepClone(OriginalData);
			req.PkiType = ToPkiTypeString(PkiType);

			var certs = new X509Certificates();
			if(MerchantCertificate != null)
			{
				certs.Certificate.Add(MerchantCertificate.Export(X509ContentType.Cert));
			}
			foreach(var cert in AdditionalCertificates)
			{
				certs.Certificate.Add(cert.Export(X509ContentType.Cert));
			}
			var ms = new MemoryStream();
			Serializer.Serialize(ms, certs);
			req.PkiData = ms.ToArray();
			req.SerializedPaymentDetails = Details.ToBytes();
			req.Signature = Signature;
			if(Details.Version != 1)
			{
				req.PaymentDetailsVersion = Details.Version;
			}
			Serializer.Serialize(output, req);
		}

		private string ToPkiTypeString(PkiType pkitype)
		{
			switch(pkitype)
			{
				case PkiType.None:
					return "none";
				case PkiType.X509Sha1:
					return "x509+sha1";
				case PkiType.X509Sha256:
					return "x509+sha256";
				default:
					throw new NotSupportedException(pkitype.ToString());
			}
		}

		private static PkiType ToPkiType(string str)
		{
			switch(str)
			{
				case "none":
					return PkiType.None;
				case "x509+sha256":
					return PkiType.X509Sha256;
				case "x509+sha1":
					return PkiType.X509Sha1;
				default:
					throw new NotSupportedException(str);
			}
		}

		public PkiType PkiType
		{
			get;
			set;
		}

		/// <summary>
		/// Get the merchant name from the certificate subject
		/// </summary>
		public string MerchantName
		{
			get
			{
				if(MerchantCertificate == null)
					return null;
				if(!string.IsNullOrEmpty(MerchantCertificate.FriendlyName))
					return MerchantCertificate.FriendlyName;
				else
				{
					var match = Regex.Match(MerchantCertificate.Subject, "^(CN=)?(?<Name>[^,]*)", RegexOptions.IgnoreCase);
					if(!match.Success)
						return MerchantCertificate.Subject;
					return match.Groups["Name"].Value.Trim();
				}
			}
		}

		public X509Certificate2 MerchantCertificate
		{
			get;
			set;
		}


		private readonly List<X509Certificate2> _additionalCertificates = new List<X509Certificate2>();
		public List<X509Certificate2> AdditionalCertificates
		{
			get
			{
				return _additionalCertificates;
			}
		}

		private PaymentDetails _paymentDetails = new PaymentDetails();
		public PaymentDetails Details
		{
			get
			{
				return _paymentDetails;
			}
		}

		public byte[] ToBytes()
		{
			var ms = new MemoryStream();
			WriteTo(ms);
			return ms.ToArray();
		}



		public byte[] Signature
		{
			get;
			set;
		}

		/// <summary>
		/// Verify that the certificate chain is trusted and signature correct.
		/// </summary>
		/// <returns>true if the certificate chain and the signature is trusted or if PKIType == None</returns>
		public bool Verify()
		{
			var valid = true;
			if(PkiType != PkiType.None)
				valid = VerifyChain() && VerifySignature();
			if(!valid)
				return valid;

			return Details.Expires < DateTimeOffset.UtcNow;
		}

		public bool VerifyChain(X509VerificationFlags flags = X509VerificationFlags.NoFlag)
		{
			X509Chain chain;
			return VerifyChain(out chain, flags);
		}

		public bool VerifyChain(out X509Chain chain, X509VerificationFlags flags = X509VerificationFlags.NoFlag)
		{
			chain = null;
			if(MerchantCertificate == null || PkiType == PkiType.None)
				return false;
			chain = new X509Chain();
			chain.ChainPolicy.VerificationFlags = flags;
			foreach(var additional in AdditionalCertificates)
				chain.ChainPolicy.ExtraStore.Add(additional);
			return chain.Build(MerchantCertificate);
		}

		public bool VerifySignature()
		{
			if(MerchantCertificate == null || PkiType == PkiType.None)
				return false;

			var key = (RSACryptoServiceProvider)MerchantCertificate.PublicKey.Key;
			var sig = Signature;
			Signature = new byte[0];
			byte[] data = null;
			try
			{
				data = ToBytes();
			}
			finally
			{
				Signature = sig;
			}

			byte[] hash = null;
			string hashName = null;
			if(PkiType == PkiType.X509Sha256)
			{
				hash = Hashes.SHA256(data);
				hashName = "sha256";
			}
			else if(PkiType == PkiType.X509Sha1)
			{
				hash = Hashes.Sha1(data, data.Length);
				hashName = "sha1";
			}
			else
				throw new NotSupportedException(PkiType.ToString());

			return key.VerifyHash(hash, hashName, Signature);
		}

		internal Proto.PaymentRequest OriginalData
		{
			get;
			set;
		}


		public void Sign(X509Certificate2 certificate, PkiType type)
		{
			if(type == PkiType.None)
				throw new ArgumentException("PKIType can't be none if signing");
			var privateKey = certificate.PrivateKey as RSACryptoServiceProvider;
			if(privateKey == null)
				throw new ArgumentException("Private key not present in the certificate, impossible to sign");
			MerchantCertificate = new X509Certificate2(certificate.Export(X509ContentType.Cert));
			PkiType = type;
			Signature = new byte[0];
			var data = ToBytes();
			byte[] hash = null;
			string hashName = null;
			if(type == PkiType.X509Sha256)
			{
				hash = Hashes.SHA256(data);
				hashName = "sha256";
			}
			else if(type == PkiType.X509Sha1)
			{
				hash = Hashes.Sha1(data, data.Length);
				hashName = "sha1";
			}
			else
				throw new NotSupportedException(PkiType.ToString());

			Signature = privateKey.SignHash(hash, hashName);
		}

		public readonly static string MediaType = "application/bitcoin-paymentrequest";
	}
}
#endif