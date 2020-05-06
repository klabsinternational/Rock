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

using System;
using Microsoft.Extensions.Logging;
using Microsoft.Owin;
using Microsoft.Owin.Security.Infrastructure;
using Owin;
using Rock.Data;
using Rock.Model;
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
            app.UseOpenIdConnectServer(options =>
            {
                options.Provider = new AuthorizationProvider();

                // Enable the authorization, logout, token and userinfo endpoints.
                options.AuthorizationEndpointPath = new PathString(Paths.AuthorizePath);
                options.LogoutEndpointPath = new PathString(Paths.LogoutPath);
                options.TokenEndpointPath = new PathString(Paths.TokenPath);
                options.UserinfoEndpointPath = new PathString(Paths.UserInfo);

                // Note: see AuthorizationModule.cs for more
                // information concerning ApplicationCanDisplayErrors.
                options.ApplicationCanDisplayErrors = true;
                options.AllowInsecureHttp = true;

                // Register a new ephemeral key, that is discarded when the application
                // shuts down. Tokens signed using this key are automatically invalidated.
                // This method should only be used during development.
                options.SigningCredentials.AddEphemeralKey();

                // Note: to override the default access token format and use JWT, assign AccessTokenHandler:
                //
                // options.AccessTokenHandler = new JwtSecurityTokenHandler
                // {
                //     InboundClaimTypeMap = new Dictionary<string, string>(),
                //     OutboundClaimTypeMap = new Dictionary<string, string>()
                // };
                //
                // Note: when using JWT as the access token format, you have to register a signing key.
                //
                // You can register a new ephemeral key, that is discarded when the application shuts down.
                // Tokens signed using this key are automatically invalidated and thus this method
                // should only be used during development:
                //
                // options.SigningCredentials.AddEphemeralKey();
                //
                // On production, using a X.509 certificate stored in the machine store is recommended.
                // You can generate a self-signed certificate using Pluralsight's self-cert utility:
                // https://s3.amazonaws.com/pluralsight-free/keith-brown/samples/SelfCert.zip
                //
                // options.SigningCredentials.AddCertificate("7D2A741FE34CC2C7369237A5F2078988E17A6A75");
                //
                // Alternatively, you can also store the certificate as an embedded .pfx resource
                // directly in this assembly or in a file published alongside this project:
                //
                // options.SigningCredentials.AddCertificate(
                //     assembly: typeof(Startup).GetTypeInfo().Assembly,
                //     resource: "Nancy.Server.Certificate.pfx",
                //     password: "Owin.Security.OpenIdConnect.Server");

                // Register the logging listeners used by the OpenID Connect server middleware.
                options.UseLogging(logger => logger.AddConsole().AddDebug());
            });
        }

        /// <summary>
        /// Creates the authentication code.
        /// </summary>
        /// <param name="context">The context.</param>
        private void CreateAuthenticationCode( AuthenticationTokenCreateContext context )
        {
            var rockContext = new RockContext();
            var authenticationTicketService = new AuthTicketService( rockContext);

            context.SetToken( Guid.NewGuid().ToString() );

            authenticationTicketService.Add( new AuthTicket
            {
                Token = new Guid( context.Token ),
                SerializedTicket = context.SerializeTicket()
            } );

            rockContext.SaveChanges();
        }

        /// <summary>
        /// Receives the authentication code.
        /// </summary>
        /// <param name="context">The context.</param>
        private void ReceiveAuthenticationCode( AuthenticationTokenReceiveContext context )
        {
            var rockContext = new RockContext();
            var authenticationTicketService = new AuthTicketService( rockContext );

            var authenticationTicket = authenticationTicketService.Get( new Guid( context.Token ) );
            context.DeserializeTicket( authenticationTicket.SerializedTicket );
        }

        /// <summary>
        /// Creates the refresh token.
        /// </summary>
        /// <param name="context">The context.</param>
        private void CreateRefreshToken( AuthenticationTokenCreateContext context )
        {
            context.SetToken( context.SerializeTicket() );
        }

        /// <summary>
        /// Receives the refresh token.
        /// </summary>
        /// <param name="context">The context.</param>
        private void ReceiveRefreshToken( AuthenticationTokenReceiveContext context )
        {
            context.DeserializeTicket( context.Token );
        }
    }
}
