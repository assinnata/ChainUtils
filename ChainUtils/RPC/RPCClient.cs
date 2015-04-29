using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading.Tasks;
using ChainUtils.DataEncoders;
using Newtonsoft.Json.Linq;

namespace ChainUtils.RPC
{
	public class RpcAccount
	{
		public Money Amount
		{
			get;
			set;
		}
		public String AccountName
		{
			get;
			set;
		}
	}

	public class ChangeAddress
	{
		public Money Amount
		{
			get;
			set;
		}
		public BitcoinAddress Address
		{
			get;
			set;
		}
	}

	public class AddressGrouping
	{
		public AddressGrouping()
		{
			ChangeAddresses = new List<ChangeAddress>();
		}
		public BitcoinAddress PublicAddress
		{
			get;
			set;
		}
		public Money Amount
		{
			get;
			set;
		}
		public string Account
		{
			get;
			set;
		}

		public List<ChangeAddress> ChangeAddresses
		{
			get;
			set;
		}
	}

	public class RpcClient : IBlockRepository
	{
		private readonly NetworkCredential _credentials;
		public NetworkCredential Credentials
		{
			get
			{
				return _credentials;
			}
		}
		private readonly Uri _address;
		public Uri Address
		{
			get
			{
				return _address;
			}
		}
		private readonly Network _network;
		public Network Network
		{
			get
			{
				return _network;
			}
		}
		public RpcClient(NetworkCredential credentials, string host, Network network)
			: this(credentials, BuildUri(host, network.RpcPort), network)
		{
		}

		private static Uri BuildUri(string host, int port)
		{
			var builder = new UriBuilder();
			builder.Host = host;
			builder.Scheme = "http";
			builder.Port = port;
			return builder.Uri;
		}
		public RpcClient(NetworkCredential credentials, Uri address, Network network = null)
		{

			if(credentials == null)
				throw new ArgumentNullException("credentials");
			if(address == null)
				throw new ArgumentNullException("address");
			if(network == null)
			{
				network = new[] { Network.Main, Network.TestNet, Network.RegTest }.FirstOrDefault(n => n.RpcPort == address.Port);
				if(network == null)
					throw new ArgumentNullException("network");
			}
			_credentials = credentials;
			_address = address;
			_network = network;
		}

		public RpcResponse SendCommand(RpcOperations commandName, params object[] parameters)
		{
			return SendCommand(commandName.ToString(), parameters);
		}
		public Task<RpcResponse> SendCommandAsync(RpcOperations commandName, params object[] parameters)
		{
			return SendCommandAsync(commandName.ToString(), parameters);
		}

		/// <summary>
		/// Send a command
		/// </summary>
		/// <param name="commandName">https://en.bitcoin.it/wiki/Original_Bitcoin_client/API_calls_list</param>
		/// <param name="parameters"></param>
		/// <returns></returns>
		public RpcResponse SendCommand(string commandName, params object[] parameters)
		{
			return SendCommand(new RpcRequest(commandName, parameters));
		}
		public Task<RpcResponse> SendCommandAsync(string commandName, params object[] parameters)
		{
			return SendCommandAsync(new RpcRequest(commandName, parameters));
		}

		public RpcResponse SendCommand(RpcRequest request, bool throwIfRpcError = true)
		{
			try
			{
				return SendCommandAsync(request, throwIfRpcError).Result;
			}
			catch(AggregateException aex)
			{
				ExceptionDispatchInfo.Capture(aex.InnerException).Throw();
				return null; //Can't happen
			}
		}

		public async Task<RpcResponse> SendCommandAsync(RpcRequest request, bool throwIfRpcError = true)
		{
			var webRequest = (HttpWebRequest)WebRequest.Create(Address);
			webRequest.Credentials = Credentials;
			webRequest.ContentType = "application/json-rpc";
			webRequest.Method = "POST";

			var writer = new StringWriter();
			request.WriteJSON(writer);
			writer.Flush();
			var json = writer.ToString();
			var bytes = Encoding.UTF8.GetBytes(json);
#if !PORTABLE
			webRequest.ContentLength = bytes.Length;
#endif
			var dataStream = await webRequest.GetRequestStreamAsync().ConfigureAwait(false);
			await dataStream.WriteAsync(bytes, 0, bytes.Length).ConfigureAwait(false);
			dataStream.Dispose();
			RpcResponse response = null;
			try
			{
				using(var webResponse = await webRequest.GetResponseAsync().ConfigureAwait(false))
				{
					response = RpcResponse.Load(webResponse.GetResponseStream());
				}
				if(throwIfRpcError)
					response.ThrowIfError();
			}
			catch(WebException ex)
			{
				if(ex.Response == null)
					throw;
				response = RpcResponse.Load(ex.Response.GetResponseStream());
				if(throwIfRpcError)
					response.ThrowIfError();
			}
			return response;
		}

