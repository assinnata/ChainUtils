using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using ChainUtils.DataEncoders;
using ChainUtils.OpenAsset;
using ChainUtils.Protocol;
using ChainUtils.Stealth;

namespace ChainUtils
{
	public class DnsSeedData
	{
		string _name, _host;
		public string Name
		{
			get
			{
				return _name;
			}
		}
		public string Host
		{
			get
			{
				return _host;
			}
		}
		public DnsSeedData(string name, string host)
		{
			this._name = name;
			this._host = host;
		}
#if !PORTABLE
		IPAddress[] _addresses = null;
		public IPAddress[] GetAddressNodes()
		{
			if(_addresses != null)
				return _addresses;
			return Dns.GetHostAddresses(_host);
		}
#endif
		public override string ToString()
		{
			return _name + " (" + _host + ")";
		}
	}
	public enum Base58Type
	{
		PubkeyAddress,
		ScriptAddress,
		SecretKey,
		ExtPublicKey,
		ExtSecretKey,
		EncryptedSecretKeyEc,
		EncryptedSecretKeyNoEc,
		PassphraseCode,
		ConfirmationCode,
		StealthAddress,
		AssetId,
		ColoredAddress,
		MaxBase58Types,
	};
	public class Network
	{
		byte[][] _base58Prefixes = new byte[12][];


		string[] _pnSeed = new[] { "127.0.0.1" };


		uint _magic;
		byte[] _vAlertPubKey;
		PubKey _alertPubKey;
		public PubKey AlertPubKey
		{
			get
			{
				if(_alertPubKey == null)
				{
					_alertPubKey = new PubKey(_vAlertPubKey);
				}
				return _alertPubKey;
			}
		}

#if !PORTABLE
		List<DnsSeedData> _vSeeds = new List<DnsSeedData>();
		List<NetworkAddress> _vFixedSeeds = new List<NetworkAddress>();
#endif
		Block _genesis = new Block();

		private int _nRpcPort;
		public int RpcPort
		{
			get
			{
				return _nRpcPort;
			}
		}

		private Uint256 _hashGenesisBlock;

		private int _nDefaultPort;
		public int DefaultPort
		{
			get
			{
				return _nDefaultPort;
			}
		}



		static Network _regTest;
		public static Network RegTest
		{
			get
			{
				if(_regTest == null)
				{
					var instance = new Network();
					instance.InitReg();
					_regTest = instance;
				}
				return _regTest;
			}
		}

		private void InitReg()
		{
			InitTest();
			_magic = 0xDAB5BFFA;
			_name = "RegTest";
			_nSubsidyHalvingInterval = 150;
			_proofOfLimit = new Target(~new Uint256(0) >> 1);
			_genesis.Header.BlockTime = Utils.UnixTimeToDateTime(1296688602);
			_genesis.Header.Bits = 0x207fffff;
			_genesis.Header.Nonce = 2;
			_hashGenesisBlock = _genesis.GetHash();
			_nDefaultPort = 18444;
			//strDataDir = "regtest";
			Assert(_hashGenesisBlock == new Uint256("0x0f9188f13cb7b2c71f2a335e3a4fc328bf5beb436012afca590b1a11466e2206"));

#if !PORTABLE
			_vSeeds.Clear();  // Regtest mode doesn't have any DNS seeds.
#endif
		}


		static Network _main;
		private Target _proofOfLimit;
		private int _nSubsidyHalvingInterval;
		private string _name;

		public string Name
		{
			get
			{
				return _name;
			}
		}

		public static Network Main
		{
			get
			{
				if(_main == null)
				{
					var instance = new Network();
					instance.InitMain();
					_main = instance;
				}
				return _main;
			}
		}

