using SistemaReservas.API.Data;
using SistemaReservas.API.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddSingleton<DatabaseConnection>();
builder.Services.AddScoped<AuthService>();

builder.Services.AddCors(options => {
    options.AddPolicy("PermitirFrontend", policy => {
        policy.WithOrigins("http://localhost:5173").AllowAnyHeader().AllowAnyMethod();
    });
});

var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var database =
        scope.ServiceProvider.GetRequiredService<DatabaseConnection>();

    var authService =
        scope.ServiceProvider.GetRequiredService<AuthService>();

    await DatabaseSeeder.SeedAsync(database, authService);
}
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors("PermitirFrontend");

app.UseAuthentication();
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