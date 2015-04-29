using System;
using System.IO;
using ChainUtils.BouncyCastle.Utilities.Encoders;

namespace ChainUtils.BouncyCastle.Utilities.IO.Pem
{
	/**
	* A generic PEM writer, based on RFC 1421
	*/
	public class PemWriter
	{
		private const int LineLength = 64;

		private readonly TextWriter	writer;
		private readonly int		nlLength;
		private char[]				buf = new char[LineLength];
		
		/**
		 * Base constructor.
		 *
		 * @param out output stream to use.
		 */
		public PemWriter(TextWriter writer)
		{
			if (writer == null)
				throw new ArgumentNullException("writer");

			this.writer = writer;
			nlLength = Platform.NewLine.Length;
		}

		public TextWriter Writer
		{
			get { return writer; }
		}

		/**
		 * Return the number of bytes or characters required to contain the
		 * passed in object if it is PEM encoded.
		 *
		 * @param obj pem object to be output
		 * @return an estimate of the number of bytes
		 */
		public int GetOutputSize(PemObject obj)
		{
			// BEGIN and END boundaries.
			var size = (2 * (obj.Type.Length + 10 + nlLength)) + 6 + 4;

			if (obj.Headers.Count > 0)
			{
				foreach (PemHeader header in obj.Headers)
				{
					size += header.Name.Length + ": ".Length + header.Value.Length + nlLength;
				}

				size += nlLength;
			}

			// base64 encoding
			var dataLen = ((obj.Content.Length + 2) / 3) * 4;

			size += dataLen + (((dataLen + LineLength - 1) / LineLength) * nlLength);

			return size;
		}

		public void WriteObject(PemObjectGenerator objGen)
		{
			var obj = objGen.Generate();

			WritePreEncapsulationBoundary(obj.Type);

			if (obj.Headers.Count > 0)
			{
				foreach (PemHeader header in obj.Headers)
				{
					writer.Write(header.Name);
					writer.Write(": ");
					writer.WriteLine(header.Value);
				}

				writer.WriteLine();
			}

			WriteEncoded(obj.Content);
			WritePostEncapsulationBoundary(obj.Type);
		}

		private void WriteEncoded(byte[] bytes)
		{
			bytes = Base64.Encode(bytes);

			for (var i = 0; i < bytes.Length; i += buf.Length)
			{
				var index = 0;
				while (index != buf.Length)
				{
					if ((i + index) >= bytes.Length)
						break;

					buf[index] = (char)bytes[i + index];
					index++;
				}
				writer.WriteLine(buf, 0, index);
			}
		}

		private void WritePreEncapsulationBoundary(string type)
		{
			writer.WriteLine("-----BEGIN " + type + "-----");
		}

		private void WritePostEncapsulationBoundary(string type)
		{
			writer.WriteLine("-----END " + type + "-----");
		}
	}
}
