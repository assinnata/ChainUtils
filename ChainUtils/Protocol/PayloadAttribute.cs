using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ChainUtils.Protocol
{
	[AttributeUsage(AttributeTargets.Class)]
	public class PayloadAttribute : Attribute
	{
		static Dictionary<string, Type> _nameToType;
		static Dictionary<Type, string> _typeToName;

		static PayloadAttribute()
		{
			_nameToType = new Dictionary<string, Type>();
			_typeToName = new Dictionary<Type, string>();
			foreach(var pair in typeof(PayloadAttribute)
				.GetTypeInfo()
				.Assembly.DefinedTypes
				.Where(t => t.Namespace == typeof(PayloadAttribute).Namespace)
				.Where(t => t.IsDefined(typeof(PayloadAttribute), true))
				.Select(t =>
					new
					{
						Attr = t.GetCustomAttributes(typeof(PayloadAttribute), true).OfType<PayloadAttribute>().First(),
						Type = t
					}))
			{
				_nameToType.Add(pair.Attr.Name, pair.Type.AsType());
				_typeToName.Add(pair.Type.AsType(), pair.Attr.Name);
			}
		}

		public static string GetCommandName<T>()
		{
			return GetCommandName(typeof(T));
		}
		public static Type GetCommandType(string commandName)
		{
			Type result;
			if(!_nameToType.TryGetValue(commandName, out result))
				return typeof(UnknowPayload);
			return result;
		}
		public PayloadAttribute(string commandName)
		{
			Name = commandName;
		}
		public string Name
		{
			get;
			set;
		}

		internal static string GetCommandName(Type type)
		{
			string result;
			if(!_typeToName.TryGetValue(type, out result))
				throw new ArgumentException(type.FullName + " is not a payload");
			return result;
		}
	}
}
