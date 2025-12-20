var builder = WebApplication.CreateBuilder(args);

if (args.Length > 0 && (args[0] == "generate" || args[0] == "generate-data"))
{
    var outputDir = builder.Configuration["Local:LogDirectory"] ?? "Logs";
    await PayloadLogQuery.Utils.LogGenerator.GenerateAsync(outputDir);
    return;
}

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddMemoryCache();

builder.Services.Configure<PayloadLogQuery.Options.LogSourceOptions>(builder.Configuration.GetSection("LogSource"));
builder.Services.Configure<PayloadLogQuery.Options.LocalLogOptions>(builder.Configuration.GetSection("Local"));
builder.Services.Configure<PayloadLogQuery.Options.AzureBlobOptions>(builder.Configuration.GetSection("Azure"));



var logSource = builder.Configuration.GetSection("LogSource").GetValue<string>("Source") ?? "Local";
if (string.Equals(logSource, "Azure", StringComparison.OrdinalIgnoreCase))
{
    var azOptions = builder.Configuration.GetSection("Azure").Get<PayloadLogQuery.Options.AzureBlobOptions>() ?? new();

    builder.Services.AddSingleton<PayloadLogQuery.Abstractions.IDecryptionService>(new PayloadLogQuery.Services.KeyVaultDecryptionService(azOptions));

    builder.Services.AddSingleton<PayloadLogQuery.Abstractions.ILogProvider>(sp =>
        new PayloadLogQuery.Providers.AzureBlobLogProvider(azOptions, sp.GetRequiredService<PayloadLogQuery.Abstractions.IDecryptionService>()));
}
else
{
    var localOptions = builder.Configuration.GetSection("Local").Get<PayloadLogQuery.Options.LocalLogOptions>() ?? new();

    builder.Services.AddSingleton<PayloadLogQuery.Abstractions.IDecryptionService, PayloadLogQuery.Services.LocalDecryptionService>();

    builder.Services.AddSingleton<PayloadLogQuery.Abstractions.ILogProvider>(sp =>
        new PayloadLogQuery.Providers.LocalLogProvider(localOptions, sp.GetRequiredService<PayloadLogQuery.Abstractions.IDecryptionService>()));
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
        From = DateTimeOffset.TryParse(ctx.Request.Query["from"].FirstOrDefault(), out var from) ? from : null,
        To = DateTimeOffset.TryParse(ctx.Request.Query["to"].FirstOrDefault(), out var to) ? to : null,
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
