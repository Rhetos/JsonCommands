using System.Collections.Generic;
using System.Linq;

namespace Rhetos.JsonCommands.Host.Parsers
{
    public class ErrorResponseMetadataParser
    {
        public static Dictionary<string, string> Parse(string message)
        {
            string[] metadata = message.Split(",");
            if(metadata.Any(element => !element.Contains(':'))) 
                return new Dictionary<string, string> { { "SystemMessage", message } };

            return metadata
                .Select(element => element.Split(':', 2))
                .ToDictionary(keySelector: (element) => element[0], elementSelector: (element) => element[1]);
        }
    }
}
