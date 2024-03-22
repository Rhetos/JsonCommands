using Newtonsoft.Json;
using Rhetos.Dom.DefaultConcepts;
using Rhetos.Dom;
using System.Collections.Generic;
using System.IO;
using System;
using System.Collections;
using System.Linq;

namespace Rhetos.JsonCommands.Host.Parsers
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

        public List<(string Entity, List<(string Operation, IEntity[] Items)> Operations)> Parse()
        {
            if (!reader.Read())
                throw CreateException("Empty JSON.");

            var commands = ReadArrayOfCommands();
            ReadEnd();
            return commands;
        }

        List<(string Entity, List<(string Operation, IEntity[] Items)> Operations)> ReadArrayOfCommands()
        {
            ReadToken(JsonToken.StartArray);

            List<(string Entity, List<(string Operation, IEntity[] Items)> Operations)> commands = new();

            while (reader.TokenType != JsonToken.EndArray)
            {
                var command = ReadCommand();
                commands.Add(command);
            }

            ReadToken(JsonToken.EndArray);
            return commands;
        }

        (string Entity, List<(string Operation, IEntity[] Items)> Operations) ReadCommand()
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
            return (entity, operations);
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

        List<(string Operation, IEntity[] Items)> ReadCommandItems(Type entityType)
        {
            ReadToken(JsonToken.StartObject);

            List<(string Operation, IEntity[] items)> operations = new();

            while (reader.TokenType != JsonToken.EndObject)
            {
                string operation = ReadPropertyName();
                var items = ReadItemsArray(entityType);
                operations.Add((operation, items));
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
