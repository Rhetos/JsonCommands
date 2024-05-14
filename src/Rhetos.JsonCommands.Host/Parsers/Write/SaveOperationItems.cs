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
using System;

namespace Rhetos.JsonCommands.Host.Parsers.Write
{
    public class SaveOperationItems
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
                    throw new ClientException($"Operation '{value}' doesn't exist! The allowed operations are Delete, Update and Insert (in any casing).");
            }
        }
        public IEntity[] Items { get; set; }

        private string _operation;

        public bool IsDelete => _operation.Equals("Delete", StringComparison.OrdinalIgnoreCase);
        public bool IsUpdate => _operation.Equals("Update", StringComparison.OrdinalIgnoreCase);
        public bool IsInsert => _operation.Equals("Insert", StringComparison.OrdinalIgnoreCase);
    }
}
