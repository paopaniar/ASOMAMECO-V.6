using Asomameco.Application.DTOs;
using Asomameco.Application.Services.Interfaces;
using Asomameco.Application.Services.Implementations;
using Asomameco.Infraestructure.Data;
using Asomameco.Infraestructure.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Crypto.Generators;
using X.PagedList;
using X.PagedList.Extensions;
using SkiaSharp;
using Org.BouncyCastle.Cmp;
using Asomameco.Web.Models;

namespace Asomameco.Web.Controllers
{
    public class AsambleaController : Controller
    {
        private readonly IServiceAsamblea _serviceAsamblea;
        private readonly IServiceAsistencia _serviceAsistencia;
        private readonly IServiceUsuario _serviceUsuario;
        private readonly IServiceConfirmacion _confirmacionService;
        private readonly IServiceEstadoAsamblea _serviceEstadoAsamblea;
        private readonly EmailService _emailService;
        private readonly QrService _qrService;
        private readonly AsomamecoContext context;

        public AsambleaController(IServiceAsamblea serviceAsamblea, IServiceAsistencia serviceAsistencia, IServiceConfirmacion serviceConfirmacion,  IServiceEstadoAsamblea serviceEstadoAsamblea, AsomamecoContext _context, EmailService emailService, QrService qrService, IServiceUsuario serviceUsuario)
        {
            _serviceAsamblea = serviceAsamblea;
            _serviceAsistencia = serviceAsistencia;
            _emailService = emailService;
            _confirmacionService = serviceConfirmacion;
            _serviceEstadoAsamblea = serviceEstadoAsamblea;
            _qrService = qrService;
            context = _context;
            _serviceUsuario = serviceUsuario;
        }



        public async Task<ActionResult> IndexAdmin(int? page)
        {
            var collection = await _serviceAsamblea.ListAsync();
            //Cantidad de elementos por p�gina
            return View(collection.ToPagedList(page ?? 1, 10));
        }

