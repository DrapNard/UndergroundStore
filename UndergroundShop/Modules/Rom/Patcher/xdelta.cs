using System;
using System.IO;
using UndergroundShop.Management;

namespace UndergroundShop.Modules.Rom.Patcher
{
    internal class Xdelta
    {
        public void ApplyPatch(string sourceFilePath, string patchFilePath, string outputFilePath)
        {
            using FileStream sourceStream = new FileStream(sourceFilePath, FileMode.Open, FileAccess.Read);
            using FileStream patchStream = new FileStream(patchFilePath, FileMode.Open, FileAccess.Read);
            using FileStream outputStream = new FileStream(outputFilePath, FileMode.Create, FileAccess.Write);

            try
            {
                // Validate the source file with checksum (optional, based on the patch file format)
                // Example: ValidateChecksum(sourceStream, patchStream);

                while (patchStream.Position < patchStream.Length)
                {
                    byte instruction = (byte)patchStream.ReadByte();

                    if (instruction == 0x01)  // Copy operation
                    {
                        int offset = ReadInt32(patchStream);
                        int length = ReadInt32(patchStream);

                        // Ensure the offset and length are within the bounds of the source file
                        if (offset < 0 || offset + length > sourceStream.Length)
                        {
                            throw new InvalidDataException("Invalid copy instruction: out of bounds.");
                        }

                        // Seek to the correct position in the source file
                        sourceStream.Seek(offset, SeekOrigin.Begin);

                        byte[] buffer = new byte[length];
                        sourceStream.Read(buffer, 0, length);
                        outputStream.Write(buffer, 0, length);
                    }
                    else if (instruction == 0x02)  // Add operation
                    {
                        int length = ReadInt32(patchStream);
                        byte[] data = new byte[length];

                        int bytesRead = patchStream.Read(data, 0, length);
                        if (bytesRead != length)
                        {
                            throw new InvalidDataException("Failed to read the expected number of bytes for the add operation.");
                        }

                        outputStream.Write(data, 0, length);
                    }
                    else
                    {
                        // Log unknown instructions and continue
                        MessageManagement.ConsoleMessage($"Unknown instruction {instruction} in patch file.", 4);
                    }
                }

                // Optionally, you can validate the output file checksum here, ensuring patching was successful
                // Example: ValidateOutputChecksum(outputStream);
            }
            catch (Exception ex)
            {
                // Error handling, ensuring exceptions are caught and logged appropriately
                MessageManagement.ConsoleMessage($"Error applying patch: {ex.Message}", 4);
                throw;
            }
        }

        private int ReadInt32(Stream stream)
        {
            byte[] buffer = new byte[4];
            int bytesRead = stream.Read(buffer, 0, 4);
            if (bytesRead != 4)
            {
                throw new InvalidDataException("Failed to read 4 bytes for a 32-bit integer.");
            }
            return BitConverter.ToInt32(buffer, 0);
        }

        // Implementing checksum validation using Adler-32 checksum (RFC 1950)
        private uint ComputeAdler32Checksum(Stream stream)
        {
            const uint MOD_ADLER = 65521;
            uint a = 1, b = 0;

            int byteValue;
            while ((byteValue = stream.ReadByte()) != -1)
            {
                a = (a + (uint)byteValue) % MOD_ADLER;
                b = (b + a) % MOD_ADLER;
            }

            return (b << 16) | a;
        }

        // Example checksum validation
        private void ValidateChecksum(FileStream sourceStream, FileStream patchStream)
        {
            // Reset the stream positions for checksum calculation
            sourceStream.Seek(0, SeekOrigin.Begin);
            patchStream.Seek(0, SeekOrigin.Begin);

            uint sourceChecksum = ComputeAdler32Checksum(sourceStream);
            uint patchChecksum = ComputeAdler32Checksum(patchStream);

            // Compare against the expected checksum values stored in the patch file
            // This part assumes the patch file has the checksum information

            // You could throw an exception or log a message if the checksums don't match
            if (sourceChecksum != patchChecksum)
            {
                throw new InvalidDataException("Source or patch file checksum does not match.");
            }
        }

        // Optionally, validate the output file checksum to ensure the patch was applied correctly
        private void ValidateOutputChecksum(FileStream outputStream)
        {
            // Reset the stream position
            outputStream.Seek(0, SeekOrigin.Begin);

            // Calculate checksum on the patched output
            uint outputChecksum = ComputeAdler32Checksum(outputStream);

            // Log the checksum or compare with an expected value if needed
            MessageManagement.ConsoleMessage($"Output checksum: {outputChecksum}", 1);
        }
    }
}
