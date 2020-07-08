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
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Web.UI;
using System.Web.UI.WebControls;

using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Security;
using Rock.Web.UI;
using Rock.Web.UI.Controls;

namespace RockWeb.Blocks.GroupScheduling
{
    /// <summary>
    ///
    /// </summary>
    [DisplayName( "Group Scheduler" )]
    [Category( "Group Scheduling" )]
    [Description( "Allows group schedules for groups and locations to be managed by a scheduler." )]

    [IntegerField(
        "Number of Weeks To Show",
        Description = "The number of weeks out that can scheduled.",
        IsRequired = true,
        DefaultIntegerValue = 6,
        Order = 0,
        Key = AttributeKey.FutureWeeksToShow )]
    public partial class GroupScheduler : RockBlock
    {
        /// <summary>
        /// 
        /// </summary>
        protected class AttributeKey
        {
            /// <summary>
            /// The future weeks to show
            /// </summary>
            public const string FutureWeeksToShow = "FutureWeeksToShow";
        }

        #region PageParameterKeys

        /// <summary>
        /// 
        /// </summary>
        private static class PageParameterKey
        {
            public const string GroupId = "GroupId";
        }

        #endregion PageParameterKeys

        #region UserPreferenceKeys

        private static class UserPreferenceKey
        {
            public const string SelectedGroupId = "SelectedGroupId";
            public const string SelectedDate = "SelectedDate";
            public const string SelectAllSchedules = "SelectAllSchedules";
            public const string SelectedIndividualScheduleId = "SelectedIndividualScheduleId";
            public const string SelectedGroupLocationIds = "SelectedGroupLocationIds";
            public const string SelectedResourceListSourceType = "SelectedResourceListSourceType";
            public const string GroupMemberFilterType = "GroupMemberFilterType";
            public const string AlternateGroupId = "AlternateGroupId";
            public const string DataViewId = "DataViewId";
        }

        #endregion UserPreferanceKeys

        #region Base Control Methods

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            RockPage.AddScriptLink( "~/Scripts/dragula.min.js", true );
            RockPage.AddCSSLink( "~/Themes/Rock/Styles/group-scheduler.css", true );

            this.AddConfigurationUpdateTrigger( upnlContent );

            LoadDropDowns();

            btnCopyToClipboard.Visible = true;
            RockPage.AddScriptLink( this.Page, "~/Scripts/clipboard.js/clipboard.min.js" );
            string script = string.Format(
@"new ClipboardJS('#{0}');
    $('#{0}').tooltip();
",
btnCopyToClipboard.ClientID );

            ScriptManager.RegisterStartupScript( btnCopyToClipboard, btnCopyToClipboard.GetType(), "share-copy", script, true );
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
                LoadFilterFromUserPreferencesOrURL();
                ApplyFilter();
            }

            if ( Page.IsPostBack )
            {
                // handle manual __doPostback events
                string postbackArgs = Request.Params["__EVENTARGUMENT"];
                if ( !string.IsNullOrWhiteSpace( postbackArgs ) )
                {
                    if ( postbackArgs == "select-all-locations" )
                    {
                        var locationItems = cblGroupLocations.Items.OfType<ListItem>().ToList();
                        bool selected = locationItems.All( a => !a.Selected );
                        foreach ( var cbLocation in locationItems )
                        {
                            cbLocation.Selected = selected;
                        }

                        ApplyFilter();
                    }
                }
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Loads the drop downs.
        /// </summary>
        private void LoadDropDowns()
        {
            int numOfWeeks = GetAttributeValue( AttributeKey.FutureWeeksToShow ).AsIntegerOrNull() ?? 6;

            ddlWeek.Items.Clear();

            var sundayDate = RockDateTime.Now.SundayDate();
            int weekNum = 0;
            while ( weekNum < numOfWeeks )
            {
                string weekTitle = string.Format( "Week of {0} to {1}", sundayDate.AddDays( -6 ).ToShortDateString(), sundayDate.ToShortDateString() );
                ddlWeek.Items.Add( new ListItem( weekTitle, sundayDate.ToISO8601DateString() ) );
                weekNum++;
                sundayDate = sundayDate.AddDays( 7 );
            }
        }

        /// <summary>
        /// Updates the list of schedules for the selected group
        /// </summary>
        private void UpdateScheduleList()
        {
            Group group = GetSelectedGroup();

            if ( group == null )
            {
                pnlScheduler.Visible = false;
                return;
            }

            bool canSchedule = group.IsAuthorized( Authorization.EDIT, this.CurrentPerson ) || group.IsAuthorized( Authorization.SCHEDULE, this.CurrentPerson );
            if ( !canSchedule )
            {
                nbNotice.Heading = "Sorry";
                nbNotice.Text = "<p>You're not authorized to schedule resources for the selected group.</p>";
                nbNotice.NotificationBoxType = NotificationBoxType.Warning;
                nbNotice.Visible = true;
                pnlScheduler.Visible = false;
                return;
            }
            else
            {
                nbNotice.Visible = false;
            }

            nbGroupWarning.Visible = false;
            pnlGroupScheduleLocations.Visible = false;
            pnlScheduler.Visible = false;

            if ( group == null )
            {
                return;
            }

            var groupLocations = group.GroupLocations.ToList();

            var groupSchedules = groupLocations
                .Where( gl => gl.Location.IsActive )
                .SelectMany( gl => gl.Schedules )
                .Where( s => s.IsActive )
                .DistinctBy( a => a.Guid )
                .ToList();

            if ( !groupSchedules.Any() )
            {
                nbGroupWarning.Text = "Group does not have any locations or schedules";
                nbGroupWarning.Visible = true;
                return;
            }

            pnlGroupScheduleLocations.Visible = true;
            pnlScheduler.Visible = true;

            // if a schedule is already selected, set it as the selected schedule (if it still exists for this group)
            var selectedScheduleId = rblIndividualSchedule.SelectedValue.AsIntegerOrNull();

            rblIndividualSchedule.Items.Clear();

            List<Schedule> sortedScheduleList = groupSchedules.OrderByOrderAndNextScheduledDateTime();

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
                listItem.Selected = selectedScheduleId.HasValue && selectedScheduleId.Value == schedule.Id;
                rblIndividualSchedule.Items.Add( listItem );
            }

            if ( rblIndividualSchedule.SelectedItem == null )
            {
                rblIndividualSchedule.SetValue( sortedScheduleList.FirstOrDefault() );
            }
        }

