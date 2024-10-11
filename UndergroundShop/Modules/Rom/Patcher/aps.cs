using System;
using System.IO;
using UndergroundShop.Management;

namespace UndergroundShop.Modules.Rom.Patcher
{
    public class APSpatcher
    {
        public static void ApplyAPS(string romPath, string patchPath, string outputPath)
        {
            try
            {
                // Read ROM and patch files into byte arrays
                byte[] romData = File.ReadAllBytes(romPath);
                byte[] patchData = File.ReadAllBytes(patchPath);

                // Parse the APS patch file and apply changes
                ApplyPatch(romData, patchData);

                // Save the patched ROM
                File.WriteAllBytes(outputPath, romData);
                MessageManagement.ConsoleMessage("APS patch applied successfully.", 2); // Information
            }
            catch (Exception ex)
            {
                MessageManagement.ConsoleMessage($"Error applying APS patch: {ex.Message}", 5); // Fatal error
            }
        }

        // Method to apply the APS patch to the ROM data
        private static void ApplyPatch(byte[] romData, byte[] patchData)
        {
            int patchOffset = 0;
            while (patchOffset < patchData.Length)
            {
                // Read the offset from the patch file (4 bytes)
                int romOffset = BitConverter.ToInt32(patchData, patchOffset);
                patchOffset += 4;

                // Read the block size (2 bytes) that indicates how many bytes to modify
                int blockSize = BitConverter.ToUInt16(patchData, patchOffset);
                patchOffset += 2;

                // Apply the patch data block
                for (int i = 0; i < blockSize; i++)
                {
                    if (romOffset + i < romData.Length)
                    {
                        romData[romOffset + i] = patchData[patchOffset];
                    }
                    patchOffset++;
                }

                MessageManagement.ConsoleMessage($"Patched {blockSize} bytes at offset {romOffset}.", 1); // Debug
            }
        }
    }
}
