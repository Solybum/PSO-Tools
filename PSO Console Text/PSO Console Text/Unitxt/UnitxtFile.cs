using System.Collections.Generic;
using Newtonsoft.Json;

namespace PSOCT.Unitxt
{
    [JsonObject(MemberSerialization.OptIn)]
    public class UnitxtFile
    {
        [JsonProperty] public int tableValue;
        [JsonProperty] public List<List<short>> SomeTables;
        [JsonProperty] public List<List<byte>> SomeTables2;
        [JsonProperty] public List<UnitxtGroup> StringGroups;

        public UnitxtFile()
        {
            SomeTables = new List<List<short>>();
            SomeTables2 = new List<List<byte>>();
            StringGroups = new List<UnitxtGroup>();
        }
    }
}