        /// <summary>
        /// Gets the selected group.
        /// </summary>
        /// <returns></returns>
        private Group GetSelectedGroup()
        {
            var groupId = hfGroupId.Value.AsIntegerOrNull();
            var rockContext = new RockContext();
            Group group = null;
            if ( groupId.HasValue )
            {
                group = new GroupService( rockContext ).GetNoTracking( groupId.Value );
            }

            return group;
        }

        /// <summary>
        /// Loads the filter from user preferences or the URL
        /// </summary>
        private void LoadFilterFromUserPreferencesOrURL()
        {
            var selectedSundayDate = this.GetUrlSettingOrBlockUserPreference( UserPreferenceKey.SelectedDate ).AsDateTime();
            var selectedWeekItem = ddlWeek.Items.FindByValue( selectedSundayDate.ToISO8601DateString() );
            if ( selectedWeekItem != null )
            {
                selectedWeekItem.Selected = true;
            }
            else
            {
                ddlWeek.SelectedIndex = 0;
            }

            int? pageParameterGroupID = this.PageParameter( PageParameterKey.GroupId ).AsIntegerOrNull();
            if ( pageParameterGroupID.HasValue )
            {
                hfGroupId.Value = pageParameterGroupID.ToString();
                gpGroup.SetValue( pageParameterGroupID );
                gpGroup.Enabled = false;
            }
            else
            {
                hfGroupId.Value = this.GetUrlSettingOrBlockUserPreference( UserPreferenceKey.SelectedGroupId );
                gpGroup.SetValue( hfGroupId.Value.AsIntegerOrNull() );
            }

            UpdateScheduleList();
            cbAllSchedules.Checked = this.GetUrlSettingOrBlockUserPreference( UserPreferenceKey.SelectAllSchedules ).AsBoolean();
            rblIndividualSchedule.SetValue( this.GetUrlSettingOrBlockUserPreference( UserPreferenceKey.SelectedIndividualScheduleId ).AsIntegerOrNull() );

            UpdateGroupLocationList();
            cblGroupLocations.SetValues( this.GetUrlSettingOrBlockUserPreference( UserPreferenceKey.SelectedGroupLocationIds ).SplitDelimitedValues().AsIntegerList() );

            var resourceListSourceType = this.GetUrlSettingOrBlockUserPreference( UserPreferenceKey.SelectedResourceListSourceType ).ConvertToEnumOrNull<SchedulerResourceListSourceType>() ?? SchedulerResourceListSourceType.Group;
            var groupMemberFilterType = this.GetUrlSettingOrBlockUserPreference( UserPreferenceKey.GroupMemberFilterType ).ConvertToEnumOrNull<SchedulerResourceGroupMemberFilterType>() ?? SchedulerResourceGroupMemberFilterType.ShowAllGroupMembers;

            SetResourceListSourceType( resourceListSourceType, groupMemberFilterType );

            gpResourceListAlternateGroup.SetValue( this.GetUrlSettingOrBlockUserPreference( UserPreferenceKey.AlternateGroupId ).AsIntegerOrNull() );
            dvpResourceListDataView.SetValue( this.GetUrlSettingOrBlockUserPreference( UserPreferenceKey.DataViewId ).AsIntegerOrNull() );
        }

        /// <summary>
        /// Gets the URL setting (if there is one) or block user preference.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        private string GetUrlSettingOrBlockUserPreference( string key )
        {
            string setting = Request.QueryString[key];
            if ( setting != null )
            {
                return setting;
            }

            return this.GetBlockUserPreference( key );
        }

