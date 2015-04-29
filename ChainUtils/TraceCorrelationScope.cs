using System;
using System.Diagnostics;

namespace ChainUtils
{
#if NOTRACESOURCE
		internal
#else
		public
#endif
	 class TraceCorrelationScope : IDisposable
	{
		private Guid _activity;
		private Guid _old;

		public Guid OldActivity
		{
			get
			{
				return _old;
			}
			private set
			{
				_old = value;
			}
		}

		bool _transfered;

		TraceSource _source;
		public TraceCorrelationScope(Guid activity, TraceSource source, bool traceTransfer)
		{
			_old = Trace.CorrelationManager.ActivityId;
			this._activity = activity;

			_transfered = _old != activity && traceTransfer;
			if(_transfered)
			{
				_source = source;
				_source.TraceTransfer(0, "transfer", activity);
			}
			Trace.CorrelationManager.ActivityId = activity;
		}


		#region IDisposable Members

		public void Dispose()
		{
			if(_transfered)
			{
				_source.TraceTransfer(0, "transfer", _old);
			}
			Trace.CorrelationManager.ActivityId = _old;
		}

		#endregion
	}
#if NOTRACESOURCE
		internal
#else
		public
#endif 
	class TraceCorrelation
	{

		TraceSource _source;
		string _activityName;
		public TraceCorrelation(TraceSource source, string activityName)
			: this(Guid.NewGuid(), source, activityName)
		{

		}
		public TraceCorrelation(Guid activity, TraceSource source, string activityName)
		{
			_source = source;
			_activityName = activityName;
			this._activity = activity;
		}

		Guid _activity;
		public Guid Activity
		{
			get
			{
				return _activity;
			}
			private set
			{
				_activity = value;
			}
		}

		volatile bool _first = true;
		public TraceCorrelationScope Open(bool traceTransfer = true)
		{
			var scope = new TraceCorrelationScope(_activity, _source, traceTransfer);
			if(_first)
			{
				_first = false;
				_source.TraceEvent(TraceEventType.Start, 0, _activityName);
			}
			return scope;
		}

		public void LogInside(Action act)
		{
			using(Open())
			{
				act();
			}
		}








		public override string ToString()
		{
			return _activityName;
		}
	}
}
