using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ChainUtils.Protocol;

namespace ChainUtils
{
	public class CachedNoSqlRepository : NoSqlRepository
	{
		class Raw : IBitcoinSerializable
		{
			public Raw()
			{

			}
			public Raw(byte[] data)
			{
				var str = new VarString();
				str.FromBytes(data);
				_data = str.GetString(true);
			}
			private byte[] _data = new byte[0];
			public byte[] Data
			{
				get
				{
					return _data;
				}
			}
			#region IBitcoinSerializable Members

			public void ReadWrite(BitcoinStream stream)
			{
				stream.ReadWriteAsVarString(ref _data);
			}

			#endregion
		}

		public CachedNoSqlRepository(NoSqlRepository inner)
		{
			_innerRepository = inner;
		}
		private readonly NoSqlRepository _innerRepository;
		public NoSqlRepository InnerRepository
		{
			get
			{
				return _innerRepository;
			}
		}
		Dictionary<string, byte[]> _table = new Dictionary<string, byte[]>();
		HashSet<string> _removed = new HashSet<string>();
		HashSet<string> _added = new HashSet<string>();
		ReaderWriterLock _lock = new ReaderWriterLock();

		public override async Task PutBatch(IEnumerable<Tuple<string, IBitcoinSerializable>> values)
		{
			await base.PutBatch(values).ConfigureAwait(false);
			await _innerRepository.PutBatch(values).ConfigureAwait(false);
		}

		protected override Task PutBytesBatch(IEnumerable<Tuple<string, byte[]>> enumerable)
		{
			using(_lock.LockWrite())
			{
				foreach(var data in enumerable)
				{
					if(data.Item2 == null)
					{
						_table.Remove(data.Item1);
						_removed.Add(data.Item1);
						_added.Remove(data.Item1);
					}
					else
					{
						_table.AddOrReplace(data.Item1, data.Item2);
						_removed.Remove(data.Item1);
						_added.Add(data.Item1);
					}
				}
			}
			return Task.FromResult(true);
		}

		protected override async Task<byte[]> GetBytes(string key)
		{
			byte[] result = null;
			bool found;
			using(_lock.LockRead())
			{
				found = _table.TryGetValue(key, out result);
			}
			if(!found)
			{
				var raw = await InnerRepository.GetAsync<Raw>(key).ConfigureAwait(false);
				if(raw != null)
				{
					result = raw.Data;
					using(_lock.LockWrite())
					{
						_table.AddOrReplace(key, raw.Data);
					}
				}
			}
			return result;
		}

		public void Flush()
		{
			using(_lock.LockWrite())
			{
				InnerRepository
					.PutBatch(_removed.Select(k => Tuple.Create<string, IBitcoinSerializable>(k, null))
							  .Concat(_added.Select(k => Tuple.Create<string, IBitcoinSerializable>(k, new Raw(_table[k])))));
				_removed.Clear();
				_added.Clear();
				_table.Clear();
			}
		}
	}
}
