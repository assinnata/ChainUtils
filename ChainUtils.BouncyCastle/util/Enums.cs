using System;
using ChainUtils.BouncyCastle.Utilities.Date;
#if NETCF_1_0 || NETCF_2_0 || SILVERLIGHT
using System.Reflection;
#endif

namespace ChainUtils.BouncyCastle.Utilities
{
	internal abstract class Enums
	{
		internal static Enum GetEnumValue(Type enumType, string s)
		{
			if(!enumType.GetTypeInfo().IsEnum)
				throw new ArgumentException("Not an enumeration type", "enumType");

			// We only want to parse single named constants
			if(s.Length > 0 && char.IsLetter(s[0]) && s.IndexOf(',') < 0)
			{
				s = s.Replace('-', '_');
				s = s.Replace('/', '_');

#if NETCF_1_0
                FieldInfo field = enumType.GetField(s, BindingFlags.Static | BindingFlags.Public);
                if (field != null)
                {
                    return (Enum)field.GetValue(null);
                }
#else
				return (Enum)Enum.Parse(enumType, s, false);
#endif
			}

			throw new ArgumentException();
		}

		internal static Array GetEnumValues(Type enumType)
		{
			if(!enumType.GetTypeInfo().IsEnum)
				throw new ArgumentException("Not an enumeration type", "enumType");

			return Enum.GetValues(enumType);
		}

		internal static Enum GetArbitraryValue(Type enumType)
		{
			var values = GetEnumValues(enumType);
			var pos = (int)(DateTimeUtilities.CurrentUnixMs() & int.MaxValue) % values.Length;
			return (Enum)values.GetValue(pos);
		}
	}
}
