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

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Rhetos.JsonCommands.Host.Parsers;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;

namespace Rhetos.JsonCommands.Host.Utilities
{
    /// <summary>
    /// Converts exceptions to a HTTP WEB response that contains JSON-serialized string error message.
    /// </summary>
    public class ErrorReporting
    {
        private readonly ILocalizer localizer;

        public ErrorReporting(IRhetosComponent<ILocalizer> rhetosLocalizer)
        {
            this.localizer = rhetosLocalizer.Value;
        }

        public static object CreateErrorResponseMessage(string userMessage, string systemMessage, bool useLegacyErrorResponse)
        {
            if (useLegacyErrorResponse)
                return new LegacyErrorResponse
                {
                    UserMessage = userMessage,
                    SystemMessage = systemMessage,
                };
            else if (userMessage == null)
                return new ErrorResponse
                {
                    Error = new ErrorResponseData
                    {
                        Message = systemMessage
                    }
                };
            else
                return new ErrorResponse
                {
                    Error = new ErrorResponseData
                    {
                        Message = userMessage,
                        Metadata = ErrorResponseMetadataParser.Parse(systemMessage),
                    }
                };
        }

        private class ErrorResponse
        {
            public ErrorResponseData Error { get; set; }
        }

        private class ErrorResponseData
        {
            public string Message { get; set; }

            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public Dictionary<string, string> Metadata { get; set; }
        }

        private class LegacyErrorResponse
        {
            public string UserMessage { get;set; }

            public string SystemMessage { get; set; }
        }

        public ErrorDescription CreateResponseFromException(Exception exception, bool useLegacyErrorResponse)
        {
            int statusCode;
            string userMessage;
            string systemMessage;
            LogLevel logLevel;

            string commandSummary = ExceptionsUtility.GetCommandSummary(exception);
            string logMessage = exception.ToString() + (string.IsNullOrEmpty(commandSummary) ? ""
                    : Environment.NewLine + "Command: " + commandSummary);

            if (exception is UserException userException)
            {
                statusCode = StatusCodes.Status400BadRequest;
                logLevel = LogLevel.Trace;

                userMessage = localizer[userException.UserMessage, userException.MessageParameters];
                systemMessage = userException.SystemMessage;
            }
            else if (exception is ClientException clientException)
            {
                statusCode = GetStatusCode(clientException);
                logLevel = LogLevel.Information;

                userMessage = statusCode == (int)System.Net.HttpStatusCode.BadRequest
                    // The ClientExceptionUserMessage is intended for invalid request format with status code BadRequest (default).
                    // Other error types are not correctly described with that message so clientException.Message is returned instead.
                    ? localizer[ErrorMessages.ClientExceptionUserMessage]
                    // Differences between versions when statusCode <> BadRequest:
                    // - v4 and v5.1 returns clientException.Message
                    // - v5.0 returns ClientExceptionUserMessage
                    // - v5.1 returns localized clientException.Message
                    : localizer[clientException.Message];
                systemMessage = clientException.Message;
            }
            else
            {
                statusCode = StatusCodes.Status500InternalServerError;
                logLevel = LogLevel.Error;

                userMessage = null;
                systemMessage = ErrorMessages.GetInternalServerErrorMessage(localizer, exception);
            }


            object errorResponse = CreateErrorResponseMessage(userMessage, systemMessage, useLegacyErrorResponse);

            return new ErrorDescription(statusCode, errorResponse, logLevel, logMessage);
        }

        private int GetStatusCode(ClientException clientException)
        {
            // HACK: Old Rhetos plugins could not specify the status code. Here we match by message convention.
            if (clientException.Message == "User is not authenticated." && (int)clientException.HttpStatusCode == StatusCodes.Status400BadRequest)
                return StatusCodes.Status401Unauthorized;
            else
                return (int)clientException.HttpStatusCode;
        }
    }

    public record ErrorDescription(int HttpStatusCode, object Response, LogLevel Severity, string LogMessage);
}
