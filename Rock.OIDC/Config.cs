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
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.OAuth;

namespace Rock.OIDC
{
    /// <summary>
    /// Configuration for the OAuth Implementation
    /// </summary>
    internal static class Config
    {
        /// <summary>
        /// Gets the o authentication options.
        /// </summary>
        /// <value>
        /// The o authentication options.
        /// </value>
        internal static OAuthAuthorizationServerOptions OAuthOptions =>
            new OAuthAuthorizationServerOptions
            {
                TokenEndpointPath = new PathString( "/Token" ),
                Provider = new OAuthAppProvider(),
                AccessTokenExpireTimeSpan = TimeSpan.FromMinutes( 60 ),
                AllowInsecureHttp = true,
                AuthenticationMode = AuthenticationMode.Active
            };
    }
}