        /// <summary>
        /// Saves the user preferences and updates the resource list and locations based on the filter
        /// </summary>
        private void ApplyFilter()
        {
            var group = this.GetSelectedGroup();
            int groupId = 0;
            if ( group != null )
            {
                groupId = group.Id;
            }

            List<int> scheduleIds = GetSelectedScheduleIds();

            var allSelectedLocationIds = new HashSet<int>( hfAllSelectedLocationIds.Value.SplitDelimitedValues().AsIntegerList() );
            foreach ( var displayedLocationItem in cblGroupLocations.Items.OfType<ListItem>() )
            {
                var locationId = displayedLocationItem.Value.AsInteger();
                if ( displayedLocationItem.Selected )
                {
                    allSelectedLocationIds.Add( locationId );
                }
                else
                {
                    allSelectedLocationIds.Remove( locationId );
                }
            }

            hfAllSelectedLocationIds.Value = allSelectedLocationIds.ToList().AsDelimited( "," );

            this.SetBlockUserPreference( UserPreferenceKey.SelectedGroupId, groupId.ToString() );
            this.SetBlockUserPreference( UserPreferenceKey.SelectedDate, ddlWeek.SelectedValue );
            this.SetBlockUserPreference( UserPreferenceKey.SelectedGroupLocationIds, allSelectedLocationIds.ToList().AsDelimited( "," ) );
            this.SetBlockUserPreference( UserPreferenceKey.SelectAllSchedules, cbAllSchedules.Checked.ToString() );
            this.SetBlockUserPreference( UserPreferenceKey.SelectedIndividualScheduleId, rblIndividualSchedule.SelectedValue );

            rblIndividualSchedule.Visible = cbAllSchedules.Checked == false;

            var resourceListSourceType = ( SchedulerResourceListSourceType ) hfSchedulerResourceListSourceType.Value.AsInteger();
            var groupMemberFilterType = ( SchedulerResourceGroupMemberFilterType ) hfResourceGroupMemberFilterType.Value.AsInteger();

            if ( group != null && group.SchedulingMustMeetRequirements )
            {
                // don't show options for other groups or people if SchedulingMustMeetRequirements
                // this is because people from other groups wouldn't meet scheduling requirements (since they aren't in the same group as the Attendance Occurrence)
                btnAlternateGroup.Visible = false;
                btnParentGroup.Visible = false;
                btnDataView.Visible = false;
                pnlAddPerson.Visible = false;
                ppAddPerson.Visible = false;
                if ( resourceListSourceType != SchedulerResourceListSourceType.Group )
                {
                    resourceListSourceType = SchedulerResourceListSourceType.Group;
                    SetResourceListSourceType( resourceListSourceType, groupMemberFilterType );
                }
            }
            else
            {
                btnAlternateGroup.Visible = true;

                // only show the ParentGroup option of the group has a parent group
                btnParentGroup.Visible = group != null && group.ParentGroup != null;

                btnDataView.Visible = true;
                pnlAddPerson.Visible = true;
                ppAddPerson.Visible = true;
            }

            this.SetBlockUserPreference( UserPreferenceKey.SelectedResourceListSourceType, resourceListSourceType.ToString() );

            
            this.SetBlockUserPreference( UserPreferenceKey.GroupMemberFilterType, groupMemberFilterType.ToString() );

            this.SetBlockUserPreference( UserPreferenceKey.AlternateGroupId, gpResourceListAlternateGroup.SelectedValue );
            this.SetBlockUserPreference( UserPreferenceKey.DataViewId, dvpResourceListDataView.SelectedValue );

            pnlResourceFilterAlternateGroup.Visible = resourceListSourceType == SchedulerResourceListSourceType.AlternateGroup;
            pnlResourceFilterDataView.Visible = resourceListSourceType == SchedulerResourceListSourceType.DataView;

            bool filterIsValid = groupId > 0 && scheduleIds.Any() && cblGroupLocations.SelectedValues.Any();
            pnlScheduler.Visible = filterIsValid && !group.DisableScheduling;
            nbFilterInstructions.Visible = !filterIsValid;

            var disableScheduling = group != null && group.DisableScheduling;
            nbSchedulingDisabled.Visible = disableScheduling;

            if ( disableScheduling )
            {
                nbSchedulingDisabled.Text = string.Format( "Scheduling is disabled for the {0} group.", group.Name );
            }
            pnlGroupScheduleLocations.Visible = groupId > 0;

            if ( filterIsValid && !group.DisableScheduling )
            {
                InitResourceList();
                BindAttendanceOccurrences();
            }

            // Create URL for selected settings
            var pageReference = CurrentPageReference;
            foreach ( var setting in GetBlockUserPreferences() )
            {
                pageReference.Parameters.AddOrReplace( setting.Key, setting.Value );
            }

            Uri uri = new Uri( Request.Url.ToString() );
            btnCopyToClipboard.Attributes["data-clipboard-text"] = uri.GetLeftPart( UriPartial.Authority ) + pageReference.BuildUrl();
            btnCopyToClipboard.Disabled = false;
        }

        /// <summary>
        /// Gets the selected schedule ids.
        /// </summary>
        /// <returns></returns>
        private List<int> GetSelectedScheduleIds()
        {
            List<int> scheduleIds = new List<int>();
            if ( cbAllSchedules.Checked )
            {
                var allScheduleIds = rblIndividualSchedule.Items.OfType<ListItem>().Select( a => a.Value.AsInteger() ).ToList();
                scheduleIds.AddRange( allScheduleIds );
            }
            else
            {
                scheduleIds.Add( rblIndividualSchedule.SelectedValue.AsInteger() );
            }

            return scheduleIds;
        }

