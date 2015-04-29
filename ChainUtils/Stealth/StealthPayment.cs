﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace ChainUtils.Stealth
{
	public class StealthSpendKey
	{
		private readonly StealthPayment _payment;
		public StealthPayment Payment
		{
			get
			{
				return _payment;
			}
		}
		private readonly KeyId _id;
		public KeyId Id
		{
			get
			{
				return _id;
			}
		}
		public StealthSpendKey(KeyId id, StealthPayment payment)
		{
			_id = id;
			_payment = payment;
		}

		public BitcoinAddress GetAddress(Network network)
		{
			return new BitcoinAddress(Id, network);
		}
	}

	public class StealthPayment
	{
		public StealthPayment(BitcoinStealthAddress address, Key ephemKey, StealthMetadata metadata)
		{
			Metadata = metadata;
			ScriptPubKey = CreatePaymentScript(address.SignatureCount, address.SpendPubKeys, ephemKey, address.ScanPubKey);

			if(address.SignatureCount > 1)
			{
				Redeem = ScriptPubKey;
				ScriptPubKey = ScriptPubKey.Hash.ScriptPubKey;
			}
			SetStealthKeys();
		}

		public static Script CreatePaymentScript(int sigCount, PubKey[] spendPubKeys, Key ephemKey, PubKey scanPubKey)
		{
			return CreatePaymentScript(sigCount, spendPubKeys.Select(p => p.Uncover(ephemKey, scanPubKey)).ToArray());
		}

		public static Script CreatePaymentScript(int sigCount, PubKey[] uncoveredPubKeys)
		{
			if(sigCount == 1 && uncoveredPubKeys.Length == 1)
			{
				return PayToPubkeyHashTemplate.Instance.GenerateScriptPubKey(uncoveredPubKeys[0].Hash);
			}
			else
			{
				return PayToMultiSigTemplate.Instance.GenerateScriptPubKey(sigCount, uncoveredPubKeys);
			}
		}

		public static Script CreatePaymentScript(BitcoinStealthAddress address, PubKey ephemKey, Key scan)
		{
			return CreatePaymentScript(address.SignatureCount, address.SpendPubKeys.Select(p => p.UncoverReceiver(scan, ephemKey)).ToArray());
		}


		public static KeyId[] ExtractKeyIDs(Script script)
		{
			var keyId = PayToPubkeyHashTemplate.Instance.ExtractScriptPubKeyParameters(script);
			if(keyId != null)
			{
				return new[] { keyId };
			}
			else
			{
				var para = PayToMultiSigTemplate.Instance.ExtractScriptPubKeyParameters(script);
				if(para == null)
					throw new ArgumentException("Invalid stealth spendable output script", "spendable");
				return para.PubKeys.Select(k => k.Hash).ToArray();
			}
		}

		public StealthSpendKey[] StealthKeys
		{
			get;
			private set;
		}
		public BitcoinAddress[] GetAddresses(Network network)
		{
			return StealthKeys.Select(k => k.GetAddress(network)).ToArray();
		}

		public StealthPayment(Script scriptPubKey, Script redeem, StealthMetadata metadata)
		{
			Metadata = metadata;
			ScriptPubKey = scriptPubKey;
			Redeem = redeem;
			SetStealthKeys();
		}

		private void SetStealthKeys()
		{
			StealthKeys = ExtractKeyIDs(Redeem ?? ScriptPubKey).Select(id => new StealthSpendKey(id, this)).ToArray();
		}


		public StealthMetadata Metadata
		{
			get;
			private set;
		}
		public Script ScriptPubKey
		{
			get;
			private set;
		}
		public Script Redeem
		{
			get;
			private set;
		}

		public void AddToTransaction(Transaction transaction, Money value)
		{
			if(transaction == null)
				throw new ArgumentNullException("transaction");
			if(value == null)
				throw new ArgumentNullException("value");
			transaction.Outputs.Add(new TxOut(0, Metadata.Script));
			transaction.Outputs.Add(new TxOut(value, ScriptPubKey));
		}

		public static StealthPayment[] GetPayments(Transaction transaction, BitcoinStealthAddress address, Key scan)
		{
			var result = new List<StealthPayment>();
			for(var i = 0 ; i < transaction.Outputs.Count - 1 ; i++)
			{
				var metadata = StealthMetadata.TryParse(transaction.Outputs[i].ScriptPubKey);
				if(metadata != null && (address == null || address.Prefix.Match(metadata.BitField)))
				{
					var scriptPubKey = transaction.Outputs[i + 1].ScriptPubKey;
					var scriptId = PayToScriptHashTemplate.Instance.ExtractScriptPubKeyParameters(scriptPubKey);
					var expectedScriptPubKey = address == null ? scriptPubKey : null;
					Script redeem = null;

					if(scriptId != null)
					{
						if(address == null)
							throw new ArgumentNullException("address");
						redeem = CreatePaymentScript(address, metadata.EphemKey, scan);
						expectedScriptPubKey = redeem.Hash.ScriptPubKey;
						if(expectedScriptPubKey != scriptPubKey)
							continue;
					}

					var payment = new StealthPayment(scriptPubKey, redeem, metadata);
					if(scan != null)
					{
						if(address != null && payment.StealthKeys.Length != address.SpendPubKeys.Length)
							continue;

						if(expectedScriptPubKey == null)
						{
							expectedScriptPubKey = CreatePaymentScript(address, metadata.EphemKey, scan);
						}

						if(expectedScriptPubKey != scriptPubKey)
							continue;
					}
					result.Add(payment);
				}
			}
			return result.ToArray();
		}


	}
}