		private void InitMain()
		{
			SpendableCoinbaseDepth = 1;
			_name = "Main";
			// The message start string is designed to be unlikely to occur in normal data.
			// The characters are rarely used upper ASCII, not valid as UTF-8, and produce
			// a large 4-byte int at any alignment.
			_magic = 0xD9B4BEF9;
			_vAlertPubKey = Encoders.Hex.DecodeData("04fc9702847840aaf195de8442ebecedf5b095cdbb9bc716bda9110971b28a49e0ead8564ff0db22209e0374782c093bb899692d524e9d6a6956e7c5ecbcd68284");
			_nDefaultPort = 44001;
			_nRpcPort = 33001;
			/*
            _ProofOfLimit = new Target(~new uint256(0) >> 32);
			nSubsidyHalvingInterval = 210000;

			Transaction txNew = new Transaction();
			txNew.Version = 1;
			txNew.Inputs.Add(new TxIn());
			txNew.Outputs.Add(new TxOut());
			txNew.Inputs[0].ScriptSig = new Script(DataEncoders.Encoders.Hex.DecodeData("04ffff001d0104455468652054696d65732030332f4a616e2f32303039204368616e63656c6c6f72206f6e206272696e6b206f66207365636f6e64206261696c6f757420666f722062616e6b73"));
			txNew.Outputs[0].Value = 50 * Money.COIN;
			txNew.Outputs[0].ScriptPubKey = new Script() + DataEncoders.Encoders.Hex.DecodeData("04678afdb0fe5548271967f1a67130b7105cd6a828e03909a67962e0ea1f61deb649f6bc3f4cef38c4f35504e51ec112de5c384df7ba0b8d578a4c702b6bf11d5f") + OpcodeType.OP_CHECKSIG;
			genesis.Transactions.Add(txNew);
			genesis.Header.HashPrevBlock = 0;
			genesis.UpdateMerkleRoot();
			genesis.Header.Version = 1;
			genesis.Header.BlockTime = Utils.UnixTimeToDateTime(1231006505);
			genesis.Header.Bits = 0x1d00ffff;
			genesis.Header.Nonce = 2083236893;

			hashGenesisBlock = genesis.GetHash();
            */
			//assert(hashGenesisBlock == new uint256("0x000000000019d6689c085ae165831e934ff763ae46a2a6c172b3f1b60a8ce26f"));
			//assert(genesis.Header.HashMerkleRoot == new uint256("0x4a5e1e4baab89f3a32518a88c31bc87f618f76673e2cc77ab2127b7afdeda33b"));
#if !PORTABLE
			//vSeeds.Add(new DNSSeedData("bitcoin.sipa.be", "seed.bitcoin.sipa.be"));
#endif
			_base58Prefixes[(int)Base58Type.PubkeyAddress] = new byte[] { (0x41) };
			_base58Prefixes[(int)Base58Type.ScriptAddress] = new byte[] { (5) };
			_base58Prefixes[(int)Base58Type.SecretKey] = new byte[] { (193) };
			_base58Prefixes[(int)Base58Type.EncryptedSecretKeyNoEc] = new byte[] { 0x01, 0x42 };
			_base58Prefixes[(int)Base58Type.EncryptedSecretKeyEc] = new byte[] { 0x01, 0x43 };
			_base58Prefixes[(int)Base58Type.ExtPublicKey] = new byte[] { (0x04), (0x88), (0xB2), (0x1E) };
			_base58Prefixes[(int)Base58Type.ExtSecretKey] = new byte[] { (0x04), (0x88), (0xAD), (0xE4) };
			_base58Prefixes[(int)Base58Type.PassphraseCode] = new byte[] { 0x2C, 0xE9, 0xB3, 0xE1, 0xFF, 0x39, 0xE2 };
			_base58Prefixes[(int)Base58Type.ConfirmationCode] = new byte[] { 0x64, 0x3B, 0xF6, 0xA8, 0x9A };
			_base58Prefixes[(int)Base58Type.StealthAddress] = new byte[] { 0x2a };
			_base58Prefixes[(int)Base58Type.AssetId] = new byte[] { 23 };
			_base58Prefixes[(int)Base58Type.ColoredAddress] = new byte[] { 0x13 };

			// Convert the pnSeeds array into usable address objects.
			var rand = new Random();
			var nOneWeek = TimeSpan.FromDays(7);
#if !PORTABLE
			for(var i = 0 ; i < _pnSeed.Length ; i++)
			{
				// It'll only connect to one or two seed nodes because once it connects,
				// it'll get a pile of addresses with newer timestamps.
				var ip = IPAddress.Parse(_pnSeed[i]);
				var addr = new NetworkAddress();
				// Seed nodes are given a random 'last seen time' of between one and two
				// weeks ago.
				addr.Time = DateTime.UtcNow - (TimeSpan.FromSeconds(rand.NextDouble() * nOneWeek.TotalSeconds)) - nOneWeek;
				addr.Endpoint = new IPEndPoint(ip, DefaultPort);
				_vFixedSeeds.Add(addr);
			}
#endif
		}


