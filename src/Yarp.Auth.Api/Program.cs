var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

app.MapGet("api/hello", () => "Hello from downstream API");

app.Run();
