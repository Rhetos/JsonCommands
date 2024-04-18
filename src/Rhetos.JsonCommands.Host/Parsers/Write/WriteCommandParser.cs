using Newtonsoft.Json;
using Rhetos.Dom.DefaultConcepts;
using Rhetos.Dom;
using System.Collections.Generic;
using System.IO;
using System;
using System.Collections;
using System.Linq;

namespace Rhetos.JsonCommands.Host.Parsers.Write
{
    public class WriteCommandsParser : IDisposable
    {
        IDomainObjectModel dom;
        StringReader stringReader;
        JsonTextReader reader;
        JsonSerializer serializer;

        public WriteCommandsParser(string json, IDomainObjectModel dom)
        {
            this.dom = dom;
            stringReader = new StringReader(json);
            reader = new JsonTextReader(stringReader);
            serializer = new JsonSerializer();
        }

        public void Dispose()
        {
            if (reader != null)
            {
                ((IDisposable)reader).Dispose();
                reader = null;
            }
            if (stringReader != null)
            {
                stringReader.Dispose();
                stringReader = null;
            }
        }

        public List<Command> Parse()
        {
            if (!reader.Read())
                throw CreateException("Empty JSON.");

            var commands = ReadArrayOfCommands();
            ReadEnd();
            return commands;
        }

        List<Command> ReadArrayOfCommands()
        {
            ReadToken(JsonToken.StartArray);

            List<Command> commands = new();

            while (reader.TokenType != JsonToken.EndArray)
            {
                var command = ReadCommand();
                commands.Add(command);
            }

            ReadToken(JsonToken.EndArray);
            return commands;
        }

        Command ReadCommand()
        {
            ReadToken(JsonToken.StartObject);

            if (reader.TokenType != JsonToken.PropertyName)
                throw CreateException("Komanda je prazna.");

            string entity = ReadPropertyName();
            var entityType = dom.GetType(entity);
            var operations = ReadCommandItems(entityType);

            if (reader.TokenType == JsonToken.PropertyName)
                throw CreateException("Svaka komanda u listi treba sadržavati samo po jedan entitet.");

            ReadToken(JsonToken.EndObject);
            return new Command
            {
                Entity = entity,
                Operations = operations
            };
        }

        object ReadToken(JsonToken jsonToken)
        {
            if (reader.TokenType != jsonToken)
                throw CreateException($"Expected tokent type {jsonToken}. Provided token is {reader.TokenType}.");

            object value = reader.Value;
            reader.Read();
            return value;
        }

        string ReadPropertyName()
        {
            var propertyName = ReadToken(JsonToken.PropertyName);
            return (string)propertyName;
        }

        List<SaveOperationItems> ReadCommandItems(Type entityType)
        {
            ReadToken(JsonToken.StartObject);

            List<SaveOperationItems> operations = new();

            while (reader.TokenType != JsonToken.EndObject)
            {
                operations.Add(new SaveOperationItems
                {
                    Operation = ReadPropertyName(),
                    Items = ReadItemsArray(entityType)
                });
            }

            ReadToken(JsonToken.EndObject);
            return operations;
        }

        IEntity[] ReadItemsArray(Type entityType)
        {
            var itemsObject = serializer.Deserialize(reader, entityType.MakeArrayType());
            var items = ((ICollection)itemsObject).Cast<IEntity>().ToArray();
            reader.Read();
            return items;
        }

        void ReadArrayOfItems()
        {
            ReadToken(JsonToken.StartArray);

            while (reader.TokenType != JsonToken.EndArray)
            {
                ReadCommand();
            }

            ReadToken(JsonToken.EndArray);
        }

        void ReadEnd()
        {
            if (reader.Read())
                throw CreateException("Not the end of JSON text.");
        }

        Exception CreateException(string message)
        {
            return new JsonException(message + $" At line {reader.LineNumber}, position {reader.LinePosition}.");
        }
    }
}
