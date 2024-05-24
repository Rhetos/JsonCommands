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
using Rhetos.Dom.DefaultConcepts;
using Rhetos.JsonCommands.Host.Test.Tools;
using System;

namespace Rhetos.JsonCommands.Host.Test
{
    public class JsonCommandsTestCleanup : IDisposable
    {
        public void Dispose()
        {
            using var factory = new CustomWebApplicationFactory();
            using var scope = factory.Services.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IRhetosComponent<Common.DomRepository>>().Value;

            var testBooks = repository.Bookstore.Book.Load(book => book.Name.StartsWith("__Test__"));
            repository.Bookstore.Book.Delete(testBooks);

            scope.ServiceProvider.GetRequiredService<IRhetosComponent<IUnitOfWork>>().Value.CommitAndClose();
        }
    }
}