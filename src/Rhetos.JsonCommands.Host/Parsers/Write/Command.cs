using Rhetos.Dom.DefaultConcepts;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Rhetos.JsonCommands.Host.Parsers.Write
{
    public class Command
    {
        public string Entity { get; set; }
        public List<SaveOperationItems> Operations {
            get => _operations;
            set
            {
                if (value.Where((op) => op.IsDelete).Count() > 1) throw new FormatException("There are multiple Delete elements. Only one is allowed");
                if (value.Where((op) => op.IsInsert).Count() > 1) throw new FormatException("There are multiple Insert elements. Only one is allowed");
                if (value.Where((op) => op.IsUpdate).Count() > 1) throw new FormatException("There are multiple Update elements. Only one is allowed");
                _operations = value;
            }
        }

        private List<SaveOperationItems> _operations;
        public IEntity[] DeleteOperationItems()
        {
            return Operations.FirstOrDefault((op) => op.IsDelete)?.Items;
        }
        public IEntity[] InsertOperationItems()
        {
            return Operations.FirstOrDefault((op) => op.IsInsert)?.Items;
        }
        public IEntity[] UpdateOperationItems()
        {
            return Operations.FirstOrDefault((op) => op.IsUpdate)?.Items;
        }
    }
}
