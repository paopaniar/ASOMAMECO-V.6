using Asomameco.Infraestructure.Models;
using Asomameco.Infraestructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ASOMAMECO.Test.Integration
{
    public class ConfirmacionAsistenciaIntegrationTest
    {
        [Fact]
        public async Task ConfirmarAsistencia_BaseDeDatosReal()
        {
            var options = new DbContextOptionsBuilder<AsomamecoContext>()
                .UseSqlServer("Server=.;Database=ASOMAMECO;Trusted_Connection=True;TrustServerCertificate=True;")
                .Options;

            var usuarioId = new Random().Next(100000, 999999);
            var asambleaId = new Random().Next(100000, 999999);
            var fechaAsamblea = DateTime.Now.AddDays(7);

            using var context = new AsomamecoContext(options);

            // Crear usuario de prueba
            var usuario = new Usuario
            {
                Id = usuarioId,
                Nombre = "UsuarioTest",
                Apellidos = "Prueba",
                Cedula = "111111111",
                Correo = "confirmacion_test@example.com",
                Contraseña = "test123",
                Estado1 = 1,
                Estado2 = 1,
                Telefono = 12345678,
                Tipo = 1
            };

            // Crear asamblea de prueba
            var asamblea = new Asamblea
            {
                Id = asambleaId,
                Fecha = fechaAsamblea,
                Estado = 1
            };

            // Insertar datos previos
            context.Usuario.Add(usuario);
            context.Asamblea.Add(asamblea);
            await context.SaveChangesAsync();

            // Confirmar asistencia
            var confirmacion = new Confirmacion
            {
                IdMiembro = usuarioId,
                IdAsamblea = asambleaId,
                FechaConfirmacion = DateTime.Now,
                Metodo = 1 // 1 = Confirmación manual, por ejemplo
            };

            context.Confirmacion.Add(confirmacion);
            await context.SaveChangesAsync();

            // Validar que la confirmación exista
            var confirmacionGuardada = await context.Confirmacion
                .FirstOrDefaultAsync(c => c.IdMiembro == usuarioId && c.IdAsamblea == asambleaId);

            Assert.NotNull(confirmacionGuardada);
            Assert.Equal(usuarioId, confirmacionGuardada.IdMiembro);
            Assert.Equal(asambleaId, confirmacionGuardada.IdAsamblea);

            // Limpieza
            context.Confirmacion.Remove(confirmacionGuardada);
            context.Usuario.Remove(usuario);
            context.Asamblea.Remove(asamblea);
            await context.SaveChangesAsync();
        }
    }
}
