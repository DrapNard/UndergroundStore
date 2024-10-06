using System.IO;
using PleOps.XdeltaSharp.Decoder;
using UndergroundShop.Management;

namespace UndergroundShop.Modules.Rom.Patcher
{
    internal class Xdelta
    {
        public Xdelta(string Rom, string Patch, string outputPath) 
        {
            var input = new FileStream(Rom, FileMode.Open);
            var patch = new FileStream(Patch, FileMode.Open);
            var output = new FileStream(outputPath, FileMode.Create);

            var decoder = new Decoder(input, patch, output);
            decoder.ProgressChanged += progress => MessageManagement.ConsoleMessage($"Patching progress: {progress}", 2);

            decoder.Run();
        }
    }
}
