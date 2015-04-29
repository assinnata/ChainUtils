using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ChainUtils.Crypto;
using ChainUtils.DataEncoders;

namespace ChainUtils.Protocol
{
	[Payload("alert")]
	public class AlertPayload : Payload, IBitcoinSerializable
	{
		/// <summary>
		/// Used for knowing if an alert is valid in past of future
		/// </summary>
		public DateTimeOffset? Now
		{
			get;
			set;
		}

		VarString _payload;
		VarString _signature;

		int _version;
		long _relayUntil;
		long _expiration;
		public DateTimeOffset Expiration
		{
			get
			{
				return Utils.UnixTimeToDateTime((uint)_expiration);
			}
			set
			{
				_expiration = Utils.DateTimeToUnixTime(value);
			}
		}

		int _id;
		int _cancel;
		int[] _setCancel = new int[0];
		int _minVer;
		int _maxVer;
		VarString[] _setSubVer = new VarString[0];
		int _priority;
		VarString _comment;
		VarString _statusBar;
		VarString _reserved;

		public string[] SetSubVer
		{
			get
			{
				var messages = new List<string>();
				foreach(var v in _setSubVer)
				{
					messages.Add(Encoders.ASCII.EncodeData(v.GetString()));
				}
				return messages.ToArray();
			}
			set
			{
				var messages = new List<VarString>();
				foreach(var v in value)
				{
					messages.Add(new VarString(Encoders.ASCII.DecodeData(v)));
				}
				_setSubVer = messages.ToArray();
			}
		}

		public string Comment
		{
			get
			{
				return Encoders.ASCII.EncodeData(_comment.GetString());
			}
			set
			{
				_comment = new VarString(Encoders.ASCII.DecodeData(value));
			}
		}
		public string StatusBar
		{
			get
			{
				return Encoders.ASCII.EncodeData(_statusBar.GetString());
			}
			set
			{
				_statusBar = new VarString(Encoders.ASCII.DecodeData(value));
			}
		}

		#region IBitcoinSerializable Members

		public override void ReadWriteCore(BitcoinStream stream)
		{
			stream.ReadWrite(ref _payload);
			if(!stream.Serializing)
			{
				var payloadStream = new BitcoinStream(_payload.GetString());
				payloadStream.CopyParameters(stream);

				ReadWritePayloadFields(payloadStream);

			}

			stream.ReadWrite(ref _signature);
		}

		private void ReadWritePayloadFields(BitcoinStream payloadStream)
		{
			payloadStream.ReadWrite(ref _version);
			payloadStream.ReadWrite(ref _relayUntil);
			payloadStream.ReadWrite(ref _expiration);
			payloadStream.ReadWrite(ref _id);
			payloadStream.ReadWrite(ref _cancel);
			payloadStream.ReadWrite(ref _setCancel);
			payloadStream.ReadWrite(ref _minVer);
			payloadStream.ReadWrite(ref _maxVer);
			payloadStream.ReadWrite(ref _setSubVer);
			payloadStream.ReadWrite(ref _priority);
			payloadStream.ReadWrite(ref _comment);
			payloadStream.ReadWrite(ref _statusBar);
			payloadStream.ReadWrite(ref _reserved);
		}

		private void UpdatePayload(BitcoinStream stream)
		{
			var ms = new MemoryStream();
			var seria = new BitcoinStream(ms, true);
			seria.CopyParameters(stream);
			ReadWritePayloadFields(seria);
			_payload = new VarString(ms.ToArray());
		}

		#endregion

		public void UpdateSignature(Key key, ProtocolVersion version = ProtocolVersion.PROTOCOL_VERSION)
		{
			UpdatePayload();
			_signature = new VarString(key.Sign(Hashes.Hash256(_payload.GetString())).ToDer());
		}

		public void UpdatePayload(ProtocolVersion version = ProtocolVersion.PROTOCOL_VERSION)
		{
			UpdatePayload(new BitcoinStream(new byte[0])
			{
				ProtocolVersion = version
			});
		}

		public bool CheckSignature(Network network)
		{
			return CheckSignature(network.AlertPubKey);
		}
		public bool CheckSignature(PubKey key)
		{
			return key.Verify(Hashes.Hash256(_payload.GetString()), _signature.GetString());
		}

		public bool IsInEffect
		{
			get
			{
				var now = Now ?? DateTimeOffset.Now;
				return now < Expiration;
			}
		}

		public bool AppliesTo(int nVersion, string strSubVerIn)
		{
			return IsInEffect
					&& _minVer <= nVersion && nVersion <= _maxVer
					&& (SetSubVer.Length == 0 || SetSubVer.Contains(strSubVerIn));
		}

		public override string ToString()
		{
			return StatusBar;
		}
	}
}
