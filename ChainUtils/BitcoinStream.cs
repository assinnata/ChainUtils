#if !PORTABLE
using System.Linq;
using System.Reflection;
#endif
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using ChainUtils.Protocol;


namespace ChainUtils
{
	public class Scope : IDisposable
	{
		Action _close;
		public Scope(Action open, Action close)
		{
			this._close = close;
			open();
		}

		#region IDisposable Members

		public void Dispose()
		{
			_close();
		}

		#endregion

		public static IDisposable Nothing
		{
			get
			{
				return new Scope(() =>
				{
				}, () =>
				{
				});
			}
		}
	}
	public partial class BitcoinStream
	{
		int _maxArraySize = Int32.MaxValue;
		public int MaxArraySize
		{
			get
			{
				return _maxArraySize;
			}
			set
			{
				_maxArraySize = value;
			}
		}

		//ReadWrite<T>(ref T data)
		static MethodInfo _readWriteTyped;
		static BitcoinStream()
		{
			_readWriteTyped =
				typeof(BitcoinStream)
				.GetTypeInfo()
				.DeclaredMethods
				.Where(m => m.Name == "ReadWrite")
				.Where(m => m.IsGenericMethodDefinition)
				.Where(m => m.GetParameters().Length == 1)
				.Where(m => m.GetParameters().Any(p => p.ParameterType.IsByRef))
				.First();

		}

		private readonly Stream _inner;
		public Stream Inner
		{
			get
			{
				return _inner;
			}
		}

		private readonly bool _serializing;
		public bool Serializing
		{
			get
			{
				return _serializing;
			}
		}
		public BitcoinStream(Stream inner, bool serializing)
		{
			_serializing = serializing;
			_inner = inner;
		}

		public BitcoinStream(byte[] bytes)
			: this(new MemoryStream(bytes), false)
		{
		}

		public Script ReadWrite(Script data)
		{
			if(Serializing)
			{
				var bytes = data == null ? Script.Empty.ToBytes(true) : data.ToBytes(true);
				ReadWriteAsVarString(ref bytes);
				return data;
			}
			else
			{
				var varString = new VarString();
				varString.ReadWrite(this);
				return new Script(varString.GetString());
			}
		}

		public void ReadWrite(ref Script script)
		{
			if(Serializing)
				ReadWrite(script);
			else
				script = ReadWrite(script);
		}

		public T ReadWrite<T>(T data) where T : IBitcoinSerializable
		{
			ReadWrite<T>(ref data);
			return data;
		}

		public void ReadWriteAsVarString(ref byte[] bytes)
		{
			var str = new VarString(bytes);
			ReadWrite(ref str);
			bytes = str.GetString(true);
		}

		public void ReadWrite(Type type, ref object obj)
		{
			try
			{
				var parameters = new[] { obj };
				_readWriteTyped.MakeGenericMethod(type).Invoke(this, parameters);
				obj = parameters[0];
			}
			catch(TargetInvocationException ex)
			{
				throw ex.InnerException;
			}
		}

		public void ReadWrite(ref byte data)
		{
			ReadWriteByte(ref data);
		}
		public byte ReadWrite(byte data)
		{
			ReadWrite(ref data);
			return data;
		}

		public void ReadWrite(ref bool data)
		{
			var d = data ? (byte)1 : (byte)0;
			ReadWriteByte(ref d);
			data = (d == 0 ? false : true);
		}

		public void ReadWriteStruct<T>(ref T data) where T : struct, IBitcoinSerializable
		{
			data.ReadWrite(this);
		}

		public void ReadWrite<T>(ref T data) where T : IBitcoinSerializable
		{
			var obj = data;
			if(obj == null)
				obj = Activator.CreateInstance<T>();
			obj.ReadWrite(this);
			if(!Serializing)
				data = obj;
		}

		public void ReadWrite<T>(ref List<T> list) where T : IBitcoinSerializable, new()
		{
			ReadWriteList<List<T>, T>(ref list);
		}

		public void ReadWrite<TList, TItem>(ref TList list)
			where TList : List<TItem>, new()
			where TItem : IBitcoinSerializable, new()
		{
			ReadWriteList<TList, TItem>(ref list);
		}

		private void ReadWriteList<TList, TItem>(ref TList data)
				where TList : List<TItem>, new()
				where TItem : IBitcoinSerializable, new()
		{
			var dataArray = data == null ? null : data.ToArray();
			if(Serializing && dataArray == null)
			{
				dataArray = new TItem[0];
			}
			ReadWriteArray(ref dataArray);
			if(!Serializing)
			{
				if(data == null)
					data = new TList();
				else
					data.Clear();
				data.AddRange(dataArray);
			}
		}

