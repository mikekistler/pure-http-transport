using Microsoft.AspNetCore.HttpLogging;
using PureHttpMcpServer.Resources;
using PureHttpMcpServer.Tools;
using PureHttpMcpServer.Prompts;
using PureHttpTransport;
using PureHttpTransport.OpenApiExtensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Register the background service
builder.Services.AddHostedService<MockNotifications>();

builder.Services.AddProblemDetails();

builder.Services.AddOpenApi(options =>
{
    options.AddOpenApiTransformers();
});

builder.Services.AddHttpLogging(logging =>
{
    logging.LoggingFields = HttpLoggingFields.RequestMethod
                            | HttpLoggingFields.RequestPath
                            // | HttpLoggingFields.RequestQuery
                            | HttpLoggingFields.RequestHeaders
                            // | HttpLoggingFields.RequestBody
                            | HttpLoggingFields.ResponseStatusCode
                            // | HttpLoggingFields.ResponseHeaders
                            // | HttpLoggingFields.ResponseBody
                            ;
    logging.CombineLogs = false;
});


// Register the custom ResultJsonConverter for minimal APIs
builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(opts =>
{
    opts.SerializerOptions.Converters.Add(new ResultJsonConverter());
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseHttpLogging();
}

app.UseHttpsRedirection();

app.UseStatusCodePages();

app.UsePureHttpTransport();

// Assign instances instead of types
PureHttpTransport.ToolsEndpoints.MockTools = new MockTools();
PureHttpTransport.ResourcesEndpoints.MockResources = new MockResources();
PureHttpTransport.PromptsEndpoints.MockPrompts = new MockPrompts();

app.Run();

public partial class Program { }