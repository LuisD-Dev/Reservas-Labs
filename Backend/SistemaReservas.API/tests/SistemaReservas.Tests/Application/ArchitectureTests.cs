using System.Reflection;
using System.Xml.Linq;
using SistemaReservas.API.Controllers;
using SistemaReservas.Application.Interfaces;
using SistemaReservas.Domain.Entities;
using SistemaReservas.Infrastructure.Data;

namespace SistemaReservas.Tests.Application
{
    public class ArchitectureTests
    {
        [Fact]
        public void ProjectReference_DeclaraExactamenteElGrafoAutorizado()
        {
            var directorio = new DirectoryInfo(AppContext.BaseDirectory);
            while (directorio is not null && !File.Exists(Path.Combine(directorio.FullName, "SistemaReservas.API.slnx")))
                directorio = directorio.Parent;
            Assert.NotNull(directorio);
            var referencias = new Dictionary<string, string[]>
            {
                ["Domain"] = [],
                ["Infrastructure"] = ["Domain"],
                ["Application"] = ["Domain", "Infrastructure"],
                ["API"] = ["Application", "Infrastructure"],
                ["Tests"] = ["API", "Application", "Domain", "Infrastructure"]
            };
            foreach (var (proyecto, esperadas) in referencias)
            {
                var archivo = Path.Combine(directorio.FullName, proyecto == "Tests" ? "tests" : "src",
                    $"SistemaReservas.{proyecto}", $"SistemaReservas.{proyecto}.csproj");
                var actuales = XDocument.Load(archivo).Descendants("ProjectReference")
                    .Select(e => Path.GetFileNameWithoutExtension(e.Attribute("Include")!.Value.Replace('\\', Path.DirectorySeparatorChar)))
                    .Select(n => n.Replace("SistemaReservas.", "")).Order();
                Assert.Equal(esperadas.Order(), actuales);
            }
        }

        [Fact]
        public void Ensamblados_ConservanNombresYGrafoSinCiclos()
        {
            var assemblies = new[] { typeof(Usuario).Assembly, typeof(AppDbContext).Assembly,
                typeof(IAuthService).Assembly, typeof(AuthController).Assembly };
            var esperado = new Dictionary<string, string[]>
            {
                ["SistemaReservas.Domain"] = [],
                ["SistemaReservas.Infrastructure"] = ["SistemaReservas.Domain"],
                ["SistemaReservas.Application"] = ["SistemaReservas.Domain", "SistemaReservas.Infrastructure"],
                ["SistemaReservas.API"] = ["SistemaReservas.Application", "SistemaReservas.Domain", "SistemaReservas.Infrastructure"]
            };
            // El compilador registra tambien referencias transitivas usadas en firmas publicas.
            foreach (var assembly in assemblies)
            {
                var nombre = assembly.GetName().Name!;
                var referencias = Referencias(assembly);
                Assert.Equal(esperado[nombre].Order(), referencias.Order());
            }
            bool TieneCiclo(string actual, HashSet<string> ruta)
            {
                if (!ruta.Add(actual)) return true;
                foreach (var referencia in esperado[actual])
                    if (TieneCiclo(referencia, new HashSet<string>(ruta))) return true;
                return false;
            }
            Assert.All(esperado.Keys, nombre => Assert.False(TieneCiclo(nombre, [])));
            Assert.Equal(assemblies.Select(a => a.GetName().Name!).Order(), Referencias(typeof(ArchitectureTests).Assembly).Order());
        }

        [Fact]
        public void Contratos_NoSeDuplicanYLosServiciosNoExponenEntidades()
        {
            var app = typeof(IAuthService).Assembly;
            var infraestructura = typeof(AppDbContext).Assembly;
            Assert.Equal(5, app.GetExportedTypes().Count(t => t.IsInterface));
            Assert.Equal(8, infraestructura.GetExportedTypes().Count(t => t.IsInterface));
            Assert.Empty(app.GetExportedTypes().Where(t => t.IsInterface)
                .Select(t => t.Name).Intersect(infraestructura.GetExportedTypes().Where(t => t.IsInterface).Select(t => t.Name)));
            bool ContieneEntidad(Type type) => type.Namespace == "SistemaReservas.Domain.Entities"
                || (type.IsGenericType && type.GenericTypeArguments.Any(ContieneEntidad));
            foreach (var method in app.GetExportedTypes().Where(t => t.IsInterface).SelectMany(t => t.GetMethods()))
                Assert.False(ContieneEntidad(method.ReturnType), method.ToString());
            Assert.DoesNotContain(typeof(IAuthService).GetMethods(), m => m.Name == "HashPassword");
            Assert.DoesNotContain(typeof(Usuario).Assembly.GetReferencedAssemblies(), r =>
                r.Name!.Contains("Mapster") || r.Name.Contains("EntityFramework") || r.Name.Contains("AspNetCore"));
        }

        private static string[] Referencias(Assembly assembly) => assembly.GetReferencedAssemblies()
            .Select(r => r.Name!).Where(n => n.StartsWith("SistemaReservas.")).ToArray();
    }
}
