using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using SistemaReservas.API.Data;
using SistemaReservas.API.Repositories;
using SistemaReservas.API.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddSingleton<DatabaseConnection>();

// NFR2 - Separar acceso a datos: los servicios reciben los repositorios por interfaz.
builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();
builder.Services.AddScoped<ILaboratorioRepository, LaboratorioRepository>();
builder.Services.AddScoped<IDisponibilidadRepository, DisponibilidadRepository>();

builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<LaboratorioService>();
builder.Services.AddScoped<DisponibilidadService>();
builder.Services.AddSingleton<TokenService>();

// NFR1 - #43 Autenticación con JWT
var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSection["Key"]
    ?? throw new InvalidOperationException("Falta la configuración Jwt:Key.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSection["Issuer"],
            ValidAudience = jwtSection["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            NameClaimType = ClaimTypes.Name,
            RoleClaimType = ClaimTypes.Role,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    options.AddPolicy("PermitirFrontend", policy =>
    {
        policy
            .WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    using (var scope = app.Services.CreateScope())
    {
        var database =
            scope.ServiceProvider.GetRequiredService<DatabaseConnection>();

        var authService =
            scope.ServiceProvider.GetRequiredService<AuthService>();

        await DatabaseSeeder.SeedAsync(database, authService);
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
}

app.Run();