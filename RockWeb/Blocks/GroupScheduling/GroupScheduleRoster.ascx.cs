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
using System.Diagnostics;
using System.Linq;
using System.Linq.Dynamic;
using System.Web.UI;
using System.Web.UI.WebControls;

using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Lava;
using Rock.Model;
using Rock.Utility;
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
        Key = AttributeKey.RefreshIntervalSeconds,
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
            public const string RefreshIntervalSeconds = "RefreshIntervalSeconds";
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

            RockPage.AddCSSLink( "~/Themes/Rock/Styles/group-scheduler.css", true );

            Debug.WriteLine( "IsPostBack:{0}, IsInAsyncPostBack:{1} {2}", this.IsPostBack, ScriptManager.GetCurrent( this.Page ).IsInAsyncPostBack, RockDateTime.Now );

            if ( !this.IsPostBack )
            {
                UpdateLiveRefreshConfiguration( this.GetAttributeValue( AttributeKey.EnableLiveRefresh ).AsBoolean() );
                PopulateRoster();
            }
        }

        /// <summary>
        /// Updates the live refresh configuration.
        /// </summary>
        /// <param name="enableLiveRefresh">if set to <c>true</c> [enable live refresh].</param>
        private void UpdateLiveRefreshConfiguration( bool enableLiveRefresh )
        {
            if ( enableLiveRefresh )
            {
                hfRefreshTimerSeconds.Value = this.GetAttributeValue( AttributeKey.RefreshIntervalSeconds );
            }
            else
            {
                hfRefreshTimerSeconds.Value = string.Empty;
            }

            lLiveUpdateEnabled.Visible = enableLiveRefresh;
            lLiveUpdateDisabled.Visible = !enableLiveRefresh;
        }

        #endregion

        #region Events

        /// <summary>
        /// Handles the Click event of the lbRefresh control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void lbRefresh_Click( object sender, EventArgs e )
        {
            PopulateRoster();
        }

        /// <summary>
        /// Handles the BlockUpdated event of the control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void Block_BlockUpdated( object sender, EventArgs e )
        {
            UpdateLiveRefreshConfiguration( this.GetAttributeValue( AttributeKey.EnableLiveRefresh ).AsBoolean() );
            PopulateRoster();
        }

        private bool? _displayRole = null;
        private string _rosterLavaTemplate = null;
        private Dictionary<int, List<ScheduledIndividual>> _confirmedScheduledIndividualsForOccurrenceId = null;

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

            var lOccurrenceRosterHTML = e.Item.FindControl( "lOccurrenceRosterHTML" ) as Literal;
            var scheduleDate = attendanceOccurrence.Schedule.GetNextStartDateTime( attendanceOccurrence.OccurrenceDate );
            var scheduledIndividuals = _confirmedScheduledIndividualsForOccurrenceId.GetValueOrNull( attendanceOccurrence.Id );
            if ( ( scheduleDate == null || scheduleDate.Value.Date != attendanceOccurrence.OccurrenceDate ) )
            {
                // scheduleDate can be later than the OccurrenceDate (or null) if there are exclusions that cause the schedule
                // to not occur on the occurrence date. In this case, don't show the roster unless there are somehow individuals
                // scheduled for this occurrence.
                if ( scheduledIndividuals == null || !scheduledIndividuals.Any() )
                {
                    lOccurrenceRosterHTML.Text = string.Empty;
                }
                else
                {
                    // lava will get a null scheduleDate which can indicate that it isn't scheduled
                }
            }

            var mergeFields = LavaHelper.GetCommonMergeFields( this.RockPage );
            mergeFields.Add( "Group", attendanceOccurrence.Group );
            mergeFields.Add( "Location", attendanceOccurrence.Location );
            mergeFields.Add( "Schedule", attendanceOccurrence.Schedule );
            mergeFields.Add( "ScheduleDate", scheduleDate );
            mergeFields.Add( "DisplayRole", _displayRole );


            mergeFields.Add( "ScheduledIndividuals", scheduledIndividuals );





            var rosterHtml = _rosterLavaTemplate.ResolveMergeFields( mergeFields );

            // we don't need view state
            lOccurrenceRosterHTML.ViewStateMode = ViewStateMode.Disabled;
            lOccurrenceRosterHTML.Text = rosterHtml;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Populates the roster.
        /// </summary>
        private void PopulateRoster()
        {
            RosterConfiguration rosterConfiguration = this.GetBlockUserPreference( UserPreferenceKey.RosterConfigurationJSON )
                .FromJsonOrNull<RosterConfiguration>();

            if ( rosterConfiguration == null || !rosterConfiguration.IsConfigured() )
            {
                ShowConfigurationDialog();
                return;
            }

            int[] scheduleIds = rosterConfiguration.ScheduleIds;
            int[] locationIds = rosterConfiguration.LocationIds;
            List<int> parentGroupIds = rosterConfiguration.GroupIds.ToList();

            this._displayRole = rosterConfiguration.DisplayRole;
            this._rosterLavaTemplate = this.GetAttributeValue( AttributeKey.RosterLavaTemplate );

            var allGroupIds = new List<int>();
            allGroupIds.AddRange( parentGroupIds );

            var rockContext = new RockContext();

            if ( rosterConfiguration.IncludeChildGroups )
            {
                var groupService = new GroupService( rockContext );
                foreach ( var groupId in parentGroupIds )
                {
                    var childGroupIds = groupService.GetAllDescendentGroupIds( groupId, false );
                    allGroupIds.AddRange( childGroupIds );
                }
            }

            allGroupIds = allGroupIds.Distinct().ToList();

            var attendanceOccurrenceService = new AttendanceOccurrenceService( rockContext );

            var currentDate = RockDateTime.Today;

            // only show occurrences for the current day
            var attendanceOccurrenceQuery = attendanceOccurrenceService
                .Queryable()
                .Where( a => a.ScheduleId.HasValue && a.LocationId.HasValue && a.GroupId.HasValue )
                .WhereDeducedIsActive()
                .Where( a => allGroupIds.Contains( a.GroupId.Value ) )
                .Where( a => locationIds.Contains( a.LocationId.Value ) )
                .Where( a => scheduleIds.Contains( a.ScheduleId.Value ) )
                .Where( a => a.OccurrenceDate == currentDate );

            // limit attendees to ones that confirmed (or are checked-in regardless of confirmation status)
            var confirmedAttendancesForOccurrenceQuery = attendanceOccurrenceQuery
                    .SelectMany( a => a.Attendees )
                    .Include( a => a.PersonAlias.Person ).WhereScheduledPersonConfirmed();

            _confirmedScheduledIndividualsForOccurrenceId = confirmedAttendancesForOccurrenceQuery
                .AsNoTracking()
                .ToList()
                .GroupBy( a => a.OccurrenceId )
                .ToDictionary(
                    k => k.Key,
                    v => v.Select( a => new ScheduledIndividual
                    {
                        Attendance = a,
                        Person = a.PersonAlias.Person,
                        CurrentlyCheckedIn = a.DidAttend == true
                    } )
                    .ToList() );


            List<AttendanceOccurrence> attendanceOccurrenceList = attendanceOccurrenceQuery
                .Include( a => a.Schedule )
                .Include( a => a.Attendees )
                .Include( a => a.Group )
                .Include( a => a.Location )
                .AsNoTracking()
                .ToList()
                .OrderBy( a => a.OccurrenceDate )
                .ThenBy( a => a.Schedule.Order )
                .ThenBy( a => a.Schedule.GetNextStartDateTime( a.OccurrenceDate ) )
                .ThenBy( a => a.Location.Name )
                .ToList();

            rptAttendanceOccurrences.DataSource = attendanceOccurrenceList;
            rptAttendanceOccurrences.DataBind();
        }

        #endregion

        #region Configuration Related

        /// <summary>
        /// Handles the Click event of the btnConfiguration control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnConfiguration_Click( object sender, EventArgs e )
        {
            ShowConfigurationDialog();
        }

        /// <summary>
        /// Shows the configuration dialog.
        /// </summary>
        private void ShowConfigurationDialog()
        {
            // don't do the live refresh when the configuration dialog is showing
            UpdateLiveRefreshConfiguration( false );

            RosterConfiguration rosterConfiguration = this.GetBlockUserPreference( UserPreferenceKey.RosterConfigurationJSON )
                            .FromJsonOrNull<RosterConfiguration>();

            if ( rosterConfiguration == null )
            {
                rosterConfiguration = new RosterConfiguration();
            }

            gpGroups.SetValues( rosterConfiguration.GroupIds ?? new int[0] );
            cbIncludeChildGroups.Checked = rosterConfiguration.IncludeChildGroups;

            // 
            UpdateScheduleList();
            lbSchedules.SetValues( rosterConfiguration.ScheduleIds ?? new int[0] );

            UpdateLocationListFromSelectedSchedules();
            cblLocations.SetValues( rosterConfiguration.LocationIds ?? new int[0] );

            cbDisplayRole.Checked = rosterConfiguration.DisplayRole;



            mdRosterConfiguration.Show();
        }

        /// <summary>
        /// Updates the lists for selected groups.
        /// </summary>
        private void UpdateListsForSelectedGroups()
        {
            UpdateScheduleList();
            UpdateLocationListFromSelectedSchedules();
        }

        /// <summary>
        /// Updates the list of schedules for the selected groups
        /// </summary>
        private void UpdateScheduleList()
        {
            var rockContext = new RockContext();
            var includedGroupsQuery = GetSelectedGroupsQuery( rockContext );

            nbGroupWarning.Visible = false;
            nbLocationsWarning.Visible = false;

            if ( !includedGroupsQuery.Any() )
            {
                nbGroupWarning.Text = "Select at least one group.";
                nbGroupWarning.Visible = true;
                return;
            }

            var groupLocationService = new GroupLocationService( rockContext );
            var groupLocationsQuery = groupLocationService.Queryable()
                .Where( a => includedGroupsQuery.Any( x => x.Id == a.GroupId ) )
                .Where( a => a.Group.GroupType.IsSchedulingEnabled == true && a.Group.DisableScheduling == false )
                .Distinct();

            var groupSchedulesQuery = groupLocationsQuery
                .Where( gl => gl.Location.IsActive )
                .SelectMany( gl => gl.Schedules )
                .Where( s => s.IsActive );

            var groupSchedulesList = groupSchedulesQuery.AsNoTracking()
                .AsEnumerable()
                .DistinctBy( a => a.Guid )
                .ToList();

            lbSchedules.Visible = true;
            if ( !groupSchedulesList.Any() )
            {
                lbSchedules.Visible = false;
                nbGroupWarning.Text = "The selected groups do not have any locations or schedules";
                nbGroupWarning.Visible = true;
                return;
            }

            nbGroupWarning.Visible = false;

            // get any of the currently schedule ids, and reselect them if they still exist
            var selectedScheduleIds = lbSchedules.SelectedValues.AsIntegerList();

            lbSchedules.Items.Clear();

            List<Schedule> sortedScheduleList = groupSchedulesList.OrderByOrderAndNextScheduledDateTime();

            foreach ( var schedule in sortedScheduleList )
            {
                var listItem = new ListItem();
                if ( schedule.Name.IsNotNullOrWhiteSpace() )
                {
                    listItem.Text = schedule.Name;
                }
                else
                {
                    listItem.Text = schedule.FriendlyScheduleText;
                }

                listItem.Value = schedule.Id.ToString();
                listItem.Selected = selectedScheduleIds.Contains( schedule.Id );
                lbSchedules.Items.Add( listItem );
            }

            // update selectedSchedules to ones that are still selected after updating schedule list
            selectedScheduleIds = lbSchedules.SelectedValues.AsIntegerList();
        }

        /// <summary>
        /// Returns a queryable of the selected groups
        /// </summary>
        /// <returns></returns>
        private IQueryable<Group> GetSelectedGroupsQuery( RockContext rockContext )
        {
            GroupService groupService;
            int[] selectedGroupIds = gpGroups.SelectedValues.AsIntegerList().ToArray();
            bool includeChildGroups = cbIncludeChildGroups.Checked;

            groupService = new GroupService( rockContext );
            var includedGroupIds = ( selectedGroupIds ?? new int[0] ).ToList();
            if ( includeChildGroups )
            {
                foreach ( var selectedGroupId in selectedGroupIds )
                {
                    var childGroupIds = groupService.GetAllDescendentGroupIds( selectedGroupId, false );

                    includedGroupIds.AddRange( childGroupIds );
                }
            }

            var groupsQuery = groupService.GetByIds( includedGroupIds.Distinct().ToList() );
            groupsQuery = groupsQuery.HasSchedulingEnabled();

            return groupsQuery;
        }

        /// <summary>
        /// Updates the location list from selected schedules.
        /// </summary>
        private void UpdateLocationListFromSelectedSchedules()
        {
            int[] selectedScheduleIds = lbSchedules.SelectedValues.AsIntegerList().ToArray();

            cblLocations.Visible = true;
            nbLocationsWarning.Visible = false;

            if ( !selectedScheduleIds.Any() )
            {
                cblLocations.Visible = false;
                nbLocationsWarning.Text = "Select at least one schedule to see available locations";
                nbLocationsWarning.Visible = true;
                return;
            }

            var rockContext = new RockContext();
            var includedGroupsQuery = GetSelectedGroupsQuery( rockContext );

            var groupLocationService = new GroupLocationService( rockContext );

            var groupLocationsQuery = groupLocationService.Queryable()
                .Where( a => includedGroupsQuery.Any( x => x.Id == a.GroupId ) )
                .Where( a => a.Group.GroupType.IsSchedulingEnabled == true && a.Group.DisableScheduling == false )
                .Distinct();

            // narrow down group locations that ones for the selected schedules
            groupLocationsQuery = groupLocationsQuery.Where( a => a.Schedules.Any( s => selectedScheduleIds.Contains( s.Id ) ) );

            var locationList = groupLocationsQuery.Select( a => a.Location )
                .AsNoTracking()
                .ToList()
                .DistinctBy( a => a.Id )
                .OrderBy( a => a.ToString() ).ToList();

            // get any of the currently location ids, and reselect them if they still exist
            var selectedLocationIds = cblLocations.SelectedValues.AsIntegerList();
            cblLocations.Items.Clear();

            foreach ( var location in locationList )
            {
                var locationListItem = new ListItem( location.ToString(), location.Id.ToString() );
                locationListItem.Selected = selectedLocationIds.Contains( location.Id );
                cblLocations.Items.Add( locationListItem );
            }

            if ( !locationList.Any() )
            {
                cblLocations.Visible = false;
                nbLocationsWarning.Text = "The selected groups do not have any locations for the selected schedules";
                return;
            }
        }

        /// <summary>
        /// Handles the SaveClick event of the mdRosterConfiguration control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void mdRosterConfiguration_SaveClick( object sender, EventArgs e )
        {
            RosterConfiguration rosterConfiguration = this.GetBlockUserPreference( UserPreferenceKey.RosterConfigurationJSON )
                .FromJsonOrNull<RosterConfiguration>();

            if ( rosterConfiguration == null )
            {
                rosterConfiguration = new RosterConfiguration();
            }

            rosterConfiguration.GroupIds = gpGroups.SelectedValuesAsInt().ToArray();
            rosterConfiguration.IncludeChildGroups = cbIncludeChildGroups.Checked;
            rosterConfiguration.LocationIds = cblLocations.SelectedValuesAsInt.ToArray();
            rosterConfiguration.ScheduleIds = lbSchedules.SelectedValuesAsInt.ToArray();
            rosterConfiguration.DisplayRole = cbDisplayRole.Checked;

            this.SetBlockUserPreference( UserPreferenceKey.RosterConfigurationJSON, rosterConfiguration.ToJson() );
            mdRosterConfiguration.Hide();

            UpdateLiveRefreshConfiguration( this.GetAttributeValue( AttributeKey.EnableLiveRefresh ).AsBoolean() );
            PopulateRoster();
        }

        /// <summary>
        /// Handles the SelectItem event of the gpGroups control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void gpGroups_SelectItem( object sender, EventArgs e )
        {
            UpdateListsForSelectedGroups();
        }

        /// <summary>
        /// Handles the CheckedChanged event of the cbIncludeChildGroups control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void cbIncludeChildGroups_CheckedChanged( object sender, EventArgs e )
        {
            UpdateListsForSelectedGroups();
        }

        /// <summary>
        /// Handles the SelectedIndexChanged event of the lbSchedules control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void lbSchedules_SelectedIndexChanged( object sender, EventArgs e )
        {
            UpdateLocationListFromSelectedSchedules();
        }

        #endregion

        #region Classes

        public class RosterConfiguration : RockDynamic
        {
            public int[] GroupIds { get; set; }
            public bool IncludeChildGroups { get; set; }
            public int[] LocationIds { get; set; }
            public int[] ScheduleIds { get; set; }
            public bool DisplayRole { get; set; }

            public bool IsConfigured()
            {
                return GroupIds != null && LocationIds != null && ScheduleIds != null;
            }
        }

        public class ScheduledIndividual : RockDynamic
        {
            public Attendance Attendance { get; set; }
            public Person Person { get; set; }
            public GroupMember GroupMember { get; set; }
            public bool CurrentlyCheckedIn { get; set; }
        }

        #endregion

        
    }
}