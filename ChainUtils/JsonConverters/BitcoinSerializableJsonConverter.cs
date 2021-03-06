﻿using System;
using System.IO;
using System.Reflection;
using ChainUtils.DataEncoders;
using Newtonsoft.Json;

namespace ChainUtils.JsonConverters
{
	class BitcoinSerializableJsonConverter : JsonConverter
	{
		public override bool CanConvert(Type objectType)
		{
			return typeof(IBitcoinSerializable).GetTypeInfo().IsAssignableFrom(objectType.GetTypeInfo());
		}

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			if(reader.TokenType == JsonToken.Null)
				return null;

			try
			{

				var obj = (IBitcoinSerializable)Activator.CreateInstance(objectType);
				var bytes = Encoders.Hex.DecodeData((string)reader.Value);
				obj.ReadWrite(bytes);
				return obj;
			}
			catch(EndOfStreamException)
			{
			}
			catch(FormatException)
			{
			}
			throw new FormatException("Invalid bitcoin object of type " + objectType.Name + " : " + reader.Path);
		}

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			var bytes = ((IBitcoinSerializable)value).ToBytes();
			writer.WriteValue(Encoders.Hex.EncodeData(bytes));
		}
	}
}
