﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;
using ProtoBuf;

#if !NOPROTOBUF
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

// Option: missing-value detection (*Specified/ShouldSerialize*/Reset*) enabled

// Generated from: PaymentRequest.proto
namespace Proto
{
	using payments = Proto;
	[Serializable, ProtoContract(Name = @"Output")]
	internal partial class Output : IExtensible
	{
		public Output()
		{
		}

		private ulong? _amount;
		[ProtoMember(1, IsRequired = false, Name = @"amount", DataFormat = DataFormat.TwosComplement)]
		public ulong Amount
		{
			get
			{
				return _amount ?? (ulong)0;
			}
			set
			{
				_amount = value;
			}
		}
		[XmlIgnore]
		[Browsable(false)]
		public bool AmountSpecified
		{
			get
			{
				return _amount != null;
			}
			set
			{
				if(value == (_amount == null))
					_amount = value ? Amount : (ulong?)null;
			}
		}
		private bool ShouldSerializeamount()
		{
			return AmountSpecified;
		}
		private void Resetamount()
		{
			AmountSpecified = false;
		}

		private byte[] _script;
		[ProtoMember(2, IsRequired = true, Name = @"script", DataFormat = DataFormat.Default)]
		public byte[] Script
		{
			get
			{
				return _script;
			}
			set
			{
				_script = value;
			}
		}
		private IExtension _extensionObject;
		IExtension IExtensible.GetExtensionObject(bool createIfMissing)
		{
			return Extensible.GetExtensionObject(ref _extensionObject, createIfMissing);
		}
	}

	[Serializable, ProtoContract(Name = @"PaymentDetails")]
	internal partial class PaymentDetails : IExtensible
	{
		public PaymentDetails()
		{
		}

		private string _network;
		[ProtoMember(1, IsRequired = false, Name = @"network", DataFormat = DataFormat.Default)]
		public string Network
		{
			get
			{
				return _network ?? @"main";
			}
			set
			{
				_network = value;
			}
		}
		[XmlIgnore]
		[Browsable(false)]
		public bool NetworkSpecified
		{
			get
			{
				return _network != null;
			}
			set
			{
				if(value == (_network == null))
					_network = value ? Network : (string)null;
			}
		}
		private bool ShouldSerializenetwork()
		{
			return NetworkSpecified;
		}
		private void Resetnetwork()
		{
			NetworkSpecified = false;
		}

		private readonly List<Output> _outputs = new List<Output>();
		[ProtoMember(2, Name = @"outputs", DataFormat = DataFormat.Default)]
		public List<Output> Outputs
		{
			get
			{
				return _outputs;
			}
		}

		private ulong _time;
		[ProtoMember(3, IsRequired = true, Name = @"time", DataFormat = DataFormat.TwosComplement)]
		public ulong Time
		{
			get
			{
				return _time;
			}
			set
			{
				_time = value;
			}
		}
		private ulong? _expires;
		[ProtoMember(4, IsRequired = false, Name = @"expires", DataFormat = DataFormat.TwosComplement)]
		public ulong Expires
		{
			get
			{
				return _expires ?? default(ulong);
			}
			set
			{
				_expires = value;
			}
		}
		[XmlIgnore]
		[Browsable(false)]
		public bool ExpiresSpecified
		{
			get
			{
				return _expires != null;
			}
			set
			{
				if(value == (_expires == null))
					_expires = value ? Expires : (ulong?)null;
			}
		}
		private bool ShouldSerializeexpires()
		{
			return ExpiresSpecified;
		}
		private void Resetexpires()
		{
			ExpiresSpecified = false;
		}

		private string _memo;
		[ProtoMember(5, IsRequired = false, Name = @"memo", DataFormat = DataFormat.Default)]
		public string Memo
		{
			get
			{
				return _memo ?? "";
			}
			set
			{
				_memo = value;
			}
		}
		[XmlIgnore]
		[Browsable(false)]
		public bool MemoSpecified
		{
			get
			{
				return _memo != null;
			}
			set
			{
				if(value == (_memo == null))
					_memo = value ? Memo : (string)null;
			}
		}
		private bool ShouldSerializememo()
		{
			return MemoSpecified;
		}
		private void Resetmemo()
		{
			MemoSpecified = false;
		}

		private string _paymentUrl;
		[ProtoMember(6, IsRequired = false, Name = @"payment_url", DataFormat = DataFormat.Default)]
		public string PaymentUrl
		{
			get
			{
				return _paymentUrl ?? "";
			}
			set
			{
				_paymentUrl = value;
			}
		}
		[XmlIgnore]
		[Browsable(false)]
		public bool PaymentUrlSpecified
		{
			get
			{
				return _paymentUrl != null;
			}
			set
			{
				if(value == (_paymentUrl == null))
					_paymentUrl = value ? PaymentUrl : (string)null;
			}
		}
		private bool ShouldSerializepayment_url()
		{
			return PaymentUrlSpecified;
		}
		private void Resetpayment_url()
		{
			PaymentUrlSpecified = false;
		}

