using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ChainUtils
{
	public class InMemoryNoSqlRepository : NoSqlRepository
	{
		Dictionary<string, byte[]> _table = new Dictionary<string, byte[]>();

		protected override Task PutBytesBatch(IEnumerable<Tuple<string, byte[]>> enumerable)
		{
			foreach(var data in enumerable)
			{
				if(data.Item2 == null)
				{
					_table.Remove(data.Item1);
				}
				else
					_table.AddOrReplace(data.Item1, data.Item2);
			}
			return Task.FromResult(true);
		}

		protected override Task<byte[]> GetBytes(string key)
		{
			byte[] result = null;
			_table.TryGetValue(key, out result);
			return Task.FromResult(result);
		}
	}
}