		public UnspentCoin[] ListUnspent()
		{
			var response = SendCommand("listunspent");
			return ((JArray)response.Result).Select(i => new UnspentCoin((JObject)i)).ToArray();
		}
		public async Task<UnspentCoin[]> ListUnspentAsync()
		{
			var response = await SendCommandAsync("listunspent").ConfigureAwait(false);
			return ((JArray)response.Result).Select(i => new UnspentCoin((JObject)i)).ToArray();
		}

		/// <summary>
		/// Get the estimated fee per kb for being confirmed in nblock
		/// </summary>
		/// <param name="nblock"></param>
		/// <returns></returns>
		public Money EstimateFee(int nblock)
		{
			var response = SendCommand(RpcOperations.Estimatefee, nblock);
			var result = 0.0m;
			try
			{
				result = decimal.Parse(response.Result.ToString(), NumberStyles.AllowExponent | NumberStyles.AllowDecimalPoint);
			}
			catch(FormatException)
			{
				result = decimal.Parse(response.Result.ToString(), NumberStyles.AllowExponent | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture);
			}
			return Money.Coins(result);
		}

		/// <summary>
		/// Get the estimated fee per kb for being confirmed in nblock
		/// </summary>
		/// <param name="nblock"></param>
		/// <returns></returns>
		public async Task<Money> EstimateFeeAsync(int nblock)
		{
			var response = await SendCommandAsync(RpcOperations.Estimatefee, nblock).ConfigureAwait(false);
			return Money.Parse(response.Result.ToString());
		}

		public BitcoinAddress GetAccountAddress(string account)
		{
			var response = SendCommand("getaccountaddress", account);
			return Network.CreateFromBase58Data<BitcoinAddress>((string)response.Result);
		}
		public async Task<BitcoinAddress> GetAccountAddressAsync(string account)
		{
			var response = await SendCommandAsync("getaccountaddress", account).ConfigureAwait(false);
			return Network.CreateFromBase58Data<BitcoinAddress>((string)response.Result);
		}

		public BitcoinSecret DumpPrivKey(BitcoinAddress address)
		{
			var response = SendCommand("dumpprivkey", address.ToString());
			return Network.CreateFromBase58Data<BitcoinSecret>((string)response.Result);
		}
		public async Task<BitcoinSecret> DumpPrivKeyAsync(BitcoinAddress address)
		{
			var response = await SendCommandAsync("dumpprivkey", address.ToString()).ConfigureAwait(false);
			return Network.CreateFromBase58Data<BitcoinSecret>((string)response.Result);
		}

		public Uint256 GetBestBlockHash()
		{
			return new Uint256((string)SendCommand("getbestblockhash").Result);
		}
		public async Task<Uint256> GetBestBlockHashAsync()
		{
			return new Uint256((string)(await SendCommandAsync("getbestblockhash").ConfigureAwait(false)).Result);
		}

		public BitcoinSecret GetAccountSecret(string account)
		{
			var address = GetAccountAddress(account);
			return DumpPrivKey(address);
		}
		public async Task<BitcoinSecret> GetAccountSecretAsync(string account)
		{
			var address = await GetAccountAddressAsync(account).ConfigureAwait(false);
			return await DumpPrivKeyAsync(address).ConfigureAwait(false);
		}

		public Transaction DecodeRawTransaction(string rawHex)
		{
			var response = SendCommand("decoderawtransaction", rawHex);
			return Transaction.Parse(response.Result.ToString(), RawFormat.Satoshi);
		}
		public Transaction DecodeRawTransaction(byte[] raw)
		{
			return DecodeRawTransaction(Encoders.Hex.EncodeData(raw));
		}
		public async Task<Transaction> DecodeRawTransactionAsync(string rawHex)
		{
			var response = await SendCommandAsync("decoderawtransaction", rawHex).ConfigureAwait(false);
			return Transaction.Parse(response.Result.ToString(), RawFormat.Satoshi);
		}
		public Task<Transaction> DecodeRawTransactionAsync(byte[] raw)
		{
			return DecodeRawTransactionAsync(Encoders.Hex.EncodeData(raw));
		}

