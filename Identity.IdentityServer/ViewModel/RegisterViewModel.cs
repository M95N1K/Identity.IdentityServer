using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Identity.IdentityServer.ViewModels
{
    public class RegisterViewModel
    {
        [Required]
        [DataType(DataType.Text)]
        public string UserName { get; set; }

        [DataType(DataType.Text)]
        public string Gender { get; set; }

        [DataType(DataType.Date)]
        [UIHint("Object")]
        public DateTime Birthdate { get; set; }

        [Required]
        [DisplayName("Pass")]
        [DataType(DataType.Password)]
        [MinLength(5, ErrorMessage = "Minimum 6 characters.")]
        public string Pass { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [DisplayName("Retry Password")]
        [Compare("Pass", ErrorMessage = "The password and confirmation password do not match.")]
        [MinLength(5, ErrorMessage = "Minimum 6 characters.")]
        public string RetrPass { get; set; }

        [ScaffoldColumn(true)]
        public string ReturnUrl { get; set; }

    }
}