        /// <summary>
        /// Updates the list of group locations for the selected group
        /// </summary>
        private void UpdateGroupLocationList()
        {
            Group group = GetSelectedGroup();

            if ( group == null )
            {
                pnlScheduler.Visible = false;
                return;
            }

            if ( group != null )
            {
                bool canSchedule = group.IsAuthorized( Authorization.EDIT, this.CurrentPerson ) || group.IsAuthorized( Authorization.SCHEDULE, this.CurrentPerson );
                if ( !canSchedule )
                {
                    nbNotice.Heading = "Sorry";
                    nbNotice.Text = "<p>You're not authorized to schedule resources for the selected group.</p>";
                    nbNotice.NotificationBoxType = NotificationBoxType.Warning;
                    nbNotice.Visible = true;
                    pnlScheduler.Visible = false;
                    return;
                }
                else
                {
                    nbNotice.Visible = false;
                }

                pnlScheduler.Visible = true;
                List<int> scheduleIds = GetSelectedScheduleIds();

                var rockContext = new RockContext();
                var groupLocationsQuery = new GroupLocationService( rockContext ).Queryable()
                    .Where( gl =>
                        gl.GroupId == group.Id &&
                        gl.Schedules.Any( s => scheduleIds.Contains( s.Id ) ) &&
                        gl.Location.IsActive )
                    .OrderBy( a => new { a.Order, a.Location.Name } )
                    .AsNoTracking();

                var groupLocationsList = groupLocationsQuery.ToList();

                if ( !groupLocationsList.Any() && scheduleIds.Any() )
                {
                    nbGroupWarning.Text = "Group does not have any locations for the selected schedule";
                    nbGroupWarning.Visible = true;
                }
                else if ( scheduleIds.Any() )
                {
                    nbGroupWarning.Visible = false;
                }

                // get the location ids of the selected group locations so that we can keep the selected locations even if the group changes
                var allSelectedLocationIds = hfAllSelectedLocationIds.Value.SplitDelimitedValues().AsIntegerList();

                var selectedGroupLocationIds = allSelectedLocationIds;
                var selectedLocationIds = new GroupLocationService( new RockContext() ).GetByIds( selectedGroupLocationIds ).Select( a => a.LocationId ).ToList();

                cblGroupLocations.Items.Clear();
                foreach ( var groupLocation in groupLocationsList )
                {
                    var groupLocationItem = new ListItem( groupLocation.Location.ToString(), groupLocation.Id.ToString() );
                    groupLocationItem.Selected = selectedLocationIds.Contains( groupLocation.LocationId );
                    cblGroupLocations.Items.Add( groupLocationItem );
                }

                // if there aren't any locations select, default to selecting all
                if ( !cblGroupLocations.SelectedValues.Any() )
                {
                    foreach ( var item in cblGroupLocations.Items.OfType<ListItem>() )
                    {
                        item.Selected = true;
                    }
                }
            }
        }

        /// <summary>
        /// Set the Resource List hidden fields which groupScheduler.js uses to populate the Resource List
        /// </summary>
        private void InitResourceList()
        {
            int groupId = hfGroupId.Value.AsInteger();
            int? resourceGroupId = null;
            int? resourceDataViewId = null;
            List<int> scheduleIds = GetSelectedScheduleIds();

            hfResourceAdditionalPersonIds.Value = string.Empty;

            var resourceListSourceType = hfSchedulerResourceListSourceType.Value.ConvertToEnum<SchedulerResourceListSourceType>();
            switch ( resourceListSourceType )
            {
                case SchedulerResourceListSourceType.Group:
                    {
                        resourceGroupId = hfGroupId.Value.AsInteger();
                        break;
                    }

                case SchedulerResourceListSourceType.AlternateGroup:
                    {
                        resourceGroupId = gpResourceListAlternateGroup.SelectedValue.AsInteger();
                        break;
                    }

                case SchedulerResourceListSourceType.DataView:
                    {
                        resourceDataViewId = dvpResourceListDataView.SelectedValue.AsInteger();
                        break;
                    }
            }

            hfOccurrenceGroupId.Value = hfGroupId.Value;
            hfOccurrenceScheduleIds.Value = scheduleIds.AsDelimited( "," );
            hfOccurrenceSundayDate.Value = ddlWeek.SelectedValue.AsDateTime().ToISO8601DateString();

            hfResourceGroupId.Value = resourceGroupId.ToString();
            hfResourceDataViewId.Value = resourceDataViewId.ToString();
            hfResourceAdditionalPersonIds.Value = string.Empty;
        }