		/// <summary>
		/// getrawtransaction only returns on txn which are not entirely spent unless you run bitcoinq with txindex=1.
		/// </summary>
		/// <param name="txid"></param>
		/// <returns></returns>
		public Transaction GetRawTransaction(Uint256 txid, bool throwIfNotFound = true)
		{
			try
			{
				return GetRawTransactionAsync(txid, throwIfNotFound).Result;
			}
			catch(AggregateException aex)
			{
				ExceptionDispatchInfo.Capture(aex.InnerException).Throw();
				return null; //Can't happen
			}
		}

		public async Task<Transaction> GetRawTransactionAsync(Uint256 txid, bool throwIfNotFound = true)
		{
			var response = await SendCommandAsync(new RpcRequest("getrawtransaction", new[] { txid.ToString() }), throwIfNotFound).ConfigureAwait(false);
			if(throwIfNotFound)
				response.ThrowIfError();
			if(response.Error != null && response.Error.Code == RpcErrorCode.RpcInvalidAddressOrKey)
				return null;
			else
				response.ThrowIfError();
			var tx = new Transaction();
			tx.ReadWrite(Encoders.Hex.DecodeData(response.Result.ToString()));
			return tx;
		}


		public void SendRawTransaction(byte[] bytes)
		{
			SendCommand("sendrawtransaction", Encoders.Hex.EncodeData(bytes));
		}
		public void SendRawTransaction(Transaction tx)
		{
			SendRawTransaction(tx.ToBytes());
		}
		public Task SendRawTransactionAsync(byte[] bytes)
		{
			return SendCommandAsync("sendrawtransaction", Encoders.Hex.EncodeData(bytes));
		}
		public Task SendRawTransactionAsync(Transaction tx)
		{
			return SendRawTransactionAsync(tx.ToBytes());
		}

		public void LockUnspent(params OutPoint[] outpoints)
		{
			LockUnspentCore(false, outpoints);
		}
		public void UnlockUnspent(params OutPoint[] outpoints)
		{
			LockUnspentCore(true, outpoints);
		}

		public Task LockUnspentAsync(params OutPoint[] outpoints)
		{
			return LockUnspentCoreAsync(false, outpoints);
		}
		public Task UnlockUnspentAsync(params OutPoint[] outpoints)
		{
			return LockUnspentCoreAsync(true, outpoints);
		}

		private void LockUnspentCore(bool unlock, OutPoint[] outpoints)
		{
			try
			{
				LockUnspentCoreAsync(unlock, outpoints).Wait();
			}
			catch(AggregateException ex)
			{
				ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
				return;
			}
		}
		private async Task LockUnspentCoreAsync(bool unlock, OutPoint[] outpoints)
		{
			if(outpoints == null || outpoints.Length == 0)
				return;
			var parameters = new List<object>();
			parameters.Add(unlock);
			var array = new JArray();
			parameters.Add(array);
			foreach(var outp in outpoints)
			{
				var obj = new JObject();
				obj["txid"] = outp.Hash.ToString();
				obj["vout"] = outp.N;
				array.Add(obj);
			}
			await SendCommandAsync("lockunspent", parameters.ToArray()).ConfigureAwait(false);
		}

		public BlockHeader GetBlockHeader(int height)
		{
			var hash = GetBlockHash(height);
			return GetBlockHeader(hash);
		}
		public async Task<BlockHeader> GetBlockHeaderAsync(int height)
		{
			var hash = await GetBlockHashAsync(height).ConfigureAwait(false);
			return await GetBlockHeaderAsync(hash).ConfigureAwait(false);
		}

		/// <summary>
		/// Get the a whole block
		/// </summary>
		/// <param name="blockId"></param>
		/// <returns></returns>
		public async Task<Block> GetBlockAsync(Uint256 blockId)
		{
			var resp = await SendCommandAsync("getblock", blockId.ToString(), false).ConfigureAwait(false);
			return new Block(Encoders.Hex.DecodeData(resp.Result.ToString()));
		}

		/// <summary>
		/// Get the a whole block
		/// </summary>
		/// <param name="blockId"></param>
		/// <returns></returns>
		public  Block GetBlock(Uint256 blockId)
		{
			try
			{
				return GetBlockAsync(blockId).Result;
			}
			catch(AggregateException aex)
			{
				ExceptionDispatchInfo.Capture(aex.InnerException).Throw();
				throw;
			}
		}

		public BlockHeader GetBlockHeader(Uint256 blockHash)
		{
			var resp = SendCommand("getblock", blockHash.ToString());
			return ParseBlockHeader(resp);
		}
		public async Task<BlockHeader> GetBlockHeaderAsync(Uint256 blockHash)
		{
			var resp = await SendCommandAsync("getblock", blockHash.ToString()).ConfigureAwait(false);
			return ParseBlockHeader(resp);
		}

