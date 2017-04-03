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
        // Store the string offset when written back to binary
        // This is so I can write their pointers later on
        public List<int> stringOffsets;
        public UnitxtGCGroup()
        {
            entries = new List<string>();
            stringOffsets = new List<int>();
        }
    }
}