        /// <summary>
        /// Binds the Attendance Occurrences ( Which shows the Location for the Attendance Occurrence for the selected Group + DateTime + Location ).
        /// groupScheduler.js will populate these with the scheduled resources
        /// </summary>
        private void BindAttendanceOccurrences()
        {
            var occurrenceSundayDate = hfOccurrenceSundayDate.Value.AsDateTime().Value.Date;
            var occurrenceSundayWeekStartDate = occurrenceSundayDate.AddDays( -6 );

            // make sure we don't let them schedule dates in the past
            if ( occurrenceSundayWeekStartDate <= RockDateTime.Today )
            {
                occurrenceSundayWeekStartDate = RockDateTime.Today;
            }

            var scheduleIds = GetSelectedScheduleIds();

            var rockContext = new RockContext();
            var occurrenceSchedules = new ScheduleService( rockContext ).GetByIds( scheduleIds ).AsNoTracking().ToList();

            if ( !occurrenceSchedules.Any() )
            {
                btnAutoSchedule.Visible = false;
                return;
            }

            // get all the occurrences for the selected week for the selected schedules (It could be more than once a week if it is a daily scheduled, or it might not be in the selected week if it is every 2 weeks, etc)
            List<DateTime> scheduleOccurrenceDateTimeList = new List<DateTime>();
            foreach ( var occurrenceSchedule in occurrenceSchedules )
            {
                var occurrenceStartTimes = occurrenceSchedule.GetScheduledStartTimes( occurrenceSundayWeekStartDate, occurrenceSundayDate.AddDays( 1 ) );
                scheduleOccurrenceDateTimeList.AddRange( occurrenceStartTimes );
            }

            scheduleOccurrenceDateTimeList = scheduleOccurrenceDateTimeList.Distinct().ToList();

            if ( !scheduleOccurrenceDateTimeList.Any() )
            {
                btnAutoSchedule.Visible = false;
                return;
            }

            var occurrenceDateList = scheduleOccurrenceDateTimeList.Select( a => a.Date ).Distinct().ToList();
            btnAutoSchedule.Visible = true;

            var attendanceOccurrenceService = new AttendanceOccurrenceService( rockContext );
            var selectedGroupLocationIds = cblGroupLocations.SelectedValuesAsInt;

            foreach ( var scheduleId in scheduleIds )
            {
                List<AttendanceOccurrence> missingAttendanceOccurrenceListForSchedule =
                    attendanceOccurrenceService.CreateMissingAttendanceOccurrences( occurrenceDateList, scheduleId, selectedGroupLocationIds );

                attendanceOccurrenceService.AddRange( missingAttendanceOccurrenceListForSchedule );
                rockContext.SaveChanges();
            }


            IQueryable<AttendanceOccurrenceService.AttendanceOccurrenceGroupLocationScheduleConfigJoinResult> attendanceOccurrenceGroupLocationScheduleConfigQuery
                = attendanceOccurrenceService.AttendanceOccurrenceGroupLocationScheduleConfigJoinQuery( occurrenceDateList, scheduleIds, selectedGroupLocationIds );

            var attendanceOccurrencesList = attendanceOccurrenceGroupLocationScheduleConfigQuery.AsNoTracking()
                .OrderBy( a => a.GroupLocation.Order ).ThenBy( a => a.GroupLocation.Location.Name )
                .Select( a => new AttendanceOccurrenceRowItem
                {
                    LocationName = a.AttendanceOccurrence.Location.Name,
                    GroupLocationOrder = a.GroupLocation.Order,
                    LocationId = a.AttendanceOccurrence.LocationId,
                    Schedule = a.AttendanceOccurrence.Schedule,
                    OccurrenceDate = a.AttendanceOccurrence.OccurrenceDate,
                    AttendanceOccurrenceId = a.AttendanceOccurrence.Id,
                    CapacityInfo = new CapacityInfo
                    {
                        MinimumCapacity = a.GroupLocationScheduleConfig.MinimumCapacity,
                        DesiredCapacity = a.GroupLocationScheduleConfig.DesiredCapacity,
                        MaximumCapacity = a.GroupLocationScheduleConfig.MaximumCapacity
                    }
                } ).ToList();

            var groupId = hfGroupId.Value.AsInteger();

            var attendanceOccurrencesOrderedList = attendanceOccurrencesList.OrderBy( a => a.ScheduledDateTime ).ThenBy( a => a.GroupLocationOrder ).ThenBy( a => a.LocationName ).ToList();

            // if there are any people that signed up with no location preference, add the to a special list of "No Location Preference" occurrences to the top of the list
            var unassignedLocationOccurrenceList = attendanceOccurrenceService.Queryable()
                .Where( a => occurrenceDateList.Contains( a.OccurrenceDate )
                    && a.ScheduleId.HasValue
                    && scheduleIds.Contains( a.ScheduleId.Value )
                    && a.GroupId == groupId
                    && a.LocationId.HasValue == false )
                .Where( a => a.Attendees.Any( x => x.RequestedToAttend == true || x.ScheduledToAttend == true ) )
                .Select( a => new AttendanceOccurrenceRowItem
                {
                    LocationName = "No Location Preference",
                    GroupLocationOrder = 0,
                    LocationId = null,
                    Schedule = a.Schedule,
                    OccurrenceDate = a.OccurrenceDate,
                    AttendanceOccurrenceId = a.Id,
                    CapacityInfo = new CapacityInfo()
                } )
                .ToList()
                .OrderBy( a => a.ScheduledDateTime )
                .ToList();


            attendanceOccurrencesOrderedList.InsertRange( 0, unassignedLocationOccurrenceList );

            rptAttendanceOccurrences.DataSource = attendanceOccurrencesOrderedList;
            rptAttendanceOccurrences.DataBind();

            hfDisplayedOccurrenceIds.Value = attendanceOccurrencesOrderedList.Select( a => a.AttendanceOccurrenceId ).ToList().AsDelimited( "," );
        }

