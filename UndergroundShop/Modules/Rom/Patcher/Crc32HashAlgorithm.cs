using System;
using System.Security.Cryptography;

namespace UndergroundShop.Modules.Rom.Patcher
{
    public class Crc32 : HashAlgorithm
    {
        private uint[] table;
        private uint crc;

        public Crc32()
        {
            table = GenerateTable();
            crc = 0xFFFFFFFF;
        }

        public override void Initialize()
        {
            crc = 0xFFFFFFFF;
        }

        protected override void HashCore(byte[] array, int ibStart, int cbSize)
        {
            for (int i = ibStart; i < ibStart + cbSize; i++)
            {
                crc = (crc >> 8) ^ table[(crc ^ array[i]) & 0xFF];
            }
        }

        protected override byte[] HashFinal()
        {
            crc = ~crc;
            return BitConverter.GetBytes(crc);
        }

        public uint ComputeChecksum(byte[] bytes)
        {
            Initialize();
            HashCore(bytes, 0, bytes.Length);
            return crc;
        }

        private uint[] GenerateTable()
        {
            uint[] table = new uint[256];
            const uint polynomial = 0xEDB88320;

            for (uint i = 0; i < 256; i++)
            {
                uint crc = i;
                for (uint j = 8; j > 0; j--)
                {
                    if ((crc & 1) == 1)
                    {
                        crc = (crc >> 1) ^ polynomial;
                    }
                    else
                    {
                        crc >>= 1;
                    }
                }
                table[i] = crc;
            }

            return table;
        }

        public uint GetCurrentHashAsUInt32()
        {
            return ~crc; // Final CRC value (inverted)
        }

        public static uint HashToUInt32(ReadOnlySpan<byte> data)
        {
            var crc32 = new Crc32();
            crc32.Initialize();
            crc32.HashCore(data.ToArray(), 0, data.Length);
            return crc32.GetCurrentHashAsUInt32();
        }
    }

}
