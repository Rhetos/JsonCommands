﻿/*
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

using Autofac;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Rhetos.Dom.DefaultConcepts;
using TestApp;

namespace Rhetos.JsonCommands.Host
{
    public static class WebApplicationFactoryExtensions
    {
        public static LogEntries GetLogEntries(this WebApplicationFactory<Startup> factory)
        {
            return factory.Services.GetRequiredService<LogEntries>();
        }

        /// <summary>
        /// The log from <see cref="GetLogEntries"/> will contain more detailed log entries:
        /// <see cref="LogLevel.Trace"/> and above, instead of default <see cref="LogLevel.Information"/> and above.
        /// </summary>
        public static IWebHostBuilder UseTraceLogging(this IWebHostBuilder builder)
        {
            return builder.ConfigureLogging(logging
                => logging.Services.AddSingleton(new FakeLoggerOptions { MinLogLevel = LogLevel.Trace }));
        }

        public static IWebHostBuilder UseLegacyErrorResponse(this IWebHostBuilder builder, bool useLegacyErrorResponse = true)
        {
            return builder.ConfigureServices(
                services => services.PostConfigure<JsonCommandsOptions>(
                    options => options.UseLegacyErrorResponse = useLegacyErrorResponse));
        }

        /// <summary>
        /// See <see cref="CommonConceptsRuntimeOptions.DynamicTypeResolution"/>.
        /// </summary>
        public static IWebHostBuilder SetRhetosDynamicTypeResolution(this IWebHostBuilder webHostBuilder)
        {
            webHostBuilder.ConfigureServices(
                services => services.AddRhetosHost(
                    (serviceProvider, rhetosHostBuilder) => rhetosHostBuilder.ConfigureContainer(
                        containerBuilder => containerBuilder.RegisterInstance(
                            new CommonConceptsRuntimeOptions { DynamicTypeResolution = true }))));

            return webHostBuilder;
        }
    }
}
