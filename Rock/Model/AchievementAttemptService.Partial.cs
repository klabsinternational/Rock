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
using System.Data.Entity;
using System.Linq;
using Rock.Data;
using Rock.Web.Cache;

namespace Rock.Model
{
    /// <summary>
    /// Service/Data access class for <see cref="AchievementAttempt"/> entity objects.
    /// </summary>
    public partial class AchievementAttemptService
    {
        /// <summary>
        /// Gets attempts by streak type identifier.
        /// </summary>
        /// <param name="streakTypeId">The streak type identifier.</param>
        /// <returns></returns>
        public IQueryable<AchievementAttempt> GetByStreakTypeId( int streakTypeId )
        {
            var streakTypeEntityId = EntityTypeCache.Get<StreakType>().Id;

            return Queryable().Where( aa =>
                aa.AchievementType.SourceEntityId == streakTypeId &&
                aa.AchievementType.SourceEntityTypeId == streakTypeEntityId );
        }

        /// <summary>
        /// Gets attempts by streak identifier.
        /// </summary>
        /// <param name="streakId">The streak identifier.</param>
        /// <returns></returns>
        public IQueryable<AchievementAttempt> GetByStreakId( int streakId )
        {
            var rockContext = Context as RockContext;
            var streakService = new StreakService( rockContext );
            var streakTypeEntityId = EntityTypeCache.Get<StreakType>().Id;

            var streakTypeIdQuery = streakService.Queryable()
                .AsNoTracking()
                .Where( s => s.Id == streakId )
                .Select( s => s.StreakTypeId );

            return Queryable().Where( aa =>
                streakTypeIdQuery.Contains( aa.AchievementType.SourceEntityId ) &&
                aa.AchievementType.SourceEntityTypeId == streakTypeEntityId &&
                aa.AchieverEntityId == streakId );
        }
    }
}