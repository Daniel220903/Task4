using AppLogin.Data;
using AppLogin.Models;
using AppLogin.ViewModels;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static AppLogin.Controllers.HomeController;

namespace AppLogin.Controllers{
    public class UsuarioController : Controller{
        private readonly AppDBContext _appDbContext;

        public UsuarioController(AppDBContext appDbContext){
            _appDbContext = appDbContext;
        }

        public class UserActionPayload{
            public string ActionType { get; set; }
            public List<string> SelectedUsers { get; set; }
        }

        [HttpPost]
        public async Task<IActionResult> AccionUsuario([FromBody] UserActionPayload payload)
        {
            var actionType = payload.ActionType;
            var userIds = payload.SelectedUsers;

            var userIdClaim = User.FindFirst("id");
            int usuarioId = 0;

            if (userIdClaim != null){
                usuarioId = int.Parse(userIdClaim.Value);
            }

            DateTime currentDateTime = DateTime.Now;
           
            switch (actionType){
                case "borrar":
                    foreach (var userId in userIds){
                        int id = int.Parse(userId);
                        Usuario? user = await _appDbContext.Usuarios
                            .Where(u => u.IdUsuario == id)
                            .FirstOrDefaultAsync();

                        UserAction action = new UserAction(){
                            UsuarioId = usuarioId,
                            ActionName = "Delete",
                            UserAffected = id,
                            CreatedAt = currentDateTime,
                            IsDeleted = false,
                        };
                        await _appDbContext.Actions.AddAsync(action);

                        if (user != null){
                            _appDbContext.Usuarios.Remove(user);
                        }
                    }
                    break;
                case "bloquear":
                    foreach (var userId in userIds){
                        int id = int.Parse(userId);
                        Usuario? user = await _appDbContext.Usuarios
                            .Where(u => u.IdUsuario == id)
                            .FirstOrDefaultAsync();


                        UserAction action = new UserAction()
                        {
                            UsuarioId = usuarioId,
                            ActionName = "Block",
                            UserAffected = id,
                            CreatedAt = currentDateTime,
                            IsDeleted = false,
                        };
                        await _appDbContext.Actions.AddAsync(action);

                        if (user != null)
                        {
                            user.status = 1;
                        }
                    }

                    break;
                case "desbloquear":
                    foreach (var userId in userIds){
                        int id = int.Parse(userId);
                        Usuario? user = await _appDbContext.Usuarios
                            .Where(u => u.IdUsuario == id)
                            .FirstOrDefaultAsync();

                        UserAction action = new UserAction()
                        {
                            UsuarioId = usuarioId,
                            ActionName = "Unblock",
                            UserAffected = id,
                            CreatedAt = currentDateTime,
                            IsDeleted = false,
                        };
                        await _appDbContext.Actions.AddAsync(action);

                        if (user != null){
                            user.status = 0;
                        }
                    }
                    break;
                default:
                    return RedirectToAction("Index", "Home");
                    break;
            }
            await _appDbContext.SaveChangesAsync();
            return RedirectToAction("Index", "Home");
        }

        
        [HttpGet]
        public async Task<IActionResult> activityReport(int id)
        {
            // Verificar si el claim "id" existe
            var userClaim = User.FindFirst("id");
            if (userClaim == null)
            {
                TempData["Mensaje"] = "User claim not found.";
                return RedirectToAction("Login", "Acceso");
            }

            int userId;
            if (!int.TryParse(userClaim.Value, out userId))
            {
                TempData["Mensaje"] = "Invalid user ID.";
                return RedirectToAction("Login", "Acceso");
            }

            // Buscar usuario actual
            Usuario? usuario_validacion = await _appDbContext.Usuarios
                .Where(u => u.IdUsuario == userId)
                .FirstOrDefaultAsync();

            if (usuario_validacion == null || usuario_validacion.status == 1)
            {
                TempData["Mensaje"] = "The user has been blocked or not found.";
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return RedirectToAction("Login", "Acceso");
            }

            // Buscar usuario solicitado
            Usuario? usuario_encontrado = await _appDbContext.Usuarios
                .Where(u => u.IdUsuario == id)
                .FirstOrDefaultAsync();

            if (usuario_encontrado == null)
            {
                TempData["Mensaje"] = "User not found.";
                return RedirectToAction("Login", "Acceso");
            }

            var usuarioViewModels = new List<UserActionVM>();
            var acciones = await _appDbContext.Actions.ToListAsync();

            var usuarioViewModel = new UserActionVM
            {
                IdUsuario = usuario_encontrado.IdUsuario,
                NombreCompleto = usuario_encontrado.NombreCompleto,
                Correo = usuario_encontrado.Correo,
                CreatedAt = usuario_encontrado.CreatedAt,
                status = usuario_encontrado.status,
                IsDeleted = usuario_encontrado.IsDeleted,
            };

            foreach (var accion in acciones)
            {
                if (accion.UsuarioId == usuario_encontrado.IdUsuario && accion.ActionName == "Logged")
                {
                    usuarioViewModel.LastLogged = accion.CreatedAt;
                }

                if (accion.UsuarioId == usuario_encontrado.IdUsuario && accion.ActionName == "Block")
                {
                    var usuarioAfectado = await (from a in _appDbContext.Actions
                                                 join u in _appDbContext.Usuarios
                                                 on a.UserAffected equals u.IdUsuario
                                                 where a.UserAffected == accion.UserAffected
                                                 where a.ActionName == "Block"
                                                 select new { u.IdUsuario, u.Correo, a.CreatedAt, a.Id })
                                                 .Distinct()
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

                if (accion.UserAffected == usuario_encontrado.IdUsuario && accion.ActionName == "Block")
                {
                    var usuarioAfectado = await (from a in _appDbContext.Actions
                                                 join u in _appDbContext.Usuarios
                                                 on a.UsuarioId equals u.IdUsuario
                                                 where a.UsuarioId == accion.UsuarioId
                                                 where a.ActionName == "Block"
                                                 select new { u.IdUsuario, u.Correo, a.CreatedAt, a.Id })
                                                 .Distinct()
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

            return View(usuarioViewModels);
        }


    }
}
