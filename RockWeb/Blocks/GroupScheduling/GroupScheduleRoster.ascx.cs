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
using System.ComponentModel;
using System.Data.Entity;
using System.Linq;
using System.Linq.Dynamic;
using System.Web.UI;

using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Lava;
using Rock.Model;
using Rock.Web.UI;
using Rock.Web.UI.Controls;

namespace RockWeb.Blocks.GroupScheduling
{
    /// <summary>
    ///
    /// </summary>
    [DisplayName( "Group Schedule Roster" )]
    [Category( "Group Scheduling" )]
    [Description( "Allows a person to view and print a roster by defining group schedule criteria." )]

    #region Block Attributes

    [BooleanField(
        "Enable Live Refresh",
        Key = AttributeKey.EnableLiveRefresh,
        Description = "The Email address to show.",
        IsRequired = true,
        DefaultBooleanValue = true,
        Order = 0 )]

    [RangeSlider(
        "Refresh Interval (seconds)",
        Key = AttributeKey.RefreshInterval,
        IsRequired = true,
        Description = "The number of seconds to refresh the page. Note that setting this option too low could put a strain on the server if loaded on several clients at once.",
        DefaultIntegerValue = 30,
        MinValue = 10,
        MaxValue = 600,
        Order = 1 )]

    [CodeEditorField(
        "Roster Lava Template",
        Key = AttributeKey.RosterLavaTemplate,
        DefaultValue = AttributeDefault.RosterLavaDefault,
        EditorMode = CodeEditorMode.Lava,
        EditorHeight = 200,
        Order = 2 )]

    #endregion Block Attributes
    public partial class GroupScheduleRoster : RockBlock
    {
        private static class AttributeDefault
        {
            // TODO, have lava in this block, instead of in a file
            public const string RosterLavaDefault = @"{% include '~~/Assets/Lava/GroupScheduleRoster.lava' %}";
        }

        #region Attribute Keys

        private static class AttributeKey
        {
            public const string EnableLiveRefresh = "EnableLiveRefresh";
            public const string RefreshInterval = "RefreshInterval";
            public const string RosterLavaTemplate = "RosterLavaTemplate";
        }

        #endregion Attribute Keys

        #region PageParameterKeys

        private static class PageParameterKey
        {
            public const string GroupIds = "GroupIds";
            public const string LocationIds = "LocationIds";
            public const string ScheduleIds = "ScheduleIds";
            public const string IncludeChildGroups = "IncludeChildGroups";
        }

        #endregion PageParameterKeys

        #region UserPreferenceKeys

        private static class UserPreferenceKey
        {
            public const string RosterConfigurationJSON = "RosterConfigurationJSON";
        }

        #endregion PageParameterKeys

        #region Fields

        #endregion

        #region Properties

        #endregion

        #region Base Control Methods

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            // this event gets fired after block settings are updated. it's nice to repaint the screen if these settings would alter it
            this.BlockUpdated += Block_BlockUpdated;
            this.AddConfigurationUpdateTrigger( upnlContent );
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );


            if ( !Page.IsPostBack )
            {
                PopulateRoster();
            }
        }

        #endregion

        #region Events

        /// <summary>
        /// Handles the BlockUpdated event of the control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void Block_BlockUpdated( object sender, EventArgs e )
        {
            //
        }

        private bool? _displayRole = null;

        /// <summary>
        /// Handles the ItemDataBound event of the rptAttendanceOccurrences control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Web.UI.WebControls.RepeaterItemEventArgs"/> instance containing the event data.</param>
        protected void rptAttendanceOccurrences_ItemDataBound( object sender, System.Web.UI.WebControls.RepeaterItemEventArgs e )
        {
            var attendanceOccurrence = e.Item.DataItem as AttendanceOccurrence;
            if ( attendanceOccurrence == null )
            {
                return;
            }

            var mergeFields = LavaHelper.GetCommonMergeFields( this.RockPage );
            mergeFields.Add( "Group", attendanceOccurrence.Group );
            mergeFields.Add( "Location", attendanceOccurrence.Location );
            mergeFields.Add( "Schedule", attendanceOccurrence.Schedule );
            mergeFields.Add( "ScheduleDate", attendanceOccurrence.OccurrenceDate );
            mergeFields.Add( "DisplayRole", _displayRole );

        }

        /// <summary>
        /// Handles the Click event of the btnConfiguration control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnConfiguration_Click( object sender, EventArgs e )
        {

        }

        #endregion

        #region Methods

        private void PopulateRoster()
        {
            RosterConfiguration rosterConfiguration = this.GetBlockUserPreference( UserPreferenceKey.RosterConfigurationJSON )
                .FromJsonOrNull<RosterConfiguration>();

            rosterConfiguration = rosterConfiguration ?? new RosterConfiguration();

            int[] scheduleIds = rosterConfiguration.ScheduleIds;
            int[] locationIds = rosterConfiguration.LocationIds;
            List<int> groupIds = rosterConfiguration.GroupIds.ToList();

            this._displayRole = rosterConfiguration.DisplayRole;

            var rockContext = new RockContext();
            
            if ( rosterConfiguration.IncludeChildGroups )
            {
                var groupService = new GroupService( rockContext );
                foreach ( var groupId in groupIds )
                {
                    var childGroupIds = groupService.GetAllDescendentGroupIds( groupId, false );
                    groupIds.AddRange( childGroupIds );
                }
            }

            groupIds = groupIds.Distinct().ToList();

            var attendanceOccurrenceService = new AttendanceOccurrenceService( rockContext );

            var currentDate = RockDateTime.Today;
            var maxDate = currentDate.AddDays( 6 );

            var attendanceOccurrenceQuery = attendanceOccurrenceService
                .Queryable()
                .Where( a => a.ScheduleId.HasValue && a.LocationId.HasValue && a.GroupId.HasValue )
                .WhereDeducedIsActive()
                .Where( a => groupIds.Contains( a.GroupId.Value ) )
                .Where( a => locationIds.Contains( a.LocationId.Value ) )
                .Where( a => scheduleIds.Contains( a.ScheduleId.Value ) )
                .Where( a => a.OccurrenceDate >= currentDate && a.OccurrenceDate <= maxDate );

            // TODO: Order by Schedule.Order then NextStartDateTime?
            var attendanceOccurrenceList = attendanceOccurrenceQuery
                .Include( a => a.Schedule )
                .Include( a => a.Attendees )
                .Include( a => a.Group )
                .Include( a => a.Location )
                .AsNoTracking()
                .ToList()
                .OrderBy( a => a.OccurrenceDate )
                .OrderBy( a => a.Schedule.GetNextStartDateTime( currentDate ) )
                .ToList();

            rptAttendanceOccurrences.DataSource = attendanceOccurrenceList;
            rptAttendanceOccurrences.DataBind();
        }

        #endregion

        #region Classes

        public class RosterConfiguration
        {
            public int[] GroupIds { get; set; }
            public bool IncludeChildGroups { get; set; }
            public int[] LocationIds { get; set; }
            public int[] ScheduleIds { get; set; }
            public bool DisplayRole { get; set; }
        }

        #endregion
    }
}