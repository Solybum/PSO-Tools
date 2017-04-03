using System.Collections.Generic;
using Newtonsoft.Json;

namespace UnitxtGC
{
    [JsonObject(MemberSerialization.OptIn)]
    public class UnitxtGCFile
    {
        [JsonProperty] public List<List<short>> SomeTables;
        [JsonProperty] public List<UnitxtGCGroup> StringGroups;

        public UnitxtGCFile()
        {
            SomeTables = new List<List<short>>();
            StringGroups = new List<UnitxtGCGroup>();
        }
    }
}
