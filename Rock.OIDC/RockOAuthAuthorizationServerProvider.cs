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
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.OAuth;
using Rock.Data;
using Rock.Model;

namespace Rock.OIDC
{
    internal class RockOAuthAuthorizationServerProvider : OAuthAuthorizationServerProvider
    {
        /// <summary>
        /// Check the resource owner's credentials (ie Ted Decker)
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public override Task GrantResourceOwnerCredentials( OAuthGrantResourceOwnerCredentialsContext context )
        {
            var rockContext = new RockContext();
            var userLoginService = new UserLoginService( rockContext );
            var user = userLoginService.GetByUserNameAndPassword( context.UserName, context.Password );

            if ( user != null )
            {
                var claims = new List<Claim>()
                {
                    new Claim( ClaimTypes.Name, context.UserName )
                };

                var claimsIdentity = new ClaimsIdentity( claims, OAuthDefaults.AuthenticationType );
                var ticket = new AuthenticationTicket( claimsIdentity, new AuthenticationProperties() );
                context.Validated( ticket );
            }

            return base.GrantResourceOwnerCredentials( context );
        }

        /// <summary>`
        /// Check the client credentials (ie Church Online)
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public override Task ValidateClientAuthentication( OAuthValidateClientAuthenticationContext context )
        {
            if ( context.TryGetBasicCredentials( out var clientId, out var clientSecret ) )
            {
                var rockContext = new RockContext();
                var authClientService = new AuthClientService( rockContext );
                var authClient = authClientService.GetByClientIdAndSecret( clientId, clientSecret );

                if ( authClient != null )
                {
                    context.Validated();
                }
            }

            return base.ValidateClientAuthentication( context );
        }

        /// <summary>
        /// Called to validate that the context.ClientId is a registered "client_id", and that the context.RedirectUri a "redirect_uri"
        /// registered for that client. This only occurs when processing the Authorize endpoint. The application MUST implement this
        /// call, and it MUST validate both of those factors before calling context.Validated. If the context.Validated method is called
        /// with a given redirectUri parameter, then IsValidated will only become true if the incoming redirect URI matches the given redirect URI.
        /// If context.Validated is not called the request will not proceed further.
        /// </summary>
        /// <param name="context">The context of the event carries information in and results out.</param>
        /// <returns>
        /// Task to enable asynchronous execution
        /// </returns>
        public override Task ValidateClientRedirectUri( OAuthValidateClientRedirectUriContext context )
        {
            var rockContext = new RockContext();
            var authClientService = new AuthClientService( rockContext );
            var authClient = authClientService.GetByClientId( context.ClientId );

            if ( authClient != null && authClient.RedirectUrl.Equals( context.RedirectUri, System.StringComparison.OrdinalIgnoreCase ) )
            {
                context.Validated();
            }

            return base.ValidateClientRedirectUri( context );
        }
    }
}