        [Authorize]
        public async Task<ActionResult> Details(int id)
        {
            try
            {
            
                var @object = await _serviceAsamblea.FindByIdAsync(id);
                if (@object == null)
                {
                    throw new Exception("Asamblea no existente");

                }

                return View(@object);

            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }




        [Authorize]
        [HttpGet]
        public async Task<ActionResult> Create()
        {
            var consecutivo = await context.Asamblea.OrderByDescending(x => x.Id).FirstOrDefaultAsync();
            ViewBag.id = consecutivo?.Id + 1 ?? 1;

            //var estados = await _serviceEstadoAsamblea.ListAsync();
            //ViewBag.ListEstado = new SelectList(estados, "Id", "Descripcion"); // Cambio de MultiSelectList a SelectList

            return View();
        }


        [Authorize]
        [HttpGet]
        public async Task<IActionResult> RegistroAsistenciaAsync()
        {
            // Obtener todos los tipos de usuario
            var asambleas =  await _serviceAsamblea.ListAsync();
            var asambleasFiltradas = asambleas?
                .Where(asa => asa.Estado == 1)  // Filtrar por Estado = 1
                .OrderBy(asa => asa.Fecha)       // Ordenar de m�s vieja a m�s reciente
                .Select(asa => new
                {
                    asa.Id,
                    Fecha = asa.Fecha.ToString("dd/MMMM/yyyy HH:mm") // Convertir Fecha a string
                })
                .ToList();

            // Asignar los tipos de usuario filtrados al ViewBag
            ViewBag.ListAsamblea = asambleasFiltradas;

            return View();
        }

        [HttpPost]
        public async Task<IActionResult>  RegistrarAsistencia(AsistenciaDTO model)
        {


            // Obtener usuario autenticado
            var user = await _serviceUsuario.FindByIdAsync(model.IdMiembro);


            // Verificar si el usuario ya tiene asistencia registrada en la asamblea
            var asistenciaExistente = await _serviceAsamblea.VerificarAsistencia(model.IdMiembro, model.IdAsamblea);


            if (user != null)
            {
                if (asistenciaExistente)
                {
                    if (ModelState.IsValid)
                    {

                        model.IdMiembro = model.IdMiembro;
                        model.IdAsamblea = model.IdAsamblea;
            
                        model.FechaHoraLlegada = DateTime.Now;


                       await _serviceAsamblea.ConfirmAttendance(model);
                        TempData["Mensaje"] = "Asistencia registrada correctamente";
                        return RedirectToAction("RegistroAsistencia");
                    }
                }
                ViewBag.ErrorMessage = "Usuario no confirmado para esta asamblea";

                return RedirectToAction("UsuarioNoConfirmado", "Error");
            }
            ViewBag.ErrorMessage = "Usuario no encontrado";
            return View(model);
        }

        public async Task<IActionResult> Analitica()
        {
            var asambleas = await _serviceAsamblea.ListAsync();
            var confirmaciones = await _confirmacionService.ListAsync();
            var asistencias = await _serviceAsistencia.ListAsync();
            var usuarios = await _serviceUsuario.ListAsync();

            var analitica = asambleas.Select(a => new AsambleaAnaliticaViewModel
            {
                NombreAsamblea = $"Asamblea {a.Id}",
                FechaAsamblea = a.Fecha,
                TotalInvitados = usuarios.Count,
                TotalConfirmados = confirmaciones.Count(c => c.IdAsamblea == a.Id),
                TotalAsistentes = asistencias.Count(ass => ass.IdAsamblea == a.Id)
            }).ToList();

            return View(analitica);
        }


        public async Task<IActionResult> ListaConfirmacion()
        {
            // Obtener los datos de cada servicio
            var asambleas = await _serviceAsamblea.ListAsync();
            var usuarios = await _serviceUsuario.ListAsync();
            var confirmaciones = await _confirmacionService.ListAsync();

            // Unir la informaci�n usando LINQ
            var listaConfirmaciones = (from c in confirmaciones
                                       join u in usuarios on c.IdMiembro equals u.Id
                                       join a in asambleas on c.IdAsamblea equals a.Id
                                       select new ConfirmacionAsistenciaViewModel
                                       {
                                           IdMiembro = c.IdMiembro,
                                           Nombre = u.Nombre + " " + u.Apellidos,
                                           IdAsamblea = c.IdAsamblea,
                                           FechaAsamblea = a.Fecha,
                                           FechaConfirmacion = c.FechaConfirmacion
                                       }).ToList();

            // Pasar la lista consolidada a la vista
            return View(listaConfirmaciones);
        }


        [Authorize]
        // POST: AsambleaController/Create
        [HttpPost]

        public async Task<ActionResult> Create(AsambleaDTO dto)
        {

            try
            {


                //Validaci�n del formulario
                if (!ModelState.IsValid)
                {
                    // Lee del ModelState todos los errores que
                    // vienen para el lado del server
                    string errors = string.Join("; ", ModelState.Values
                                       .SelectMany(x => x.Errors)
                                       .Select(x => x.ErrorMessage));
                    ViewBag.ErrorMessage = errors;
                    return View();
                }

                dto.Estado = 1;
                //Crear
                await _serviceAsamblea.AddAsync(dto);





                //Enviar correo de notificacion a todos los usuarios de que hay una nueva asamblea creada
                var usuarios = context.Usuario.ToList();


                foreach (var usuario in usuarios)
                {

                    string urlConfirmacion = $"https://localhost:7282/Asamblea/Confirmar?id={usuario.Id}&idAsamblea={dto.Id}";
                    string mensaje = $@"
            <h2>Notificaci�n de Asamblea</h2>
            <p>Se ha programado una nueva asamblea para el <strong>{dto.Fecha.ToShortDateString()}</strong> a las <strong>{dto.Fecha.Hour}</strong>:<strong>{dto.Fecha.Minute}</strong>.</p>
            <p>Por favor, confirma tu asistencia haciendo clic en el bot�n de abajo:</p>
            <a href='{urlConfirmacion}' 
               style='background-color: #28a745; color: white; padding: 10px 15px; text-decoration: none; border-radius: 5px;'>
               S�, Confirmo
            </a>
            <p>Si no puedes asistir, no es necesario hacer nada.</p>";

                    await _emailService.EnviarCorreoAsync(usuario.Correo, "Confirmaci�n de Asamblea", mensaje);



                }
                return RedirectToAction("IndexAdmin");
            }
            catch (Exception ex)
            {
                var innerException = ex.InnerException?.Message ?? ex.Message;
                ViewBag.ErrorMessage = $"Error al guardar el Asamblea: {innerException}";  
                return View(dto);
            }
        }

        [HttpGet]
        //el nombre de los parametros debe de ser exactamente igual al de los que se le ponen en la url del metodo Create donde la url del boton de confirmacion es generada
        public async Task<IActionResult> Confirmar(int id, int idAsamblea)
        {
            var usuario = await context.Usuario.FindAsync(id);
            if (usuario == null) return RedirectToAction("NotFound", "Error");


            var asamblea = await context.Asamblea.FindAsync(idAsamblea);
            if (asamblea == null) return RedirectToAction("NotFound", "Error");


            usuario.Estado2 = 1; // Confirmar asistencia
            //AQUI SE CONFIRMA QUE SE ASISTIRA
            await context.SaveChangesAsync();

            // Generar el QR con la ID del usuario
            var qrCodePath = _qrService.GenerarQR(usuario.Id.ToString()); // Obtener la ruta del archivo QR

            // Crear el mensaje con el QR
            string mensaje = $@"
    <h2>�Confirmaci�n Exitosa!</h2>
    <p>Gracias por confirmar tu asistencia. Presenta este c�digo QR en la entrada:</p>
    <p>Nos vemos en la asamblea.</p>";

            ConfirmacionDTO confirmacion = new ConfirmacionDTO();

            confirmacion.IdMiembro = id;
            confirmacion.IdAsamblea= idAsamblea;
            confirmacion.Metodo = 0;
            confirmacion.FechaConfirmacion = DateTime.Now;

            //Confirmar hacia la base de datos
            await _serviceAsamblea.Confirmation(confirmacion);

            // Enviar el correo con el archivo QR adjunto
            await _emailService.EnviarCorreoConAdjuntoAsync(usuario.Correo, "Asistencia Confirmada", mensaje, qrCodePath);
        


            // Intentar eliminar el archivo QR de manera segura, reintentando si est� en uso
            // Esperar un tiempo adicional antes de intentar eliminar el archivo (por ejemplo, 5 segundos)
            await Task.Delay(2000); // Espera 5 segundos
            try
            {
                // Intentar eliminar el archivo despu�s de verificar que no est� en uso
                bool fileDeleted = false;
                int attempts = 0;
                while (!fileDeleted && attempts < 10)
                {
                    if (!IsFileInUse(qrCodePath))
                    {
                        System.IO.File.Delete(qrCodePath);
                        fileDeleted = true;
                    }
                    else
                    {
                        // Esperar 200 ms antes de reintentar
                        await Task.Delay(200);
                        attempts++;
                    }
                }

                // Si despu�s de 5 intentos no se puede eliminar el archivo, loguear el error
                if (!fileDeleted)
                {
                    return RedirectToAction("ConfirmacionExito");
                }
            }
            catch (Exception ex)
            {
                // Log el error si ocurre alguna excepci�n al eliminar el archivo
               return Content($"Error al eliminar el archivo QR: {ex.Message}");
            }

            // Redirigir a la vista de confirmaci�n exitosa
            return RedirectToAction("ConfirmacionExito");
        }

        // M�todo que verifica si el archivo est� en uso
        private bool IsFileInUse(string filePath)
        {
            try
            {
                using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                {
                    // Si no se lanza una excepci�n, el archivo no est� en uso
                    return false;
                }
            }
            catch (IOException)
            {
                // Si se lanza una excepci�n, significa que el archivo est� en uso
                return true;
            }
        }

        public IActionResult ConfirmacionExito()
        {
            // Vista que muestra un mensaje de �xito de la confirmaci�n
            ViewBag.Message = "�Gracias por confirmar tu asistencia! Estamos encantados de contar contigo.";
            return View();
        }



        [Authorize]
        // GET: AsambleaController/Edit/
        public async Task<ActionResult> Edit(int id)
        {
            var @object = await _serviceAsamblea.FindByIdAsync(id);
          

            // Obtener todos los Estados de Asamblea
            var allEstadosAsamblea = await _serviceEstadoAsamblea.ListAsync();

            // Filtrar los Estados de Asamblea con valores 2 y 3
            var EstadosAsambleaFiltrados = allEstadosAsamblea
                .Where(tu => tu.Id == 1 || tu.Id == 2 || tu.Id == 3) // Suponiendo que 'Id' es el campo que contiene el valor de Estado
                .ToList();

            // Asignar los Estados de Asamblea filtrados al ViewBag
            ViewBag.ListRol = EstadosAsambleaFiltrados;
            ViewBag.Id = @object.Id;

            // Enviar el Estado de Asamblea actual al ViewBag para que sea seleccionado
            ViewBag.SelectedEstadoAsamblea = @object.EstadoNavigation.Id; // Aseg�rate de que 'EstadoAsambleaId' es el campo adecuado

            return View(@object);
        }

        //// POST: ProcesoPreparacionController/Edit/5
        [HttpPost]
        [Authorize]

        public async Task<ActionResult> Edit(int id, AsambleaDTO dto)
        {
            try
            {
                // Obtener el Asamblea actual de la base de datos
                var AsambleaActual = await _serviceAsamblea.FindByIdAsync(id);             
          
                await _serviceAsamblea.UpdateAsync(id, dto);

         


                //Enviar correo de notificacion a todos los usuarios de que hay una nueva asamblea creada
                var usuarios = context.Usuario.ToList();


                foreach (var usuario in usuarios)
                {

                    string urlConfirmacion = $"https://localhost:7282/Asamblea/Confirmar?id={usuario.Id}&idAsamblea={dto.Id}";
                    string mensaje = $@"
            <h2>Notificaci�n de Asamblea</h2>
            <p>Se ha realizado una modificaci�n de una asamblea para el <strong>{dto.Fecha.ToShortDateString()}</strong> a las <strong>{dto.Fecha.Hour}</strong>:<strong>{dto.Fecha.Minute}</strong>.</p>
            <p>Por favor, confirma tu asistencia haciendo clic en el bot�n de abajo:</p>
            <a href='{urlConfirmacion}' 
               style='background-color: #28a745; color: white; padding: 10px 15px; text-decoration: none; border-radius: 5px;'>
               S�, Confirmo
            </a>
            <p>Si no puedes asistir, no es necesario hacer nada.</p>";

                    await _emailService.EnviarCorreoAsync(usuario.Correo, "Confirmaci�n de Asamblea", mensaje);



                }
                return RedirectToAction("IndexAdmin");
            }
            catch (Exception ex)
            {
                var innerException = ex.InnerException?.Message ?? ex.Message;
                ViewBag.ErrorMessage = $"Error al guardar el Asamblea: {innerException}";
                return View(dto);
            }
        }


        [HttpPost]
        [Authorize]

        public async Task<ActionResult> Delete(int id)
        {
            try
            {
                var Asamblea = await _serviceAsamblea.FindByIdAsync(id);
                if (Asamblea == null)
                {
                    throw new Exception("Asamblea no existente");
                }


                // Actualizar
                await _serviceAsamblea.DeleteAsync(id, Asamblea);

                // Redirigir a IndexAdmin despu�s de eliminar
                return RedirectToAction("IndexAdmin");
            }
            catch (Exception ex)
            {
                var innerException = ex.InnerException?.Message ?? ex.Message;
                ViewBag.ErrorMessage = $"Error al eliminar el Asamblea: {innerException}";

                // Redirigir a IndexAdmin en caso de error
                return RedirectToAction("IndexAdmin");
            }
        }
    }
}
