using System.Collections.Generic;
using System.Linq;

namespace Rhetos.JsonCommands.Host.Parsers.Write
{
    public class Command
    {
        public string Entity { get; set; }
        public List<CommandItem> Operations { get; set; }
        public List<CommandItem> DeleteOperations => Operations.Where((op) => op.IsDelete).ToList();
        public List<CommandItem> InsertOperations => Operations.Where((op) => op.IsInsert).ToList();
        public List<CommandItem> UpdateOperations => Operations.Where((op) => op.IsUpdate).ToList();
    }
}
