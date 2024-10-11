using System;
using System.IO;
using UndergroundShop.Management;

namespace UndergroundShop.Modules.Rom.Patcher
{
    public class MODpatcher
    {
        public static void ApplyMOD(string romPath, string patchPath, string outputPath)
        {
            try
            {
                // Load ROM and MOD patch into byte arrays
                byte[] romData = File.ReadAllBytes(romPath);
                byte[] modData = File.ReadAllBytes(patchPath);

                // Validate the MOD patch header
                if (!ValidateMODHeader(modData))
                {
                    MessageManagement.ConsoleMessage("Invalid MOD patch file.", 4); // Error
                    return;
                }

                // Start applying patch blocks
                int patchOffset = 8; // Assuming the header is 8 bytes
                while (patchOffset < modData.Length)
                {
                    // Read the ROM offset (4 bytes)
                    int romOffset = BitConverter.ToInt32(modData, patchOffset);
                    patchOffset += 4;

                    // Read the block size (4 bytes)
                    int blockSize = BitConverter.ToInt32(modData, patchOffset);
                    patchOffset += 4;

                    // Apply the patch data to the ROM
                    for (int i = 0; i < blockSize; i++)
                    {
                        if (romOffset + i < romData.Length)
                        {
                            romData[romOffset + i] = modData[patchOffset];
                        }
                        patchOffset++;
                    }

                    MessageManagement.ConsoleMessage($"Patched {blockSize} bytes at offset {romOffset}.", 1); // Debug
                }

                // Save the patched ROM
                File.WriteAllBytes(outputPath, romData);
                MessageManagement.ConsoleMessage("MOD patch applied successfully.", 2); // Information
            }
            catch (Exception ex)
            {
                MessageManagement.ConsoleMessage($"Error applying MOD patch: {ex.Message}", 5); // Fatal error
            }
        }

        // Helper method to validate the MOD patch header
        private static bool ValidateMODHeader(byte[] modData)
        {
            // Example header validation: Check for a specific MOD signature
            string modSignature = "MODPATCH";
            for (int i = 0; i < modSignature.Length; i++)
            {
                if (modData[i] != modSignature[i])
                {
                    return false;
                }
            }
            return true;
        }
    }
}
