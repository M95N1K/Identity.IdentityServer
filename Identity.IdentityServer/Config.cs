using IdentityServer4;
using IdentityServer4.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Identity.IdentityServer
{
    public static class Config
    {
        internal static IEnumerable<Client> GetClients() =>
            new List<Client>
            {
                new Client
                {
                    ClientId = "client_id",
                    ClientSecrets = {new Secret("client_secrets".Sha256()) },
                    AllowedGrantTypes = GrantTypes.ClientCredentials,
                    AllowedScopes = {"clientAPI"}
                },

                 //client_Pkce
                new Client
                {
                    ClientId = "client_Pkce",
                    RequireConsent = false,
                    RequirePkce = true,
                    RequireClientSecret = false,
                    AllowedGrantTypes = GrantTypes.Code,
                    AllowedScopes =
                    {
                        "clientAPI",
                        IdentityServerConstants.StandardScopes.OpenId,
                        IdentityServerConstants.StandardScopes.Profile,
                        IdentityServerConstants.StandardScopes.Email,
                        "role",
                    },

                    //AllowedCorsOrigins = { "*/*" },
                    RedirectUris = { "https://localhost:5001/authentication/login-callback"},
                    PostLogoutRedirectUris = {"https://localhost:5001/authentication/logout-callback"},
                },

                new Client
                {
                    ClientId = "client_mvc",
                    ClientSecrets = { new Secret("mvc_secret".Sha256())},
                    AllowedGrantTypes = GrantTypes.Code,

                    AllowedScopes =
                    {
                        "clientAPI",
                        IdentityServerConstants.StandardScopes.OpenId,
                        IdentityServerConstants.StandardScopes.Profile,
                        "role",

                    },
                    RedirectUris = { "https://localhost:7001/signin-oidc" },
                    RequireConsent = false,

                    AccessTokenLifetime = 5,
                    AllowOfflineAccess = true,
                    PostLogoutRedirectUris = { "https://localhost:7001/signout-callback-oidc" },
                    //AlwaysIncludeUserClaimsInIdToken = true,
                    Enabled = true,
                }
            };

        internal static IEnumerable<IdentityResource> GetIdentityREsources()
        {
            yield return new IdentityResources.OpenId();
            yield return new IdentityResources.Profile();
            yield return new IdentityResource("roles", "User role(s)", new List<string> { "role" });
        }

        internal static IEnumerable<ApiScope> GetApiScopes()
        {
            yield return new ApiScope("clientAPI");
            yield return new ApiScope("role");
            yield return new ApiScope("email");
        }

        internal static IEnumerable<ApiResource> GetApiResources()
        {
            yield return new ApiResource("clientAPI") { Scopes = { "clientAPI" } };
            yield return new ApiResource("role") { Scopes = { "role" } };
            yield return new ApiResource("email") { Scopes = { "email" } };
        }
    }
}
