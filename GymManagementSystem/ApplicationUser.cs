using Microsoft.AspNetCore.Identity;

namespace GymManagementSystem.Models
{
    // We extend IdentityUser to add our own custom fields
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; } = string.Empty;

        // "Admin" or "Member"
        public string Role { get; set; } = string.Empty;
    }
}