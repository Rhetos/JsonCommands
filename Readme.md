# JsonCommands

JsonCommands is a web API plugin package for [Rhetos development platform](https://github.com/Rhetos/Rhetos).

It provides **a JSON web service** for all entities and other readable data structures,
that allows executing multiple read or write commands in one web request.

See [rhetos.org](http://www.rhetos.org/) for more information on Rhetos.

1. [Features](#features)
   1. [General rules](#general-rules)
   2. [Writing data](#writing-data)
   3. [Reading data](#reading-data)
   4. [Error response](#error-response)
2. [Installation](#installation)
   1. [Configure JSON format](#configure-json-format)
3. [How to contribute](#how-to-contribute)
   1. [Building and testing the source code](#building-and-testing-the-source-code)

## Features

### General rules

Any POST request should contain a header: `Content-Type: application/json; charset=utf-8`

Examples in this article will assume that your application's base URI is `https://localhost:5000`.

### Writing data

Send a POST request to `https://localhost:5000/jc/write`.
The POST request should have a following format:

```json
[
  {
    "Bookstore.Book": {
      "Delete": [
        { "ID": "00a7302a-df84-43a4-8c1c-6f7aa13c63b4" }
      ],
      "Update": [
        { "ID": "8faa49db-aa6a-4e0c-9459-c1a16826ffc5", "Title": "Some other book" },
        { "ID": "9e76a291-a76f-43e3-85ba-60bb88c3900b", "Title": "Yet another book" }
      ],
      "Insert": [
        { "ID": "ed609ccf-346e-423d-9e21-145571dbaee9", "Title": "The Art of Computer Programming" }
      ]
    }
  },
  {
    "Bookstore.Comment": {
      "Insert": [
        { "Text": "Very interesting", "BookID": "ed609ccf-346e-423d-9e21-145571dbaee9" },
        { "Text": "Educational", "BookID": "ed609ccf-346e-423d-9e21-145571dbaee9" }
      ]
    }
  }
]
```

For each entity write block (for example for Bookstore.Book above), internally Rhetos will always execute delete operation first, then update, then insert. If a custom order is needed within one write command, the client can control it by using multiple blocks for same entity. For example:

```json
[
  {  "Bookstore.Book": { "Insert": [...] } },
  {  "Bookstore.Book": { "Delete": [...] } },
  {  "Bookstore.Book": { "Update": [...] } }
]
```

In case of a successful write, the command returns an empty response (HTTP 200).
In case of an error, see the response format in the [Error response](#error-response) section below.

### Reading data

Send a POST or a GET request to `https://localhost:5000/jc/read`.

If using POST (recommended), send the command parameters in the request body.
If using GET, send the command parameters as a query parameter "q".

The **read command parameters** should have the following format (without the comments).

```js
[ // Array of read commands
  {
    "Bookstore.Book": {
      "ReadRecords": true, // Read the array of records. The default value is 'true', so this line can be removed.
      "ReadTotalCount": false, // Read the total records count. The default value is 'false', so this line can be removed.
      "Filters": [ // See the text below for the information on the Filters parameter.
        {
          "Property": "Title",
          "Operation": "startswith",
          "Value": "The"
        },
        {
          "Filter": "Bookstore.CommonMisspelling"
        }
      ],
      "Top": 20, // The default value is '0' (read all records).
      "Skip": 0, // The default value is '0' (no paging), so this line can be removed.
      "Sort": [ "-Code", "ID" ] // The minus sign ('-') specifies the *descending* sort. 'Sort' is required if Top or Skip is used.
    }
  },
  {
    "Bookstore.Comment": {
      "ReadRecords": false,
      "ReadTotalCount": true
    }
  }
]
```

The **Filters parameter** in the read command is an array of filters.
When applying multiple filters in a same request, the intersection of the filtered data is returned (AND).
The filters in the array can be any of the following types:

1. **Generic** property filter
   * Format: `{"Property":...,"Operation":..., "Value":...}`
   * Example: select items where year is greater than 2005: `[{"Property":"Year","Operation":"Greater", "Value":2005}]`
   * Available operations:
     * `Equals`, `NotEquals`, `Greater`, `GreaterEqual`, `Less`, `LessEqual`
     * `In`, `NotIn` -- Parameter Value is a JSON array.
     * `StartsWith`, `EndsWith`, `Contains`, `NotContains` -- String only.
     * `DateIn`, `DateNotIn` -- Date or DateTime property only, provided value must be string.
       Returns whether the property's value is within a given day, month or year.
       Valid value format is *yyyy-mm-dd*, *yyyy-mm* or *yyyy*.
2. **Specific filter** without a parameter
   * Format: `{"Filter":...}` (provide a full name of the filter)
   * Specific filters refer to concepts such as **ItemFilter**, **ComposableFilterBy** and **FilterBy**,
     and also other [predefined filters](https://github.com/Rhetos/Rhetos/wiki/Filters-and-other-read-methods#predefined-filters) available in the object model.
   * Example: get long books from the Bookstore demo by applying
     [ItemFilter LongBooks](https://github.com/Rhetos/Bookstore/blob/master/src/Bookstore.Service/DslScripts/AdditionalExamples/ExampleFilters.rhe)
     on Book entity: `[{"Filter":"Bookstore.LongBooks"}]`
3. **Specific filter** with a parameter
   * Format: `{"Filter":...,"Value":...}` (value is usually a JSON object)
   * Example: get books with at least 700 pages from the Bookstore demo by applying
     [ComposableFilterBy LongBooks3](https://github.com/Rhetos/Bookstore/blob/master/src/Bookstore.Service/DslScripts/AdditionalExamples/ExampleFilters.rhe)
     on Book entity: `[{"Filter":"Bookstore.LongBooks3","Value":{"MinimumPages":700}}]`

The read command returns the **response** is in the following format (without the comments).

```js
{
  "Data": [ // Array of command responses
    {
      // Bookstore.Book
      "Records": [
        { "Code": "001", "Title": "The Art of Computer Programming" },
        { "Code": "002", "Title": "Some other book" }
      ]
    },
    {
      // Bookstore.Comment
      "TotalCount": 0
    }
  ]
}
```

In case of an error, see the response format in the [Error response](#error-response) section below.

### Error response

The response status code will indicate the success of the request:

* 200 - OK,
* 4xx - client error (incorrect data or request format, authentication or authorization error),
* 500 - internal server error.

In case of an error, the response body will contain more information on the error. It is a JSON object in the following format (without the comments):

```js
{
  "Error"
  {
    "Message": "... error message for the end user",
    "Metadata": // Optional additional information on the error (depends on the error type)
    {
      // Examples of metadata.
      "SystemMessage": "... system details on the error for the fronted developer",
      "DataStructure": "... the entity that caused the error",
      "Property": "... the property that caused the error",
      ...
    }
  }
}
```

If the configuration option "JsonCommandsOptions.UseLegacyErrorResponse" is enabled, the error response will be
a different JSON object with properties: "UserMessage" (a message that should be displayed to the end user)
and "SystemMessage" (additional error metadata for better client UX).
Use the `AddJsonCommands()` method parameter to configure the option.

## Installation

Installing this package to a Rhetos web application:

1. Add "Rhetos.JsonCommands" NuGet package, available at the [NuGet.org](https://www.nuget.org/) on-line gallery.
2. Extend Rhetos services configuration (at `services.AddRhetosHost`) with the JsonCommands API:
   ```cs
   .AddJsonCommands();
   ```

### Configure JSON format

Depending on your intended client applications, you can use standard ASP.NET Core features
to configure the JSON response formatting in Program.cs or Startup.cs.

For compatibility with [Rhetos.FloydExtensions](https://www.nuget.org/packages/Rhetos.FloydExtensions),
you can configure the JSON object serialization for all properties to start with an uppercase letter:

```cs
// If not using Newtonsoft.Json:
builder.Services.AddControllers()
  .AddJsonOptions(options => options.JsonSerializerOptions.PropertyNamingPolicy = null);

// If using Newtonsoft.Json:
builder.Services.AddControllers()
  .AddNewtonsoftJson(o => o.UseMemberCasing());
```

## How to contribute

Contributions are very welcome. The easiest way is to fork this repo, and then
make a pull request from your fork. The first time you make a pull request, you
may be asked to sign a Contributor Agreement.
For more info see [How to Contribute](https://github.com/Rhetos/Rhetos/wiki/How-to-Contribute) on Rhetos wiki.

### Building and testing the source code

* Note: This package is already available at the [NuGet.org](https://www.nuget.org/) online gallery.
  You don't need to build it from source in order to use it in your application.
* To build the package from source, run `Clean.bat`, `Build.bat` and `Test.bat`.
* For the test script to work, you need to create an empty database and
  a settings file `test\TestApp\ConnectionString.local.json`
  with the database connection string (configuration key "ConnectionStrings:RhetosConnectionString").
* The build output is a NuGet package in the "Install" subfolder.
