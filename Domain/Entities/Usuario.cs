using Microsoft.AspNetCore.Identity;

namespace users_api.Domain.Entities
{
    public enum Role { USER, ADMIN }

    public class Usuario : IdentityUser
    {
        public string Nome { get; set; } = string.Empty;
        public Role Role { get; set; } = Role.USER;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }
}
