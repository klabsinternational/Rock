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
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Configuration;
using System.Web;

using Rock.Attribute;
using Rock.Model;
using Rock.Security;

namespace Rock.OIDC.Security.Authentication
{
    /// <summary>
    /// Authenticates a client id and secret using the Rock database
    /// </summary>
    [Description( "Oauth Client Authentication Provider" )]
    [Export( typeof( AuthenticationComponent ) )]
    [ExportMetadata( "ComponentName", "OauthClient" )]

    [IntegerField(
        name: "BCrypt Cost Factor",
        description: "The higher this number, the more secure BCrypt can be. However it also will be slower.",
        required: false,
        defaultValue: 11 )]

    public class OauthClient : AuthenticationComponent
    {
        private static byte[] _encryptionKey;

        /// <summary>
        /// Gets the type of the service.
        /// </summary>
        /// <value>
        /// The type of the service.
        /// </value>
        public override AuthenticationServiceType ServiceType => AuthenticationServiceType.Internal;

        /// <summary>
        /// Determines if user is directed to another site (i.e. Facebook, Gmail, Twitter, etc) to confirm approval of using
        /// that site's credentials for authentication.
        /// </summary>
        /// <value>
        /// The requires remote authentication.
        /// </value>
        public override bool RequiresRemoteAuthentication => false;

        /// <summary>
        /// Initializes the <see cref="OauthClient" /> class.
        /// </summary>
        /// <exception cref="System.Configuration.ConfigurationErrorsException">Authentication requires a 'PasswordKey' app setting</exception>
        static OauthClient()
        {
            var passwordKey = System.Configuration.ConfigurationManager.AppSettings["PasswordKey"];

            if ( passwordKey.IsNullOrWhiteSpace() )
            {
                throw new ConfigurationErrorsException( "Authentication requires a 'PasswordKey' app setting" );
            }

            _encryptionKey = Encryption.HexToByte( passwordKey );
        }

        /// <summary>
        /// Authenticates the specified user name.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="clientSecret">The secret.</param>
        /// <returns></returns>
        public override bool Authenticate( UserLogin user, string clientSecret )
        {
            try
            {
                return AuthenticateBcrypt( user, clientSecret );
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Encodes the password.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="password"></param>
        /// <returns></returns>
        public override string EncodePassword( UserLogin user, string password )
        {
            return EncodeBcrypt( password );
        }

        /// <summary>
        /// Authenticates the user based on a request from a third-party provider.  Will set the username and returnUrl values.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="userName">Name of the user.</param>
        /// <param name="returnUrl">The return URL.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public override bool Authenticate( HttpRequest request, out string userName, out string returnUrl )
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Generates the login URL.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public override Uri GenerateLoginUrl( HttpRequest request )
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Tests the Http Request to determine if authentication should be tested by this
        /// authentication provider.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public override bool IsReturningFromAuthentication( HttpRequest request )
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets the URL of an image that should be displayed.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public override string ImageUrl()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets a value indicating whether [supports change password].
        /// </summary>
        /// <value>
        /// <c>true</c> if [supports change password]; otherwise, <c>false</c>.
        /// </value>
        public override bool SupportsChangePassword => false;

        /// <summary>
        /// Changes the password.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="oldPassword">The old password.</param>
        /// <param name="newPassword">The new password.</param>
        /// <param name="warningMessage">The warning message.</param>
        /// <returns>
        /// A <see cref="System.Boolean" /> value that indicates if the password change was successful. <c>true</c> if successful; otherwise <c>false</c>.
        /// </returns>
        /// <exception cref="System.Exception">Cannot change password on external service type</exception>
        public override bool ChangePassword( UserLogin user, string oldPassword, string newPassword, out string warningMessage )
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Sets the password.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="password">The password.</param>
        public override void SetPassword( UserLogin user, string password )
        {
            user.Password = EncodePassword( user, password );
            user.LastPasswordChangedDateTime = RockDateTime.Now;
            user.IsPasswordChangeRequired = false;
        }

        /// <summary>
        /// Authenticates the bcrypt.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="password">The password.</param>
        /// <returns></returns>
        private bool AuthenticateBcrypt( UserLogin user, string password )
        {
            try
            {
                return BCrypt.Net.BCrypt.Verify( password, user.Password );
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Encodes the bcrypt.
        /// </summary>
        /// <param name="password">The password.</param>
        /// <returns></returns>
        private string EncodeBcrypt( string password )
        {
            var workFactor = GetBcryptCostFactor();
            var salt = BCrypt.Net.BCrypt.GenerateSalt( workFactor );
            return BCrypt.Net.BCrypt.HashPassword( password, salt );
        }

        /// <summary>
        /// Gets the bcrypt cost factor.
        /// </summary>
        /// <returns></returns>
        private int GetBcryptCostFactor()
        {
            return GetAttributeValue( "BCryptCostFactor" ).AsIntegerOrNull() ?? 11;
        }
    }
}