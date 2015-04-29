#if !NOSOCKET
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace ChainUtils.Protocol
{
	public enum PeerOrigin : byte
	{
		Manual,
		Addr,
		Advertised,
		DnsSeed,
		HardSeed,
	}
	public class Peer
	{
		public Peer(PeerOrigin origin, NetworkAddress address)
		{
			_origin = origin;
			_networkAddress = address;
		}
		private readonly PeerOrigin _origin;
		public PeerOrigin Origin
		{
			get
			{
				return _origin;
			}
		}
		private readonly NetworkAddress _networkAddress;
		public NetworkAddress NetworkAddress
		{
			get
			{
				return _networkAddress;
			}
		}

		public Peer Clone()
		{
			return new Peer(Origin, NetworkAddress.Clone());
		}
	}
	public class PeerTable : InMemoryPeerTableRepository
	{
		Dictionary<string, Peer> _peerSeeds = new Dictionary<string, Peer>();
		public PeerTable()
		{
			ValiditySpan = TimeSpan.FromHours(3.0);
		}
		public bool Randomize
		{
			get;
			set;
		}




		public Peer[] GetActivePeers(int maxCount)
		{
			maxCount = Math.Min(1000, maxCount);
			var result = new List<Peer>();
			lock(Peers)
			{
				result.AddRange(Peers
									.Select(p => p.Value)
									.Concat(_peerSeeds.Select(p => p.Value))
									.OrderBy(p => p.Origin)
									.ThenBy(p => p.NetworkAddress.Ago)
									.Take(maxCount));
			}
			var shuffled = result.ToArray();
			if(Randomize)
				Utils.Shuffle(shuffled);
			return shuffled;
		}

		public override void WritePeers(IEnumerable<Peer> peers)
		{
			lock(Peers)
			{
				var normalPeers = peers.Where(p => p.Origin != PeerOrigin.DnsSeed && p.Origin != PeerOrigin.HardSeed);
				base.WritePeers(normalPeers);
				var seedPeers = peers.Where(p => p.Origin == PeerOrigin.DnsSeed || p.Origin == PeerOrigin.HardSeed);
				foreach(var s in seedPeers)
				{
					_peerSeeds.AddOrReplace(s.NetworkAddress.Endpoint.ToString(), s);
				}
			}
		}

		public override IEnumerable<Peer> GetPeers()
		{
			lock(Peers)
			{
				return base.GetPeers().Concat(_peerSeeds.Select(s => s.Value)).ToList();
			}
		}


		private bool IsFree(Peer p, bool seedsAsFree = true)
		{
			if(p == null)
				return true;
			var isExpired = p.NetworkAddress.Ago > TimeSpan.FromHours(3.0);
			var isSeed = p.Origin == PeerOrigin.DnsSeed ||
							p.Origin == PeerOrigin.HardSeed;

			return isSeed ? seedsAsFree : isExpired;
		}

		public int CountUsed(bool seedsAsFree = true)
		{
			lock(Peers)
			{
				return Peers.Concat(_peerSeeds).Where(p => !IsFree(p.Value, seedsAsFree)).Count();
			}
		}



		public Peer GetPeer(IPEndPoint endpoint)
		{
			if(endpoint == null)
				throw new ArgumentNullException("endpoint");
			if(endpoint.AddressFamily == AddressFamily.InterNetwork)
				endpoint = new IPEndPoint(endpoint.Address.MapToIPv6(), endpoint.Port);
			lock(Peers)
			{
				Peer existing = null;
				Peers.TryGetValue(endpoint.ToString(), out existing);
				return existing;
			}
		}



		public void RemovePeer(Peer peer)
		{
			lock(Peers)
			{
				Peers.Remove(peer.NetworkAddress.Endpoint.ToString());
				_peerSeeds.Remove(peer.NetworkAddress.Endpoint.ToString());
			}
		}
	}
}
#endif