using System;
using System.Linq;
using ChainUtils.DataEncoders;

namespace ChainUtils
{
	public abstract class Base58Data
	{
		protected byte[] VchData = new byte[0];
		protected byte[] VchVersion = new byte[0];
		protected string WifData = "";
		private Network _network;
		public Network Network
		{
			get
			{
				return _network;
			}
		}

		public Base58Data(string base64, Network expectedNetwork = null)
		{
			_network = expectedNetwork;
			SetString(base64);
		}
		public Base58Data(byte[] rawBytes, Network network)
		{
			if(network == null)
				throw new ArgumentNullException("network");
			_network = network;
			SetData(rawBytes);
		}

		public static Base58Data GetFromBase58Data(string base58, Network expectedNetwork = null)
		{
			return Network.CreateFromBase58Data(base58, expectedNetwork);
		}

		private void SetString(string psz)
		{
			if(_network == null)
			{
				_network = Network.GetNetworkFromBase58Data(psz);
				if(_network == null)
					throw new FormatException("Invalid " + GetType().Name);
			}

			var vchTemp = Encoders.Base58Check.DecodeData(psz);
			var expectedVersion = _network.GetVersionBytes(Type);


			VchVersion = vchTemp.Take((int)expectedVersion.Length).ToArray();
			if(!Utils.ArrayEqual(VchVersion, expectedVersion))
				throw new FormatException("The version prefix does not match the expected one " + String.Join(",", expectedVersion));

			VchData = vchTemp.Skip((int)expectedVersion.Length).ToArray();
			WifData = psz;

			if(!IsValid)
				throw new FormatException("Invalid " + GetType().Name);

		}


		private void SetData(byte[] vchData)
		{
			this.VchData = vchData;
			VchVersion = _network.GetVersionBytes(Type);
			WifData = Encoders.Base58Check.EncodeData(VchVersion.Concat(vchData).ToArray());

			if(!IsValid)
				throw new FormatException("Invalid " + GetType().Name);
		}


		protected virtual bool IsValid
		{
			get
			{
				return true;
			}
		}

		public abstract Base58Type Type
		{
			get;
		}



		public string ToWif()
		{
			return WifData;
		}
		public byte[] ToBytes()
		{
			return VchData.ToArray();
		}
		public override string ToString()
		{
			return WifData;
		}

		public override bool Equals(object obj)
		{
			var item = obj as Base58Data;
			if(item == null)
				return false;
			return ToString().Equals(item.ToString());
		}
		public static bool operator ==(Base58Data a, Base58Data b)
		{
			if(ReferenceEquals(a, b))
				return true;
			if(((object)a == null) || ((object)b == null))
				return false;
			return a.ToString() == b.ToString();
		}

		public static bool operator !=(Base58Data a, Base58Data b)
		{
			return !(a == b);
		}

		public override int GetHashCode()
		{
			return ToString().GetHashCode();
		}
	}
}
