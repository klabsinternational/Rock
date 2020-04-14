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
    public partial class InteractionChannel : Rock.Migrations.RockMigration
    {
        /// <summary>
        /// Operations to be performed during the upgrade process.
        /// </summary>
        public override void Up()
        {
            AddColumn("dbo.Interaction", "InteractionChannelId", c => c.Int(nullable: false));
            Sql(
@"UPDATE i 
SET i.[InteractionChannelId] = ic.[InteractionChannelId]
FROM
    [Interaction] i
    JOIN [InteractionComponent] ic ON ic.[Id] = i.[InteractionComponentId];" );

            CreateIndex("dbo.Interaction", "InteractionChannelId");
            AddForeignKey("dbo.Interaction", "InteractionChannelId", "dbo.InteractionChannel", "Id");
        }
        
        /// <summary>
        /// Operations to be performed during the downgrade process.
        /// </summary>
        public override void Down()
        {
            DropForeignKey("dbo.Interaction", "InteractionChannelId", "dbo.InteractionChannel");
            DropIndex("dbo.Interaction", new[] { "InteractionChannelId" });
            DropColumn("dbo.Interaction", "InteractionChannelId");
        }
    }
}
