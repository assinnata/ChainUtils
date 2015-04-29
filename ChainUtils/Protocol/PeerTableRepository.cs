#if !NOSOCKET
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Text;
#if !NOSQLITE
using System.Data;
using System.Data.SQLite;
#endif

namespace ChainUtils.Protocol
{
	public abstract class PeerTableRepository
	{
		public PeerTableRepository()
		{
			ValiditySpan = TimeSpan.FromHours(3);
		}
		public TimeSpan ValiditySpan
		{
			get;
			set;
		}



		public abstract IEnumerable<Peer> GetPeers();
		public abstract void WritePeers(IEnumerable<Peer> peer);
		public void WritePeer(Peer peer)
		{
			WritePeers(new[] { peer });
		}
	}

	public class InMemoryPeerTableRepository : PeerTableRepository
	{
		protected Dictionary<string, Peer> Peers = new Dictionary<string, Peer>();
		public override IEnumerable<Peer> GetPeers()
		{
			ClearTable();
			var result = new List<Peer>();
			foreach(var p in Peers.ToArray())
			{
				result.Add(p.Value);
			}
			return result;
		}

		private void ClearTable()
		{
			foreach(var p in Peers.ToArray())
			{
				var ago = p.Value.NetworkAddress.Ago;
				if(ValiditySpan < ago || ago < TimeSpan.FromMinutes(-30))
				{
					Peers.Remove(p.Key);
				}
			}
		}

		public override void WritePeers(IEnumerable<Peer> peers)
		{
			foreach(var peer in peers)
			{
				Peers.AddOrReplace(peer.NetworkAddress.Endpoint.ToString(), peer);
			}
			ClearTable();
		}
	}

#if !NOSQLITE
	public class SqLitePeerTableRepository : PeerTableRepository, IDisposable
	{
		[DataContract]
		class PeerBlob
		{
			public PeerBlob()
			{

			}
			public PeerBlob(Peer peer)
			{
				Origin = (byte)peer.Origin;
				Address = peer.NetworkAddress.Endpoint.Address.ToString();
				Port = peer.NetworkAddress.Endpoint.Port;
				LastSeen = peer.NetworkAddress.Time;
			}
			public Peer ToPeer()
			{
				return new Peer((PeerOrigin)Origin, new NetworkAddress()
				{
					Time = LastSeen,
					Endpoint = new IPEndPoint(IPAddress.Parse(Address), Port)
				});
			}
			[DataMember]
			public byte Origin
			{
				get;
				set;
			}
			[DataMember]
			public string Address
			{
				get;
				set;
			}
			[DataMember]
			public int Port
			{
				get;
				set;
			}
			[DataMember]
			public DateTimeOffset LastSeen
			{
				get;
				set;
			}
		}

		private readonly SQLiteConnection _connection;
		public SqLitePeerTableRepository(string fileName)
		{
			ValiditySpan = TimeSpan.FromDays(1.0);

			var builder = new SQLiteConnectionStringBuilder();
			builder.DataSource = fileName;

			if(!File.Exists(fileName))
			{
				SQLiteConnection.CreateFile(fileName);
				_connection = new SQLiteConnection(builder.ToString());
				_connection.Open();

				var command = _connection.CreateCommand();
				command.CommandText = "Create Table PeerTable(Endpoint TEXT UNIQUE,LastSeen UNSIGNED INTEGER, Data TEXT)";
				command.ExecuteNonQuery();
			}
			else
			{
				_connection = new SQLiteConnection(builder.ToString());
				_connection.Open();
			}

		}

		public override IEnumerable<Peer> GetPeers()
		{
			ClearTable();

			var command = _connection.CreateCommand();
			command.CommandText = "Select * from PeerTable";
			var reader = command.ExecuteReader();
			while(reader.Read())
			{
				yield return Deserialize<PeerBlob>((string)reader["Data"]).ToPeer();
			}
		}

		private void ClearTable()
		{
			var command = _connection.CreateCommand();
			command.CommandText = "Delete from PeerTable Where LastSeen < @d";
			command.Parameters.Add("@d", DbType.UInt64).Value = Utils.DateTimeToUnixTime(DateTimeOffset.UtcNow - ValiditySpan);
			command.ExecuteNonQuery();
		}

		public override void WritePeers(IEnumerable<Peer> peers)
		{
			var command = _connection.CreateCommand();
			var builder = new StringBuilder();
			var i = 0;
			foreach(var peer in peers)
			{
				builder.AppendLine("Insert Or Replace INTO PeerTable(Endpoint, LastSeen,Data) Values(@a" + i + ",@b" + i + ",@c" + i + ");");
				command.Parameters.Add("@a" + i, DbType.String).Value = peer.NetworkAddress.Endpoint.ToString();
				command.Parameters.Add("@b" + i, DbType.UInt64).Value = Utils.DateTimeToUnixTime(peer.NetworkAddress.Time);
				command.Parameters.Add("@c" + i, DbType.String).Value = Serialize(new PeerBlob(peer));
				i++;
			}
			command.CommandText = builder.ToString();
			if(command.CommandText == "")
				return;
			command.ExecuteNonQuery();
			ClearTable();
		}

		public static string Serialize<T>(T obj)
		{
			var seria = new DataContractSerializer(typeof(T));
			var ms = new MemoryStream();
			seria.WriteObject(ms, obj);
			ms.Position = 0;
			return new StreamReader(ms).ReadToEnd();
		}

		public static T Deserialize<T>(string str)
		{
			var seria = new DataContractSerializer(typeof(T));
			var ms = new MemoryStream();
			var writer = new StreamWriter(ms);
			writer.Write(str);
			writer.Flush();
			ms.Position = 0;
			return (T)seria.ReadObject(ms);
		}

	#region IDisposable Members

		public void Dispose()
		{
			_connection.Close();
		}

		#endregion
	}
#endif
}
#endif