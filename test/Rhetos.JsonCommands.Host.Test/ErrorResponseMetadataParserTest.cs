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

using Rhetos.JsonCommands.Host.Parsers;
using System.Linq;
using Xunit;

namespace Rhetos.JsonCommands.Host.Test
{
    public class ErrorResponseMetadataParserTest
    {
        [Theory]
        // Simple key-value:
        [InlineData("singleKey:singleValue", "singleKey-singleValue")]
        [InlineData("key1:value1,key2:value2,key3:value3", "key1-value1,key2-value2,key3-value3")]
        [InlineData("key 1:value 1,key 2:value 2", "key 1-value 1,key 2-value 2")]
        [InlineData("key1:value.1,key2:value.2", "key1-value.1,key2-value.2")]
        // Nonparsable:
        [InlineData("Nonparsable", "SystemMessage-Nonparsable")]
        [InlineData("Nonparsable,2", "SystemMessage-Nonparsable,2")]
        [InlineData("key1,value1", "SystemMessage-key1,value1")]
        [InlineData("key1:value,1,key2:value,2", "SystemMessage-key1:value,1,key2:value,2")]
        [InlineData("k:v,", "SystemMessage-k:v,")]
        [InlineData(":", "SystemMessage-:")]
        [InlineData(",", "SystemMessage-,")]
        [InlineData(":,", "SystemMessage-:,")]
        // Null and empty:
        [InlineData(null, null)]
        [InlineData("", "SystemMessage-")]
        // Colon in value might be valid:
        [InlineData("key1:value:1", "key1-value:1")]
        [InlineData("key1:value:1,key2:value:2", "key1-value:1,key2-value:2")]
        // Whitespaces in keys:
        [InlineData("key1:value1, key2:value2 , key3:value3 ", "key1-value1,key2-value2 ,key3-value3 ")]
        public void ParseTest(string systemMessage, string expectedMetadataReport)
        {
            var metadata = ErrorResponseMetadataParser.Parse(systemMessage);
            string report = metadata != null ? string.Join(",", metadata.Select(e => $"{e.Key}-{e.Value}")) : null;
            Assert.Equal(expectedMetadataReport, report);
        }
    }
}
