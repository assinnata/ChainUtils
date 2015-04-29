
using System;
using System.Collections.Generic;
using System.Linq;

namespace ChainUtils
{
	public class MedianFilterInt32 
	{
		Queue<Int32> _vValues;
		Queue<Int32> _vSorted;
		uint _nSize;

		public MedianFilterInt32(uint size, Int32 initialValue)
		{
			_nSize = size;
			_vValues = new Queue<Int32>((int)size);
			_vValues.Enqueue(initialValue);
			_vSorted = new Queue<Int32>(_vValues);
		}

		public Int32 Median
		{
			get
			{
				var size = _vSorted.Count;
				if(size <= 0)
					throw new InvalidOperationException("size <= 0");

				var sortedList = _vSorted.ToList();
				if(size % 2 == 1)
				{
					return sortedList[size / 2];
				}
				else // Even number of elements
				{
					return (sortedList[size / 2 - 1] + sortedList[size / 2]) / 2;
				}
			}
		}

		public void Input(Int32 value)
		{
			if(_vValues.Count == _nSize)
			{
				_vValues.Dequeue();
			}
			_vValues.Enqueue(value);
			_vSorted = new Queue<Int32>(_vValues.OrderBy(o => o));
		}
	}
	public class MedianFilterInt64 
	{
		Queue<Int64> _vValues;
		Queue<Int64> _vSorted;
		uint _nSize;

		public MedianFilterInt64(uint size, Int64 initialValue)
		{
			_nSize = size;
			_vValues = new Queue<Int64>((int)size);
			_vValues.Enqueue(initialValue);
			_vSorted = new Queue<Int64>(_vValues);
		}

		public Int64 Median
		{
			get
			{
				var size = _vSorted.Count;
				if(size <= 0)
					throw new InvalidOperationException("size <= 0");

				var sortedList = _vSorted.ToList();
				if(size % 2 == 1)
				{
					return sortedList[size / 2];
				}
				else // Even number of elements
				{
					return (sortedList[size / 2 - 1] + sortedList[size / 2]) / 2;
				}
			}
		}

		public void Input(Int64 value)
		{
			if(_vValues.Count == _nSize)
			{
				_vValues.Dequeue();
			}
			_vValues.Enqueue(value);
			_vSorted = new Queue<Int64>(_vValues.OrderBy(o => o));
		}
	}
}