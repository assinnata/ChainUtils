using System.Security.Cryptography;

namespace ChainUtils
{
	public class RngCryptoServiceProviderRandom : IRandom
	{
		readonly RNGCryptoServiceProvider _instance;
		public RngCryptoServiceProviderRandom()
		{
			_instance = new RNGCryptoServiceProvider();
		}
		#region IRandom Members

		public void GetBytes(byte[] output)
		{
			_instance.GetBytes(output);
		}

		#endregion
	}

	public partial class RandomUtils
	{
		static RandomUtils()
		{
			//Thread safe http://msdn.microsoft.com/en-us/library/system.security.cryptography.rngcryptoserviceprovider(v=vs.110).aspx
			Random = new RngCryptoServiceProviderRandom();
		}
	}
}
