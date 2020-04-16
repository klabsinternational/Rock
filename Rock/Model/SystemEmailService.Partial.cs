﻿// <copyright>
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
using System.Linq;

using Rock.Data;

namespace Rock.Model
{
    /// <summary>
    /// SystemEmail Service class
    /// </summary>
    [Obsolete( "Use SystemCommunicationService instead." )]
    [RockObsolete( "1.10" )]
    public partial class SystemEmailService : Service<SystemEmail>
    {
        
    }

    /// <summary>
    /// Generated Extension Methods
    /// </summary>
    [Obsolete( "Use SystemCommunicationService instead." )]
    [RockObsolete( "1.10" )]
    public static partial class SystemEmailExtensionMethods
    {
        
    }
}