﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by the Rock.CodeGeneration project
//     Changes to this file will be lost when the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------
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
using System.Linq;

using Rock.Data;

namespace Rock.Model
{
    /// <summary>
    /// AchievementType Service class
    /// </summary>
    public partial class AchievementTypeService : Service<AchievementType>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AchievementTypeService"/> class
        /// </summary>
        /// <param name="context">The context.</param>
        public AchievementTypeService(RockContext context) : base(context)
        {
        }

        /// <summary>
        /// Determines whether this instance can delete the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <returns>
        ///   <c>true</c> if this instance can delete the specified item; otherwise, <c>false</c>.
        /// </returns>
        public bool CanDelete( AchievementType item, out string errorMessage )
        {
            errorMessage = string.Empty;
 
            if ( new Service<AchievementTypePrerequisite>( Context ).Queryable().Any( a => a.PrerequisiteAchievementTypeId == item.Id ) )
            {
                errorMessage = string.Format( "This {0} is assigned to a {1}.", AchievementType.FriendlyTypeName, AchievementTypePrerequisite.FriendlyTypeName );
                return false;
            }  
            return true;
        }
    }

    /// <summary>
    /// Generated Extension Methods
    /// </summary>
    public static partial class AchievementTypeExtensionMethods
    {
        /// <summary>
        /// Clones this AchievementType object to a new AchievementType object
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="deepCopy">if set to <c>true</c> a deep copy is made. If false, only the basic entity properties are copied.</param>
        /// <returns></returns>
        public static AchievementType Clone( this AchievementType source, bool deepCopy )
        {
            if (deepCopy)
            {
                return source.Clone() as AchievementType;
            }
            else
            {
                var target = new AchievementType();
                target.CopyPropertiesFrom( source );
                return target;
            }
        }

        /// <summary>
        /// Copies the properties from another AchievementType object to this AchievementType object
        /// </summary>
        /// <param name="target">The target.</param>
        /// <param name="source">The source.</param>
        public static void CopyPropertiesFrom( this AchievementType target, AchievementType source )
        {
            target.Id = source.Id;
            target.ComponentEntityTypeId = source.ComponentEntityTypeId;
            target.AchievementFailureWorkflowTypeId = source.AchievementFailureWorkflowTypeId;
            target.AchievementIconCssClass = source.AchievementIconCssClass;
            target.AchievementStartWorkflowTypeId = source.AchievementStartWorkflowTypeId;
            target.AchievementStepStatusId = source.AchievementStepStatusId;
            target.AchievementStepTypeId = source.AchievementStepTypeId;
            target.AchievementSuccessWorkflowTypeId = source.AchievementSuccessWorkflowTypeId;
            target.AllowOverAchievement = source.AllowOverAchievement;
            target.BadgeLavaTemplate = source.BadgeLavaTemplate;
            target.CategoryId = source.CategoryId;
            target.Description = source.Description;
            target.ForeignGuid = source.ForeignGuid;
            target.ForeignKey = source.ForeignKey;
            target.IsActive = source.IsActive;
            target.MaxAccomplishmentsAllowed = source.MaxAccomplishmentsAllowed;
            target.Name = source.Name;
            target.ResultsLavaTemplate = source.ResultsLavaTemplate;
            target.SourceEntityQualifierValue = source.SourceEntityQualifierValue;
            target.CreatedDateTime = source.CreatedDateTime;
            target.ModifiedDateTime = source.ModifiedDateTime;
            target.CreatedByPersonAliasId = source.CreatedByPersonAliasId;
            target.ModifiedByPersonAliasId = source.ModifiedByPersonAliasId;
            target.Guid = source.Guid;
            target.ForeignId = source.ForeignId;

        }
    }
}
