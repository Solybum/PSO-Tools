using System.Collections.Generic;
using Newtonsoft.Json;

namespace UnitxtGC
{
    [JsonObject(MemberSerialization.OptIn)]
    public class UnitxtGCGroup
    {
        [JsonProperty] public string name;
        [JsonProperty] public int count;
        [JsonProperty] public List<string> entries;
        
        // Ofsets for when writing back to binary
        public int groupOffset;
        public List<int> stringOffsets;
        public UnitxtGCGroup()
        {
            entries = new List<string>();
            stringOffsets = new List<int>();
        }
    }
}
