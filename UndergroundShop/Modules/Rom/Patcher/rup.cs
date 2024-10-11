using System;
using System.IO;
using UndergroundShop.Management;

namespace UndergroundShop.Modules.Rom.Patcher
{
    public class RUPpatcher
    {
        public static void ApplyRUP(string romPath, string patchPath, string outputPath)
        {
            try
            {
                // Load ROM and patch files
                byte[] romData = File.ReadAllBytes(romPath);
                byte[] patchData = File.ReadAllBytes(patchPath);

                // Validate the RUP header (first 3 bytes)
                if (patchData[0] != 'R' || patchData[1] != 'U' || patchData[2] != 'P')
                {
                    MessageManagement.ConsoleMessage("Invalid RUP patch file.", 4); // Error
                    return;
                }

                // Start applying the patch data
                int patchOffset = 4; // Skip the header
                while (patchOffset < patchData.Length)
                {
                    // Read the offset (4 bytes)
                    int romOffset = BitConverter.ToInt32(patchData, patchOffset);
                    patchOffset += 4;

                    // Read the block size (2 bytes)
                    int blockSize = BitConverter.ToUInt16(patchData, patchOffset);
                    patchOffset += 2;

                    // Apply the patch data to the ROM
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

                // Save the patched ROM
                File.WriteAllBytes(outputPath, romData);
                MessageManagement.ConsoleMessage("RUP patch applied successfully.", 2); // Information
            }
            catch (Exception ex)
            {
                MessageManagement.ConsoleMessage($"Error applying RUP patch: {ex.Message}", 5); // Fatal error
            }
        }
    }
}
