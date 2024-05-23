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

using Newtonsoft.Json;
using Rhetos.Dom;
using Rhetos.Dom.DefaultConcepts;
using Rhetos.JsonCommands.Host.Utilities;
using Rhetos.Processing.DefaultCommands;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Rhetos.JsonCommands.Host.Parsers.Write
{
    public class WriteCommandsParser
    {
        private readonly IDomainObjectModel _dom;
        private readonly JsonSerializer _serializer = new();

        public WriteCommandsParser(IRhetosComponent<IDomainObjectModel> dom)
        {
            _dom = dom.Value;
        }

        public List<SaveEntityCommandInfo> Parse(string json)
        {
            using var stringReader = new StringReader(json);
            using var reader = new JsonTextReader(stringReader);

            if (!reader.Read())
                throw CreateClientException(reader, "Empty JSON.");

            List<SaveEntityCommandInfo> commands;
            try
            {
                commands = ReadArrayOfCommands(reader);
                ReadEnd(reader, "array");
            }
            catch (JsonException e)
            {
                throw CreateClientException(reader, "Invalid JSON format.", e.Message);
            }
            return commands;
        }

        List<SaveEntityCommandInfo> ReadArrayOfCommands(JsonTextReader reader)
        {
            ReadToken(reader, JsonToken.StartArray);

            List<SaveEntityCommandInfo> commands = new();

            while (reader.TokenType != JsonToken.EndArray)
            {
                var command = ReadCommand(reader);
                commands.Add(command);
            }

            ReadToken(reader, JsonToken.EndArray);
            return commands;
        }

        SaveEntityCommandInfo ReadCommand(JsonTextReader reader)
        {
            ReadToken(reader, JsonToken.StartObject);

            if (reader.TokenType != JsonToken.PropertyName)
                throw CreateClientException(reader, "There is an empty command.");

            string entity = ReadPropertyName(reader);
            var entityType = _dom.GetType(entity);
            if (entityType == null)
                throw CreateClientException(reader, $"Incorrect entity name '{entity}'.");
            var operations = ReadCommandItems(reader, entityType);

            if (reader.TokenType == JsonToken.PropertyName)
                throw CreateClientException(reader, "Each write command should contain only one entity name. For other entity type, add a separate command to the commands array.");

            ReadToken(reader, JsonToken.EndObject);
            return CreateSaveCommand(entity, operations);
        }

        private SaveEntityCommandInfo CreateSaveCommand(string entity, List<SaveOperationItems> saveOperations)
        {
            IEntity[] dataToDelete = null;
            IEntity[] dataToUpdate = null;
            IEntity[] dataToInsert = null;

            foreach (var saveOperation in saveOperations)
            {
                switch (saveOperation.Operation.ToUpperInvariant())
                {
                    case "DELETE":
                        SetData(ref dataToDelete, saveOperation);
                        break;
                    case "UPDATE":
                        SetData(ref dataToUpdate, saveOperation);
                        break;
                    case "INSERT":
                        SetData(ref dataToInsert, saveOperation);
                        break;
                    default:
                        throw new ClientException($"Invalid save operation '{saveOperation.Operation}'.");
                }
            }

            return new SaveEntityCommandInfo
            {
                Entity = entity,
                DataToDelete = dataToDelete,
                DataToUpdate = dataToUpdate,
                DataToInsert = dataToInsert
            };
        }

        private void SetData(ref IEntity[] commandData, SaveOperationItems saveOperation)
        {
            if (commandData != null)
                throw new ClientException($"There are multiple '{saveOperation.Operation}' operations. Please combine them into a single operation with multiple records.");
            commandData = saveOperation.Items;
        }

        object ReadToken(JsonTextReader reader, JsonToken jsonToken)
        {
            if (reader.TokenType != jsonToken)
                throw CreateClientException(reader, $"Expected token type {jsonToken}. Provided token is {reader.TokenType}.");

            object value = reader.Value;
            reader.Read();
            return value;
        }

        string ReadPropertyName(JsonTextReader reader)
        {
            var propertyName = ReadToken(reader, JsonToken.PropertyName);
            return (string)propertyName;
        }

        List<SaveOperationItems> ReadCommandItems(JsonTextReader reader, Type entityType)
        {
            ReadToken(reader, JsonToken.StartObject);

            List<SaveOperationItems> operations = new();

            while (reader.TokenType != JsonToken.EndObject)
            {
                operations.Add(new SaveOperationItems
                {
                    Operation = ReadPropertyName(reader),
                    Items = ReadItemsArray(reader, entityType)
                });
            }

            ReadToken(reader, JsonToken.EndObject);
            return operations;
        }

        IEntity[] ReadItemsArray(JsonTextReader reader, Type entityType)
        {
            var itemsObject = _serializer.Deserialize(reader, entityType.MakeArrayType());
            var items = ((ICollection)itemsObject).Cast<IEntity>().ToArray();
            reader.Read();
            return items;
        }

        void ReadEnd(JsonTextReader reader, string lastType)
        {
            if (reader.Read())
                throw CreateClientException(reader, $"Unexpected JSON text after the end of JSON {lastType}.");
        }

        /// <summary>
        /// The <paramref name="message"/> should not contain any (potentially sensitive) data from the request.
        /// It should contain only the structural information.
        /// Provide <paramref name="additionalDataForLog"/> with additional information that may contain fragments of the request data.
        /// </summary>
        Exception CreateClientException(JsonTextReader reader, string message, string additionalDataForLog = null)
        {
            string exceptionMessage = $"{message} At line {reader.LineNumber}, position {reader.LinePosition}.";
            if (additionalDataForLog != null)
                exceptionMessage += " See the server log for more details on the error.";

            var ce = new ClientException(exceptionMessage);
            if (additionalDataForLog != null)
                ce.Data[JsonHelper.RhetosJsonErrorErrorMetadata] = additionalDataForLog;
            return ce;
        }
    }
}
