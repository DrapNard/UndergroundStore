using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UndergroundShop.Core.Source
{
    internal class SourceDictionary
    {
    }

    public class GameList
    {
        public required List<string> Info { get; set; }
        public required List<GameInfo> Games { get; set; } // Changed to List
    }
}
