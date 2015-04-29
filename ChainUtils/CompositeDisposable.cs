using System;

namespace ChainUtils
{
	public class CompositeDisposable : IDisposable
	{
		IDisposable[] _disposables;
		public CompositeDisposable(params IDisposable[] disposables)
		{
			_disposables = disposables;
		}
		#region IDisposable Members

		public void Dispose()
		{
			if(_disposables != null)
				foreach(var dispo in _disposables)
					dispo.Dispose();
		}

		#endregion
	}
}