		private byte[] _merchantData;
		[ProtoMember(7, IsRequired = false, Name = @"merchant_data", DataFormat = DataFormat.Default)]
		public byte[] MerchantData
		{
			get
			{
				return _merchantData ?? null;
			}
			set
			{
				_merchantData = value;
			}
		}
		[XmlIgnore]
		[Browsable(false)]
		public bool MerchantDataSpecified
		{
			get
			{
				return _merchantData != null;
			}
			set
			{
				if(value == (_merchantData == null))
					_merchantData = value ? MerchantData : (byte[])null;
			}
		}
		private bool ShouldSerializemerchant_data()
		{
			return MerchantDataSpecified;
		}
		private void Resetmerchant_data()
		{
			MerchantDataSpecified = false;
		}

		private IExtension _extensionObject;
		IExtension IExtensible.GetExtensionObject(bool createIfMissing)
		{
			return Extensible.GetExtensionObject(ref _extensionObject, createIfMissing);
		}
	}

	[Serializable, ProtoContract(Name = @"PaymentRequest")]
	internal partial class PaymentRequest : IExtensible
	{
		public PaymentRequest()
		{
		}

		private uint? _paymentDetailsVersion;
		[ProtoMember(1, IsRequired = false, Name = @"payment_details_version", DataFormat = DataFormat.TwosComplement)]
		public uint PaymentDetailsVersion
		{
			get
			{
				return _paymentDetailsVersion ?? (uint)1;
			}
			set
			{
				_paymentDetailsVersion = value;
			}
		}
		[XmlIgnore]
		[Browsable(false)]
		public bool PaymentDetailsVersionSpecified
		{
			get
			{
				return _paymentDetailsVersion != null;
			}
			set
			{
				if(value == (_paymentDetailsVersion == null))
					_paymentDetailsVersion = value ? PaymentDetailsVersion : (uint?)null;
			}
		}
		private bool ShouldSerializepayment_details_version()
		{
			return PaymentDetailsVersionSpecified;
		}
		private void Resetpayment_details_version()
		{
			PaymentDetailsVersionSpecified = false;
		}

		private string _pkiType;
		[ProtoMember(2, IsRequired = false, Name = @"pki_type", DataFormat = DataFormat.Default)]
		public string PkiType
		{
			get
			{
				return _pkiType ?? @"none";
			}
			set
			{
				_pkiType = value;
			}
		}
		[XmlIgnore]
		[Browsable(false)]
		public bool PkiTypeSpecified
		{
			get
			{
				return _pkiType != null;
			}
			set
			{
				if(value == (_pkiType == null))
					_pkiType = value ? PkiType : (string)null;
			}
		}
		private bool ShouldSerializepki_type()
		{
			return PkiTypeSpecified;
		}
		private void Resetpki_type()
		{
			PkiTypeSpecified = false;
		}

		private byte[] _pkiData;
		[ProtoMember(3, IsRequired = false, Name = @"pki_data", DataFormat = DataFormat.Default)]
		public byte[] PkiData
		{
			get
			{
				return _pkiData ?? null;
			}
			set
			{
				_pkiData = value;
			}
		}
		[XmlIgnore]
		[Browsable(false)]
		public bool PkiDataSpecified
		{
			get
			{
				return _pkiData != null;
			}
			set
			{
				if(value == (_pkiData == null))
					_pkiData = value ? PkiData : (byte[])null;
			}
		}
		private bool ShouldSerializepki_data()
		{
			return PkiDataSpecified;
		}
		private void Resetpki_data()
		{
			PkiDataSpecified = false;
		}

		private byte[] _serializedPaymentDetails;
		[ProtoMember(4, IsRequired = true, Name = @"serialized_payment_details", DataFormat = DataFormat.Default)]
		public byte[] SerializedPaymentDetails
		{
			get
			{
				return _serializedPaymentDetails;
			}
			set
			{
				_serializedPaymentDetails = value;
			}
		}
		private byte[] _signature;
		[ProtoMember(5, IsRequired = false, Name = @"signature", DataFormat = DataFormat.Default)]
		public byte[] Signature
		{
			get
			{
				return _signature ?? null;
			}
			set
			{
				_signature = value;
			}
		}
		[XmlIgnore]
		[Browsable(false)]
		public bool SignatureSpecified
		{
			get
			{
				return _signature != null;
			}
			set
			{
				if(value == (_signature == null))
					_signature = value ? Signature : (byte[])null;
			}
		}
		private bool ShouldSerializesignature()
		{
			return SignatureSpecified;
		}
		private void Resetsignature()
		{
			SignatureSpecified = false;
		}

