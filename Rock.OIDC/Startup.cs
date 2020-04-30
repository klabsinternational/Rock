// <copyright>
// Copyright by the Spark Development Network
//
// Licensed under the Rock Community License (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.rockrms.com/license
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//

using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using IdentityServer3.Core.Configuration;
using IdentityServer3.Core.Extensions;
using IdentityServer3.Core.Models;
using IdentityServer3.Core.Services.InMemory;
using Owin;
using Rock.Utility;

namespace Rock.OIDC
{
    public class Startup : IRockOwinStartup
    {
        /// <summary>
        /// All IRockStartup classes will be run in order by this value. If class does not depend on an order, return zero.
        /// </summary>
        /// <value>
        /// The order.
        /// </value>
        public int StartupOrder => 0;

        /// <summary>
        /// Method that will be run at Rock Owin startup
        /// </summary>
        /// <param name="app"></param>
        public void OnStartup( IAppBuilder app )
        {
            var factory = GetFactory();
            var certificate = GetCertificate();

            app.UseIdentityServer( new IdentityServerOptions
            {
                SiteName = "Rock",
                SigningCertificate = certificate,
                Factory = factory,
                RequireSsl = false,
                /*AuthenticationOptions = new AuthenticationOptions
                {
                    LoginPageLinks = new List<LoginPageLink> { LoginPageLink}
                }*/
            } );
        }

        /// <summary>
        /// Gets the certificate.
        /// </summary>
        /// <returns></returns>
        private static X509Certificate2 GetCertificate()
        {
            var store = new X509Store( StoreLocation.LocalMachine );
            store.Open( OpenFlags.ReadOnly );

            foreach(var cert in store.Certificates)
            {
                if (cert.Subject == "CN=dummy.rockrms.com" )
                {
                    return cert;
                }
            }

            return null;
        }

        /// <summary>
        /// Gets the factory.
        /// </summary>
        /// <returns></returns>
        private static IdentityServerServiceFactory GetFactory()
        {
            var users = new List<InMemoryUser>
            {
                new InMemoryUser
                {
                    Username = "tdecker",
                    Password = "password",
                    Subject = "ted's userlogin guid goes here",
                    Claims = new List<Claim>
                    {
                        new Claim("name", "Ted Decker"),
                        new Claim("email", "tdekcer@example.com"),
                        new Claim("role", "User")
                    }
                }
            };

            var clients = new Client[]
            {
                new Client{
                    ClientId = "church_online",
                    ClientName = "Church Online",
                    Flow = Flows.Implicit,
                    RedirectUris = new List<string>
                    {
                        "http://localhost:5969/"
                    },
                    AllowedScopes = new List<string>
                    {
                        StandardScopes.OpenId.Name,
                        StandardScopes.Email.Name,
                        "roles"
                    }
                }
            };

            var scopes = new Scope[]
            {
                StandardScopes.OpenId,
                StandardScopes.Email,
                new Scope
                {
                    Name = "roles",
                    Claims = new List<ScopeClaim>
                    {
                        new ScopeClaim("role")
                    }
                }
            };

            var factory = new IdentityServerServiceFactory();
            factory.UseInMemoryClients(clients);
            factory.UseInMemoryScopes( scopes );
            factory.UseInMemoryUsers( users );

            return factory;
        }
    }
}
