using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FridgeScan.Models
{
    public class ProductInfo
    {
        public string Barcode { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public string ImageUrl { get; set; }
        public string? ThumbUrl { get; internal set; }
    }
}
