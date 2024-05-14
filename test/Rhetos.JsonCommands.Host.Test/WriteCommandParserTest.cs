using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Rhetos.Dom;
using Rhetos.JsonCommands.Host.Parsers.Write;
using Rhetos.JsonCommands.Host.Test.Tools;
using System;
using System.Collections.Generic;
using System.CommandLine.Parsing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestApp;
using Xunit;

namespace Rhetos.JsonCommands.Host.Test
{
    public class WriteCommandParserTest
    {

        [Theory]
        [InlineData("", "Empty JSON.")]
        [InlineData("[", "Expected token type StartObject.")]
        [InlineData("[{]", "Invalid property identifier character: ].")]
        [InlineData("[{}]", "There is an empty command.")]
        public void ParserTestShouldFail(string json, string exceptionStartsWith)
        {
            var factory = new CustomWebApplicationFactory<Startup>();
            using var scope = factory.Services.CreateScope();
            var dom = scope.ServiceProvider.GetRequiredService<IRhetosComponent<IDomainObjectModel>>().Value;

            WriteCommandsParser parser = new WriteCommandsParser(json, dom);

            try
            {
                parser.Parse();
                Assert.True(false, "The parser.Parse() call should have thrown a JsonException.");
            }
            catch (JsonException ex)
            {
                Assert.StartsWith(exceptionStartsWith, ex.Message);
            }
            catch (Exception)
            {
                Assert.True(false, "The exception type is incorrect.");
            }
        }

        [Fact]
        public void CorrectCommand()
        {
            var factory = new CustomWebApplicationFactory<Startup>();
            using var scope = factory.Services.CreateScope();
            var dom = scope.ServiceProvider.GetRequiredService<IRhetosComponent<IDomainObjectModel>>().Value;

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

            WriteCommandsParser parser = new WriteCommandsParser(json, dom);

            List<Command> commands = parser.Parse();
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
            var dom = scope.ServiceProvider.GetRequiredService<IRhetosComponent<IDomainObjectModel>>().Value;

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

            WriteCommandsParser parser = new WriteCommandsParser(json, dom);

            try
            {
                List<Command> commands = parser.Parse();
                Assert.True(false, "The parser.Parse() call should have thrown a JsonException.");
            }
            catch (FormatException ex)
            {
                Assert.StartsWith("There are multiple Insert elements.", ex.Message);
            }
            catch (Exception)
            {
                Assert.True(false, "The exception type is incorrect.");
            }
        }
    }
}
