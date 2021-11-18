using Identity.IdentityServer.Models.Entity;
using System.Text.Json;

namespace Identity.IdentityServer.Infrastructure
{
    public static class Extension
    {
        public static UserInfo ToUserInfo(this User user)
        {
            var json = JsonSerializer.Serialize(user);

            var result = JsonSerializer.Deserialize<UserInfo>(json);

            return result;
        }
    }
}
