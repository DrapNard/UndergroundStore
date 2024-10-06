using System.IO;
using BpsNet;

namespace UndergroundShop.Modules.Rom.Patcher
{
    internal class Bps
    {
        public Bps(string Rom, string Patch)
        {
            byte[] original = File.ReadAllBytes(Rom);
            var patch = new BpsPatch(File.ReadAllBytes(Patch));
            byte[] patched = patch.Apply(original);
        }
    }
}
