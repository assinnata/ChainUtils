using System;
using System.Threading;

namespace ChainUtils
{
	public class PerformanceSnapshot
	{
		
		public PerformanceSnapshot(long readen, long written)
		{
			_totalWrittenBytes = written;
			_totalReadenBytes = readen;	
		}
		private readonly long _totalWrittenBytes;
		public long TotalWrittenBytes
		{
			get
			{
				return _totalWrittenBytes;
			}
		}

		long _totalReadenBytes;
		public long TotalReadenBytes
		{
			get
			{
				return _totalReadenBytes;
			}
			set
			{
				_totalReadenBytes = value;
			}
		}
		public TimeSpan Elapsed
		{
			get
			{
				return Taken - Start;
			}
		}
		public ulong ReadenBytesPerSecond
		{
			get
			{
				return (ulong)((double)TotalReadenBytes / Elapsed.TotalSeconds);
			}
		}
		public ulong WrittenBytesPerSecond
		{
			get
			{
				return (ulong)((double)TotalWrittenBytes / Elapsed.TotalSeconds);
			}
		}

		public static PerformanceSnapshot operator -(PerformanceSnapshot end, PerformanceSnapshot start)
		{
			if(end.Start != start.Start)
			{
				throw new InvalidOperationException("Performance snapshot should be taken from the same point of time");
			}
			if(end.Taken < start.Taken)
			{
				throw new InvalidOperationException("The difference of snapshot can't be negative");
			}
			return new PerformanceSnapshot(end.TotalReadenBytes - start.TotalReadenBytes,
											end.TotalWrittenBytes - start.TotalWrittenBytes)
			{
				Start = start.Taken,
				Taken = end.Taken
			};
		}

		public override string ToString()
		{
			return "Read : " + ToKbSec(ReadenBytesPerSecond) + ", Write : " + ToKbSec(WrittenBytesPerSecond);
		}

		private string ToKbSec(ulong bytesPerSec)
		{
			var speed = ((double)bytesPerSec / 1024.0);
			return speed.ToString("0.00") + " KB/S)";
		}

		public DateTime Start
		{
			get;
			set;
		}

		public DateTime Taken
		{
			get;
			set;
		}
	}
	public class PerformanceCounter
	{
		public PerformanceCounter()
		{
			_start = DateTime.UtcNow;
		}

		long _writtenBytes;
		public long WrittenBytes
		{
			get
			{
				return _writtenBytes;
			}
		}


		public void AddWritten(long count)
		{
			Interlocked.Add(ref _writtenBytes, count);
		}
		public void AddReaden(long count)
		{
			Interlocked.Add(ref _readenBytes, count);
		}

		long _readenBytes;
		public long ReadenBytes
		{
			get
			{
				return _readenBytes;
			}
		}

		public PerformanceSnapshot Snapshot()
		{
#if !PORTABLE
			Thread.MemoryBarrier();
#endif
			var snap = new PerformanceSnapshot(ReadenBytes,WrittenBytes)
			{
				Start = Start,
				Taken = DateTime.UtcNow
			};
			return snap;
		}

		DateTime _start;
		public DateTime Start
		{
			get
			{
				return _start;
			}
		}
		public TimeSpan Elapsed
		{
			get
			{
				return DateTime.UtcNow - Start;
			}
		}

		public override string ToString()
		{
			return Snapshot().ToString();
		}

		internal void Add(PerformanceCounter counter)
		{
			AddWritten(counter.WrittenBytes);
			AddReaden(counter.ReadenBytes);
		}
	}
}
