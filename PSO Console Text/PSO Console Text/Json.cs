using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace PSOCT
{
    public abstract class Json
    {
        public static string Serialize(object data, bool format = false)
        {
            StringBuilder sb = new StringBuilder();
            JsonTextWriter jw = new JsonTextWriter(new StringWriter(sb));
            JsonSerializer js = new JsonSerializer();

            if (format)
            {
                jw.Formatting = Formatting.Indented;
                jw.Indentation = 4;
                jw.IndentChar = ' ';
            }

            js.Serialize(jw, data);

            return sb.ToString();
        }
        public static T Deserialize<T>(byte[] data, int offset, int count)
        {
            using (StreamReader sr = new StreamReader(new MemoryStream(data, offset, count)))
            {
                JsonSerializer json = new JsonSerializer();
                json.ObjectCreationHandling = ObjectCreationHandling.Replace;
                return json.Deserialize<T>(new JsonTextReader(sr));
            }
        }
    }
}
