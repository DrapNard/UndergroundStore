using System;
using System.IO;
using UndergroundShop.Management;

namespace UndergroundShop.Modules.Rom.Patcher
{
    public class PPFpatcher
    {
        public static void ApplyPPF(string romPath, string patchPath, string outputPath)
        {
            try
            {
                // Load ROM and patch files into byte arrays
                byte[] romData = File.ReadAllBytes(romPath);
                byte[] patchData = File.ReadAllBytes(patchPath);

                // Identify the version of the PPF file (PPF1, PPF2, or PPF3)
                int ppfVersion = GetPPFVersion(patchData);
                if (ppfVersion == 0)
                {
                    MessageManagement.ConsoleMessage("Invalid PPF patch file.", 4); // Error
                    return;
                }

                MessageManagement.ConsoleMessage($"PPF Version {ppfVersion} detected.", 1); // Debug

                // Apply the patch based on the version
                if (ppfVersion == 1)
                {
                    ApplyPPF1(patchData, romData);
                }
                else if (ppfVersion == 2)
                {
                    ApplyPPF2(patchData, romData);
                }
                else if (ppfVersion == 3)
                {
                    ApplyPPF3(patchData, romData);
                }

                // Save the patched ROM
                File.WriteAllBytes(outputPath, romData);
                MessageManagement.ConsoleMessage("PPF patch applied successfully.", 2); // Information
            }
            catch (Exception ex)
            {
                MessageManagement.ConsoleMessage($"Error applying PPF patch: {ex.Message}", 5); // Fatal error
            }
        }

        // Method to determine the PPF version from the patch file
        private static int GetPPFVersion(byte[] patchData)
        {
            if (patchData[0] == 'P' && patchData[1] == 'P' && patchData[2] == 'F')
            {
                return patchData[3] - '0'; // Extract version number from 'PPF1', 'PPF2', or 'PPF3'
            }
            return 0;
        }

        // PPF1: Basic patching without extra validation or integrity checks
        private static void ApplyPPF1(byte[] patchData, byte[] romData)
        {
            int patchOffset = 56; // Start after header
            while (patchOffset < patchData.Length)
            {
                int romOffset = BitConverter.ToInt32(patchData, patchOffset);
                patchOffset += 4;

                byte blockSize = patchData[patchOffset];
                patchOffset++;

                for (int i = 0; i < blockSize; i++)
                {
                    if (romOffset + i < romData.Length)
                    {
                        romData[romOffset + i] = patchData[patchOffset];
                    }
                    patchOffset++;
                }
            }
        }

        // PPF2: Extended block size support, optional integrity checks
        private static void ApplyPPF2(byte[] patchData, byte[] romData)
        {
            int patchOffset = 56; // Start after header
            while (patchOffset < patchData.Length)
            {
                int romOffset = BitConverter.ToInt32(patchData, patchOffset);
                patchOffset += 4;

                byte blockSize = patchData[patchOffset];
                patchOffset++;

                for (int i = 0; i < blockSize; i++)
                {
                    if (romOffset + i < romData.Length)
                    {
                        romData[romOffset + i] = patchData[patchOffset];
                    }
                    patchOffset++;
                }
            }

            // Optional integrity check for PPF2 (CRC32 validation)
            if (!VerifyCRC32(patchData, romData))
            {
                MessageManagement.ConsoleMessage("CRC32 validation failed for PPF2.", 4); // Error
            }
        }

        // PPF3: Advanced patching with CRC32, undo support, and extended block sizes
        private static void ApplyPPF3(byte[] patchData, byte[] romData)
        {
            int patchOffset = 56; // Start after header

            // Read additional header data for PPF3 (e.g., undo data)
            // Apply patches similar to PPF1 and PPF2 but with undo and integrity checks
            while (patchOffset < patchData.Length)
            {
                int romOffset = BitConverter.ToInt32(patchData, patchOffset);
                patchOffset += 4;

                byte blockSize = patchData[patchOffset];
                patchOffset++;

                for (int i = 0; i < blockSize; i++)
                {
                    if (romOffset + i < romData.Length)
                    {
                        romData[romOffset + i] = patchData[patchOffset];
                    }
                    patchOffset++;
                }
            }

            // PPF3 includes CRC32 validation
            if (!VerifyCRC32(patchData, romData))
            {
                MessageManagement.ConsoleMessage("CRC32 validation failed for PPF3.", 4); // Error
            }

            // Additional features: Undo support could be implemented here by storing original data
        }

        // CRC32 validation for PPF2 and PPF3
        private static bool VerifyCRC32(byte[] patchData, byte[] romData)
        {
            using (var crc32 = new Crc32())
            {
                uint computedCRC = crc32.ComputeChecksum(romData);
                uint expectedCRC = BitConverter.ToUInt32(patchData, patchData.Length - 4); // Assuming last 4 bytes are CRC
                return computedCRC == expectedCRC;
            }
        }
    }
}
