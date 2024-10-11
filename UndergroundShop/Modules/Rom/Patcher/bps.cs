using System;
using System.IO;
using System.Text;
using UndergroundShop.Management;

namespace UndergroundShop.Modules.Rom.Patcher
{
    public class BpsPatch
    {
        private byte[] Actions { get; init; }

        private enum ActionType
        {
            SourceRead = 0,
            TargetRead = 1,
            SourceCopy = 2,
            TargetCopy = 3
        }

        public int SourceSize { get; init; }
        public int TargetSize { get; init; }
        public string Metadata { get; init; }
        public uint SourceChecksum { get; init; }
        public uint TargetChecksum { get; init; }
        public uint? PatchChecksum { get; private set; }

        public BpsPatch(byte[] data)
        {
            if (data.Length < 12 || data[0] != 'B' || data[1] != 'P' || data[2] != 'S' || data[3] != '1')
                MessageManagement.ConsoleMessage("Patch header is not BPS1", 4);

            int readIndex = 4;
            SourceSize = (int)ReadNumber(data, ref readIndex);
            TargetSize = (int)ReadNumber(data, ref readIndex);
            int metadataSize = (int)ReadNumber(data, ref readIndex);
            Metadata = Encoding.UTF8.GetString(data, readIndex, metadataSize);
            int actionStartIndex = readIndex + metadataSize;
            Actions = new byte[data.Length - 12 - actionStartIndex];
            Array.Copy(data, actionStartIndex, Actions, 0, Actions.Length);
            readIndex = data.Length - 12;
            SourceChecksum = ReadUint(data, ref readIndex);
            TargetChecksum = ReadUint(data, ref readIndex);
            PatchChecksum = ReadUint(data, ref readIndex);

            uint computedPatchChecksum = Crc32.HashToUInt32(new ReadOnlySpan<byte>(data, 0, data.Length - 4));
            if (PatchChecksum != computedPatchChecksum)
            {
                MessageManagement.ConsoleMessage("Patch checksum is invalid", 4);
            }
        }

        private BpsPatch(byte[] source, byte[] target, string metadata, byte[] actions)
        {
            SourceSize = source.Length;
            TargetSize = target.Length;
            Metadata = metadata;
            SourceChecksum = Crc32.HashToUInt32(source);
            TargetChecksum = Crc32.HashToUInt32(target);
            Actions = actions;
            PatchChecksum = 0;
        }

        public byte[] GetBytes()
        {
            using var memoryStream = new MemoryStream();
            memoryStream.Write(new byte[] { (byte)'B', (byte)'P', (byte)'S', (byte)'1' });

            WriteNumber((uint)SourceSize, memoryStream);
            WriteNumber((uint)TargetSize, memoryStream);
            byte[] metadata = Encoding.UTF8.GetBytes(Metadata);
            WriteNumber((uint)metadata.Length, memoryStream);

            memoryStream.Write(metadata);
            memoryStream.Write(Actions);

            memoryStream.Write(UintToByte(SourceChecksum));
            memoryStream.Write(UintToByte(TargetChecksum));

            uint computedPatchChecksum = Crc32.HashToUInt32(memoryStream.ToArray());
            memoryStream.Write(UintToByte(computedPatchChecksum));

            return memoryStream.ToArray();
        }

        public byte[] Apply(byte[] source)
        {
            byte[] target = new byte[TargetSize];
            int outputOffset = 0, sourceRelativeOffset = 0, targetRelativeOffset = 0;
            int actionIndex = 0;

            while (actionIndex < Actions.Length)
            {
                uint data = ReadNumber(Actions, ref actionIndex);
                uint command = data & 3;
                uint length = (data >> 2) + 1;

                switch ((ActionType)command)
                {
                    case ActionType.SourceRead:
                        Buffer.BlockCopy(source, outputOffset, target, outputOffset, (int)length);
                        outputOffset += (int)length;
                        break;

                    case ActionType.TargetRead:
                        Buffer.BlockCopy(Actions, actionIndex, target, outputOffset, (int)length);
                        actionIndex += (int)length;
                        outputOffset += (int)length;
                        break;

                    case ActionType.SourceCopy:
                        data = ReadNumber(Actions, ref actionIndex);
                        sourceRelativeOffset += (data & 1) == 0 ? (int)(data >> 1) : -(int)(data >> 1);
                        Buffer.BlockCopy(source, sourceRelativeOffset, target, outputOffset, (int)length);
                        outputOffset += (int)length;
                        sourceRelativeOffset += (int)length;
                        break;

                    case ActionType.TargetCopy:
                        data = ReadNumber(Actions, ref actionIndex);
                        targetRelativeOffset += (data & 1) == 0 ? (int)(data >> 1) : -(int)(data >> 1);
                        Buffer.BlockCopy(target, targetRelativeOffset, target, outputOffset, (int)length);
                        outputOffset += (int)length;
                        targetRelativeOffset += (int)length;
                        break;
                }
            }

            return target;
        }

        private static uint ReadNumber(byte[] data, ref int readIndex)
        {
            ulong result = 0, shift = 1;

            while (true)
            {
                if (readIndex >= data.Length)
                    MessageManagement.ConsoleMessage("Attempted to read beyond buffer size.", 4);

                byte x = data[readIndex++];
                result += (byte)(x & 0x7F) * shift;
                if ((x & 0x80) != 0) break;
                shift <<= 7;
                result += shift;

                if (result > uint.MaxValue)
                    MessageManagement.ConsoleMessage("Number is out of allowed range", 4);
            }

            return (uint)result;
        }

        private static void WriteNumber(uint number, Stream data)
        {
            while (true)
            {
                byte x = (byte)(number & 0x7F);
                number >>= 7;
                if (number == 0)
                {
                    data.WriteByte((byte)(x | 0x80));
                    break;
                }
                data.WriteByte(x);
                number--;
            }
        }

        private static uint ReadUint(byte[] data, ref int readIndex)
        {
            if (readIndex + 4 > data.Length)
                MessageManagement.ConsoleMessage("Attempted to read beyond buffer size.", 4);

            uint result = BitConverter.ToUInt32(data, readIndex);
            readIndex += 4;
            return result;
        }

        private static byte[] UintToByte(uint value)
        {
            return BitConverter.GetBytes(value);
        }
    }
}
