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
using System.Collections.Generic;
using TestApp;
using Xunit;

namespace Rhetos.JsonCommands.Host.Test
{
    public class WriteCommandParserTest
    {

        [Theory]
        [InlineData("", "Empty JSON.")]
        [InlineData("[", "Expected token type StartObject.")]
        [InlineData("[{]", "Invalid JSON format. At line 1, position 2. See the server log for more details on the error.", "Invalid property identifier character: ].")]
        [InlineData("[{}]", "There is an empty command.")]
        public void ParserTestShouldFail(string json, string clientError, string serverLog = null)
        {
            var factory = new CustomWebApplicationFactory<Startup>();
            using var scope = factory.Services.CreateScope();
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
            var factory = new CustomWebApplicationFactory<Startup>();
            using var scope = factory.Services.CreateScope();
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

            List<Command> commands = parser.Parse(json);
            Assert.Single(commands);
            Assert.Equal("Bookstore.Book", commands[0].Entity);
            Assert.Single(commands[0].Operations);
            Assert.True(commands[0].Operations[0].IsInsert);
        }

        [Fact]
        public void MultipleSameCommands()
        {
            var factory = new CustomWebApplicationFactory<Startup>();
            using var scope = factory.Services.CreateScope();
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
            Assert.StartsWith("There are multiple Insert elements.", ex.Message);
        }
    }
}
