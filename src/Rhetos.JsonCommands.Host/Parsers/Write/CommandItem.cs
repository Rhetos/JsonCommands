using Rhetos.Dom.DefaultConcepts;
using System;

namespace Rhetos.JsonCommands.Host.Parsers.Write
{
    public class CommandItem
    {
        public string Operation 
        { 
            get => _operation;
            set
            {
                if (value.Equals("Delete", StringComparison.OrdinalIgnoreCase)
                    || value.Equals("Update", StringComparison.OrdinalIgnoreCase)
                    || value.Equals("Insert", StringComparison.OrdinalIgnoreCase))
                    _operation = value;
                else
                    throw new ClientException($"Operation {{{value}}} doesn't exist! The allowed operations are Delete, Update and Insert (in any casing).");
            }
        }
        public IEntity[] Items { get; set; }

        private string _operation;
    }
}
