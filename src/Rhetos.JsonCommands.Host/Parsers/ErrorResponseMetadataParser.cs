using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace Rhetos.JsonCommands.Host.Parsers
{
    public class ErrorResponseMetadataParser
    {
        public static Dictionary<string, string> Parse(string message)
        {
            try
            {
                return message.Split(",").Select(pair => pair.Split(':')).ToDictionary(
                    keySelector: (element) => element[0],
                    elementSelector: (element) => element[1]);
            }
            catch
            {
                return new Dictionary<string, string>
                {
                    { "SystemMessage", message }
                };
            }
        }
    }
}
