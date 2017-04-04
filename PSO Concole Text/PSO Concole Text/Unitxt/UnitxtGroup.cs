using System.Collections.Generic;
using Newtonsoft.Json;

namespace PSOCT.Unitxt
{
    [JsonObject(MemberSerialization.OptIn)]
    public class UnitxtGroup
    {
        [JsonProperty] public string name;
        [JsonProperty] public int count;
        [JsonProperty] public List<string> entries;
        
        // Ofsets for when writing back to binary
        public int groupOffset;
        public UnitxtGroup()
        {
            entries = new List<string>();
        }
    }
}
