using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using Rhetos;
using Rhetos.Dom;
using Rhetos.Dom.DefaultConcepts;
using Rhetos.JsonCommands.Host.Filters;
using Rhetos.JsonCommands.Host.Utilities;
using Rhetos.Processing;
using Rhetos.Processing.DefaultCommands;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Rhetos.JsonCommands.Host.Controllers
{
    /// <summary>
    /// Web API za snimanje više zapisa odjednom.
    /// Omogućuje da se u jednom web requestu (i u jednoj db transakciji) odjednom inserta, deletea i updatea više zapisa od više različitih entiteta.
    /// Primjer JSON formata za podatke koje treba poslati je opisan u ovom komentaru: https://github.com/Rhetos/Rhetos/issues/355#issuecomment-915180224
    /// </summary>
    [Route("jc")]
    [ApiController]
    [ServiceFilter(typeof(ApiCommitOnSuccessFilter))]
    [ServiceFilter(typeof(ApiExceptionFilter))]
    public class JsonCommandsController : ControllerBase
    {
        private readonly IDomainObjectModel _dom;
        private readonly IProcessingEngine _processingEngine;
        private readonly IRhetosComponent<GenericFilterHelper> _genericFilterHelper;

        public JsonCommandsController(IRhetosComponent<IDomainObjectModel> dom, IRhetosComponent<IProcessingEngine> processingEngine, IRhetosComponent<GenericFilterHelper> genericFilterHelper)
        {
            _dom = dom.Value;
            _processingEngine = processingEngine.Value;
            _genericFilterHelper = genericFilterHelper;
        }

        [HttpPost("write")]
        public IActionResult Write(List<Dictionary<string, JObject>> commands)
        {
            foreach (var commandDict in commands)
            {
                var command = commandDict.Single(); // Each command is deserialized as a dictionary to simplify the code, but only one key-value pair is allowed.
                string entityName = command.Key;
                Type entityType = _dom.GetType(entityName);
                Type itemsType = typeof(WriteCommandItems<>).MakeGenericType(entityType);
                dynamic items = command.Value.ToObject(itemsType);

                var saveEntityCommand = new SaveEntityCommandInfo
                {
                    Entity = entityName,
                    DataToDelete = items.Delete,
                    DataToUpdate = items.Update,
                    DataToInsert = items.Insert
                };
                _processingEngine.Execute(saveEntityCommand);
            }
            return Ok();
        }

        [HttpPost("read")]
        public IActionResult Read(List<Dictionary<string, ReadCommand>> commands)
        {
            List<ReadCommandResult> results = new List<ReadCommandResult>();
            foreach (var commandDict in commands)
            {
                var command = commandDict.Single(); // Each command is deserialized as a dictionary to simplify the code, but only one key-value pair is allowed.
                string entityName = command.Key;
                ReadCommand properties = command.Value;
                new QueryParameters(_genericFilterHelper).FinishPartiallyDeserializedFilters(entityName, properties.Filters);

                var readEntityCommand = new ReadCommandInfo
                {
                    DataSource = entityName,
                    Filters = properties.Filters,
                    OrderByProperties = properties.Sort?.Select((e) => new OrderByProperty() {
                        Property = e.StartsWith('-') ? e.Substring(1) : e,
                        Descending = e.StartsWith('-')
                    }).ToArray(),
                    ReadRecords = properties.ReadRecords,
                    ReadTotalCount = properties.ReadTotalCount,
                    Skip = properties.Skip,
                    Top = properties.Top,
                };
                results.Add(_processingEngine.Execute(readEntityCommand));
            }
            return Ok(results);
        }

        private class WriteCommandItems<T> where T : IEntity
        {
            public T[] Delete { get; set; }
            public T[] Update { get; set; }
            public T[] Insert { get; set; }
        }

        public class ReadCommand
        {
            public FilterCriteria[] Filters { get; set; } = null;
            public string[] Sort { get; set; } = null;
            public bool ReadRecords { get; set; } = true;
            public bool ReadTotalCount { get; set; } = false;
            public int Skip { get; set; } = 0;
            public int Top { get; set; } = 0;
        }
    }
}
