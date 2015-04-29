using System;
using System.Globalization;
using System.Linq;
using System.Numerics;

namespace ChainUtils
{
	public class Target
	{
		static Target _difficulty1 = new Target(new byte[] { 0x1d, 0x00, 0xff, 0xff });
		public static Target Difficulty1
		{
			get
			{
				return _difficulty1;
			}
		}

		public Target(uint compact)
			: this(ToBytes(compact))
		{

		}

		private static byte[] ToBytes(uint bits)
		{
			return new[]
			{
				(byte)(bits >> 24),
				(byte)(bits >> 16),
				(byte)(bits >> 8),
				(byte)(bits)
			};
		}



		BigInteger _target;
		public Target(byte[] compact)
		{
			if(compact.Length == 4)
			{
				var exp = compact[0];
				var val = compact.Skip(1).Take(3).Reverse().ToArray();
				_target = new BigInteger(val) << 8 * (exp - 3);
			}
			else
				throw new FormatException("Invalid number of bytes");
		}

#if !NOBIGINT
		public Target(BigInteger target)
#else
		internal Target(BigInteger target)
#endif
		{
			_target = target;
			_target = new Target(ToCompact())._target;
		}
		public Target(Uint256 target)
		{
			_target = new BigInteger(target.ToBytes());
			_target = new Target(ToCompact())._target;
		}

		public static implicit operator Target(uint a)
		{
			return new Target(a);
		}
		public static implicit operator uint(Target a)
		{
			var bytes = a._target.ToByteArray().Reverse().ToArray();
			var val = bytes.Take(3).Reverse().ToArray();
			var exp = (byte)(bytes.Length);
			var missing = 4 - val.Length;
			if(missing > 0)
				val = val.Concat(new byte[missing]).ToArray();
			if(missing < 0)
				val = val.Take(-missing).ToArray();
			return (uint)val[0] + (uint)(val[1] << 8) + (uint)(val[2] << 16) + (uint)(exp << 24);
		}

		double? _difficulty;
		public double Difficulty
		{
			get
			{
				if(_difficulty == null)
				{
					BigInteger remainder;
					var quotient = BigInteger.DivRem(Difficulty1._target, _target, out remainder);
					var decimalPart = BigInteger.Zero;
					for(var i = 0 ; i < 12 ; i++)
					{
						var div = (remainder * 10) / _target;

						decimalPart *= 10;
						decimalPart += div;

						remainder = remainder * 10 - div * _target;
					}
					_difficulty = double.Parse(quotient.ToString() + "." + decimalPart.ToString(), new NumberFormatInfo()
					{
						NegativeSign = "-",
						NumberDecimalSeparator = "."
					});
				}
				return _difficulty.Value;
			}
		}



		public override bool Equals(object obj)
		{
			var item = obj as Target;
			if(item == null)
				return false;
			return _target.Equals(item._target);
		}
		public static bool operator ==(Target a, Target b)
		{
			if(ReferenceEquals(a, b))
				return true;
			if(((object)a == null) || ((object)b == null))
				return false;
			return a._target == b._target;
		}

		public static bool operator !=(Target a, Target b)
		{
			return !(a == b);
		}

		public override int GetHashCode()
		{
			return _target.GetHashCode();
		}

#if !NOBIGINT
		public BigInteger ToBigInteger()
#else
		internal BigInteger ToBigInteger()
#endif
		{
			return _target;
		}

		public uint ToCompact()
		{
			return (uint)this;
		}

		public Uint256 ToUInt256()
		{
			var array = _target.ToByteArray();
			var missingZero = 32 - array.Length;
			if(missingZero < 0)
				throw new InvalidOperationException("Awful bug, this should never happen");
			if(missingZero != 0)
			{
				array = array.Concat(new byte[missingZero]).ToArray();
			}
			return new Uint256(array);
		}

		public override string ToString()
		{
			return ToUInt256().ToString();
		}
	}
}
