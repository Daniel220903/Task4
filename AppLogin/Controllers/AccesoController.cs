using Microsoft.AspNetCore.Mvc;

using AppLogin.Data;
using AppLogin.Models;
using Microsoft.EntityFrameworkCore;
using AppLogin.ViewModels;

using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System;
using static System.Collections.Specialized.BitVector32;

namespace AppLogin.Controllers
{
    public class AccesoController : Controller
    {

        private readonly AppDBContext _appDbContext;
        public AccesoController(AppDBContext appDbContext)
        {
            _appDbContext = appDbContext;
        }

        [HttpGet]
        public IActionResult Registrarse()
        {
            if (User.Identity!.IsAuthenticated) { return RedirectToAction("Index", "Home"); }

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Registrarse(UsuarioVM modelo)
        {
            Usuario? usuario_encontrado = await _appDbContext.Usuarios.Where(u =>
                    u.Correo == modelo.Correo).FirstOrDefaultAsync();


            if (usuario_encontrado != null){
                ViewData["Mensaje"] = "This email is already registered.";
                return View();
            }else if (modelo.Clave != modelo.ConfirmarClave){
                ViewData["Mensaje"] = "The passwords do not match.";
                return View();
            }else if (usuario_encontrado != null && usuario_encontrado.status == 1){
                ViewData["Mensaje"] = "This user is blocked.";
                return View();
            }

            DateTime currentDateTime = DateTime.Now;

            Usuario usuario = new Usuario()
            {
                NombreCompleto = modelo.NombreCompleto,
                Correo = modelo.Correo,
                Clave = modelo.Clave,
                status = 0,
                CreatedAt = currentDateTime,
                IsDeleted = false
            };

            await _appDbContext.Usuarios.AddAsync(usuario);
            await _appDbContext.SaveChangesAsync();

            if (usuario.IdUsuario != 0 && usuario.IdUsuario != null)
            {
                TempData["Mensaje"] = "User Created Successfully";
                return RedirectToAction("Login", "Acceso");
            }

            ViewData["Mensaje"] = "User could not be created, error";
            return View();
        }

        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity!.IsAuthenticated) { return RedirectToAction("Index", "Home"); }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginVM modelo){
            Console.Write("llega aqui");
            Usuario? usuario_encontrado = await _appDbContext.Usuarios.Where(u =>
                    u.Correo == modelo.Correo
                    && u.Clave == modelo.Clave).FirstOrDefaultAsync();

            if (usuario_encontrado == null){
                ViewData["Mensaje"] = "No matches found";
                return View();
            }else if (usuario_encontrado.status == 1)
            {
                ViewData["Mensaje"] = "This user is blocked";
                return View();
            }

            List<Claim> claims = new List<Claim>() {
                new Claim("id", usuario_encontrado.IdUsuario.ToString()),
                new Claim(ClaimTypes.Name, usuario_encontrado.NombreCompleto)
            };

            ClaimsIdentity claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            AuthenticationProperties properties = new AuthenticationProperties() { 
                AllowRefresh = true,
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                properties
            );

            UserAction? action_found = await _appDbContext.Actions
                .Where(u => u.UsuarioId == usuario_encontrado.IdUsuario && u.ActionName == "Logged")
                .FirstOrDefaultAsync();

            DateTime currentDateTime = DateTime.Now;

            if (action_found == null)
            {
                UserAction action = new UserAction(){
                    UsuarioId = usuario_encontrado.IdUsuario,
                    ActionName = "Logged",
                    CreatedAt = currentDateTime,
                    IsDeleted = false,
                };
                await _appDbContext.Actions.AddAsync(action);
            }else{
                action_found.CreatedAt = currentDateTime;
            }
            await _appDbContext.SaveChangesAsync();

            return RedirectToAction("Index", "Home");
        }
    }
}
