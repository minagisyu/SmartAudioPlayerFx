using System;
using System.Collections.Generic;

namespace Quala
{
	partial class Extension
	{
		static uint[] _crc32_table;

		// byte[]
		public static uint ToCrc32(this byte[] data)
		{
			if(_crc32_table == null)
			{
				// init
				_crc32_table = new uint[256];
				for(uint n = 0; n < 256; n++)
				{
					uint c = n;
					for(uint k = 0; k < 8; k++)
					{
						c = (((c & 1) != 0) ? (0xedb88320) : 0) ^ (c >> 1);
					}
					_crc32_table[n] = c;
				}
			}

			uint crc = 0 ^ 0xFFFFFFFF;
			for(int n = 0; n < data.Length; n++)
				crc = _crc32_table[(crc ^ data[n]) & 0xFF] ^ (crc >> 8);

			return (crc ^ 0xFFFFFFFF);
		}
	}
}