        /// <summary>
        /// 
        /// </summary>
        private class CapacityInfo
        {
            /// <summary>
            /// Gets or sets the minimum capacity.
            /// </summary>
            /// <value>
            /// The minimum capacity.
            /// </value>
            public int? MinimumCapacity { get; set; }

            /// <summary>
            /// Gets or sets the desired capacity.
            /// </summary>
            /// <value>
            /// The desired capacity.
            /// </value>
            public int? DesiredCapacity { get; set; }

            /// <summary>
            /// Gets or sets the maximum capacity.
            /// </summary>
            /// <value>
            /// The maximum capacity.
            /// </value>
            public int? MaximumCapacity { get; set; }
        }

        /// <summary>
        ///
        /// </summary>
        private class AttendanceOccurrenceRowItem
        {
            /// <summary>
            /// Gets or sets the attendance occurrence identifier.
            /// </summary>
            /// <value>
            /// The attendance occurrence identifier.
            /// </value>
            public int AttendanceOccurrenceId { get; set; }

            /// <summary>
            /// Gets or sets the capacity information.
            /// </summary>
            /// <value>
            /// The capacity information.
            /// </value>
            public CapacityInfo CapacityInfo { get; set; }

            /// <summary>
            /// Gets or sets the name of the location.
            /// </summary>
            /// <value>
            /// The name of the location.
            /// </value>
            public string LocationName { get; set; }

            /// <summary>
            /// Gets or sets the location identifier.
            /// NOTE: There should only be one that doesn't have a LocationId, and it should only be shown if there are assignments in it
            /// </summary>
            /// <value>
            /// The location identifier.
            /// </value>
            public int? LocationId { get; set; }

            /// <summary>
            /// Gets or sets the schedule.
            /// </summary>
            /// <value>
            /// The schedule.
            /// </value>
            public Schedule Schedule { get; set; }

            /// <summary>
            /// Gets the Schedule's scheduled date time the Occurrence Date
            /// </summary>
            /// <value>
            /// The scheduled date time.
            /// </value>
            public DateTime? ScheduledDateTime
            {
                get
                {
                    return Schedule.GetNextStartDateTime( this.OccurrenceDate );
                }
            }

            /// <summary>
            /// Gets or sets the occurrence date.
            /// </summary>
            /// <value>
            /// The occurrence date.
            /// </value>
            public DateTime OccurrenceDate { get; set; }

            /// <summary>
            /// Gets or sets the group location order.
            /// </summary>
            /// <value>
            /// The group location order.
            /// </value>
            public int GroupLocationOrder { get; internal set; }
        }

        /// <summary>
        /// Handles the ItemDataBound event of the rptAttendanceOccurrences control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RepeaterItemEventArgs"/> instance containing the event data.</param>
        protected void rptAttendanceOccurrences_ItemDataBound( object sender, RepeaterItemEventArgs e )
        {
            var attendanceOccurrenceRowItem = e.Item.DataItem as AttendanceOccurrenceRowItem;
            var attendanceOccurrenceId = attendanceOccurrenceRowItem.AttendanceOccurrenceId;
            var pnlScheduledOccurrence = e.Item.FindControl( "pnlScheduledOccurrence" ) as Panel;
            var pnlStatusLabels = e.Item.FindControl( "pnlStatusLabels" ) as Panel;
            var lOccurrenceScheduledDateTime = e.Item.FindControl( "lOccurrenceScheduledDateTime" ) as Literal;

            // hide the scheduled occurrence when it is empty if is the one that doesn't have a Location assigned
            bool hasLocation = attendanceOccurrenceRowItem.LocationId.HasValue;
            pnlScheduledOccurrence.Attributes["data-has-location"] = hasLocation.Bit().ToString();

            // hide the status labels if is the one that doesn't have a Location assigned
            pnlStatusLabels.Visible = hasLocation;

            var hfAttendanceOccurrenceId = e.Item.FindControl( "hfAttendanceOccurrenceId" ) as HiddenField;
            var hfAttendanceOccurrenceDate = e.Item.FindControl( "hfAttendanceOccurrenceDate" ) as HiddenField;
            var hfLocationScheduleMinimumCapacity = e.Item.FindControl( "hfLocationScheduleMinimumCapacity" ) as HiddenField;
            var hfLocationScheduleDesiredCapacity = e.Item.FindControl( "hfLocationScheduleDesiredCapacity" ) as HiddenField;
            var hfLocationScheduleMaximumCapacity = e.Item.FindControl( "hfLocationScheduleMaximumCapacity" ) as HiddenField;
            var lLocationTitle = e.Item.FindControl( "lLocationTitle" ) as Literal;
            hfAttendanceOccurrenceId.Value = attendanceOccurrenceId.ToString();

            if ( attendanceOccurrenceRowItem.CapacityInfo != null )
            {
                hfLocationScheduleMinimumCapacity.Value = attendanceOccurrenceRowItem.CapacityInfo.MinimumCapacity.ToString();
                hfLocationScheduleDesiredCapacity.Value = attendanceOccurrenceRowItem.CapacityInfo.DesiredCapacity.ToString();
                hfLocationScheduleMaximumCapacity.Value = attendanceOccurrenceRowItem.CapacityInfo.MaximumCapacity.ToString();
            }

            lLocationTitle.Text = attendanceOccurrenceRowItem.LocationName;
            if ( attendanceOccurrenceRowItem.ScheduledDateTime.HasValue )
            {
                // show date in 'Sunday, June 15, 2008 9:15 PM' format
                lOccurrenceScheduledDateTime.Text = attendanceOccurrenceRowItem.ScheduledDateTime.Value.ToString( "f" );
                hfAttendanceOccurrenceDate.Value = attendanceOccurrenceRowItem.ScheduledDateTime.Value.Date.ToISO8601DateString();
            }
        }

