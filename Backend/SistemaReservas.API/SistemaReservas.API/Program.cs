using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using SistemaReservas.API.Data;
using SistemaReservas.API.Repositories;
using SistemaReservas.API.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

// EF Core DbContext
builder.Services.AddDbContext<SistemaReservasDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// NFR2 - Tiempo inyectable: la hora actual se obtiene de TimeProvider
// para poder reemplazarla en las pruebas.
builder.Services.AddSingleton(TimeProvider.System);

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
        var context = scope.ServiceProvider.GetRequiredService<SistemaReservasDbContext>();

        var authService =
            scope.ServiceProvider.GetRequiredService<AuthService>();

        await EfDatabaseSeeder.SeedAsync(context, authService);
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
    app.MapGet("/db-test", async (SistemaReservasDbContext context) =>
    {
        try
        {
            var canConnect = await context.Database.CanConnectAsync();
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