using Microsoft.AspNetCore.Identity;

namespace CatalystPMS.Infrastructure.Identity
{
    public class ApplicationUser : IdentityUser
    {

        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        //public string Username { get; set; } = string.Empty;
        //public string Email { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string FullName => $"{FirstName} {LastName}";

        //public string Id { get; internal set; }
    }
}
