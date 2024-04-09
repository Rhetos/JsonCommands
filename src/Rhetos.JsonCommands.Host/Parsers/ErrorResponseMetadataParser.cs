using Newtonsoft.Json;
using System.Collections.Generic;

namespace Rhetos.JsonCommands.Host.Parsers
{
    public class ErrorResponseMetadataParser
    {
        public static Dictionary<string, string> Parse(string json)
        {
            try
            {
                return JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
            }
            catch
            {
                return new Dictionary<string, string>
                {
                    { "SystemMessage", json }
                };
            }
        }
    }
}
