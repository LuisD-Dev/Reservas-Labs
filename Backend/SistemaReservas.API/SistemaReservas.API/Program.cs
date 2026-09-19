using SistemaReservas.API.Data;
using SistemaReservas.API.Services;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddSingleton<DatabaseConnection>();
builder.Services.AddScoped<AuthService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();
app.MapGet("/db-test", async (DatabaseConnection database) =>
{
    try
    {
        using var connection = database.CreateConnection();

        await connection.OpenAsync();

        return Results.Ok("Conexión con SQL Server exitosa.");
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
});
app.Run();
