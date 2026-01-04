using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FridgeScan.Models
{
    public class PexelsImage
    {
        public string Src { get; set; }
    }

    public class PexelsSearchResponse
    {
        public List<PexelsPhoto> Photos { get; set; }
    }

    public class PexelsPhoto
    {
        public PexelsSrc Src { get; set; }
    }

    public class PexelsSrc
    {
        public string Original { get; set; }
    }

}
