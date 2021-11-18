using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Identity.IdentityServer.Models.Entity
{
    public class UserInfo: User
    {
        public string Role { get; set; }
    }
}
