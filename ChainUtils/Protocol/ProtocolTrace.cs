using System;
using System.Diagnostics;

namespace ChainUtils.Protocol
{
	public class NodeServerTrace
	{
		static TraceSource _trace = new TraceSource("ChainUtils.NodeServer");
		internal static TraceSource Trace
		{
			get
			{
				return _trace;
			}
		}

		public static void ErrorWhileRetrievingDnsSeedIp(string name, Exception ex)
		{
			_trace.TraceEvent(TraceEventType.Warning, 0, "Impossible to resolve dns for seed " + name + " " + Utils.ExceptionToString(ex));
		}


		public static void Warning(string msg, Exception ex)
		{
			_trace.TraceEvent(TraceEventType.Warning, 0, msg + " " + Utils.ExceptionToString(ex));
		}

		public static void ExternalIpReceived(string ip)
		{
			_trace.TraceInformation("External ip received : " + ip);
		}

		internal static void ExternalIpFailed(Exception ex)
		{
			_trace.TraceEvent(TraceEventType.Error, 0, "External ip cannot be detected " + Utils.ExceptionToString(ex));
		}

		internal static void Information(string info)
		{
			_trace.TraceInformation(info);
		}

		internal static void Error(string msg, Exception ex)
		{
			_trace.TraceEvent(TraceEventType.Error, 0, msg + " " + Utils.ExceptionToString(ex));
		}

		internal static void Warning(string msg)
		{
			Warning(msg, null);
		}

		internal static void PeerTableRemainingPeerToGet(int count)
		{
			_trace.TraceInformation("Remaining peer to get : " + count);
		}

		internal static void ConnectionToSelfDetected()
		{
			Warning("Connection to self detected, abort connection");
		}

		internal static void Verbose(string str)
		{
			_trace.TraceEvent(TraceEventType.Verbose, 0, str);
		}
	}
}