		static Network _testNet;
		public static Network TestNet
		{
			get
			{
				if(_testNet == null)
				{
					var instance = new Network();
					instance.InitTest();
					_testNet = instance;
				}
				return _testNet;
			}
		}
		private void InitTest()
		{
			InitMain();
			_name = "TestNet";
			_magic = 0x0709110B;

			_vAlertPubKey = Encoders.Hex.DecodeData("04302390343f91cc401d56d68b123028bf52e5fca1939df127f63c6467cdf9c8e2c14b61104cf817d0b780da337893ecc4aaff1309e536162dabbdb45200ca2b0a");
			_nDefaultPort = 18333;
			_nRpcPort = 18332;
			//strDataDir = "testnet3";

			// Modify the testnet genesis block so the timestamp is valid for a later start.
			_genesis.Header.BlockTime = Utils.UnixTimeToDateTime(1296688602);
			_genesis.Header.Nonce = 414098458;
			_hashGenesisBlock = _genesis.GetHash();
			Assert(_hashGenesisBlock == new Uint256("0x000000000933ea01ad0ee984209779baaec3ced90fa3f408719526f8d77f4943"));

#if !PORTABLE
			_vFixedSeeds.Clear();
			_vSeeds.Clear();
			_vSeeds.Add(new DnsSeedData("bitcoin.petertodd.org", "testnet-seed.bitcoin.petertodd.org"));
			_vSeeds.Add(new DnsSeedData("bluematt.me", "testnet-seed.bluematt.me"));
#endif
			_base58Prefixes[(int)Base58Type.PubkeyAddress] = new byte[] { (111) };
			_base58Prefixes[(int)Base58Type.ScriptAddress] = new byte[] { (196) };
			_base58Prefixes[(int)Base58Type.SecretKey] = new byte[] { (239) };
			_base58Prefixes[(int)Base58Type.ExtPublicKey] = new byte[] { (0x04), (0x35), (0x87), (0xCF) };
			_base58Prefixes[(int)Base58Type.ExtSecretKey] = new byte[] { (0x04), (0x35), (0x83), (0x94) };
			_base58Prefixes[(int)Base58Type.StealthAddress] = new byte[] { 0x2b };
			_base58Prefixes[(int)Base58Type.AssetId] = new byte[] { 115 };
		}

		private static void Assert(bool v)
		{
			if(!v)
				throw new InvalidOperationException("Invalid network");
		}

		public BitcoinSecret CreateBitcoinSecret(string base58)
		{
			return new BitcoinSecret(base58, this);
		}

		/// <summary>
		/// Create a bitcoin address from base58 data, return a BitcoinAddress or BitcoinScriptAddress
		/// </summary>
		/// <param name="base58">base58 address</param>
		/// <exception cref="System.FormatException">Invalid base58 address</exception>
		/// <returns>BitcoinScriptAddress, BitcoinAddress</returns>
		public BitcoinAddress CreateBitcoinAddress(string base58)
		{
			var type = GetBase58Type(base58);
			if(!type.HasValue)
				throw new FormatException("Invalid Base58 version");
			if(type == Base58Type.PubkeyAddress)
				return new BitcoinAddress(base58, this);
			if(type == Base58Type.ScriptAddress)
				return new BitcoinScriptAddress(base58, this);
			throw new FormatException("Invalid Base58 version");
		}

		public BitcoinScriptAddress CreateBitcoinScriptAddress(string base58)
		{
			return new BitcoinScriptAddress(base58, this);
		}

		private Base58Type? GetBase58Type(string base58)
		{
			var bytes = Encoders.Base58Check.DecodeData(base58);
			for(var i = 0 ; i < _base58Prefixes.Length ; i++)
			{
				var prefix = _base58Prefixes[i];
				if(bytes.Length < prefix.Length)
					continue;
				if(Utils.ArrayEqual(bytes, 0, prefix, 0, prefix.Length))
					return (Base58Type)i;
			}
			return null;
		}


		public static Network GetNetworkFromBase58Data(string base58)
		{
			foreach(var network in GetNetworks())
			{
				var type = network.GetBase58Type(base58);
				if(type.HasValue)
				{
					if(type.Value == Base58Type.ColoredAddress)
					{
						var raw = Encoders.Base58Check.DecodeData(base58);
						var version = network.GetVersionBytes(type.Value);
						raw = raw.Skip(version.Length).ToArray();
						base58 = Encoders.Base58Check.EncodeData(raw);
						return GetNetworkFromBase58Data(base58);
					}
					return network;
				}
			}
			return null;
		}

