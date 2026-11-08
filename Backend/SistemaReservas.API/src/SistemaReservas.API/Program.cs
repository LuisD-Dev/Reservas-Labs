using SistemaReservas.Infrastructure.Interfaces;
using SistemaReservas.Infrastructure;
using SistemaReservas.Application;
using SistemaReservas.API;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(
    builder.Configuration.GetConnectionString(
        "DefaultConnection"));
builder.Services.AddApplication();
builder.Services.AddPresentation(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    using (var scope = app.Services.CreateScope())
    {
        var initializer = scope.ServiceProvider.GetRequiredService<IDatabaseInitializer>();
        await initializer.InitializeAsync();
    }

    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors("PermitirFrontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// NFR1 - #44 /db-test solo existe en Development: expone detalles del
// error de conexion que no deben verse en otros ambientes.
if (app.Environment.IsDevelopment())
{
    app.MapGet("/db-test", async (IDatabaseHealthCheck healthCheck) =>
    {
        try
        {
            var canConnect = await healthCheck.CanConnectAsync();
            if (canConnect) return Results.Ok("Conexión con SQL Server exitosa.");
            return Results.Problem("No se pudo conectar a la base de datos.");
        }
        catch (Exception ex)
        {
            return Results.Problem(ex.Message);
        }
    });
}

app.Run();
