using System;
using System.Diagnostics;
using System.Threading;
using ChainUtils.Crypto;
using ChainUtils.DataEncoders;
#if !NOSOCKET
using System.Net.Sockets;
#endif

namespace ChainUtils.Protocol
{
	public class Message : IBitcoinSerializable
	{
		uint _magic;

		public uint Magic
		{
			get
			{
				return _magic;
			}
			set
			{
				_magic = value;
			}
		}
		byte[] _command = new byte[12];

		public string Command
		{
			get
			{
				return Encoders.ASCII.EncodeData(_command);
			}
			set
			{
				_command = Encoders.ASCII.DecodeData(value.Trim().PadRight(12, '\0'));
			}
		}
		uint _length;

		public uint Length
		{
			get
			{
				return _length;
			}
			set
			{
				_length = value;
			}
		}
		uint _checksum;

		public uint Checksum
		{
			get
			{
				return _checksum;
			}
			set
			{
				_checksum = value;
			}
		}
		byte[] _payload;
		object _payloadObject;
		public object Payload
		{
			get
			{
				return _payloadObject;
			}
		}



		#region IBitcoinSerializable Members

		public void ReadWrite(BitcoinStream stream)
		{
			var verifyChechksum = false;
			if(stream.Serializing || (!stream.Serializing && !_skipMagic))
				stream.ReadWrite(ref _magic);
			stream.ReadWrite(ref _command);
			stream.ReadWrite(ref _length);
			if(stream.ProtocolVersion >= ProtocolVersion.MempoolGdVersion)
			{
				stream.ReadWrite(ref _checksum);
				verifyChechksum = true;
			}
			if(stream.Serializing)
			{
				stream.ReadWrite(ref _payload);
			}
			else
			{
				NodeServerTrace.Trace.TraceEvent(TraceEventType.Verbose, 0, "Message type readen : " + Command);
				if(_length > 0x02000000) //MAX_SIZE 0x02000000 Serialize.h
				{
					throw new FormatException("Message payload too big ( > 0x02000000 bytes)");
				}
				_payload = new byte[_length];
				stream.ReadWrite(ref _payload);

				if(verifyChechksum)
				{
					if(!VerifyChecksum())
					{
						NodeServerTrace.Trace.TraceEvent(TraceEventType.Verbose, 0, "Invalid message checksum bytes : "
															+ Encoders.Hex.EncodeData(this.ToBytes()));
						throw new FormatException("Message checksum invalid");
					}
				}
				var payloadStream = new BitcoinStream(_payload);
				payloadStream.CopyParameters(stream);

				var payloadType = PayloadAttribute.GetCommandType(Command);
				if(payloadType == typeof(UnknowPayload))
					NodeServerTrace.Trace.TraceEvent(TraceEventType.Warning, 0, "Unknown command received : " + Command);
				payloadStream.ReadWrite(payloadType, ref _payloadObject);
				NodeServerTrace.Verbose("Payload : " + _payloadObject);
			}
		}

		#endregion

		public bool VerifyChecksum()
		{
			return Checksum == Hashes.Hash256(_payload).GetLow32();
		}

		public void UpdatePayload(Payload payload, ProtocolVersion version)
		{
			if(payload == null)
				throw new ArgumentNullException("payload");
			_payloadObject = payload;
			this._payload = payload.ToBytes(version);
			_length = (uint)this._payload.Length;
			_checksum = Hashes.Hash256(this._payload).GetLow32();
			Command = payload.Command;
		}


		/// <summary>
		/// When parsing, maybe Magic is already parsed
		/// </summary>
		bool _skipMagic;

		public override string ToString()
		{
			return Command + " : " + Payload;
		}

#if !NOSOCKET
		public static Message ReadNext(Socket socket, Network network, ProtocolVersion version, CancellationToken cancellationToken)
		{
			PerformanceCounter counter;
			return ReadNext(socket, network, version, cancellationToken, out counter);
		}

		internal class CustomNetworkStream : NetworkStream
		{
			public CustomNetworkStream(Socket socket, bool own)
				: base(socket, own)
			{

			}

			public bool Connected
			{
				get
				{
					return Socket.Connected;
				}
			}
		}
		public static Message ReadNext(Socket socket, Network network, ProtocolVersion version, CancellationToken cancellationToken, out PerformanceCounter counter)
		{
			var stream = new CustomNetworkStream(socket, false);
			var bitStream = new BitcoinStream(stream, false)
			{
				ProtocolVersion = version,
				ReadCancellationToken = cancellationToken
			};

			network.ReadMagic(stream, cancellationToken);

			var message = new Message();
			using(message.SkipMagicScope(true))
			{
				message.Magic = network.Magic;
				message.ReadWrite(bitStream);
			}
			counter = bitStream.Counter;
			return message;
		}
#endif
		private IDisposable SkipMagicScope(bool value)
		{
			var old = _skipMagic;
			return new Scope(() => _skipMagic = value, () => _skipMagic = old);
		}

	}
}
