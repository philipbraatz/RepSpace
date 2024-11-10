using Doorfail.Distribution.Network;
using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Load configuration based on environment variable or command-line argument
var environment = "Debug";//Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
builder.Configuration.AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true);

// Read the URLs from the configuration
var urls = builder.Configuration.GetSection("Urls").Get<string[]>().GroupBy(g => new Uri(g).Port / 2);

if (urls == null || urls.Count() == 0)
{
    throw new InvalidOperationException("No URLs configured.");
}

var apps = new List<WebApplication>();
var nodes = LoadNodes() ?? new List<Node>();

var previousNodeUrl = urls.Last().Last();
foreach (var url in urls)
{
    var appBuilder = WebApplication.CreateBuilder(args);

    var node = new Node(new Uri(url.Last() + "/api/node"), new Uri(previousNodeUrl + "/api/node"));
    nodes.Add(node);

    // Add services to the container.
    appBuilder.Services.AddControllers();
    appBuilder.Services.AddEndpointsApiExplorer();
    appBuilder.Services.AddSwaggerGen();
    appBuilder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
    appBuilder.Services.AddSingleton(node);
    previousNodeUrl = url.Last();

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

var random = new Random();
var i = 0;
// Set up a timer to send the scripts to a random Node every minute
var timer = new Timer(async _ =>
{
    i++;
    List<Script> scripts = Enumerable.Range(i, random.Next(100)).Select(s => new Script($"Script{i++}", $"return $\"Hello from Node {{NodeId}}. {i}\";")).ToList();

    var randomNode = nodes[random.Next(nodes.Count)];
    randomNode.PrimeScript(scripts.ToArray());
    Console.WriteLine($"INJECTING {scripts.Count} scripts to Node {randomNode.Id} ----------------------------");
}, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));

// Run all apps
foreach (var app in apps)
{
    app.RunAsync().Start();
}

// Save nodes on shutdown
AppDomain.CurrentDomain.ProcessExit += (s, e) => SaveNodes(nodes);

// Wait for all apps to complete
await Task.WhenAll(apps.Select(app => app.WaitForShutdownAsync()));

void SaveNodes(List<Node> nodes)
{
    JsonSerializerOptions options = new() { WriteIndented = true };
    var json = JsonSerializer.Serialize(nodes, options);
    var jsonBytes = Encoding.UTF8.GetBytes(json);
    var compressedBytes = Compression.Compress(json, Guid.NewGuid());
    File.WriteAllBytes("nodes.bin", compressedBytes);
}

List<Node> LoadNodes()
{
    if (!File.Exists("nodes.bin"))
        return null;

    var compressedBytes = File.ReadAllBytes("nodes.bin");
    var json = Compression.Decompress(Guid.NewGuid(), compressedBytes);
    return JsonSerializer.Deserialize<List<Node>>(json);
}
