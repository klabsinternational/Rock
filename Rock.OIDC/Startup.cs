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
using Microsoft.Owin;
using Microsoft.Owin.Security.Infrastructure;
using Microsoft.Owin.Security.OAuth;
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
            var oAuthOptions = new OAuthAuthorizationServerOptions
            {
                AuthorizeEndpointPath = new PathString( Paths.AuthorizePath ),
                TokenEndpointPath = new PathString( Paths.TokenPath ),
                ApplicationCanDisplayErrors = true,
                AllowInsecureHttp = true,

                // Authorization server provider which controls the lifecycle of Authorization Server
                Provider = new RockOAuthAuthorizationServerProvider(),

                // Authorization code provider which creates and receives authorization code
                AuthorizationCodeProvider = new AuthenticationTokenProvider
                {
                    OnCreate = CreateAuthenticationCode,
                    OnReceive = ReceiveAuthenticationCode,
                },

                // Refresh token provider which creates and receives referesh token
                RefreshTokenProvider = new AuthenticationTokenProvider
                {
                    OnCreate = CreateRefreshToken,
                    OnReceive = ReceiveRefreshToken,
                }
            };

            // Setup Authorization Server
            app.UseOAuthAuthorizationServer( oAuthOptions );
            app.UseOAuthBearerTokens( oAuthOptions );
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