		private BlockHeader ParseBlockHeader(RpcResponse resp)
		{
			var header = new BlockHeader();
			header.Version = (int)resp.Result["version"];
			header.Nonce = (uint)resp.Result["nonce"];
			header.Bits = new Target(Encoders.Hex.DecodeData((string)resp.Result["bits"]));
			if(resp.Result["previousblockhash"] != null)
			{
				header.HashPrevBlock = new Uint256(Encoders.Hex.DecodeData((string)resp.Result["previousblockhash"]), false);
			}
			if(resp.Result["time"] != null)
			{
				header.BlockTime = Utils.UnixTimeToDateTime((uint)resp.Result["time"]);
			}
			if(resp.Result["merkleroot"] != null)
			{
				header.HashMerkleRoot = new Uint256(Encoders.Hex.DecodeData((string)resp.Result["merkleroot"]), false);
			}
			return header;
		}

		/// <summary>
		/// GetTransactions only returns on txn which are not entirely spent unless you run bitcoinq with txindex=1.
		/// </summary>
		/// <param name="blockHash"></param>
		/// <returns></returns>
		public IEnumerable<Transaction> GetTransactions(Uint256 blockHash)
		{
			if(blockHash == null)
				throw new ArgumentNullException("blockHash");

			var resp = SendCommand("getblock", blockHash.ToString());

			var tx = resp.Result["tx"] as JArray;
			if(tx != null)
			{
				foreach(var item in tx)
				{
					var result = GetRawTransaction(new Uint256(item.ToString()), false);
					if(result != null)
						yield return result;
				}
			}

		}

		public IEnumerable<Transaction> GetTransactions(int height)
		{
			return GetTransactions(GetBlockHash(height));
		}

		public Uint256 GetBlockHash(int height)
		{
			var resp = SendCommand("getblockhash", height);
			return new Uint256(resp.Result.ToString());
		}

		public async Task<Uint256> GetBlockHashAsync(int height)
		{
			var resp = await SendCommandAsync("getblockhash", height).ConfigureAwait(false);
			return new Uint256(resp.Result.ToString());
		}


		public int GetBlockCount()
		{
			return (int)SendCommand("getblockcount").Result;
		}
		public async Task<int> GetBlockCountAsync()
		{
			return (int)(await SendCommandAsync("getblockcount").ConfigureAwait(false)).Result;
		}

		public Uint256[] GetRawMempool()
		{
			var result = SendCommand("getrawmempool");
			var array = (JArray)result.Result;
			return array.Select(o => (string)o).Select(s => new Uint256(s)).ToArray();
		}
		public async Task<Uint256[]> GetRawMempoolAsync()
		{
			var result = await SendCommandAsync("getrawmempool").ConfigureAwait(false);
			var array = (JArray)result.Result;
			return array.Select(o => (string)o).Select(s => new Uint256(s)).ToArray();
		}

		public IEnumerable<BitcoinSecret> ListSecrets()
		{
			foreach(var grouping in ListAddressGroupings())
			{
				yield return DumpPrivKey(grouping.PublicAddress);
				foreach(var change in grouping.ChangeAddresses)
					yield return DumpPrivKey(change.Address);
			}
		}

		public IEnumerable<AddressGrouping> ListAddressGroupings()
		{
			var result = SendCommand(RpcOperations.Listaddressgroupings);
			var array = (JArray)result.Result;
			foreach(var group in array.Children<JArray>())
			{
				var grouping = new AddressGrouping();
				grouping.PublicAddress = BitcoinAddress.Create(group[0][0].ToString());
				grouping.Amount = Money.Coins(group[0][1].Value<decimal>());
				grouping.Account = group[0][2].ToString();

				foreach(var subgroup in group.Skip(1))
				{
					var change = new ChangeAddress();
					change.Address = BitcoinAddress.Create(subgroup[0].ToString());
					change.Amount = Money.Coins(subgroup[1].Value<decimal>());
					grouping.ChangeAddresses.Add(change);
				}

				yield return grouping;
			}
		}
		

		public IEnumerable<RpcAccount> ListAccounts()
		{
			var result = SendCommand(RpcOperations.Listaccounts);
			var obj = (JObject)result.Result;
			foreach(var prop in obj.Properties())
			{
				yield return new RpcAccount()
				{
					AccountName = prop.Name,
					Amount = Money.Coins((decimal)prop.Value)
				};
			}
		}


	}
}
