using Asomameco.Infraestructure.Models;
using Asomameco.Infraestructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ASOMAMECO.Test.Integration
{
    public class GuardarUsuarioIntegrationTest
    {
        [Fact]
        public async Task GuardarUsuario_BaseDeDatosReal()
        {
            var options = new DbContextOptionsBuilder<AsomamecoContext>()
                .UseSqlServer("Server=.;Database=ASOMAMECO;Trusted_Connection=True;TrustServerCertificate=True;")
                .Options;

            var usuarioId = new Random().Next(100000, 999999); // ID aleatorio

            using var context = new AsomamecoContext(options);

            var nuevoUsuario = new Usuario
            {
                Id = usuarioId,
                Nombre = "Jua33n",
                Apellidos = "Prueba2",
                Cedula = "999999999",
                Correo = "test_guardado@example.com",
                Contraseña = "abc123ewe",
                Estado1 = 1,
                Estado2 = 1,
                Tipo = 1
            };

            try
            {
                context.Usuario.Add(nuevoUsuario);
                await context.SaveChangesAsync();

                var usuarioGuardado = await context.Usuario
                    .FirstOrDefaultAsync(u => u.Correo == "test_guardado@example.com");

                Assert.NotNull(usuarioGuardado);
                Assert.Equal("Jua33n", usuarioGuardado.Nombre);
            }
            finally
            {
                // Limpieza del test: eliminar el usuario insertado
                var usuarioBorrar = await context.Usuario.FirstOrDefaultAsync(u => u.Id == usuarioId);
                if (usuarioBorrar != null)
                {
                    context.Usuario.Remove(usuarioBorrar);
                    await context.SaveChangesAsync();
                }
            }
        }
    }
}
