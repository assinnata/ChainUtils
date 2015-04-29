#if !NOHTTPCLIENT
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using ChainUtils.DataEncoders;
using Newtonsoft.Json.Linq;

namespace ChainUtils
{
	public class BlockrException : Exception
	{
		public BlockrException(JObject response)
			: base(response["message"] == null ? "Error from Blockr" : response["message"].ToString())
		{
			Code = response["code"] == null ? 0 : response["code"].Value<int>();
			Status = response["status"] == null ? null : response["status"].ToString();
		}

		public int Code
		{
			get;
			set;
		}
		public string Status
		{
			get;
			set;
		}
	}
	public class BlockrTransactionRepository : ITransactionRepository
	{
		public BlockrTransactionRepository()
			: this(null)
		{

		}
		public BlockrTransactionRepository(Network network)
		{
			if(network == null)
				network = Network.Main;
			Network = network;
		}

		public Network Network
		{
			get;
			set;
		}


		#region ITransactionRepository Members

		public async Task<Transaction> GetAsync(Uint256 txId)
		{
			while(true)
			{
				using(var client = new HttpClient())
				{
					var response = await client.GetAsync(BlockrAddress + "tx/raw/" + txId).ConfigureAwait(false);
					if(response.StatusCode == HttpStatusCode.NotFound)
						return null;
					var result = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
					var json = JObject.Parse(result);
					var status = json["status"];
					var code = json["code"];
					if(status != null && status.ToString() == "error")
					{
						throw new BlockrException(json);
					}
					var tx = new Transaction(json["data"]["tx"]["hex"].ToString());
					return tx;
				}
			}
		}

		public async Task<List<Coin>> GetUnspentAsync(string address)
		{
			while(true)
			{
				using(var client = new HttpClient())
				{
					var response = await client.GetAsync(BlockrAddress + "address/unspent/" + address).ConfigureAwait(false);
					if(response.StatusCode == HttpStatusCode.NotFound)
						return null;
					var result = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
					var json = JObject.Parse(result);
					var status = json["status"];
					var code = json["code"];
					if((status != null && status.ToString() == "error") || (json["data"]["address"].ToString() != address))
					{
						throw new BlockrException(json);
					}
					var list = new List<Coin>();
					foreach(var element in json["data"]["unspent"])
					{
						list.Add(new Coin(new Uint256(element["tx"].ToString()), (uint)element["n"], new Money((decimal)element["amount"], MoneyUnit.Btc), new Script(Encoders.Hex.DecodeData(element["script"].ToString()))));
					}
					return list;
				}
			}
		}

		public Task PutAsync(Uint256 txId, Transaction tx)
		{
			return Task.FromResult(false);
		}

		#endregion

		string BlockrAddress
		{
			get
			{
				return "http://" + (Network == Network.Main ? "" : "t") + "btc.blockr.io/api/v1/";
			}
		}
	}
}
#endif