		public void ReadWrite(ref byte[] arr)
		{
			ReadWriteBytes(ref arr);
		}
		public void ReadWrite<T>(ref T[] arr) where T : IBitcoinSerializable, new()
		{
			ReadWriteArray<T>(ref arr);
		}

		private void ReadWriteNumber(ref long value, int size)
		{
			var uvalue = unchecked((ulong)value);
			ReadWriteNumber(ref uvalue, size);
			value = unchecked((long)uvalue);
		}

		private void ReadWriteNumber(ref ulong value, int size)
		{
			var bytes = new byte[size];

			for(var i = 0 ; i < size ; i++)
			{
				bytes[i] = (byte)(value >> i * 8);
			}
			if(IsBigEndian)
				Array.Reverse(bytes);
			ReadWriteBytes(ref bytes);
			if(IsBigEndian)
				Array.Reverse(bytes);
			ulong valueTemp = 0;
			for(var i = 0 ; i < bytes.Length ; i++)
			{
				var v = (ulong)bytes[i];
				valueTemp += v << (i * 8);
			}
			value = valueTemp;
		}

		private void ReadWriteBytes(ref byte[] data)
		{
			if(Serializing)
			{
				Inner.Write(data, 0, data.Length);
				Counter.AddWritten(data.Length);
			}
			else
			{
				var readen = Inner.ReadEx(data, 0, data.Length, ReadCancellationToken);
				if(readen == -1)
					throw new EndOfStreamException("No more byte to read");
				Counter.AddReaden(readen);

			}
		}
		private PerformanceCounter _counter;
		public PerformanceCounter Counter
		{
			get
			{
				if(_counter == null)
					_counter = new PerformanceCounter();
				return _counter;
			}
		}
		private void ReadWriteByte(ref byte data)
		{
			if(Serializing)
			{
				Inner.WriteByte(data);
				Counter.AddWritten(1);
			}
			else
			{
				var readen = Inner.ReadByte();
				if(readen == -1)
					throw new EndOfStreamException("No more byte to read");
				data = (byte)readen;
				Counter.AddReaden(1);
			}
		}

		public bool IsBigEndian
		{
			get;
			set;
		}

		public IDisposable BigEndianScope()
		{
			var old = IsBigEndian;
			return new Scope(() =>
			{
				IsBigEndian = true;
			},
			() =>
			{
				IsBigEndian = old;
			});
		}

		ProtocolVersion _protocolVersion = ProtocolVersion.PROTOCOL_VERSION;
		public ProtocolVersion ProtocolVersion
		{
			get
			{
				return _protocolVersion;
			}
			set
			{
				_protocolVersion = value;
			}
		}


		public IDisposable ProtocolVersionScope(ProtocolVersion version)
		{
			var old = ProtocolVersion;
			return new Scope(() =>
			{
				ProtocolVersion = version;
			},
			() =>
			{
				ProtocolVersion = old;
			});
		}

		public void CopyParameters(BitcoinStream stream)
		{
			ProtocolVersion = stream.ProtocolVersion;
			IsBigEndian = stream.IsBigEndian;
			MaxArraySize = stream.MaxArraySize;
		}

		private bool _networkFormat;
		public bool NetworkFormat
		{
			get
			{
				return _networkFormat;
			}
		}

		public IDisposable NetworkFormatScope(bool value)
		{
			var old = _networkFormat;
			return new Scope(() =>
			{
				_networkFormat = value;
			}, () =>
			{
				_networkFormat = old;
			});
		}

		public CancellationToken ReadCancellationToken
		{
			get;
			set;
		}

		public void ReadWriteAsVarInt(ref uint val)
		{
			ulong vallong = val;
			ReadWriteAsVarInt(ref vallong);
			if(!Serializing)
				val = (uint)vallong;
		}
		public void ReadWriteAsVarInt(ref ulong val)
		{
			var value = new VarInt(val);
			ReadWrite(ref value);
			if(!Serializing)
				val = value.ToLong();
		}

		public void ReadWriteAsCompactVarInt(ref uint val)
		{
			var value = new CompactVarInt(val, sizeof(uint));
			ReadWrite(ref value);
			if(!Serializing)
				val = (uint)value.ToLong();
		}
		public void ReadWriteAsCompactVarInt(ref ulong val)
		{
			var value = new CompactVarInt(val, sizeof(ulong));
			ReadWrite(ref value);
			if(!Serializing)
				val = value.ToLong();
		}
	}
}
