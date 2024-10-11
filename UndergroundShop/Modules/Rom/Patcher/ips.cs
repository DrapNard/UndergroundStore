using System.IO;
using System.Text;
using UndergroundShop.Management;

namespace UndergroundShop.Modules.Rom.Patcher
{
    public class IpsPatcher
    {
        public static void ApplyPatch(string patchFile, string targetFile, string outputFile)
        {
            using (var patch = new FileStream(patchFile, FileMode.Open, FileAccess.Read))
            using (var target = new FileStream(targetFile, FileMode.Open, FileAccess.Read))
            using (var output = new FileStream(outputFile, FileMode.Create, FileAccess.Write))
            {
                // Read and validate IPS header ('PATCH')
                byte[] header = new byte[5];
                patch.Read(header, 0, 5);
                if (Encoding.ASCII.GetString(header) != "PATCH")
                    MessageManagement.ConsoleMessage("Invalid IPS patch file", 4);

                // Copy target file content to output
                target.CopyTo(output);

                // Process each patch record
                while (true)
                {
                    byte[] offsetBytes = new byte[3];
                    patch.Read(offsetBytes, 0, 3);
                    int offset = (offsetBytes[0] << 16) | (offsetBytes[1] << 8) | offsetBytes[2];

                    // End of file
                    if (offset == 0x454F46) // 'EOF' marker
                        break;

                    byte[] sizeBytes = new byte[2];
                    patch.Read(sizeBytes, 0, 2);
                    int size = (sizeBytes[0] << 8) | sizeBytes[1];

                    if (size == 0) // RLE block
                    {
                        byte[] rleSizeBytes = new byte[2];
                        patch.Read(rleSizeBytes, 0, 2);
                        int rleSize = (rleSizeBytes[0] << 8) | rleSizeBytes[1];

                        byte[] rleByte = new byte[1];
                        patch.Read(rleByte, 0, 1);

                        WriteToOutput(output, offset, rleByte, rleSize);
                    }
                    else // Standard patch block
                    {
                        byte[] data = new byte[size];
                        patch.Read(data, 0, size);
                        WriteToOutput(output, offset, data, size);
                    }
                }
            }
        }

        private static void WriteToOutput(Stream output, int offset, byte[] data, int length)
        {
            output.Seek(offset, SeekOrigin.Begin);
            output.Write(data, 0, length);
        }
    }
}