		/// <summary>
		/// Find automatically the data type and the network to which belong the base58 data
		/// </summary>
		/// <param name="base58">base58 data</param>
		/// <exception cref="System.FormatException">Invalid base58 data</exception>
		public static Base58Data CreateFromBase58Data(string base58, Network expectedNetwork = null)
		{
			var invalidNetwork = false;
			foreach(var network in GetNetworks())
			{
				var type = network.GetBase58Type(base58);
				if(type.HasValue)
				{
					if(type.Value == Base58Type.ColoredAddress)
					{
						var inner = BitcoinAddress.Create(BitcoinColoredAddress.GetWrappedBase58(base58, network));
						if(inner.Network != network)
							continue;
					}
					if(expectedNetwork != null && network != expectedNetwork)
					{
						invalidNetwork = true;
						continue;
					}
					return network.CreateBase58Data(type.Value, base58);
				}
			}
			if(invalidNetwork)
				throw new FormatException("Invalid network");
			throw new FormatException("Invalid base58 data");
		}

		public static T CreateFromBase58Data<T>(string base58, Network expectedNetwork = null) where T : Base58Data
		{
			var result = CreateFromBase58Data(base58, expectedNetwork) as T;
			if(result == null)
				throw new FormatException("Invalid base58 data");
			return result;
		}

		public T Parse<T>(string base58) where T : Base58Data
		{
			var type = GetBase58Type(base58);
			if(type.HasValue)
			{
				var result = CreateBase58Data(type.Value, base58) as T;
				if(result == null)
					throw new FormatException("Invalid base58 data");
				return result;
			}
			throw new FormatException("Invalid base58 data");
		}

		public Base58Data CreateBase58Data(Base58Type type, string base58)
		{
			if(type == Base58Type.ExtPublicKey)
				return CreateBitcoinExtPubKey(base58);
			if(type == Base58Type.ExtSecretKey)
				return CreateBitcoinExtKey(base58);
			if(type == Base58Type.PubkeyAddress)
				return CreateBitcoinAddress(base58);
			if(type == Base58Type.ScriptAddress)
				return CreateBitcoinScriptAddress(base58);
			if(type == Base58Type.SecretKey)
				return CreateBitcoinSecret(base58);
			if(type == Base58Type.ConfirmationCode)
				return CreateConfirmationCode(base58);
			if(type == Base58Type.EncryptedSecretKeyEc)
				return CreateEncryptedKeyEc(base58);
			if(type == Base58Type.EncryptedSecretKeyNoEc)
				return CreateEncryptedKeyNoEc(base58);
			if(type == Base58Type.PassphraseCode)
				return CreatePassphraseCode(base58);
			if(type == Base58Type.StealthAddress)
				return CreateStealthAddress(base58);
			if(type == Base58Type.AssetId)
				return CreateAssetId(base58);
			if(type == Base58Type.ColoredAddress)
				return CreateColoredAddress(base58);
			throw new NotSupportedException("Invalid Base58Data type : " + type.ToString());
		}

		private BitcoinColoredAddress CreateColoredAddress(string base58)
		{
			return new BitcoinColoredAddress(base58, this);
		}

		public BitcoinAssetId CreateAssetId(string base58)
		{
			return new BitcoinAssetId(base58, this);
		}

		public BitcoinStealthAddress CreateStealthAddress(string base58)
		{
			return new BitcoinStealthAddress(base58, this);
		}

		private BitcoinPassphraseCode CreatePassphraseCode(string base58)
		{
			return new BitcoinPassphraseCode(base58, this);
		}

		private BitcoinEncryptedSecretNoEc CreateEncryptedKeyNoEc(string base58)
		{
			return new BitcoinEncryptedSecretNoEc(base58, this);
		}

		private BitcoinEncryptedSecretEc CreateEncryptedKeyEc(string base58)
		{
			return new BitcoinEncryptedSecretEc(base58, this);
		}

		private Base58Data CreateConfirmationCode(string base58)
		{
			return new BitcoinConfirmationCode(base58, this);
		}

		private Base58Data CreateBitcoinExtPubKey(string base58)
		{
			return new BitcoinExtPubKey(base58, this);
		}


		public BitcoinExtKey CreateBitcoinExtKey(ExtKey key)
		{
			return new BitcoinExtKey(key, this);
		}