        #endregion

        #region Events

        /// <summary>
        /// Handles the ValueChanged event of the gpGroup control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void gpGroup_ValueChanged( object sender, EventArgs e )
        {
            hfGroupId.Value = gpGroup.SelectedValue.AsIntegerOrNull().ToString();
            UpdateScheduleList();
            UpdateGroupLocationList();
            ApplyFilter();
        }

        /// <summary>
        /// Handles the SelectedIndexChanged event of the ddlWeek control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void ddlWeek_SelectedIndexChanged( object sender, EventArgs e )
        {
            ApplyFilter();
        }

        /// <summary>
        /// Handles the SelectedIndexChanged event of the rblIndividualSchedule control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void rblIndividualSchedule_SelectedIndexChanged( object sender, EventArgs e )
        {
            UpdateGroupLocationList();
            ApplyFilter();
        }

        /// <summary>
        /// Handles the CheckedChanged event of the cbAllSchedules control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void cbAllSchedules_CheckedChanged( object sender, EventArgs e )
        {
            UpdateGroupLocationList();
            ApplyFilter();
        }

        /// <summary>
        /// Handles the SelectedIndexChanged event of the cblGroupLocations control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void cblGroupLocations_SelectedIndexChanged( object sender, EventArgs e )
        {
            var toggledLocation = sender;
            ApplyFilter();
        }

        /// <summary>
        /// Handles the Change event of the ResourceListSourceType control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void ResourceListSourceType_Change( object sender, EventArgs e )
        {
            SchedulerResourceListSourceType schedulerResourceListSourceType;
            SchedulerResourceGroupMemberFilterType schedulerResourceGroupMemberFilterType = SchedulerResourceGroupMemberFilterType.ShowAllGroupMembers;
            if ( sender == btnGroupMembers )
            {
                schedulerResourceListSourceType = SchedulerResourceListSourceType.Group;
                schedulerResourceGroupMemberFilterType = SchedulerResourceGroupMemberFilterType.ShowAllGroupMembers;
            }
            else if ( sender == btnGroupMembersMatchingPreference )
            {
                schedulerResourceListSourceType = SchedulerResourceListSourceType.Group;
                schedulerResourceGroupMemberFilterType = SchedulerResourceGroupMemberFilterType.ShowMatchingPreference;
            }
            else if ( sender == btnAlternateGroup )
            {
                schedulerResourceListSourceType = SchedulerResourceListSourceType.AlternateGroup;
            }
            else if ( sender == btnDataView )
            {
                schedulerResourceListSourceType = SchedulerResourceListSourceType.DataView;
            }
            else if ( sender == btnParentGroup )
            {
                schedulerResourceListSourceType = SchedulerResourceListSourceType.ParentGroup;
            }
            else
            {
                // shouldn't happen, but just in case
                schedulerResourceListSourceType = SchedulerResourceListSourceType.Group;
                schedulerResourceGroupMemberFilterType = SchedulerResourceGroupMemberFilterType.ShowAllGroupMembers;
            }


            SetResourceListSourceType( schedulerResourceListSourceType, schedulerResourceGroupMemberFilterType );
            ApplyFilter();
        }

        /// <summary>
        /// Sets the type of the resource list source.
        /// </summary>
        /// <param name="schedulerResourceListSourceType">Type of the scheduler resource list source.</param>
        /// <param name="schedulerResourceGroupMemberFilterType">Type of the scheduler resource group member filter.</param>
        private void SetResourceListSourceType( SchedulerResourceListSourceType schedulerResourceListSourceType, SchedulerResourceGroupMemberFilterType schedulerResourceGroupMemberFilterType )
        {
            hfSchedulerResourceListSourceType.Value = schedulerResourceListSourceType.ConvertToInt().ToString();
            hfResourceGroupMemberFilterType.Value = schedulerResourceGroupMemberFilterType.ConvertToInt().ToString();

            switch ( schedulerResourceListSourceType )
            {
                case SchedulerResourceListSourceType.Group:
                    {
                        if ( schedulerResourceGroupMemberFilterType == SchedulerResourceGroupMemberFilterType.ShowMatchingPreference )
                        {
                            lSelectedResourceTypeDropDownText.Text = "Group Members (Matching Preference)";
                        }
                        else
                        {
                            lSelectedResourceTypeDropDownText.Text = "Group Members";
                        }

                        sfResource.Placeholder = "Search";

                        break;
                    }

                case SchedulerResourceListSourceType.AlternateGroup:
                    {
                        lSelectedResourceTypeDropDownText.Text = "Alternate Group";
                        sfResource.Placeholder = "Search Alternate Group";
                        break;
                    }

                case SchedulerResourceListSourceType.DataView:
                    {
                        lSelectedResourceTypeDropDownText.Text = "Data View";
                        sfResource.Placeholder = "Search Data View";
                        break;
                    }

                case SchedulerResourceListSourceType.ParentGroup:
                    {
                        lSelectedResourceTypeDropDownText.Text = "Parent Group";
                        break;
                    }

                default:
                    {
                        lSelectedResourceTypeDropDownText.Text = "Group Members";
                        break;
                    }
            }
        }

