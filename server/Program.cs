using Microsoft.AspNetCore.HttpLogging;

using PureHttpTransport;
using PureHttpTransport.OpenApiExtensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

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

app.Run();

public partial class Program { }