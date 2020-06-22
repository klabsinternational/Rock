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
    /// <summary>
    ///
    /// </summary>
    public partial class AttributeIndexes : RockMigration
    {
        /// <summary>
        /// Operations to be performed during the upgrade process.
        /// </summary>
        public override void Up()
        {
            // This index is redundant because of IX_EntityTypeId_EntityTypeQualifierColumn_EntityTypeQualifierValue_Key
            RockMigrationHelper.DropIndexIfExists( "Attribute", "IX_EntityTypeId" );

            // This index is redundant because of IX_AttributeId_EntityId
            RockMigrationHelper.DropIndexIfExists( "AttributeValue", "IX_AttributeId" );

            // ValueAs* is always used with attribute id
            RockMigrationHelper.DropIndexIfExists( "AttributeValue", "IX_ValueAsBoolean" );
            RockMigrationHelper.DropIndexIfExists( "AttributeValue", "IX_ValueAsDateTime" );
            RockMigrationHelper.DropIndexIfExists( "AttributeValue", "IX_ValueAsNumeric" );

            // Include ValueAs* and fix naming of this index
            RockMigrationHelper.DropIndexIfExists( "AttributeValue", "IX_AttributeIdEntityId" );
            RockMigrationHelper.CreateIndexIfNotExists( "AttributeValue", 
                new[] { "AttributeId", "EntityId" },
                new[] { "ValueAsNumeric", "ValueAsDateTime", "ValueAsBoolean" } );

            // This index does not follow naming convention
            RockMigrationHelper.RenameIndexIfExists( "AttributeValue", "EntityAttribute", "IX_EntityId_AttributeId" );
        }

        /// <summary>
        /// Operations to be performed during the downgrade process.
        /// </summary>
        public override void Down()
        {
            // There is no need to remove the new index or re-add unnecessary indexes
        }
    }
}
