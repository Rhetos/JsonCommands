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
using Newtonsoft.Json;
using Rhetos.Dom.DefaultConcepts;
using Rhetos.JsonCommands.Host.Test.Tools;
using Rhetos.JsonCommands.Host.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Rhetos.JsonCommands.Host.Test
{
    public class FilterParametersTest : IDisposable, IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly ITestOutputHelper _output;

        public FilterParametersTest(ITestOutputHelper output, CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _output = output;
        }

        public void Dispose()
        {
            _factory.Dispose();
            GC.SuppressFinalize(this);
        }

        [Theory]
        // Generic filter:
        [InlineData(false, "Common.Claim", "[{\"Property\":\"ID\",\"Operation\":\"in\",\"Value\":[\"4a5c23ff-6525-4e17-b848-185e082a6974\",\"0f6618b1-074a-482f-be74-c6e394641209\"]}]")]
        // Specific filter with simplified class name:
        [InlineData(false, "Common.Claim", "[{\"Filter\":\"IEnumerable<Guid>\",\"Value\":[\"4a5c23ff-6525-4e17-b848-185e082a6974\",\"0f6618b1-074a-482f-be74-c6e394641209\"]}]")]
        [InlineData(false, "Common.Claim", "[{\"Filter\":\"Guid[]\",\"Value\":[\"4a5c23ff-6525-4e17-b848-185e082a6974\",\"0f6618b1-074a-482f-be74-c6e394641209\"]}]")]
        [InlineData(true, "Common.Claim", "[{\"Filter\":\"System.Guid[]\",\"Value\":[\"4a5c23ff-6525-4e17-b848-185e082a6974\",\"0f6618b1-074a-482f-be74-c6e394641209\"]}]")]
        [InlineData(true, "Common.Claim", "[{\"Filter\":\"System.Collections.Generic.IEnumerable`1[[System.Guid]]\",\"Value\":[\"4a5c23ff-6525-4e17-b848-185e082a6974\",\"0f6618b1-074a-482f-be74-c6e394641209\"]}]")]
        // Specific filter with parameter name:
        [InlineData(false, "Common.MyClaim", "[{\"Filter\":\"Common.Claim\",\"Value\":{\"ClaimResource\":\"Common.RolePermission\",\"ClaimRight\":\"Read\"}}]")]
        [InlineData(false, "Common.MyClaim", "[{\"Filter\":\"Claim\",\"Value\":{\"ClaimResource\":\"Common.RolePermission\",\"ClaimRight\":\"Read\"}}]")]
        // Deactivatable:
        [InlineData(false, "TestDeactivatable.BasicEnt", "")]
        [InlineData(true, "TestDeactivatable.BasicEnt", "[{\"Filter\":\"Rhetos.Dom.DefaultConcepts.ActiveItems,%20Rhetos.Dom.DefaultConcepts.Interfaces\"}]")]
        [InlineData(false, "TestDeactivatable.BasicEnt", "[{\"Filter\":\"Rhetos.Dom.DefaultConcepts.ActiveItems\"}]")]
        [InlineData(false, "TestDeactivatable.BasicEnt", "[{\"Filter\":\"ActiveItems\"}]")]
        // DateTime (old MS format):
        [InlineData(false, "TestHistory.Standard", "")]
        [InlineData(false, "TestHistory.Standard", "[{\"Filter\":\"System.DateTime\",\"Value\":\"/Date(1544195644420%2B0100)/\"}]")]
        public async Task SupportedFilterParameters(bool dynamicOnly, string entity, string filters)
        {
            string url = string.IsNullOrWhiteSpace(filters)
                ? $@"jc/read?q=[{{""{entity}"": {{}}}}]"
                : $@"jc/read?q=[{{""{entity}"": {{""filters"":{filters}}}}}]";

            await TestSupportedFilterParameters(false, url, shouldFail: dynamicOnly);
            await TestSupportedFilterParameters(true, url, shouldFail: false);
        }

        private async Task TestSupportedFilterParameters(bool dynamicTypeResolution, string url, bool shouldFail)
        {
            var response = await TestApiHelper.HttpGet(url, _factory, _output,
                builder =>
                {
                    if (dynamicTypeResolution)
                        builder.SetRhetosDynamicTypeResolution();
                });

            Assert.Equal(shouldFail ? (int)HttpStatusCode.BadRequest : (int)HttpStatusCode.OK, response.StatusCode);
        }

        const string urlQueryEscaped =    @"jc/read?q=[{""TestHistory.Standard"": {""filters"":[{""Filter"":""System.DateTime"",""Value"":""/Date(1544195644420%2b0100)/""}]}}]";
        const string urlQueryNotEscaped = @"jc/read?q=[{""TestHistory.Standard"": {""filters"":[{""Filter"":""System.DateTime"",""Value"":""/Date(1544195644420+0100)/""}]}}]";

        [Fact]
        public async Task JsonValueTypeAsStringWithCorrectFormat()
        {
            var response = await TestApiHelper.HttpGet(urlQueryEscaped, _factory, _output);

            Assert.Equal(
                "200 {\"Data\":[{\"Records\":[]}]}",
                $"{response.StatusCode} {response.Content}");

            var response2 = await TestApiHelper.HttpGet(urlQueryEscaped.Replace(@"""filters""", @"""ReadTotalCount"":true, ""filters"""), _factory, _output);

            Assert.Equal(
                "200 {\"Data\":[{\"Records\":[],\"TotalCount\":0}]}",
                $"{response.StatusCode} {response2.Content}");

            var response3 = await TestApiHelper.HttpGet(urlQueryEscaped.Replace(@"""filters""", @"""ReadRecords"":false, ""ReadTotalCount"":true, ""filters"""), _factory, _output);

            Assert.Equal(
                "200 {\"Data\":[{\"TotalCount\":0}]}",
                $"{response.StatusCode} {response3.Content}");
        }

        [Fact]
        public async Task JsonValueTypeAsStringWithIncorrectFormat()
        {
            var response = await TestApiHelper.HttpGet(urlQueryNotEscaped, _factory, _output, host => host.UseLegacyErrorResponse());

            Assert.Equal<object>(
                "400 {\"UserMessage\":\"Operation could not be completed because the request sent to the server was not valid or not properly formatted.\""
                    + ",\"SystemMessage\":\"The request has invalid JSON format. See the server log for more information.\"}",
                $"{response.StatusCode} {response.Content}");

            string[] exceptedLogPatterns = new[] {
                "[Information]",
                "Rhetos.ClientException: The request has invalid JSON format",
                "/Date(1544195644420 0100)/",
                "Error parsing comment. Expected: *, got D. Path '', line 1, position 1.",
                "Filter parameter: '/Date(1544195644420 0100)/'."
                // The command summary is not reported by ProcessingEngine, because the ClientException occurred before the command was constructed.
            };
            Assert.Equal(1, response.LogEntries.Select(e => e.ToString()).Count(
                entry => exceptedLogPatterns.All(pattern => entry.Contains(pattern))));
        }

        [Fact]
        public void LimitedDeserialization()
        {
            // This test checks that the QueryParameters will try to deserialized only types that are specified on the DataStructure as read parameters.

            using var scope = _factory.Services.CreateScope();
            var queryParameters = scope.ServiceProvider.GetRequiredService<QueryParameters>();
            var repository = scope.ServiceProvider.GetRequiredService<IRhetosComponent<Common.DomRepository>>().Value;

            string json = JsonConvert.SerializeObject(
                    new List<object> { new Common.Role { Name = "ABC" } },
                    new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All, TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Full });

            string CreateFilters(string filterType, string filterValueJson) => $@"[{{""Filter"":""{filterType}"",""Value"":{filterValueJson}}}]";

            {
                var extFilter = queryParameters.ParseFilterParameters(CreateFilters("IEnumerable<IEntity>", "[]"), "TestFilters.Simple");
                var result = repository.TestFilters.Simple.Load(extFilter).Single().Name;
                Assert.Equal("IE System.Collections.Generic.List`1[Rhetos.Dom.DefaultConcepts.IEntity] 0.", result);
            }
            {
                var ce = Assert.Throws<ClientException>(() => queryParameters.ParseFilterParameters(CreateFilters("IEnumerable<IEntity>", json), "TestFilters.Simple"));
                Assert.Contains("invalid JSON format", ce.Message);

                var response = scope.ServiceProvider.GetRequiredService<ErrorReporting>().CreateResponseFromException(ce, false);

                Assert.Contains(@"Could not create an instance of type Rhetos.Dom.DefaultConcepts.IEntity. Type is an interface or abstract class and cannot be instantiated.", response.LogMessage);
                Assert.DoesNotContain("IEntity", ce.Message); // The web response should not contain system details. The details are available in log.

                Assert.Contains(@"""Name"": ""ABC""", response.LogMessage);
                Assert.DoesNotContain("ABC", ce.Message); // The web response should not contain system details. The details are available in log.
            }
            {
                var e = Assert.Throws<ClientException>(() => queryParameters.ParseFilterParameters(CreateFilters("IEnumerable<Common.Role>", json), "TestFilters.Simple"));
                Assert.Contains("Filter type 'IEnumerable<Common.Role>' is not available", e.Message);
            }
            {
                var extFilter = queryParameters.ParseFilterParameters(CreateFilters("List<object>", "[]"), "TestFilters.Simple");
                var result = repository.TestFilters.Simple.Load(extFilter).Single().Name;
                Assert.Equal("List System.Collections.Generic.List`1[System.Object] 0 .", result);
            }
            {
                var extFilter = queryParameters.ParseFilterParameters(CreateFilters("List<object>", json), "TestFilters.Simple");
                var result = repository.TestFilters.Simple.Load(extFilter).Single().Name;
                Assert.Equal("List System.Collections.Generic.List`1[System.Object] 1 Newtonsoft.Json.Linq.JObject.", result);
            }
            {
                var e = Assert.Throws<ClientException>(() => queryParameters.ParseFilterParameters(CreateFilters("List<Common.Role>", json), "TestFilters.Simple"));
                Assert.Contains("Filter type 'List<Common.Role>' is not available", e.Message);
            }
        }
    }
}