		private IExtension _extensionObject;
		IExtension IExtensible.GetExtensionObject(bool createIfMissing)
		{
			return Extensible.GetExtensionObject(ref _extensionObject, createIfMissing);
		}
	}

	[Serializable, ProtoContract(Name = @"X509Certificates")]
	internal partial class X509Certificates : IExtensible
	{
		public X509Certificates()
		{
		}

		private readonly List<byte[]> _certificate = new List<byte[]>();
		[ProtoMember(1, Name = @"certificate", DataFormat = DataFormat.Default)]
		public List<byte[]> Certificate
		{
			get
			{
				return _certificate;
			}
		}

		private IExtension _extensionObject;
		IExtension IExtensible.GetExtensionObject(bool createIfMissing)
		{
			return Extensible.GetExtensionObject(ref _extensionObject, createIfMissing);
		}
	}

	[Serializable, ProtoContract(Name = @"Payment")]
	internal partial class Payment : IExtensible
	{
		public Payment()
		{
		}

		private byte[] _merchantData;
		[ProtoMember(1, IsRequired = false, Name = @"merchant_data", DataFormat = DataFormat.Default)]
		public byte[] MerchantData
		{
			get
			{
				return _merchantData ?? null;
			}
			set
			{
				_merchantData = value;
			}
		}
		[XmlIgnore]
		[Browsable(false)]
		public bool MerchantDataSpecified
		{
			get
			{
				return _merchantData != null;
			}
			set
			{
				if(value == (_merchantData == null))
					_merchantData = value ? MerchantData : (byte[])null;
			}
		}
		private bool ShouldSerializemerchant_data()
		{
			return MerchantDataSpecified;
		}
		private void Resetmerchant_data()
		{
			MerchantDataSpecified = false;
		}

		private readonly List<byte[]> _transactions = new List<byte[]>();
		[ProtoMember(2, Name = @"transactions", DataFormat = DataFormat.Default)]
		public List<byte[]> Transactions
		{
			get
			{
				return _transactions;
			}
		}

		private readonly List<Output> _refundTo = new List<Output>();
		[ProtoMember(3, Name = @"refund_to", DataFormat = DataFormat.Default)]
		public List<Output> RefundTo
		{
			get
			{
				return _refundTo;
			}
		}

		private string _memo;
		[ProtoMember(4, IsRequired = false, Name = @"memo", DataFormat = DataFormat.Default)]
		public string Memo
		{
			get
			{
				return _memo ?? "";
			}
			set
			{
				_memo = value;
			}
		}
		[XmlIgnore]
		[Browsable(false)]
		public bool MemoSpecified
		{
			get
			{
				return _memo != null;
			}
			set
			{
				if(value == (_memo == null))
					_memo = value ? Memo : (string)null;
			}
		}
		private bool ShouldSerializememo()
		{
			return MemoSpecified;
		}
		private void Resetmemo()
		{
			MemoSpecified = false;
		}

		private IExtension _extensionObject;
		IExtension IExtensible.GetExtensionObject(bool createIfMissing)
		{
			return Extensible.GetExtensionObject(ref _extensionObject, createIfMissing);
		}
	}

	[Serializable, ProtoContract(Name = @"PaymentACK")]
	internal partial class PaymentAck : IExtensible
	{
		public PaymentAck()
		{
		}

		private Payment _payment;
		[ProtoMember(1, IsRequired = true, Name = @"payment", DataFormat = DataFormat.Default)]
		public Payment Payment
		{
			get
			{
				return _payment;
			}
			set
			{
				_payment = value;
			}
		}
		private string _memo;
		[ProtoMember(2, IsRequired = false, Name = @"memo", DataFormat = DataFormat.Default)]
		public string Memo
		{
			get
			{
				return _memo ?? "";
			}
			set
			{
				_memo = value;
			}
		}
		[XmlIgnore]
		[Browsable(false)]
		public bool MemoSpecified
		{
			get
			{
				return _memo != null;
			}
			set
			{
				if(value == (_memo == null))
					_memo = value ? Memo : (string)null;
			}
		}
		private bool ShouldSerializememo()
		{
			return MemoSpecified;
		}
		private void Resetmemo()
		{
			MemoSpecified = false;
		}

		private IExtension _extensionObject;
		IExtension IExtensible.GetExtensionObject(bool createIfMissing)
		{
			return Extensible.GetExtensionObject(ref _extensionObject, createIfMissing);
		}
	}

}
#endif