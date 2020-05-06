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

        /// <summary>
        /// Called to validate that the origin of the request is a registered "client_id", and that the correct credentials for that
        /// client are present on the request. If the web application accepts Basic authentication credentials,
        /// context.TryGetBasicCredentials(out clientId, out clientSecret) may be called to acquire those values if present in the
        /// request header. If the web application accepts "client_id" and "client_secret" as form encoded POST parameters,
        /// context.TryGetFormCredentials(out clientId, out clientSecret) may be called to acquire those values if present in the
        /// request body. If context.Validated is not called the request will not proceed further.
        /// </summary>
        /// <param name="context">The context of the event carries information in and results out.</param>
        /// <returns>
        /// Task to enable asynchronous execution
        /// </returns>
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

        /// <summary>
        /// Called for each request to the Authorize endpoint to determine if the request is valid and should continue.
        /// The default behavior when using the OAuthAuthorizationServerProvider is to assume well-formed requests, with
        /// validated client credentials, should continue processing. An application may add any additional constraints.
        /// </summary>
        /// <param name="context">The context of the event carries information in and results out.</param>
        /// <returns>
        /// Task to enable asynchronous execution
        /// </returns>
        public override Task ValidateTokenRequest( OAuthValidateTokenRequestContext context )
        {
            context.Validated();
            return base.ValidateTokenRequest( context );
        }

        /// <summary>
        /// Called for each request to the Authorize endpoint to determine if the request is valid and should continue.
        /// The default behavior when using the OAuthAuthorizationServerProvider is to assume well-formed requests, with
        /// validated client redirect URI, should continue processing. An application may add any additional constraints.
        /// </summary>
        /// <param name="context">The context of the event carries information in and results out.</param>
        /// <returns>
        /// Task to enable asynchronous execution
        /// </returns>
        public override Task ValidateAuthorizeRequest( OAuthValidateAuthorizeRequestContext context )
        {
            context.Validated();
            return base.ValidateAuthorizeRequest( context );
        }

        /// <summary>
        /// Called at the final stage of an incoming Authorize endpoint request before the execution continues on to the web application
        /// component responsible for producing the html response. Anything present in the OWIN pipeline following the Authorization Server
        /// may produce the response for the Authorize page. If running on IIS any ASP.NET technology running on the server may produce
        /// the response for the Authorize page. If the web application wishes to produce the response directly in the AuthorizeEndpoint
        /// call it may write to the context.Response directly and should call context.RequestCompleted to stop other handlers from
        /// executing. If the web application wishes to grant the authorization directly in the AuthorizeEndpoint call it cay call
        /// context.OwinContext.Authentication.SignIn with the appropriate ClaimsIdentity and should call context.RequestCompleted to
        /// stop other handlers from executing.
        /// </summary>
        /// <param name="context">The context of the event carries information in and results out.</param>
        /// <returns>
        /// Task to enable asynchronous execution
        /// </returns>
        public override Task AuthorizeEndpoint( OAuthAuthorizeEndpointContext context )
        {
            return base.AuthorizeEndpoint( context );
        }
    }
}
