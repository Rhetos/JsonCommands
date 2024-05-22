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

using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Rhetos.Dom.DefaultConcepts;
using Rhetos.JsonCommands.Host.Filters;
using Rhetos.JsonCommands.Host.Parsers.Write;
using Rhetos.JsonCommands.Host.Utilities;
using Rhetos.Processing;
using Rhetos.Processing.DefaultCommands;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Rhetos.JsonCommands.Host.Controllers
{
    /// <summary>
    /// Web API used for writing and reading multiple records at once.
    /// The write method allows inserting, deleting and updating multiple records from multiple entities in a single web request (within a single database transaction).
    /// The read method allows reading multiple entity types with different filters in a single web request.
    /// An example JSON format for the data to be sent is described in this comment: https://github.com/Rhetos/Rhetos/issues/355#issuecomment-915180224
    /// </summary>
    [Route("jc")]
    [ApiController]
    [ServiceFilter(typeof(ApiCommitOnSuccessFilter))]
    [ServiceFilter(typeof(ApiExceptionFilter))]
    public class JsonCommandsController : ControllerBase
    {
        private readonly WriteCommandsParser _writeCommandsParser;
        private readonly IProcessingEngine _processingEngine;
        private readonly QueryParameters _queryParameters;

        public JsonCommandsController(WriteCommandsParser writeCommandsParser, IRhetosComponent<IProcessingEngine> processingEngine, QueryParameters queryParameters)
        {
            _writeCommandsParser = writeCommandsParser;
            _processingEngine = processingEngine.Value;
            _queryParameters = queryParameters;
        }

        [HttpPost("write")]
        public async Task<IActionResult> Write()
        {
            string body;
            using (StreamReader reader = new StreamReader(Request.Body))
            {
                body = await reader.ReadToEndAsync();
            };

            var commands = _writeCommandsParser.Parse(body);
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
        public ActionResult<ReadResponse> ReadPost([FromBody] List<Dictionary<string, ReadCommand>> commands)
        {
            return Read(commands);
        }

        [HttpGet("read")]
        public ActionResult<ReadResponse> ReadGet([FromQuery] string q)
        {
            if (string.IsNullOrEmpty(q))
                throw new ClientException($"Query parameter '{nameof(q)}' is required.");

            var commands = JsonHelper.DeserializeOrException<List<Dictionary<string, ReadCommand>>>(q);
            return Read(commands);
        }

        private ActionResult<ReadResponse> Read(List<Dictionary<string, ReadCommand>> commands)
        {
            var readCommands = new List<ICommandInfo>();

            foreach (var commandDict in commands)
            {
                if (commandDict.Count > 1)
                    throw new ClientException($"Each read command in the array can have only one entity specified ({commandDict.Count} found). To read multiple entities, add another element to the commands array.");
                var command = commandDict.Single(); // Each command is deserialized as a dictionary to simplify the code, but only one key-value pair is allowed.
                string entityName = command.Key;
                ReadCommand properties = command.Value;
                _queryParameters.FinishPartiallyDeserializedFilters(entityName, properties.Filters);

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
            var result = _processingEngine.Execute(readCommands);
            var response = new
            {
                Data = result.CommandResults.Cast<ReadCommandResult>()
                    .Select(r => new ReadCommandResponse { Records = r.Records, TotalCount = r.TotalCount })
                    .ToList()
            };
            return Ok(response);
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

        public class ReadResponse
        {
            public ICollection<ReadCommandResponse> Data { get; set; }
        }

        public class ReadCommandResponse
        {
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public object[] Records { get; set; }

            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public int? TotalCount { get; set; }
        }
    }
}
