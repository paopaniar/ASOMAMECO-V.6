using Asomameco.Infraestructure.Data;
using Asomameco.Infraestructure.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace ASOMAMECO.Test.Integration
{
    public class GuardarUsuarioIntegrationTestBaseDatos
    {
        private readonly string _connectionString;

        public GuardarUsuarioIntegrationTestBaseDatos()
        {
            // Asegurarse de que se use el directorio correcto para cargar el archivo de configuración
            var config = new ConfigurationBuilder()
                .SetBasePath(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location))  // Directorio de salida del test
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            _connectionString = config.GetConnectionString("SqlServerDataBase");

            // Depurar o verificar que la cadena se haya leído correctamente
            // System.Diagnostics.Debug.WriteLine("Cadena de conexión: " + _connectionString);
        }

        [Fact]
        public async Task GuardarUsuario_DeberiaGuardarCorrectamenteEnBaseDeDatos()
        {
            var options = new DbContextOptionsBuilder<AsomamecoContext>()
                .UseSqlServer(_connectionString)
                .Options;

            using (var context = new AsomamecoContext(options))
            {
                // Arrange: Crear un usuario de prueba (ajusta el Id según convenga)
                var nuevoUsuario = new Usuario
                {
                    // Si Id es configurado manualmente (ValueGeneratedNever()), asegúrate de que sea único
                    Id = 9999,
                    Nombre = "Juan",
                    Apellidos = "Pérez",
                    Cedula = "123456789",
                    Correo = "juan@example.com",
                    Contraseña = "123456",
                    Estado1 = 1,
                    Estado2 = 1,
                    Tipo = 1
                };

                // Act: Guardar usuario
                context.Usuario.Add(nuevoUsuario);
                await context.SaveChangesAsync();

                // Assert: Verificar si se guardó correctamente
                var usuarioGuardado = await context.Usuario
                    .FirstOrDefaultAsync(u => u.Correo == "juan@example.com");

                Assert.NotNull(usuarioGuardado);
                Assert.Equal("Juan", usuarioGuardado.Nombre);
            }
        }
    }
}
