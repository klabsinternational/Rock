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
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Linq.Dynamic;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using AspNet.Security.OpenIdConnect.Extensions;
using AspNet.Security.OpenIdConnect.Primitives;
using AspNet.Security.OpenIdConnect.Server;
using DocumentFormat.OpenXml.Office2010.ExcelAc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http.Authentication;
using NuGet;
using OpenXmlPowerTools;
using Rock;
using Rock.Auth;
using Rock.Data;
using Rock.Model;
using Rock.Web.UI;

namespace RockWeb.Blocks.Auth
{
    /// <summary>
    /// Prompts user for login credentials.
    /// </summary>
    [DisplayName( "Authorize" )]
    [Category( "Auth" )]
    [Description( "Choose to authorize the auth client to access the user's data." )]

    public partial class Authorize : RockBlock
    {
        #region Keys

        /// <summary>
        /// Page Param Keys
        /// </summary>
        private static class PageParamKey
        {
            /// <summary>
            /// The client identifier
            /// </summary>
            public const string ClientId = "client_id";

            /// <summary>
            /// The scope
            /// </summary>
            public const string Scope = "scope";
        }

        #endregion Keys

        #region Base Control Methods

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            if ( !Page.IsPostBack )
            {
                Task.Run(async () => {
                    if ( IsValidAuthorizationRequest() )
                    {
                        pnlPanel.Visible = true;
                        await BindClientName();
                        BindScopes();
                    }
                    else
                    {
                        pnlPanel.Visible = false;
                        BindValidationError();
                    }
                } ).Wait();
            }
        }

        #endregion Base Control Methods

        #region Methods

        /// <summary>
        /// Determines whether [is valid authorization request].
        /// </summary>
        /// <returns>
        ///   <c>true</c> if [is valid authorization request]; otherwise, <c>false</c>.
        /// </returns>
        private bool IsValidAuthorizationRequest()
        {
            return Response == null || Response.StatusCode / 100 == 2;
        }

        /// <summary>
        /// Gets the authorization validation error.
        /// </summary>
        /// <returns></returns>
        private string GetAuthorizationValidationError()
        {
            if ( IsValidAuthorizationRequest() )
            {
                return string.Empty;
            }

            // TODO figure out how to get the error set within Rock.Auth.AuthorizationProvider
            return "There is a problem with this authorization request and you cannot continue.";
        }

        #endregion Methods

        #region UI Bindings

        /// <summary>
        /// Binds the validation error.
        /// </summary>
        private void BindValidationError()
        {
            var error = GetAuthorizationValidationError();

            if ( error.IsNullOrWhiteSpace() )
            {
                nbNotificationBox.Visible = false;
                return;
            }

            nbNotificationBox.Visible = true;
            nbNotificationBox.Text = error;
        }

        /// <summary>
        /// Binds the name of the client.
        /// </summary>
        private async Task BindClientName()
        {
            var authClient = await GetAuthClient();

            if ( authClient != null )
            {
                lClientName.Text = authClient.Name;
            }
        }

        /// <summary>
        /// Binds the scopes.
        /// </summary>
        private void BindScopes()
        {
            var scopes = GetRequestedScopes();
            var scopeViewModels = scopes.Select( s => new ScopeViewModel {
                Name = s
            }  );

            rScopes.DataSource = scopeViewModels;
            rScopes.DataBind();
        }

        #endregion UI Bindings

        #region Data Access

        /// <summary>
        /// Gets the requested scopes.
        /// </summary>
        /// <returns></returns>
        private List<string> GetRequestedScopes()
        {
            var scopeString = PageParameter( PageParamKey.Scope ) ?? string.Empty;
            return scopeString.SplitDelimitedValues().ToList();
        }

        /// <summary>
        /// Gets the authentication client.
        /// </summary>
        /// <returns></returns>
        private async Task<AuthClient> GetAuthClient()
        {
            if ( _authClient == null )
            {
                var rockContext = new RockContext();
                var authClientService = new AuthClientService( rockContext );
                var authClientId = PageParameter( PageParamKey.ClientId );
                _authClient = await authClientService.GetByClientId( authClientId );
            }

            return _authClient;
        }
        private AuthClient _authClient = null;

        #endregion Data Access

        #region Events

        /// <summary>
        /// Handles the Click event of the btnAllow control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnAllow_Click( object sender, EventArgs e )
        {
            // Create a new ClaimsIdentity containing the claims that
            // will be used to create an id_token, a token or a code.
            var identity = new ClaimsIdentity(
                OpenIdConnectServerDefaults.AuthenticationScheme,
                OpenIdConnectConstants.Claims.Name,
                OpenIdConnectConstants.Claims.Role );

            // Note: the "sub" claim is mandatory and an exception is thrown if this claim is missing.
            var subjectClaim = new Claim( OpenIdConnectConstants.Claims.Subject, CurrentPerson.Guid.ToString() );
            subjectClaim.SetDestinations(
                OpenIdConnectConstants.Destinations.AccessToken,
                OpenIdConnectConstants.Destinations.IdentityToken );

            var nameClaim = new Claim( OpenIdConnectConstants.Claims.Name, CurrentPerson.FullName );
            subjectClaim.SetDestinations(
                OpenIdConnectConstants.Destinations.AccessToken,
                OpenIdConnectConstants.Destinations.IdentityToken );

            identity.AddClaim( subjectClaim );
            identity.AddClaim( nameClaim );

            // Create a new authentication ticket holding the user identity.
            var ticket = new AuthenticationTicket(
                new ClaimsPrincipal( identity ),
                new AuthenticationProperties(),
                OpenIdConnectServerDefaults.AuthenticationScheme );

            // Set the list of scopes granted to the client application.
            // Note: this sample always grants the "openid", "email" and "profile" scopes
            // when they are requested by the client application: a real world application
            // would probably display a form allowing to select the scopes to grant.
            ticket.SetScopes( new[]
            {
                /* openid: */ OpenIdConnectConstants.Scopes.OpenId,
                /* email: */ OpenIdConnectConstants.Scopes.Email,
                /* profile: */ OpenIdConnectConstants.Scopes.Profile,
                /* offline_access: */ OpenIdConnectConstants.Scopes.OfflineAccess
            }.Intersect( GetRequestedScopes() ) );

            // Set the resources servers the access token should be issued for.
            ticket.SetResources( "resource_server" );

            // Returning a SignInResult will ask ASOS to serialize the specified identity to build appropriate tokens.
            return SignIn( ticket.Principal, ticket.Properties, ticket.AuthenticationScheme );
        }

        protected void btnDeny_Click( object sender, EventArgs e )
        {
            // Notify ASOS that the authorization grant has been denied by the resource owner.
            // Note: OpenIdConnectServerHandler will automatically take care of redirecting
            // the user agent to the client application using the appropriate response_mode.
            return Challenge( OpenIdConnectServerDefaults.AuthenticationScheme );
        }

        #endregion Events

        #region View Models

        /// <summary>
        /// Scope View Model
        /// </summary>
        private class ScopeViewModel
        {
            /// <summary>
            /// Gets or sets the name.
            /// </summary>
            /// <value>
            /// The name.
            /// </value>
            public string Name { get; set; }
        }

        #endregion View Models
    }
}
