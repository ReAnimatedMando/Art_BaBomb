using System.ComponentModel.DataAnnotations;

namespace Art_BaBomb.Web.Models.ViewModels
{
    public class UserRoleViewModel
    {
        [Required]
        public string UserId { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string CurrentRole { get; set; } = string.Empty;

        [Required]
        public string SelectedRole { get; set; } = string.Empty;
    }
}