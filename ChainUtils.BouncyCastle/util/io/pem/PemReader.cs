using System;
using System.IO;
using System.Text;
using ChainUtils.BouncyCastle.Utilities.Encoders;

namespace ChainUtils.BouncyCastle.Utilities.IO.Pem
{
	public class PemReader
	{
		private const string BeginString = "-----BEGIN ";
		private const string EndString = "-----END ";

		private readonly TextReader reader;

		public PemReader(TextReader reader)
		{
			if (reader == null)
				throw new ArgumentNullException("reader");

			this.reader = reader;
		}

		public TextReader Reader
		{
			get { return reader; }
		}

		/// <returns>
		/// A <see cref="PemObject"/>
		/// </returns>
		/// <exception cref="IOException"></exception>
		public PemObject ReadPemObject()
		{
			var line = reader.ReadLine();

			if (line != null && line.StartsWith(BeginString))
			{
				line = line.Substring(BeginString.Length);
				var index = line.IndexOf('-');
				var type = line.Substring(0, index);

				if (index > 0)
					return LoadObject(type);
			}

			return null;
		}

		private PemObject LoadObject(string type)
		{
			var endMarker = EndString + type;
			var headers = Platform.CreateArrayList();
			var buf = new StringBuilder();

			string line;
			while ((line = reader.ReadLine()) != null
				&& line.IndexOf(endMarker) == -1)
			{
				var colonPos = line.IndexOf(':');

				if (colonPos == -1)
				{
					buf.Append(line.Trim());
				}
				else
				{
					// Process field
					var fieldName = line.Substring(0, colonPos).Trim();

					if (fieldName.StartsWith("X-"))
						fieldName = fieldName.Substring(2);

					var fieldValue = line.Substring(colonPos + 1).Trim();

					headers.Add(new PemHeader(fieldName, fieldValue));
				}
			}

			if (line == null)
			{
				throw new IOException(endMarker + " not found");
			}

			if (buf.Length % 4 != 0)
			{
				throw new IOException("base64 data appears to be truncated");
			}

			return new PemObject(type, headers, Base64.Decode(buf.ToString()));
		}
	}
}
