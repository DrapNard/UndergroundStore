using System;
using System.IO;
using PleOps.XdeltaSharp.Decoder;

namespace UndergroundShop.Module.Rom.Patcher
{
    internal class xdelta
    {
        public xdelta(string Rom, string Patch, string outputPath) 
        {
            var input = new FileStream(Rom, FileMode.Open);
            var patch = new FileStream(Patch, FileMode.Open);
            var output = new FileStream(outputPath, FileMode.Create);

            var decoder = new Decoder(input, patch, output);
            decoder.ProgressChanged += progress => Console.WriteLine($"Patching progress: {progress}");

            decoder.Run();
        }
    }
}
