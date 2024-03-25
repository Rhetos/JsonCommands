using System.Collections.Generic;

namespace Rhetos.JsonCommands.Host.Parsers.Write
{
    public class Command
    {
        public string Entity { get; set; }
        public List<CommandItem> Operations { get; set; }
    }
}
