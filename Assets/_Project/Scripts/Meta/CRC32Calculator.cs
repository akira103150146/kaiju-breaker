using System.Text;

namespace KaijuBreaker.Meta
{
    /// <summary>
    /// IEEE 802.3 CRC-32 (the zlib / Ethernet polynomial 0xEDB88320) over a UTF-8 string, formatted as an
    /// 8-character UPPERCASE hex string (meta-progression-system.md §D.2). Used for the save
    /// <c>integrity_hash</c> — detection of accidental disk corruption, not tamper resistance (§H.9).
    ///
    /// <para>Verified against the standard test vector <c>"123456789" → "CBF43926"</c>.</para>
    /// </summary>
    public static class CRC32Calculator
    {
        private const uint Polynomial = 0xEDB88320u;
        private static readonly uint[] Table = BuildTable();

        private static uint[] BuildTable()
        {
            var table = new uint[256];
            for (uint i = 0; i < 256; i++)
            {
                uint c = i;
                for (int k = 0; k < 8; k++)
                    c = (c & 1) != 0 ? Polynomial ^ (c >> 1) : c >> 1;
                table[i] = c;
            }
            return table;
        }

        /// <summary>Compute the CRC-32 of <paramref name="input"/> (UTF-8) as an 8-char uppercase hex string.</summary>
        public static string Compute(string input)
        {
            return ComputeValue(input).ToString("X8");
        }

        /// <summary>Compute the raw CRC-32 unsigned value of <paramref name="input"/> (UTF-8).</summary>
        public static uint ComputeValue(string input)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(input ?? string.Empty);
            uint crc = 0xFFFFFFFFu;
            for (int i = 0; i < bytes.Length; i++)
                crc = Table[(crc ^ bytes[i]) & 0xFF] ^ (crc >> 8);
            return crc ^ 0xFFFFFFFFu;
        }
    }
}
