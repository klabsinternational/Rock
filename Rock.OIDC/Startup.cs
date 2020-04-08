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
            var options = Config.OAuthOptions;
            app.UseOAuthBearerTokens( options );
        }
    }
}
