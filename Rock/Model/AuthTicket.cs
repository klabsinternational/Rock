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
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;
using Rock.Data;

namespace Rock.Model
{
    /// <summary>
    /// Represents an Authentication Ticket
    /// </summary>

    [RockDomain( "OIDC" )]
    [Table( "AuthTicket" )]
    [DataContract]
    public class AuthTicket : Model<AuthTicket>
    {
        /// <summary>
        /// Gets or sets the token.
        /// </summary>
        /// <value>
        /// The token.
        /// </value>
        [DataMember( IsRequired = true )]
        [Required]
        public Guid Token { get; set; }

        /// <summary>
        /// Gets or sets the serialized ticket.
        /// </summary>
        /// <value>
        /// The serialized ticket.
        /// </value>
        [DataMember( IsRequired = true )]
        [Required]
        public string SerializedTicket { get; set; }
    }
}
