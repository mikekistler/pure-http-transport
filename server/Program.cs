using PureHttpTransport;
using PureHttpTransport.OpenApiExtensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddProblemDetails();

builder.Services.AddOpenApi(options =>
{
    options.AddRequestIdTransformer();
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseStatusCodePages();

app.UsePureHttpTransport();

app.Run();

public partial class Program { }