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

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using TestApp;

namespace Rhetos.JsonCommands.Host.Test.Tools
{
    public class CustomWebApplicationFactory : WebApplicationFactory<Startup>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
                services.AddRhetosHost(ConfigureRhetos)
            );
            builder.ConfigureLogging(logging =>
            {
                logging.Services.AddSingleton<ILoggerProvider, FakeLoggerProvider>();
                logging.Services.AddSingleton(typeof(ILogger<>), typeof(FakeLogger<>));
                logging.Services.AddSingleton<LogEntries>();
                logging.Services.AddSingleton<FakeLoggerOptions>();
            });
        }

        private void ConfigureRhetos(IServiceProvider serviceProvider, IRhetosHostBuilder rhetosHostBuilder)
        {
            rhetosHostBuilder.UseRootFolder(Path.Combine("..", "..", "..", "..", "TestApp", "bin", "Debug", "net5.0"));
        }
    }
}
