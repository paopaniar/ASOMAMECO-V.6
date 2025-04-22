using Asomameco.Infraestructure.Models;
using Asomameco.Infraestructure.Data;
using Asomameco.Application.DTOs;
using Microsoft.EntityFrameworkCore;
using Xunit;
using System;
using System.Threading.Tasks;

namespace ASOMAMECO.Test.Integration
{
    public class CrearAsambleaConDTOIntegrationTest
    {
        [Fact]
        public async Task CrearAsamblea_DesdeDTO_EnBaseDeDatosReal()
        {
            var options = new DbContextOptionsBuilder<AsomamecoContext>()
                .UseSqlServer("Server=.;Database=ASOMAMECO;Trusted_Connection=True;TrustServerCertificate=True;")
                .Options;

            using var context = new AsomamecoContext(options);

            // Simula el DTO enviado desde el formulario
            var asambleaDto = new AsambleaDTO
            {
                Fecha = DateTime.Now.AddDays(7),
                Estado = 1 // Debe existir el EstadoAsamblea con ID 1
            };

            try
            {
                // Convertir el DTO en una entidad (como haría el servicio)
                var asambleaEntidad = new Asamblea
                {
                    Fecha = asambleaDto.Fecha,
                    Estado = asambleaDto.Estado
                };

                context.Asamblea.Add(asambleaEntidad);
                await context.SaveChangesAsync();

                var asambleaGuardada = await context.Asamblea
                    .FirstOrDefaultAsync(a => a.Fecha == asambleaDto.Fecha && a.Estado == asambleaDto.Estado);

                Assert.NotNull(asambleaGuardada);
                Assert.Equal(asambleaDto.Fecha, asambleaGuardada.Fecha);
                Assert.Equal(asambleaDto.Estado, asambleaGuardada.Estado);
            }
            finally
            {
                // Limpieza
                var asambleaEliminar = await context.Asamblea
                    .FirstOrDefaultAsync(a => a.Fecha == asambleaDto.Fecha && a.Estado == asambleaDto.Estado);

                if (asambleaEliminar != null)
                {
                    context.Asamblea.Remove(asambleaEliminar);
                    await context.SaveChangesAsync();
                }
            }
        }
    }
}
