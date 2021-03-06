﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading.Tasks;
using ChainUtils.DataEncoders;
#if !NOSQLITE
using System.Data;
using System.Data.SQLite;
#endif

namespace ChainUtils
{
	public abstract class NoSqlRepository
	{

		public Task PutAsync(string key, IBitcoinSerializable obj)
		{
			return PutBytes(key, obj == null ? null : obj.ToBytes());
		}

		public void Put(string key, IBitcoinSerializable obj)
		{
			try
			{
				PutAsync(key, obj).Wait();
			}
			catch(AggregateException aex)
			{
				ExceptionDispatchInfo.Capture(aex.InnerException).Throw();
			}
		}

		public async Task<T> GetAsync<T>(string key) where T : IBitcoinSerializable, new()
		{
			var data = await GetBytes(key).ConfigureAwait(false);
			if(data == null)
				return default(T);
			var obj = new T();
			obj.ReadWrite(data);
			return obj;
		}

		public T Get<T>(string key) where T : IBitcoinSerializable, new()
		{
			try
			{
				return GetAsync<T>(key).Result;
			}
			catch(AggregateException aex)
			{
				ExceptionDispatchInfo.Capture(aex.InnerException).Throw();
				return default(T);
			}
		}

		public virtual Task PutBatch(IEnumerable<Tuple<string, IBitcoinSerializable>> values)
		{
			return PutBytesBatch(values.Select(s => new Tuple<string, byte[]>(s.Item1, s.Item2 == null ? null : s.Item2.ToBytes())));
		}

		protected abstract Task PutBytesBatch(IEnumerable<Tuple<string, byte[]>> enumerable);
		protected abstract Task<byte[]> GetBytes(string key);

		protected virtual Task PutBytes(string key, byte[] data)
		{
			return PutBytesBatch(new[] { new Tuple<string, byte[]>(key, data) });
		}
	}
#if !NOSQLITE
	public class SqLiteNoSqlRepository : NoSqlRepository
	{
		private readonly SQLiteConnection _connection;
		public SqLiteNoSqlRepository(string fileName, bool? createNew = null)
		{
			if(createNew.HasValue && createNew.Value)
				if(File.Exists(fileName))
					File.Delete(fileName);

			var builder = new SQLiteConnectionStringBuilder();
			builder.DataSource = fileName;
			builder.BaseSchemaName = "";

			if(!File.Exists(fileName))
			{
				if(createNew.HasValue && !createNew.Value)
					throw new FileNotFoundException(fileName);
				SQLiteConnection.CreateFile(fileName);
				_connection = new SQLiteConnection(builder.ToString());
				_connection.Open();

				var command = _connection.CreateCommand();
				command.CommandText = "Create Table Store(Key TEXT UNIQUE, Data BLOB)";
				command.ExecuteNonQuery();
			}
			else
			{
				_connection = new SQLiteConnection(builder.ToString());
				_connection.Open();
			}
		}
		protected override async Task<byte[]> GetBytes(string key)
		{
			var command = _connection.CreateCommand();
			command.CommandText = "SELECT Data FROM Store WHERE Key = @key";
			command.Parameters.Add("@key", DbType.String).Value = key;
			using(var reader = await command.ExecuteReaderAsync().ConfigureAwait(false))
			{
				if(!await reader.ReadAsync().ConfigureAwait(false))
					return null;
				return GetBytes(reader as SQLiteDataReader);
			}
		}
		static byte[] GetBytes(SQLiteDataReader reader)
		{
			const int chunkSize = 2 * 1024;
			var buffer = new byte[chunkSize];
			long bytesRead;
			long fieldOffset = 0;
			using(var stream = new MemoryStream())
			{
				while((bytesRead = reader.GetBytes(0, fieldOffset, buffer, 0, buffer.Length)) > 0)
				{
					stream.Write(buffer, 0, (int)bytesRead);
					fieldOffset += bytesRead;
				}
				return stream.ToArray();
			}
		}

		void BuildCommand(SQLiteCommand command, StringBuilder commandBuilder, int i, Tuple<string, byte[]> kv)
		{
			if(kv.Item2 != null)
			{
				//Sql injection possible, but parameter binding took as much time as writing in the database file
				commandBuilder.AppendLine("INSERT OR REPLACE INTO Store (Key,Data) VALUES ('" + kv.Item1 + "', X'" + Encoders.Hex.EncodeData(kv.Item2) + "');");
			}
			else
			{
				commandBuilder.AppendLine("Delete from Store where Key=@key" + i + ";");
				command.Parameters.Add("@key" + i, DbType.String).Value = kv.Item1;
			}
		}

		protected override Task PutBytesBatch(IEnumerable<Tuple<string, byte[]>> enumerable)
		{
			var command = _connection.CreateCommand();
			var commandString = new StringBuilder();
			var i = 0;
			commandString.AppendLine("BEGIN TRANSACTION;");
			foreach(var kv in enumerable)
			{
				BuildCommand(command, commandString, i, kv);
				i++;
			}
			commandString.AppendLine("END TRANSACTION;");
			command.CommandText = commandString.ToString();
			if(i == 0)
				return Task.FromResult(true);
			return command.ExecuteNonQueryAsync();
		}
	}
#endif
}
