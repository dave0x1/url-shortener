using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Urls") ?? "DataSource=Urls.db";
builder.Services.AddSqlite<UrlDb>(connectionString);
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks()
    .AddDbContextCheck<UrlDb>(
        name: "Url db",
        tags: ["ready"]);

var app = builder.Build();

app.UseExceptionHandler();   // catches unhandled exceptions
app.UseStatusCodePages();   // fills empty 4xx/5xx bodies


if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapGet("/", () => {
    throw new Exception("boom");
});



app.MapPost("/links", async (AddressReq add, UrlDb db) =>
{
    if(!Uri.TryCreate(add.Address, UriKind.Absolute, out var uri))
        return Results.ValidationProblem(new Dictionary<string, string[]>
    {
        ["address"] = ["Please enter a valid address"]
    });
    if(uri.Scheme is not ("http" or "https"))
        return Results.ValidationProblem(new Dictionary<string, string[]>
    {
        ["address"] = ["Only http or https addresses allowed"]
    });
    var hash = TruncatedHasher.GetTruncatedHash(uri.AbsoluteUri);
    var existing = await db.Urls.SingleOrDefaultAsync(u => u.Address == uri.AbsoluteUri);
    var codeExisting = await db.Urls.SingleOrDefaultAsync(u => u.Code == hash);
    if(existing is not null)
    {
        return Results.Ok(existing);
    }
    if(codeExisting is not null)
    {
        return Results.Problem(
            statusCode: StatusCodes.Status409Conflict,
            title: "Code collision",
            detail: "A different address already uses the generated code.");
    }
    Url url = new() { Address = uri.AbsoluteUri, Code = hash };

    db.Urls.Add(url);
    await db.SaveChangesAsync();
    return Results.Created($"/{url.Code}", url);
});

app.MapGet("/{code}", async (string code, UrlDb db) =>
{
    var result = await db.Urls.SingleOrDefaultAsync(u => u.Code == code);
    if(result is not null)
    {
        return result.Address is not null ? Results.Redirect(result.Address) : Results.NotFound();
    } else
    {
        return Results.NotFound();
    }
});

app.MapGet("links", async (UrlDb db) =>
    await db.Urls.ToListAsync());

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false
});

app.Run();

record AddressReq {public required string Address {get; init;} }