using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<UrlDb>(opt => opt.UseInMemoryDatabase("Urls"));
builder.Services.AddOpenApi();
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapGet("/", () =>
{
    return "Hello World";
});



app.MapPost("/links", async (AddressReq add, UrlDb db) =>
{
    if(!Uri.TryCreate(add.Address, UriKind.Absolute, out var uri))
        return Results.BadRequest("Please enter a valid address");
    if(uri.Scheme is not ("http" or "https"))
        return Results.BadRequest("Only http or https addresses allowed");
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
            statusCode: StatusCodes.Status500InternalServerError,
            detail: "Could not allocate a unique code. Please retry.");
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
    


app.Run();

record AddressReq {public required string Address {get; init;} }