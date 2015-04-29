using System;
using System.Text;
using System.Threading.Tasks;
using ChainUtils.Crypto;
#if !USEBC
#endif

namespace ChainUtils
{
	public class UnsecureRandom : IRandom
	{
		Random _rand = new Random();
		#region IRandom Members

		public void GetBytes(byte[] output)
		{
			lock(_rand)
			{
				_rand.NextBytes(output);
			}
		}

		#endregion

	}


	public interface IRandom
	{
		void GetBytes(byte[] output);
	}

	public partial class RandomUtils
	{
		public static IRandom Random
		{
			get;
			set;
		}

		public static byte[] GetBytes(int length)
		{
			var data = new byte[length];
			if(Random == null)
				throw new InvalidOperationException("You must set the RNG (RandomUtils.Random) before generating random numbers");
			Random.GetBytes(data);
			PushEntropy(data);
			return data;
		}

		private static void PushEntropy(byte[] data)
		{
			if(_additionalEntropy == null || data.Length == 0)
				return;
			var pos = _entropyIndex;
			var entropy = _additionalEntropy;
			for(var i = 0 ; i < data.Length ; i++)
			{
				data[i] ^= entropy[pos % 32];
				pos++;
			}
			entropy = Hashes.SHA256(data);
			for(var i = 0 ; i < data.Length ; i++)
			{
				data[i] ^= entropy[pos % 32];
				pos++;
			}
			_entropyIndex = pos % 32;
		}

		static volatile byte[] _additionalEntropy = null;
		static volatile int _entropyIndex = 0;

		public static void AddEntropy(string data)
		{
			if(data == null)
				throw new ArgumentNullException("data");
			AddEntropy(Encoding.UTF8.GetBytes(data));
		}

		public static void AddEntropy(byte[] data)
		{
			if(data == null)
				throw new ArgumentNullException("data");
			var entropy = Hashes.SHA256(data);
			if(_additionalEntropy == null)
				_additionalEntropy = entropy;
			else
			{
				for(var i = 0 ; i < 32 ; i++)
				{
					_additionalEntropy[i] ^= entropy[i];
				}
				_additionalEntropy = Hashes.SHA256(_additionalEntropy);
			}
		}

		public static uint GetUInt32()
		{
			return BitConverter.ToUInt32(GetBytes(sizeof(uint)), 0);
		}

		public static int GetInt32()
		{
			return BitConverter.ToInt32(GetBytes(sizeof(int)), 0);
		}
		public static ulong GetUInt64()
		{
			return BitConverter.ToUInt64(GetBytes(sizeof(ulong)), 0);
		}

		public static long GetInt64()
		{
			return BitConverter.ToInt64(GetBytes(sizeof(long)), 0);
		}

		public static void GetBytes(byte[] output)
		{
			if(Random == null)
				throw new InvalidOperationException("You must set the RNG (RandomUtils.Random) before generating random numbers");
			Random.GetBytes(output);
			PushEntropy(output);
		}

		internal static Task<byte[]> GetRandomBytesAsync(int p)
		{
			throw new NotImplementedException();
		}
	}
}
