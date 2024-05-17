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

using System.Collections.Generic;
using System.Linq;

namespace Rhetos.JsonCommands.Host.Parsers
{
    public class ErrorResponseMetadataParser
    {
        public static Dictionary<string, string> Parse(string message)
        {
            if (message == null)
                return null;

            string[] metadata = message.Split(",");
            if (!metadata.Any(element => !element.Contains(':')))
            {
                var result = metadata
                    .Select(element => element.Split(':', 2))
                    .ToDictionary(keySelector: (element) => element[0].Trim(), elementSelector: (element) => element[1]);

                if (result.All(r => !string.IsNullOrEmpty(r.Key)))
                {
                    return result;
                }
            }
            return new Dictionary<string, string> { { "SystemMessage", message } };
        }
    }
}
