#if !NOSOCKET && !NOUPNP
using System;
using System.Linq;
using System.Net;
using System.Threading;
using Mono.Nat;

namespace ChainUtils.Protocol
{
	public class UpnPLease : IDisposable
	{
		public string RuleName
		{
			get;
			private set;
		}
		public UpnPLease(int[] bitcoinPorts, int internalPort, string ruleName)
		{
			RuleName = ruleName;
			_bitcoinPorts = bitcoinPorts;
			_internalPort = internalPort;
			Trace = new TraceCorrelation(NodeServerTrace.Trace, "UPNP external address and port detection");
		}
		private readonly int _internalPort;
		public int InternalPort
		{
			get
			{
				return _internalPort;
			}
		}
		public IPEndPoint ExternalEndpoint
		{
			get;
			set;
		}
		private readonly int[] _bitcoinPorts;
		public int[] BitcoinPorts
		{
			get
			{
				return _bitcoinPorts;
			}
		}
		public TraceCorrelation Trace
		{
			get;
			private set;
		}
		public TimeSpan LeasePeriod
		{
			get;
			set;
		}
		Timer Timer
		{
			get;
			set;
		}
		public Mapping Mapping
		{
			get;
			internal set;
		}

		INatDevice Device
		{
			get;
			set;
		}



		internal bool DetectExternalEndpoint(CancellationToken cancellation = default(CancellationToken))
		{
			using(Trace.Open())
			{
				var externalPort = 0;

				try
				{
					var device = GetDevice(cancellation);
					if(device == null)
						return false;
					using(Trace.Open(false))
					{
						try
						{
							var externalIp = device.GetExternalIP();
							ExternalEndpoint = Utils.EnsureIPv6(new IPEndPoint(externalIp, externalPort));
							NodeServerTrace.Information("External endpoint detected " + ExternalEndpoint);

							var mapping = device.GetAllMappings();
							externalPort = BitcoinPorts.FirstOrDefault(p => mapping.All(m => m.PublicPort != p));

							if(externalPort == 0)
								NodeServerTrace.Error("Bitcoin node ports already used " + string.Join(",", BitcoinPorts), null);

							Mapping = new Mapping(Mono.Nat.Protocol.Tcp, InternalPort, externalPort, (int)LeasePeriod.TotalSeconds)
							{
								Description = RuleName
							};
							try
							{
								device.CreatePortMap(Mapping);
							}
							catch(MappingException ex)
							{
								if(ex.ErrorCode != 725) //Does not support lease
									throw;

								Mapping.Lifetime = 0;
								device.CreatePortMap(Mapping);
							}
							NodeServerTrace.Information("Port mapping added " + Mapping);
							Device = device;
							if(Mapping.Lifetime != 0)
							{
								LogNextLeaseRenew();
								Timer = new Timer(o =>
								{
									if(_isDisposed)
										return;
									using(Trace.Open(false))
									{
										try
										{
											device.CreatePortMap(Mapping);
											NodeServerTrace.Information("Port mapping renewed");
											LogNextLeaseRenew();
										}
										catch(Exception ex)
										{
											NodeServerTrace.Error("Error when refreshing the port mapping with UPnP", ex);
										}
										finally
										{
											Timer.Change((int)CalculateNextRefresh().TotalMilliseconds, Timeout.Infinite);
										}
									}
								});
								Timer.Change((int)CalculateNextRefresh().TotalMilliseconds, Timeout.Infinite);
							}

						}
						catch(Exception ex)
						{
							NodeServerTrace.Error("Error during address port detection on the upnp device", ex);
						}
					}
				}
				catch(OperationCanceledException)
				{
					NodeServerTrace.Information("Discovery cancelled");
					throw;
				}
				catch(Exception ex)
				{
					NodeServerTrace.Error("Error during upnp discovery", ex);
				}
				return true;
			}
		}

		private static INatDevice GetDevice(CancellationToken cancellation)
		{
			var searcher = new UpnpSearcher();
			var device = searcher.SearchAndReceive(cancellation);
			if(device == null)
			{
				NodeServerTrace.Information("No UPnP device found");
				return null;
			}
			return device;
		}

		private void LogNextLeaseRenew()
		{
			NodeServerTrace.Information("Next lease renewal at " + (DateTime.Now + CalculateNextRefresh()));
		}


		private TimeSpan CalculateNextRefresh()
		{
			return TimeSpan.FromTicks((LeasePeriod.Ticks - (LeasePeriod.Ticks / 10L)));
		}


		volatile bool _isDisposed;
		public void Dispose()
		{
			if(!_isDisposed)
			{
				_isDisposed = true;
				using(Trace.Open())
				{
					StopRenew();
					if(Device != null)
					{
						Device.DeletePortMap(Mapping);
						NodeServerTrace.Information("Port mapping removed " + Mapping);
					}
				}
			}
		}

		public void StopRenew()
		{
			if(Timer != null)
			{
				using(Trace.Open())
				{
					Timer.Dispose();
					Timer = null;
					NodeServerTrace.Information("Port mapping renewal stopped");
				}
			}
		}

		public bool IsOpen()
		{
			return Device.GetAllMappings()
				  .Any(m => m.Description == Mapping.Description &&
						  m.PublicPort == Mapping.PublicPort &&
						  m.PrivatePort == Mapping.PrivatePort);
		}



		public static void ReleaseAll(string ruleName, CancellationToken cancellation = default(CancellationToken))
		{
			var device = GetDevice(cancellation);
			if(device == null)
				return;

			foreach(var m in device.GetAllMappings())
			{
				if(m.Description == ruleName)
					device.DeletePortMap(m);
			}
		}
	}
}
#endif