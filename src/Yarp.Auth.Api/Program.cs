using Asp.Versioning;
using Asp.Versioning.Builder;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    //.MinimumLevel.Override("Microsoft", LogEventLevel.Error)
    //.MinimumLevel.Override("System", LogEventLevel.Error)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Services.AddSerilog();

builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1);
    options.ReportApiVersions = true;
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ApiVersionReader = ApiVersionReader.Combine(
        new UrlSegmentApiVersionReader(),
        new HeaderApiVersionReader("X-Api-Version"));
}).AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'V";
    options.SubstituteApiVersionInUrl = true;
});

var app = builder.Build();

ApiVersionSet apiVersionSet = app.NewApiVersionSet()
    .HasApiVersion(new ApiVersion(1))
    .HasApiVersion(new ApiVersion(2))
    .ReportApiVersions()
    .Build();

// Route without version in URL, allowing versioning through header or default
app.MapGet("api/hello", () =>
    {
        Log.Logger.Information("Hello from downstream API v1 version");
        return "Hello from downstream API v1 version";
    })
    .WithApiVersionSet(apiVersionSet)
    .MapToApiVersion(1);

app.MapGet("api/hello", () =>
    {
        Log.Logger.Information("Hello from downstream API v2 version");
        return "Hello from downstream API v2 version";
    })
    .WithApiVersionSet(apiVersionSet)
    .MapToApiVersion(2);

// Existing routes with version in URL for backward compatibility
app.MapGet("api/v{version:apiVersion}/hello", () =>
    {
        Log.Logger.Information("Hello from downstream API v1 version");
        return "Hello from downstream API v1 version";
    })
    .WithApiVersionSet(apiVersionSet)
    .MapToApiVersion(1);

app.MapGet("api/v{version:apiVersion}/hello", () =>
    {
        Log.Logger.Information("Hello from downstream API v2 version");
        return "Hello from downstream API v2 version";
    })
    .WithApiVersionSet(apiVersionSet)
    .MapToApiVersion(2);

app.Run();