        /// <summary>
        /// Handles the SelectedIndexChanged event of the bgResourceListSource control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void bgResourceListSource_SelectedIndexChanged( object sender, EventArgs e )
        {
            ApplyFilter();
        }

        /// <summary>
        /// Handles the ValueChanged event of the gpResourceListAlternateGroup control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs" /> instance containing the event data.</param>
        protected void gpResourceListAlternateGroup_ValueChanged( object sender, EventArgs e )
        {
            ApplyFilter();
        }

        /// <summary>
        /// Handles the SelectedIndexChanged event of the rblGroupMemberFilter control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void rblGroupMemberFilter_SelectedIndexChanged( object sender, EventArgs e )
        {
            ApplyFilter();
        }

        /// <summary>
        /// Handles the ValueChanged event of the dvpResourceListDataView control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void dvpResourceListDataView_ValueChanged( object sender, EventArgs e )
        {
            ApplyFilter();
        }

        /// <summary>
        /// Handles the Click event of the btnAutoSchedule control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnAutoSchedule_Click( object sender, EventArgs e )
        {
            var rockContext = new RockContext();

            var attendanceOccurrenceIdList = hfDisplayedOccurrenceIds.Value.SplitDelimitedValues().AsIntegerList();

            var attendanceService = new AttendanceService( rockContext );

            // AutoSchedule the occurrences that are shown
            attendanceService.SchedulePersonsAutomaticallyForAttendanceOccurrences( attendanceOccurrenceIdList, this.CurrentPersonAlias );
            rockContext.SaveChanges();
        }

        /// <summary>
        /// Handles the Click event of the btnSendNow control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnSendNow_Click( object sender, EventArgs e )
        {
            var rockContext = new RockContext();

            var attendanceOccurrenceIdList = hfDisplayedOccurrenceIds.Value.SplitDelimitedValues().AsIntegerList();

            var attendanceService = new AttendanceService( rockContext );
            var sendConfirmationAttendancesQuery = attendanceService.GetPendingScheduledConfirmations()
                .Where( a => attendanceOccurrenceIdList.Contains( a.OccurrenceId ) )
                .Where( a => a.ScheduleConfirmationSent != true );

            List<string> errorMessages;
            var emailsSent = attendanceService.SendScheduleConfirmationSystemEmails( sendConfirmationAttendancesQuery, out errorMessages );
            bool isSendConfirmationAttendancesFound = sendConfirmationAttendancesQuery.Any();
            rockContext.SaveChanges();

            StringBuilder summaryMessageBuilder = new StringBuilder();
            ModalAlertType alertType;

            if ( errorMessages.Any() )
            {
                alertType = ModalAlertType.Alert;

                var logException = new Exception( "One or more errors occurred when sending confirmation emails: " + Environment.NewLine + errorMessages.AsDelimited( Environment.NewLine ) );

                ExceptionLogService.LogException( logException );

                summaryMessageBuilder.AppendLine( logException.Message );
            }
            else
            {
                alertType = ModalAlertType.Information;
                if ( emailsSent > 0 && isSendConfirmationAttendancesFound )
                {
                    summaryMessageBuilder.AppendLine( string.Format( "Successfully sent {0} confirmation {1}", emailsSent, "email".PluralizeIf( emailsSent != 1 ) ) );
                }
                else
                {
                    summaryMessageBuilder.AppendLine( "Everybody has already been sent a confirmation email. No additional confirmation emails sent." );
                }
            }

            maSendNowResults.Show( summaryMessageBuilder.ToString().ConvertCrLfToHtmlBr(), alertType );
        }

        #endregion

        /// <summary>
        /// Handles the SelectPerson event of the ppAddPerson control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void ppAddPerson_SelectPerson( object sender, EventArgs e )
        {
            var additionPersonIds = hfResourceAdditionalPersonIds.Value.SplitDelimitedValues().AsIntegerList();
            if ( ppAddPerson.PersonId.HasValue )
            {
                additionPersonIds.Add( ppAddPerson.PersonId.Value );
            }

            hfResourceAdditionalPersonIds.Value = additionPersonIds.AsDelimited( "," );

            // clear on the selected person
            ppAddPerson.SetValue( null );
        }




    }
}