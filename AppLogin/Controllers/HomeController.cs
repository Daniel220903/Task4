using AppLogin.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using AppLogin.Data;
using Microsoft.EntityFrameworkCore;
using AppLogin.ViewModels;
using System.Dynamic;

namespace AppLogin.Controllers
{

    [Authorize]
    public class HomeController : Controller
    {
        private readonly AppDBContext _appDbContext;
        private readonly ILogger<HomeController> _logger;

        public HomeController(AppDBContext appDbContext, ILogger<HomeController> logger)
        {
            _appDbContext = appDbContext;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            int userId = int.Parse(User.FindFirst("id").Value);

            // Verifica si el usuario fue encontrado
            Usuario? usuario_validacion = await _appDbContext.Usuarios.Where(u => u.IdUsuario == userId).FirstOrDefaultAsync();

            if (usuario_validacion == null)
            {
                // Si no se encuentra el usuario, maneja el caso apropiadamente
                TempData["Mensaje"] = "User not found.";
                return RedirectToAction("Login", "Acceso");
            }

            // Ahora puedes acceder a las propiedades del usuario sin temor a NullReferenceException
            if (usuario_validacion.status == 1)
            {
                TempData["Mensaje"] = "The user was blocked";
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return RedirectToAction("Login", "Acceso");
            }

            var usuarios = await _appDbContext.Usuarios.ToListAsync();
            var acciones = await _appDbContext.Actions.ToListAsync();

            var usuarioViewModels = new List<UserActionVM>();

            foreach (var usuario in usuarios)
            {
                var usuarioViewModel = new UserActionVM
                {
                    IdUsuario = usuario.IdUsuario,
                    NombreCompleto = usuario.NombreCompleto,
                    Correo = usuario.Correo,
                    CreatedAt = usuario.CreatedAt,
                    status = usuario.status,
                    IsDeleted = usuario.IsDeleted,
                };

                foreach (var accion in acciones)
                {
                    if (accion.UsuarioId == usuario.IdUsuario && accion.ActionName == "Logged")
                        usuarioViewModel.LastLogged = accion.CreatedAt;

                    if (accion.UsuarioId == usuario.IdUsuario && accion.ActionName == "Block")
                    {
                        var usuarioAfectado = await (from a in _appDbContext.Actions
                                                     join u in _appDbContext.Usuarios
                                                     on a.UserAffected equals u.IdUsuario
                                                     where a.UserAffected == accion.UserAffected
                                                     where a.ActionName == "Block"
                                                     select new { u.IdUsuario, u.Correo, a.CreatedAt, a.Id }).Distinct()
                                                      .FirstOrDefaultAsync();

                        if (usuarioAfectado != null)
                        {
                            var usuarioBlocked = new UsuarioBlocked
                            {
                                IdUsuario = usuarioAfectado.IdUsuario,
                                Correo = usuarioAfectado.Correo,
                                CreatedAt = usuarioAfectado.CreatedAt,
                                actionId = usuarioAfectado.Id
                            };

                            usuarioViewModel.UsersBlocked.Add(usuarioBlocked);
                        }
                    }

                    if (accion.UserAffected == usuario.IdUsuario && accion.ActionName == "Block")
                    {
                        var usuarioAfectado = await (from a in _appDbContext.Actions
                                                     join u in _appDbContext.Usuarios
                                                     on a.UsuarioId equals u.IdUsuario
                                                     where a.UsuarioId == accion.UsuarioId
                                                     where a.ActionName == "Block"
                                                     select new { u.IdUsuario, u.Correo, a.CreatedAt, a.Id }).Distinct()
                                                      .FirstOrDefaultAsync();

                        if (usuarioAfectado != null)
                        {
                            var usuarioBlocked = new UsuarioBlocked
                            {
                                IdUsuario = usuarioAfectado.IdUsuario,
                                Correo = usuarioAfectado.Correo,
                                CreatedAt = usuarioAfectado.CreatedAt,
                                actionId = usuarioAfectado.Id
                            };

                            usuarioViewModel.BlockedBy.Add(usuarioBlocked);
                        }
                    }
                }

                usuarioViewModels.Add(usuarioViewModel);
            }

            return View(usuarioViewModels);
        }


        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public  async Task<IActionResult> Salir()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Acceso");
        }

   
        public class UsuarioBlocked
        {
            public int IdUsuario { get; set; }
            public string Correo { get; set; }
            public DateTime CreatedAt { get; set; }
            public int actionId { get; set; }
        }
    }
}