		public BitcoinExtPubKey CreateBitcoinExtPubKey(ExtPubKey pubkey)
		{
			return new BitcoinExtPubKey(pubkey, this);
		}

		public BitcoinExtKey CreateBitcoinExtKey(string base58)
		{
			return new BitcoinExtKey(base58, this);
		}

		public byte[] GetVersionBytes(Base58Type type)
		{
			return _base58Prefixes[(int)type].ToArray();
		}

		public ValidationState CreateValidationState()
		{
			return new ValidationState(this);
		}

		public override string ToString()
		{
			return _name;
		}

		public Target ProofOfWorkLimit
		{
			get
			{
				return _proofOfLimit;
			}
		}

		public Block GetGenesis()
		{
			var block = new Block();
			block.ReadWrite(_genesis.ToBytes());
			return block;
		}

		public static IEnumerable<Network> GetNetworks()
		{
			yield return Main;
			yield return TestNet;
			yield return RegTest;
		}

		public static Network GetNetwork(uint magic)
		{
			return GetNetworks().FirstOrDefault(r => r.Magic == magic);
		}

		public static Network GetNetwork(string name)
		{
			name = name.ToLowerInvariant();
			switch(name)
			{
				case "main":
					return Main;
				case "testnet":
				case "testnet3":
					return TestNet;
				case "reg":
				case "regtest":
					return RegTest;
				default:
					throw new ArgumentException(String.Format("Invalid network name '{0}'", name));
			}
		}

		public BitcoinSecret CreateBitcoinSecret(Key key)
		{
			return new BitcoinSecret(key, this);
		}

		public BitcoinAddress CreateBitcoinAddress(TxDestination dest)
		{
			if(dest == null)
				throw new ArgumentNullException("dest");
			if(dest is ScriptId)
				return CreateBitcoinScriptAddress((ScriptId)dest);
			if(dest is KeyId)
				return new BitcoinAddress((KeyId)dest, this);
			throw new ArgumentException("Invalid dest type", "dest");
		}

		private BitcoinAddress CreateBitcoinScriptAddress(ScriptId scriptId)
		{
			return new BitcoinScriptAddress(scriptId, this);
		}
#if !PORTABLE
		public Message ParseMessage(byte[] bytes, ProtocolVersion version = ProtocolVersion.PROTOCOL_VERSION)
		{
			var bstream = new BitcoinStream(bytes);
			var message = new Message();
			using(bstream.ProtocolVersionScope(version))
			{
				bstream.ReadWrite(ref message);
			}
			if(message.Magic != _magic)
				throw new FormatException("Unexpected magic field in the message");
			return message;
		}

		public IEnumerable<NetworkAddress> SeedNodes
		{
			get
			{
				return _vFixedSeeds;
			}
		}
		public IEnumerable<DnsSeedData> DnsSeeds
		{
			get
			{
				return _vSeeds;
			}
		}
#endif
		public byte[] _MagicBytes;
		public byte[] MagicBytes
		{
			get
			{
				if(_MagicBytes == null)
				{
					var bytes = new[] 
					{ 
						(byte)Magic,
						(byte)(Magic >> 8),
						(byte)(Magic >> 16),
						(byte)(Magic >> 24)
					};
					_MagicBytes = bytes;
				}
				return _MagicBytes;
			}
		}
		public uint Magic
		{
			get
			{
				return _magic;
			}
		}

		public Money GetReward(int nHeight)
		{
			long nSubsidy = new Money(50 * Money.Coin);
			var halvings = nHeight / _nSubsidyHalvingInterval;

			// Force block reward to zero when right shift is undefined.
			if(halvings >= 64)
				return Money.Zero;

			// Subsidy is cut in half every 210,000 blocks which will occur approximately every 4 years.
			nSubsidy >>= halvings;

			return new Money(nSubsidy);
		}

		public bool ReadMagic(Stream stream, CancellationToken cancellation)
		{
			var bytes = new byte[1];
			for(var i = 0 ; i < MagicBytes.Length ; i++)
			{
				i = Math.Max(0, i);
				cancellation.ThrowIfCancellationRequested();

				var read = stream.ReadEx(bytes, 0, bytes.Length, cancellation);
				if(read == -1)
					return false;
				if(read != 1)
					i--;
				else if(_MagicBytes[i] != bytes[0])
					i = -1;
			}
			return true;
		}

		public int SpendableCoinbaseDepth
		{
			get;
			private set;
		}
	}
}
