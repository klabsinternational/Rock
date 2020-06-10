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
namespace Rock.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    /// <summary>
    ///
    /// </summary>
    public partial class InteractionOptimization : Rock.Migrations.RockMigration
    {
        /// <summary>
        /// Operations to be performed during the upgrade process.
        /// </summary>
        public override void Up()
        {
            Sql( @"UPDATE [Interaction] SET [Operation] = '' WHERE [Operation] IS NULL;" );

            Sql( @"ALTER TABLE Interaction ALTER COLUMN Operation nvarchar(25) NOT NULL;" );

            Sql(
                @"CREATE NONCLUSTERED INDEX [IX_InteractionComponentIdInteractionDateTimePersonAliasId]
                ON [dbo].[Interaction] ([InteractionComponentId],[InteractionDateTime],[PersonAliasId])" );
        }

        /// <summary>
        /// Operations to be performed during the downgrade process.
        /// </summary>
        public override void Down()
        {
            Sql( "DROP INDEX [Interaction].[IX_InteractionComponentIdInteractionDateTimePersonAliasId];" );
            Sql( "ALTER TABLE [Interaction] ALTER COLUMN [Operation] NVARCHAR( 25 ) NULL" );
        }
    }
}
