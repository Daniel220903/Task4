namespace AppLogin.Models
{
    public class UserAction
    {
        public int Id { get; set; }
        public int UsuarioId { get; set; }
        public string? ActionName { get; set; }
        public int? UserAffected {  get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsDeleted { get; set; }
    }
}
