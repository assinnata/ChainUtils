﻿using ChainUtils.DataEncoders;
using Newtonsoft.Json.Linq;

namespace ChainUtils.RPC
{
	public class UnspentCoin
	{
		public UnspentCoin(JObject unspent)
		{
			OutPoint = new OutPoint(new Uint256((string)unspent["txid"]), (uint)unspent["vout"]);
			Address = Network.CreateFromBase58Data<BitcoinAddress>((string)unspent["address"]);
			Account = (string)unspent["account"];
			ScriptPubKey = new Script(Encoders.Hex.DecodeData((string)unspent["scriptPubKey"]));
			var amount = (decimal)unspent["amount"];
			Amount = new Money((long)(amount * Money.Coin));
			Confirmations = (uint)unspent["confirmations"];
		}

		public OutPoint OutPoint
		{
			get;
			private set;
		}

		public BitcoinAddress Address
		{
			get;
			private set;
		}
		public string Account
		{
			get;
			private set;
		}
		public Script ScriptPubKey
		{
			get;
			private set;
		}
		public uint Confirmations
		{
			get;
			private set;
		}

		public Money Amount
		{
			get;
			private set;
		}
	}
}
