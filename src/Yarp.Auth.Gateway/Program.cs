using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

builder.Services
    .AddAuthentication(BearerTokenDefaults.AuthenticationScheme)
    .AddBearerToken();

builder.Services
    .AddAuthorization(o =>
    {
        o.AddPolicy("first-api-access",
            policy => policy.RequireAuthenticatedUser().RequireClaim("first-api-access", true.ToString()));

        o.AddPolicy("second-api-access",
            policy => policy.RequireAuthenticatedUser().RequireClaim("second-api-access", true.ToString()));
        
        o.AddPolicy("master-api-access",
            policy => policy.RequireAuthenticatedUser().RequireClaim("master-api-access", true.ToString()));
    });

builder.Services.AddRateLimiter(_ => _
    .AddFixedWindowLimiter(policyName: "FixedRateLimiter", options =>
    {
        options.Window = TimeSpan.FromSeconds(10);
        options.PermitLimit = 4;
        options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        options.QueueLimit = 2;
    }));

var app = builder.Build();

app.MapGet("login", (bool firstApi = false, bool secondApi = false, bool masterApi = true) =>
    Results.SignIn(
        new ClaimsPrincipal(
            new ClaimsIdentity(
                [
                    new Claim("user-id", Guid.NewGuid().ToString("N")),
                    new Claim("sub", Guid.NewGuid().ToString()),
                    new Claim("first-api-access", firstApi.ToString()),
                    new Claim("second-api-access", secondApi.ToString()),
                    new Claim("master-api-access", masterApi.ToString()),
                ],
                BearerTokenDefaults.AuthenticationScheme)),
        authenticationScheme: BearerTokenDefaults.AuthenticationScheme));

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.MapReverseProxy();

app.Run();