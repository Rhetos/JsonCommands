using System.Collections.Generic;
using System.Linq;

namespace Rhetos.JsonCommands.Host.Parsers.Write
{
    public class Command
    {
        public string Entity { get; set; }
        public List<SaveOperationItems> Operations { get; set; }
        public List<SaveOperationItems> DeleteOperations => Operations.Where((op) => op.IsDelete).ToList();
        public List<SaveOperationItems> InsertOperations => Operations.Where((op) => op.IsInsert).ToList();
        public List<SaveOperationItems> UpdateOperations => Operations.Where((op) => op.IsUpdate).ToList();
    }
}
