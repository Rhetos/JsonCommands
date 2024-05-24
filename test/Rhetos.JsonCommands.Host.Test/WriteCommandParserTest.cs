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

using Microsoft.Extensions.DependencyInjection;
using Rhetos.JsonCommands.Host.Parsers.Write;
using Rhetos.JsonCommands.Host.Test.Tools;
using Rhetos.JsonCommands.Host.Utilities;
using System;
using System.Linq;
using Xunit;

namespace Rhetos.JsonCommands.Host.Test
{
    public class WriteCommandParserTest : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public WriteCommandParserTest(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Theory]
        [InlineData("", "Empty JSON.")]
        [InlineData("[", "Expected token type StartObject.")]
        [InlineData("[{]", "Invalid JSON format. At line 1, position 2. See the server log for more details on the error.", "Invalid property identifier character: ].")]
        [InlineData("[{}]", "There is an empty command.")]
        [InlineData("[{\"e\":{\"insert\":[]}}]", "Incorrect entity name 'e'.")]
        [InlineData("[{\"Common.Role\":{\"op\":[]}}]", "Invalid save operation 'op'.")]
        [InlineData("[{\"Common.Role\":{\"insert\":[],\"insert\":[]}}]", "There are multiple 'insert' operations. Please combine them into a single operation with multiple records.")]
        public void ParserTestShouldFail(string json, string clientError, string serverLog = null)
        {
            using var scope = _factory.Services.CreateScope();
            var parser = scope.ServiceProvider.GetRequiredService<WriteCommandsParser>();

            var ex = Assert.Throws<ClientException>(() => parser.Parse(json));
            Assert.StartsWith(clientError, ex.Message);
            if (serverLog != null)
            {
                Assert.DoesNotContain(serverLog, ex.Message);
                var error = scope.ServiceProvider.GetRequiredService<ErrorReporting>().CreateResponseFromException(ex, useLegacyErrorResponse: false);
                Assert.Contains(serverLog, error.LogMessage);
            }
        }

        [Fact]
        public void CorrectCommand()
        {
            using var scope = _factory.Services.CreateScope();
            var parser = scope.ServiceProvider.GetRequiredService<WriteCommandsParser>();

            Guid guid = new Guid();

            string json = $@"[
                {{
                    ""Bookstore.Book"": {{
                        ""Insert"": [
                            {{ ""ID"" : ""{guid}"", ""Name"": ""__Test__The Art of Computer Programming"" }}
                        ]
                    }}
                }}
            ]";

            var commands = parser.Parse(json);

            Assert.Equal(
                "Bookstore.Book insert:1 update: delete:",
                string.Join(", ", commands.Select(c => $"{c.Entity} insert:{c.DataToInsert?.Length} update:{c.DataToUpdate?.Length} delete:{c.DataToDelete?.Length}")));
        }

        [Fact]
        public void MultipleSameCommands()
        {
            using var scope = _factory.Services.CreateScope();
            var parser = scope.ServiceProvider.GetRequiredService<WriteCommandsParser>();

            string json = $@"[
                {{
                    ""Bookstore.Book"": {{
                        ""Insert"": [
                            {{ ""ID"" : ""{new Guid()}"", ""Name"": ""__Test1__The Art of Computer Programming"" }}
                        ],
                        ""Insert"": [
                            {{ ""ID"" : ""{new Guid()}"", ""Name"": ""__Test2__The Art of Computer Programming"" }}
                        ]
                    }}
                }}
            ]";

            var ex = Assert.Throws<ClientException>(() => parser.Parse(json));
            Assert.StartsWith("There are multiple 'Insert' operations.", ex.Message);
        }
    }
}
