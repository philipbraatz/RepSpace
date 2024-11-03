var builder = WebApplication.CreateBuilder(args);

// Load configuration based on environment variable or command-line argument
var environment = "Debug";//Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
builder.Configuration.AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

// Read the URLs from the configuration
var urls = builder.Configuration.GetSection("Urls").Get<string[]>().GroupBy(g => new Uri(g).Port / 2);

if (urls == null || urls.Count() == 0)
{
    throw new InvalidOperationException("No URLs configured.");
}

var apps = new List<WebApplication>();

foreach (var url in urls)
{
    var appBuilder = WebApplication.CreateBuilder(args);

    // Copy services from the main builder
    foreach (var service in builder.Services)
    {
        appBuilder.Services.Add(service);
    }

    var webUrls = url.ToArray();
    appBuilder.WebHost.UseUrls(webUrls);

    var app = appBuilder.Build();

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Local Node Network API V1");
            c.RoutePrefix = string.Empty; // Set Swagger UI at the app's root
        });
    }

    app.UseHttpsRedirection();
    app.UseAuthorization();
    app.MapControllers();

    apps.Add(app);
}

// Run all apps
foreach (var app in apps)
{
    app.RunAsync();
}

// Wait for all apps to complete
Task.WhenAll(apps.Select(app => app.WaitForShutdownAsync())).GetAwaiter().GetResult();
