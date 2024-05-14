/*
    Copyright (C) 2014 Omega software d.o.o.

    This file is part of Rhetos.

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU Affero General Public License as
    published by the Free Software Foundation, either version 3 of the
    License, or (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU Affero General Public License for more details.

    You should have received a copy of the GNU Affero General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using Rhetos.Dom.DefaultConcepts;
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
                if (value.Where((op) => op.IsDelete).Count() > 1) throw new ClientException("There are multiple Delete elements. Only one is allowed");
                if (value.Where((op) => op.IsInsert).Count() > 1) throw new ClientException("There are multiple Insert elements. Only one is allowed");
                if (value.Where((op) => op.IsUpdate).Count() > 1) throw new ClientException("There are multiple Update elements. Only one is allowed");
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
