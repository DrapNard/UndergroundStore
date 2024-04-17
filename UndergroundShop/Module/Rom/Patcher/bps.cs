using System.IO;
using BpsNet;

namespace UndergroundShop.Module.Rom.Patcher
{
    internal class bps
    {
        public bps(string Rom, string Patch)
        {
            byte[] original = File.ReadAllBytes(Rom);
            var patch = new BpsPatch(File.ReadAllBytes(Patch));
            byte[] patched = patch.Apply(original);
        }
    }
}
