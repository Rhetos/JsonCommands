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
using Microsoft.AspNetCore.Mvc.Testing;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using TestApp;
using Xunit.Abstractions;

namespace Rhetos.JsonCommands.Host.Test.Tools
{
    public class TestApiHelper
    {
        /// <remarks>
        /// Writes the web response and server log to the test output, to help debug flaky tests.
        /// </remarks>
        public static async Task<(int StatusCode, string Content, LogEntries LogEntries)>
            HttpGet(string url, WebApplicationFactory<Startup> baseFactory, ITestOutputHelper outputHelper, Action<IWebHostBuilder> configureHost = null)
        {
            using var factory = baseFactory.WithWebHostBuilder(builder => configureHost?.Invoke(builder));
            using var client = factory.CreateClient();

            var response = await client.GetAsync(url);
            int statusCode = (int)response.StatusCode;
            string responseContent = await response.Content.ReadAsStringAsync();

            var logEntries = factory.GetLogEntries();
            outputHelper.WriteLine($"LOG ENTRIES {logEntries.Count}: " + string.Join(Environment.NewLine, logEntries));
            outputHelper.WriteLine($"WEB RESPONSE: {statusCode} {responseContent}");

            return (statusCode, responseContent, logEntries);
        }

        /// <remarks>
        /// Writes the web response and server log to the test output, to help debug flaky tests.
        /// </remarks>
        public static async Task<(int StatusCode, string Content, LogEntries LogEntries)>
            HttpPostWrite(string json, WebApplicationFactory<Startup> baseFactory, ITestOutputHelper outputHelper, Action<IWebHostBuilder> configureHost = null)
        {
            using var factory = baseFactory.WithWebHostBuilder(builder => configureHost?.Invoke(builder));
            using var client = factory.CreateClient();

            var requestContent = new StringContent(json);
            requestContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            var response = await client.PostAsync("jc/write", requestContent);
            int statusCode = (int)response.StatusCode;
            string responseContent = await response.Content.ReadAsStringAsync();

            var logEntries = factory.GetLogEntries();
            outputHelper.WriteLine($"LOG ENTRIES {logEntries.Count}: " + string.Join(Environment.NewLine, logEntries));
            outputHelper.WriteLine($"WEB RESPONSE: {statusCode} {responseContent}");

            return (statusCode, responseContent, logEntries);
        }
    }
}
