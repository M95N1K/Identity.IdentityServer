using Identity.IdentityServer.Models.Auth;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Identity.IdentityServer.ViewModels
{
    public class ExternalRegisterViewModel : RegisterViewModel
    {
        [DataType(DataType.Text)]
        public string GivenName { get; set; }

        [DataType(DataType.Text)]
        public string Surname { get; set; }
    }
}
