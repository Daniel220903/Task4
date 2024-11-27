using static AppLogin.Controllers.HomeController;

namespace AppLogin.ViewModels
{
    public class UserActionVM
    {
        public int IdUsuario { get; set; }
        public string NombreCompleto { get; set; }
        public string Correo { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? LastLogged { get; set; }
        public List<UsuarioBlocked> UsersBlocked { get; set; } = new();
        public List<UsuarioBlocked> BlockedBy { get; set; } = new();
        public bool IsDeleted { get; set; }
        public int status { get; set; }
    }
}
