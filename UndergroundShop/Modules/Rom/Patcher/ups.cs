using System;
using System.IO;
using System.Security.Cryptography;
using UndergroundShop.Management;

namespace UndergroundShop.Modules.Rom.Patcher
{
    public class UPSpatcher
    {
        public static void ApplyUPS(string romPath, string patchPath, string outputPath)
        {
            // Read the ROM and Patch files
            byte[] romData = File.ReadAllBytes(romPath);
            byte[] patchData = File.ReadAllBytes(patchPath);

            // Parse the UPS patch file
            PatchInfo patchInfo = ParseUPSPatch(patchData, romData.Length);

            if (patchInfo == null)
            {
                // Fatal error case
                MessageManagement.ConsoleMessage("Error in parsing UPS patch.", 5);
                return;
            }

            // Apply the patch by XORing the differences
            byte[] patchedRomData = ApplyPatch(patchInfo, romData);

            // Validate CRC32 checksum
            if (!VerifyCRC32(patchedRomData, patchInfo.CRC32Patched))
            {
                MessageManagement.ConsoleMessage("Checksum validation failed for patched ROM.", 4); // Error
                return;
            }

            // Save the patched ROM
            File.WriteAllBytes(outputPath, patchedRomData);
            MessageManagement.ConsoleMessage("Patch applied successfully.", 2); // Information
        }

        // Method to parse the UPS patch file structure
        private static PatchInfo ParseUPSPatch(byte[] patchData, int originalRomSize)
        {
            PatchInfo patchInfo = new();

            // The patchData should start with the 'UPS1' header, check for that
            if (patchData[0] != 'U' || patchData[1] != 'P' || patchData[2] != 'S' || patchData[3] != '1')
            {
                MessageManagement.ConsoleMessage("Invalid UPS file header.", 4); // Error
                return new PatchInfo(); // Retourne un objet vide au lieu de null
            }

            // Read sizes from patch
            int inputSize = ReadVariableLengthInteger(patchData, 4);
            int outputSize = ReadVariableLengthInteger(patchData, 8);

            MessageManagement.ConsoleMessage($"Input ROM size: {inputSize}, Output ROM size: {outputSize}", 1); // Debug

            // Ensure sizes are correct
            if (inputSize != originalRomSize)
            {
                MessageManagement.ConsoleMessage("Input ROM size does not match the expected size in the UPS patch.", 4); // Error
                return new PatchInfo(); // Retourne un objet vide au lieu de null
            }

            // Now extract CRC32 checksums (input, output, and patch CRC)
            patchInfo.CRC32Original = BitConverter.ToUInt32(patchData, patchData.Length - 12);
            patchInfo.CRC32Patched = BitConverter.ToUInt32(patchData, patchData.Length - 8);
            patchInfo.CRC32Patch = BitConverter.ToUInt32(patchData, patchData.Length - 4);

            MessageManagement.ConsoleMessage("Parsed UPS patch successfully.", 2); // Information

            return patchInfo;
        }

        // Method to apply the patch using XOR on the ROM data
        private static byte[] ApplyPatch(PatchInfo patchInfo, byte[] romData)
        {
            byte[] patchedRomData = new byte[romData.Length];
            Array.Copy(romData, patchedRomData, romData.Length);

            // Apply XOR differences
            int offset = 0;
            foreach (var diff in patchInfo.Differences)
            {
                patchedRomData[offset] ^= diff;
                offset++;
            }

            MessageManagement.ConsoleMessage("Patch applied through XOR.", 1); // Debug

            return patchedRomData;
        }

        // Method to read a variable-length integer from the patch data
        private static int ReadVariableLengthInteger(byte[] patchData, int start)
        {
            int result = 0;
            int shift = 0;

            for (int i = start; i < patchData.Length; i++)
            {
                byte currentByte = patchData[i];
                result |= (currentByte & 0x7F) << shift;
                shift += 7;
                if ((currentByte & 0x80) == 0) break;
            }

            MessageManagement.ConsoleMessage($"Read variable length integer: {result}", 0); // Verbose

            return result;
        }

        // Method to verify CRC32 checksum of the patched ROM
        private static bool VerifyCRC32(byte[] data, uint expectedCRC)
        {
            using var crc32 = new Crc32();
            uint computedCRC = crc32.ComputeChecksum(data);
            MessageManagement.ConsoleMessage($"Computed CRC: {computedCRC}, Expected CRC: {expectedCRC}", 1); // Debug
            return computedCRC == expectedCRC;
        }
    }

    // Utility class to hold patch information
    public class PatchInfo
    {
        public uint CRC32Original { get; set; }
        public uint CRC32Patched { get; set; }
        public uint CRC32Patch { get; set; }
        public byte[] Differences { get; set; } = []; // Initialisation avec un tableau vide
    }
}
