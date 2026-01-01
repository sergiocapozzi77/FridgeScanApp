using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace FridgeScan.Models
{
    public class Activity
    {
        public string Type { get; set; }

        public string Source { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("product_name")]
        public string? ProductName { get; set; }

        public string? Metadata { get; set; }

        [JsonPropertyName("$id")]
        public string RowId { get; set; } // maps $id

        [JsonIgnore]
        public string Description
        {
            get
            {
                return $"{Type} - {ProductName} (Source: {Source})";
            }
        }
    }
}
