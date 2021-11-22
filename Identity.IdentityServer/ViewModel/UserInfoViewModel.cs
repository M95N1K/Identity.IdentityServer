using Identity.IdentityServer.Models.Entity;
using Microsoft.AspNetCore.Identity;
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Identity.IdentityServer.ViewModel
{
    public class UserInfoViewModel
    {
        [ReadOnly(true)]
        public string Id { get; set; }

        public bool LockoutEnabled { get; set; }

        [DataType(DataType.Date)]
        [UIHint("Object")]
        public  DateTimeOffset? LockoutEnd { get; set; }

        public  bool TwoFactorEnabled { get; set; }

        public  bool PhoneNumberConfirmed { get; set; }
       
        public  string PhoneNumber { get; set; }
       
        public  bool EmailConfirmed { get; set; }

        public string Email { get; set; }

        public  string UserName { get; set; }
        
        public string FamilyName { get; set; }
        
        public string GivenName { get; set; }
        
        public string Nickname { get; set; }
        
        public string Gender { get; set; }
        
        public string Picture { get; set; }

        [DataType(DataType.Date)]
        [UIHint("Object")]
        public DateTime Birthdate { get; set; }
        
        public string Role { get; set; }
    }
}
