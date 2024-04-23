using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Rhetos;
using Rhetos.Dom;
using Rhetos.Dom.DefaultConcepts;
using Rhetos.JsonCommands.Host.Filters;
using Rhetos.JsonCommands.Host.Parsers.Write;
using Rhetos.JsonCommands.Host.Utilities;
using Rhetos.Processing;
using Rhetos.Processing.DefaultCommands;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Rhetos.JsonCommands.Host.Controllers
{
    /// <summary>
    /// Web API used for saving multiple records at once.
    /// ALlows inserting, deleting and updating multiple records from multiple entities in a single web request (within a single database transaction)
    /// An example JSON format for the data to be sent is described in this comment: https://github.com/Rhetos/Rhetos/issues/355#issuecomment-915180224
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
        public async Task<IActionResult> Write()
        {
            string body;
            using (StreamReader reader = new StreamReader(Request.Body))
            {
                body = await reader.ReadToEndAsync();
            };

            var commands = new WriteCommandsParser(body, _dom).Parse();
            var saveEntityCommands = new List<ICommandInfo>();

            foreach (var command in commands)
            {
                var saveEntityCommand = new SaveEntityCommandInfo
                {
                    Entity = command.Entity,
                    DataToDelete = command.DeleteOperationItems(),
                    DataToUpdate = command.UpdateOperationItems(),
                    DataToInsert = command.InsertOperationItems()
                };
                saveEntityCommands.Add(saveEntityCommand);
            }
            _processingEngine.Execute(saveEntityCommands);
            return Ok();
        }

        [HttpPost("read")]
        public IActionResult ReadPost(List<Dictionary<string, ReadCommand>> commands)
        {
            return Read(commands);
        }

        [HttpGet("read")]
        public IActionResult ReadGet(string query)
        {
            var commands = JsonConvert.DeserializeObject<List<Dictionary<string, ReadCommand>>>(query);
            return Read(commands);
        }

        private IActionResult Read(List<Dictionary<string, ReadCommand>> commands)
        {
            var readCommands = new List<ICommandInfo>();

            foreach (var commandDict in commands)
            {
                var command = commandDict.Single(); // Each command is deserialized as a dictionary to simplify the code, but only one key-value pair is allowed.
                string entityName = command.Key;
                ReadCommand properties = command.Value;
                new QueryParameters(_genericFilterHelper).FinishPartiallyDeserializedFilters(entityName, properties.Filters);

                var readCommand = new ReadCommandInfo
                {
                    DataSource = entityName,
                    Filters = properties.Filters,
                    OrderByProperties = properties.Sort?.Select((e) => new OrderByProperty()
                    {
                        Property = e.StartsWith('-') ? e.Substring(1) : e,
                        Descending = e.StartsWith('-')
                    }).ToArray(),
                    ReadRecords = properties.ReadRecords,
                    ReadTotalCount = properties.ReadTotalCount,
                    Skip = properties.Skip,
                    Top = properties.Top,
                };
                readCommands.Add(readCommand);
            }
            return Ok(_processingEngine.Execute(readCommands));
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
