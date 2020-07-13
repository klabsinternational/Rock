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
namespace Rock.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    /// <summary>
    ///
    /// </summary>
    public partial class EntityAchievements : Rock.Migrations.RockMigration
    {
        /// <summary>
        /// Operations to be performed during the upgrade process.
        /// </summary>
        public override void Up()
        {
            DropForeignKey( "dbo.StreakTypeAchievementType", "StreakTypeId", "dbo.StreakType" );
            DropIndex( "dbo.StreakTypeAchievementType", new[] { "StreakTypeId" } );
            RenameTable( name: "dbo.StreakTypeAchievementType", newName: "AchievementType" );
            RenameColumn( table: "dbo.StreakTypeAchievementTypePrerequisite", name: "PrerequisiteStreakTypeAchievementTypeId", newName: "PrerequisiteAchievementTypeId" );
            RenameColumn( table: "dbo.StreakTypeAchievementTypePrerequisite", name: "StreakTypeAchievementTypeId", newName: "AchievementTypeId" );
            RenameColumn( table: "dbo.StreakAchievementAttempt", name: "StreakTypeAchievementTypeId", newName: "AchievementTypeId" );
            RenameColumn( table: "dbo.AchievementType", name: "StreakTypeId", newName: "SourceEntityQualifierValue" );
            AlterColumn( table: "dbo.AchievementType", name: "SourceEntityQualifierValue", c => c.String() );
            RenameIndex( table: "dbo.StreakAchievementAttempt", name: "IX_StreakTypeAchievementTypeId", newName: "IX_AchievementTypeId" );
            RenameIndex( table: "dbo.StreakTypeAchievementTypePrerequisite", name: "IX_StreakTypeAchievementTypeId", newName: "IX_AchievementTypeId" );
            RenameIndex( table: "dbo.StreakTypeAchievementTypePrerequisite", name: "IX_PrerequisiteStreakTypeAchievementTypeId", newName: "IX_PrerequisiteAchievementTypeId" );
            AddColumn( "dbo.EntityType", "IsAchievementsEnabled", c => c.Boolean( nullable: false ) );

            DropForeignKey( "dbo.StreakAchievementAttempt", "StreakId", "dbo.Streak" );
            RenameTable( name: "dbo.StreakAchievementAttempt", newName: "AchievementAttempt" );
            RenameTable( name: "dbo.StreakTypeAchievementTypePrerequisite", newName: "AchievementTypePrerequisite" );
            DropIndex( "dbo.AchievementAttempt", new[] { "StreakId" } );
            RenameColumn( table: "dbo.AchievementType", name: "AchievementEntityTypeId", newName: "ComponentEntityTypeId" );
            RenameIndex( table: "dbo.AchievementType", name: "IX_AchievementEntityTypeId", newName: "IX_ComponentEntityTypeId" );
            AddColumn( "dbo.AchievementType", "SourceEntityTypeId", c => c.Int() );
            AddColumn( "dbo.AchievementType", "AchieverEntityTypeId", c => c.Int( nullable: false ) );
            AddColumn( "dbo.AchievementType", "SourceEntityQualifierColumn", c => c.String( maxLength: 50 ) );
            AddColumn( "dbo.AchievementAttempt", "AchieverEntityId", c => c.Int( nullable: false ) );
            AlterColumn( "dbo.AchievementType", "SourceEntityQualifierValue", c => c.String( maxLength: 200 ) );
            DropColumn( "dbo.AchievementAttempt", "StreakId" );
        }

        /// <summary>
        /// Operations to be performed during the downgrade process.
        /// </summary>
        public override void Down()
        {
            AddColumn( "dbo.AchievementAttempt", "StreakId", c => c.Int( nullable: false ) );
            AlterColumn( "dbo.AchievementType", "SourceEntityQualifierValue", c => c.String() );
            DropColumn( "dbo.AchievementAttempt", "AchieverEntityId" );
            DropColumn( "dbo.AchievementType", "SourceEntityQualifierColumn" );
            DropColumn( "dbo.AchievementType", "AchieverEntityTypeId" );
            DropColumn( "dbo.AchievementType", "SourceEntityTypeId" );
            RenameIndex( table: "dbo.AchievementType", name: "IX_ComponentEntityTypeId", newName: "IX_AchievementEntityTypeId" );
            RenameColumn( table: "dbo.AchievementType", name: "ComponentEntityTypeId", newName: "AchievementEntityTypeId" );
            CreateIndex( "dbo.AchievementAttempt", "StreakId" );
            AddForeignKey( "dbo.StreakAchievementAttempt", "StreakId", "dbo.Streak", "Id" );
            RenameTable( name: "dbo.AchievementTypePrerequisite", newName: "StreakTypeAchievementTypePrerequisite" );
            RenameTable( name: "dbo.AchievementAttempt", newName: "StreakAchievementAttempt" );
            
            AlterColumn( "dbo.AchievementType", "SourceEntityQualifierValue", c => c.Int( nullable: false ) );
            RenameColumn( "dbo.AchievementType", "SourceEntityQualifierValue", "StreakTypeId" );
            DropColumn( "dbo.EntityType", "IsAchievementsEnabled" );
            RenameIndex( table: "dbo.StreakTypeAchievementTypePrerequisite", name: "IX_PrerequisiteAchievementTypeId", newName: "IX_PrerequisiteStreakTypeAchievementTypeId" );
            RenameIndex( table: "dbo.StreakTypeAchievementTypePrerequisite", name: "IX_AchievementTypeId", newName: "IX_StreakTypeAchievementTypeId" );
            RenameIndex( table: "dbo.StreakAchievementAttempt", name: "IX_AchievementTypeId", newName: "IX_StreakTypeAchievementTypeId" );
            RenameColumn( table: "dbo.StreakAchievementAttempt", name: "AchievementTypeId", newName: "StreakTypeAchievementTypeId" );
            RenameColumn( table: "dbo.StreakTypeAchievementTypePrerequisite", name: "AchievementTypeId", newName: "StreakTypeAchievementTypeId" );
            RenameColumn( table: "dbo.StreakTypeAchievementTypePrerequisite", name: "PrerequisiteAchievementTypeId", newName: "PrerequisiteStreakTypeAchievementTypeId" );
            CreateIndex( "dbo.AchievementType", "StreakTypeId" );
            RenameTable( name: "dbo.AchievementType", newName: "StreakTypeAchievementType" );
            AddForeignKey( "dbo.StreakTypeAchievementType", "StreakTypeId", "dbo.StreakType", "Id", cascadeDelete: true );
        }
    }
}
