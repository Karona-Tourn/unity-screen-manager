namespace UnityEngine
{
	using System.IO;
	using System.Text;

	public class WWWTranscoder
	{
		private static byte[] lcHexChars = DefaultEncoding.GetBytes ("0123456789abcdef");
		private static byte qpEscapeChar = 0x3d;
		private static byte[] qpForbidden = DefaultEncoding.GetBytes ("&;=?\"'%+_");
		private static byte qpSpace = 0x5f;
		private static byte[] ucHexChars = DefaultEncoding.GetBytes ("0123456789ABCDEF");
		private static byte urlEscapeChar = 0x25;
		private static byte[] urlForbidden = DefaultEncoding.GetBytes ("@&;:<>=?\"'/\\!#%+$,{}|^[]`");
		private static byte urlSpace = 0x2b;

		public static Encoding DefaultEncoding
		{
			get { return Encoding.ASCII; }
		}

		private static byte[] Byte2Hex (byte b, byte[] hexChars)
		{
			return new byte[] { hexChars[b >> 4], hexChars[b & 15] };
		}

		private static bool ByteArrayContains (byte[] array, byte b)
		{
			int length = array.Length;
			for (int i = 0; i < length; i++)
			{
				if (array[i] == b)
				{
					return true;
				}
			}
			return false;
		}

		public static byte[] Decode (byte[] input, byte escapeChar, byte space)
		{
			using (MemoryStream stream = new MemoryStream (input.Length))
			{
				for (int i = 0; i < input.Length; i++)
				{
					if (input[i] == space)
					{
						stream.WriteByte (0x20);
					}
					else if ((input[i] == escapeChar) && ((i + 2) < input.Length))
					{
						i++;
						stream.WriteByte (Hex2Byte (input, i++));
					}
					else
					{
						stream.WriteByte (input[i]);
					}
				}
				return stream.ToArray ();
			}
		}

		public static byte[] Encode (byte[] input, byte escapeChar, byte space, byte[] forbidden, bool uppercase)
		{
			using (MemoryStream stream = new MemoryStream (input.Length * 2))
			{
				for (int i = 0; i < input.Length; i++)
				{
					if (input[i] == 0x20)
					{
						stream.WriteByte (space);
					}
					else if (((input[i] < 0x20) || (input[i] > 0x7e)) || ByteArrayContains (forbidden, input[i]))
					{
						stream.WriteByte (escapeChar);
						stream.Write (Byte2Hex (input[i], !uppercase ? lcHexChars : ucHexChars), 0, 2);
					}
					else
					{
						stream.WriteByte (input[i]);
					}
				}
				return stream.ToArray ();
			}
		}

		private static byte Hex2Byte (byte[] b, int offset)
		{
			byte num = 0;
			for (int i = offset; i < (offset + 2); i++)
			{
				num = (byte)(num * 0x10);
				int num3 = b[i];
				if ((num3 >= 0x30) && (num3 <= 0x39))
				{
					num3 -= 0x30;
				}
				else if ((num3 >= 0x41) && (num3 <= 0x4b))
				{
					num3 -= 0x37;
				}
				else if ((num3 >= 0x61) && (num3 <= 0x66))
				{
					num3 -= 0x57;
				}
				if (num3 > 15)
				{
					return 0x3f;
				}
				num = (byte)(num + ((byte)num3));
			}
			return num;
		}

		public static string QPDecode (string toEncode)
		{
			return QPDecode (toEncode, Encoding.UTF8);
		}

		public static byte[] QPDecode (byte[] toEncode)
		{
			return Decode (toEncode, qpEscapeChar, qpSpace);
		}

		public static string QPDecode (string toEncode, Encoding e)
		{
			byte[] bytes = Decode (DefaultEncoding.GetBytes (toEncode), qpEscapeChar, qpSpace);
			return e.GetString (bytes, 0, bytes.Length);
		}

		public static string QPEncode (string toEncode)
		{
			return QPEncode (toEncode, Encoding.UTF8);
		}

		public static byte[] QPEncode (byte[] toEncode)
		{
			return Encode (toEncode, qpEscapeChar, qpSpace, qpForbidden, true);
		}

		public static string QPEncode (string toEncode, Encoding e)
		{
			byte[] bytes = Encode (e.GetBytes (toEncode), qpEscapeChar, qpSpace, qpForbidden, true);
			return DefaultEncoding.GetString (bytes, 0, bytes.Length);
		}

		public static bool SevenBitClean (string s)
		{
			return SevenBitClean (s, Encoding.UTF8);
		}

		public static bool SevenBitClean (byte[] input)
		{
			for (int i = 0; i < input.Length; i++)
			{
				if ((input[i] < 0x20) || (input[i] > 0x7e))
				{
					return false;
				}
			}
			return true;
		}

		public static bool SevenBitClean (string s, Encoding e)
		{
			return SevenBitClean (e.GetBytes (s));
		}

		public static string URLDecode (string toEncode)
		{
			return URLDecode (toEncode, Encoding.UTF8);
		}

		public static byte[] URLDecode (byte[] toEncode)
		{
			return Decode (toEncode, urlEscapeChar, urlSpace);
		}

		public static string URLDecode (string toEncode, Encoding e)
		{
			byte[] bytes = Decode (DefaultEncoding.GetBytes (toEncode), urlEscapeChar, urlSpace);
			return e.GetString (bytes, 0, bytes.Length);
		}

		public static string URLEncode (string toEncode)
		{
			return URLEncode (toEncode, Encoding.UTF8);
		}

		public static byte[] URLEncode (byte[] toEncode)
		{
			return Encode (toEncode, urlEscapeChar, urlSpace, urlForbidden, false);
		}

		public static string URLEncode (string toEncode, Encoding e)
		{
			byte[] bytes = Encode (e.GetBytes (toEncode), urlEscapeChar, urlSpace, urlForbidden, false);
			return DefaultEncoding.GetString (bytes, 0, bytes.Length);
		}
	}
}
