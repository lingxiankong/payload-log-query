using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// Add initialization generation check
if (args.Length > 0 && (args[0] == "generate" || args[0] == "generate-data"))
{
    var outputDir = builder.Configuration["Local:LogDirectory"] ?? "Logs";
    await PayloadLogQuery.Utils.LogGenerator.GenerateAsync(outputDir);
    return;
}

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddMemoryCache();

// Options pattern
builder.Services.AddOptions<PayloadLogQuery.Options.LogSourceOptions>()
    .BindConfiguration("LogSource");
builder.Services.AddOptions<PayloadLogQuery.Options.LocalLogOptions>()
    .BindConfiguration("Local");
builder.Services.AddOptions<PayloadLogQuery.Options.AzureBlobOptions>()
    .BindConfiguration("Azure");

// Register Azure Clients
builder.Services.AddAzureClients(clientBuilder =>
{
    // Register BlobServiceClient
    // We get the options manually here to check if we have a connection string or account name
    // But standard pattern is usually just AddBlobServiceClient(connStr) or AddBlobServiceClient(uri).
    // Let's rely on the configuration section or manually configure.

    // We'll trust the custom provider to pick up the options, BUT looking at AzureBlobLogProvider,
    // it was creating its own BlobServiceClient.
    // Best practice: Register BlobServiceClient here.

    clientBuilder.AddBlobServiceClient(builder.Configuration.GetSection("Azure:StorageAccountName").Value is string accountName
        ? new Uri($"https://{accountName}.blob.core.windows.net")
        : new Uri("https://unknown.blob.core.windows.net"));

    // Use DefaultAzureCredential by default for the clientBuilder
    clientBuilder.UseCredential(new Azure.Identity.DefaultAzureCredential());
});


var logSource = builder.Configuration.GetSection("LogSource").GetValue<string>("Source") ?? "Local";

if (string.Equals(logSource, "Azure", StringComparison.OrdinalIgnoreCase))
{
    // Register the implementation for Azure
    builder.Services.AddSingleton<PayloadLogQuery.Abstractions.IDecryptionService, PayloadLogQuery.Services.KeyVaultDecryptionService>();
    builder.Services.AddSingleton<PayloadLogQuery.Abstractions.ILogProvider, PayloadLogQuery.Providers.AzureBlobLogProvider>();
}
else
{
    // Register the implementation for Local
    builder.Services.AddSingleton<PayloadLogQuery.Abstractions.IDecryptionService, PayloadLogQuery.Services.LocalDecryptionService>();
    builder.Services.AddSingleton<PayloadLogQuery.Abstractions.ILogProvider, PayloadLogQuery.Providers.LocalLogProvider>();
}

builder.Services.AddSingleton<PayloadLogQuery.Services.ServiceSessionCache>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthorization();

app.UseStaticFiles();
app.MapRazorPages();

app.MapGet("/metadata", async (PayloadLogQuery.Services.ServiceSessionCache cache, CancellationToken ct) =>
{
    var data = await cache.GetAsync(ct);
    return Results.Json(data);
});



app.MapGet("/payload-log/stream", async (HttpContext ctx, PayloadLogQuery.Abstractions.ILogProvider provider) =>
{
    var q = new PayloadLogQuery.Models.LogQuery
    {
        Keyword = ctx.Request.Query["q"].FirstOrDefault(),
        StatusCode = int.TryParse(ctx.Request.Query["status"].FirstOrDefault(), out var sc) ? sc : null,
        From = DateTimeOffset.TryParse(ctx.Request.Query["from"].FirstOrDefault(), System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal, out var from) ? from : null,
        To = DateTimeOffset.TryParse(ctx.Request.Query["to"].FirstOrDefault(), System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal, out var to) ? to : null,
        Limit = int.TryParse(ctx.Request.Query["limit"].FirstOrDefault(), out var limit) ? limit : null,
        ExcludeFrom = bool.TryParse(ctx.Request.Query["excludeFrom"].FirstOrDefault(), out var exc) && exc
    };

    var serviceName = ctx.Request.Query["serviceName"].FirstOrDefault();
    var sessionId = ctx.Request.Query["sessionId"].FirstOrDefault();
    if (string.IsNullOrWhiteSpace(serviceName) || string.IsNullOrWhiteSpace(sessionId))
    {
        ctx.Response.StatusCode = StatusCodes.Status400BadRequest;
        await ctx.Response.WriteAsync("serviceName and sessionId are required");
        return;
    }

    ctx.Response.Headers["Cache-Control"] = "no-cache";
    ctx.Response.Headers["Content-Type"] = "text/event-stream";
    await foreach (var entry in provider.StreamAsync(serviceName, sessionId, q, ctx.RequestAborted))
    {
        var ts = entry.Timestamp?.ToString("O") ?? string.Empty;
        var json = System.Text.Json.JsonSerializer.Serialize(new { timestamp = ts, content = entry.Content });
        await ctx.Response.WriteAsync($"data: {json}\n\n");
        await ctx.Response.Body.FlushAsync();
        if (ctx.RequestAborted.IsCancellationRequested) break;
    }
});

app.Run();
