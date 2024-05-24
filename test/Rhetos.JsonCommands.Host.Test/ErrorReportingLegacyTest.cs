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

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Rhetos.JsonCommands.Host.Filters;
using Rhetos.JsonCommands.Host.Test.Tools;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Rhetos.JsonCommands.Host.Test
{
    public class ErrorReportingLegacyTest : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly ITestOutputHelper _output;
        private readonly CustomWebApplicationFactory _factory;

        public ErrorReportingLegacyTest(ITestOutputHelper output, CustomWebApplicationFactory factory)
        {
            _output = output;
            _factory = factory;
        }

        [Theory]
        [InlineData("1",
            @"400 {""UserMessage"":""test1"",""SystemMessage"":""test2""}",
            "[Trace] Rhetos.JsonCommands.Host.Filters.ApiExceptionFilter:|Rhetos.UserException: test1|SystemMessage: test2")]
        [InlineData("2",
            @"400 {""UserMessage"":""test1"",""SystemMessage"":null}",
            "[Trace] Rhetos.JsonCommands.Host.Filters.ApiExceptionFilter:|Rhetos.UserException: test1")]
        [InlineData("3",
            @"400 {""UserMessage"":""Exception of type 'Rhetos.UserException' was thrown."",""SystemMessage"":null}",
            "[Trace] Rhetos.JsonCommands.Host.Filters.ApiExceptionFilter:|Rhetos.UserException: Exception of type 'Rhetos.UserException' was thrown.")]
        public async Task UserExceptionResponse(string index, string expectedResponse, string expectedLogPatterns)
        {
            var response = await PostAsyncTest($"__Test__UserExceptionResponse{index}", builder => builder.UseTraceLogging());

            Assert.Equal<object>(expectedResponse, $"{response.StatusCode} {response.Content}");

            string apiExceptionLog = response.LogEntries.Select(e => e.ToString()).Where(e => e.Contains("ApiExceptionFilter")).SingleOrDefault();
            foreach (var pattern in expectedLogPatterns.Split('|'))
                Assert.Contains(pattern, apiExceptionLog);
        }

        [Fact]
        public async Task LocalizedUserException()
        {
            var response = await PostAsyncTest($"__Test__LocalizedUserException", builder => builder.UseTraceLogging());

            Assert.Equal(@"400 {""UserMessage"":""TestErrorMessage 1000"",""SystemMessage"":null}", $"{response.StatusCode} {response.Content}");

            string[] exceptedLogPatterns = new[]
            {
                "[Trace] Rhetos.JsonCommands.Host.Filters.ApiExceptionFilter:",
                "Rhetos.UserException: TestErrorMessage 1000",
                // The command summary is not reported by ProcessingEngine for UserExceptions, to improved performance.
            };
            Assert.Equal(1, response.LogEntries.Select(e => e.ToString()).Count(
                entry => exceptedLogPatterns.All(pattern => entry.Contains(pattern))));
        }

        [Fact]
        public async Task LocalizedUserExceptionInvalidFormat()
        {
            var response = await PostAsyncTest($"__Test__LocalizedUserExceptionInvalidFormat");

            Assert.StartsWith(
                @"500 {""UserMessage"":null,""SystemMessage"":""Internal server error occurred. See server log for more information. (ArgumentException, ",
                $"{response.StatusCode} {response.Content}");

            Assert.DoesNotContain(@"TestErrorMessage", $"{response.StatusCode} {response.Content}");
            Assert.DoesNotContain(@"1000", $"{response.StatusCode} {response.Content}");

            string[] exceptedLogPatterns = new[]
            {
                "[Error] Rhetos.JsonCommands.Host.Filters.ApiExceptionFilter",
                "System.ArgumentException: Invalid error message format. Message: \"TestErrorMessage {0} {1}\", Parameters: \"1000\". Index (zero based) must be greater than or equal to zero and less than the size of the argument list.",
                // The command summary is not reported by ProcessingEngine for UserExceptions, to improved performance.
            };
            Assert.Equal(1, response.LogEntries.Select(e => e.ToString()).Count(
                entry => exceptedLogPatterns.All(pattern => entry.Contains(pattern))));
        }

        [Fact]
        public async Task ClientExceptionResponse()
        {
            var response = await PostAsyncTest($"__Test__ClientExceptionResponse");

            Assert.Equal<object>(
                "400 {\"UserMessage\":\"Operation could not be completed because the request sent to the server was not valid or not properly formatted.\""
                    + ",\"SystemMessage\":\"test exception\"}",
                $"{response.StatusCode} {response.Content}");

            string[] exceptedLogPatterns = new[] {
                "[Information] Rhetos.JsonCommands.Host.Filters.ApiExceptionFilter:",
                "Rhetos.ClientException: test exception",
                "Rhetos.Command.Summary: SaveEntityCommandInfo Bookstore.Book" };
            Assert.Equal(1, response.LogEntries.Select(e => e.ToString()).Count(
                entry => exceptedLogPatterns.All(pattern => entry.Contains(pattern))));
        }

        [Fact]
        public async Task ServerExceptionResponse()
        {
            var response = await PostAsyncTest($"__Test__ServerExceptionResponse");

            Assert.StartsWith
                ("500 {\"UserMessage\":null,\"SystemMessage\":\"Internal server error occurred. See server log for more information. (ArgumentException, " + DateTime.Now.ToString("yyyy-MM-dd"),
                $"{response.StatusCode} {response.Content}");

            string[] exceptedLogPatterns = new[] {
                "[Error] Rhetos.JsonCommands.Host.Filters.ApiExceptionFilter:",
                "System.ArgumentException: test exception",
                "Rhetos.Command.Summary: SaveEntityCommandInfo Bookstore.Book" };
            Assert.Equal(1, response.LogEntries.Select(e => e.ToString()).Count(
                entry => exceptedLogPatterns.All(pattern => entry.Contains(pattern))));
        }

        [Theory]
        [InlineData(
            "[{0}]",
            "Invalid JSON format. At line 1, position 3. See the server log for more details on the error.",
            "Invalid JavaScript property identifier character: }. Path '[0]', line 1, position 3.")]
        [InlineData(
            @"[{""Bookstore.Book"": {""Insert"": [0]}}]",
            "Invalid JSON format. At line 1, position 33. See the server log for more details on the error.",
            "Error converting value 0 to type 'Bookstore.Book'. Path '[0]['Bookstore.Book'].Insert[0]', line 1, position 33.")]
        public async Task InvalidWebRequestFormatResponse(string json, string clientError, string serverLog)
        {
            var response = await TestApiHelper.HttpPostWrite(json, _factory, _output, builder => builder.UseLegacyErrorResponse());

            string expectedResponse = "400 {\"UserMessage\":\"Operation could not be completed because the request sent to the server was not valid or not properly formatted.\","
                + "\"SystemMessage\":\"" + clientError + "\"}";
            Assert.Equal(expectedResponse, $"{response.StatusCode} {response.Content}");

            Assert.Contains(serverLog, response.LogEntries.SingleOrDefault(
                entry => entry.LogLevel == LogLevel.Information
                    && entry.CategoryName.Contains(nameof(ApiExceptionFilter)))?.Message);
        }

        private async Task<(int StatusCode, string Content, LogEntries LogEntries)>
            PostAsyncTest(string testName, Action<IWebHostBuilder> configureHost = null)
        {
            string json = $@"
                [
                  {{
                    ""Bookstore.Book"": {{
                      ""Insert"": [
                        {{ ""ID"": ""{Guid.NewGuid()}"", ""Name"": ""{testName}"" }}
                      ]
                    }}
                  }}
                ]";

            return await TestApiHelper.HttpPostWrite(json, _factory, _output,
                builder =>
                {
                    builder.UseLegacyErrorResponse();
                    configureHost?.Invoke(builder);
                });
        }
    }
}
