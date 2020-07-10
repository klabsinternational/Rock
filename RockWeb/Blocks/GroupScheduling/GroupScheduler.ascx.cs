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
using System.IO;
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
            public const string ListedGroupIds = "GroupIds";
        }

        #endregion PageParameterKeys

        #region UserPreferenceKeys

        private static class UserPreferenceKey
        {
            public const string SelectedGroupId = "SelectedGroupId";
            public const string ListedGroupIds = "ListedGroupIds";



            // TODO, get this all wired up
            public const string ShowChildGroups = "ShowChildGroups";




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
            // ### DEBUG ###
            string compileMessages;
            var cssFilePath = this.Request.MapPath( "~/Themes/Rock/Styles/group-scheduler.css" );
            var cssFileDateTime = File.GetLastWriteTime( cssFilePath );

            var lessFilePath = this.Request.MapPath( "~/Themes/Rock/Styles/group-scheduler.less" );
            var lessDateTime = File.GetLastWriteTime( lessFilePath );

            if ( lessDateTime > cssFileDateTime )
            {
                RockTheme.GetThemes().Where( a => a.Name == "Rock" ).First().Compile( out compileMessages );
            }

            //


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
        }

        #endregion

        #region Methods

        private List<DateTime> _listedSundayDates = null;

        /// <summary>
        /// Loads the drop downs.
        /// </summary>
        private void LoadDropDowns()
        {
            int numOfWeeks = GetAttributeValue( AttributeKey.FutureWeeksToShow ).AsIntegerOrNull() ?? 6;

            _listedSundayDates = new List<DateTime>();

            var sundayDate = RockDateTime.Now.SundayDate();
            int weekNum = 0;
            while ( weekNum < numOfWeeks )
            {
                _listedSundayDates.Add( sundayDate );
                weekNum++;
                sundayDate = sundayDate.AddDays( 7 );
            }

            rptWeekSelector.DataSource = _listedSundayDates;
            rptWeekSelector.DataBind();
        }

        /// <summary>
        /// Shows the scheduler.
        /// </summary>
        /// <param name="visible">if set to <c>true</c> [visible].</param>
        private void ShowScheduler( bool visible )
        {
            pnlScheduler.Visible = visible;
        }

        /// <summary>
        /// Updates the list of schedules for the listed groups
        /// </summary>
        private void UpdateScheduleList()
        {
            List<Group> authorizedListedGroups = GetAuthorizedListedGroups( true );

            if ( !authorizedListedGroups.Any() )
            {
                ShowScheduler( false );
                return;
            }

            nbGroupWarning.Visible = false;
            pnlGroupScheduleLocations.Visible = false;
            ShowScheduler( false );

            List<Schedule> groupSchedules = GetGroupSchedules( authorizedListedGroups );

            if ( !groupSchedules.Any() )
            {
                if ( authorizedListedGroups.Count > 1 )
                {
                    nbGroupWarning.Text = "The selected groups do not have any locations or schedules";
                }
                else
                {
                    nbGroupWarning.Text = "The group does not have any locations or schedules";
                }

                nbGroupWarning.Visible = true;
                return;
            }

            pnlGroupScheduleLocations.Visible = true;
            ShowScheduler( true );

            // if a schedule is already selected, set it as the selected schedule (if it still exists for this group)
            var selectedScheduleId = hfSelectedScheduleId.Value.AsIntegerOrNull();

            var listedSchedules = groupSchedules.ToList();

            // add null for "All Schedules"
            listedSchedules.Insert( 0, null );

            rptScheduleSelector.DataSource = listedSchedules;
            rptScheduleSelector.DataBind();

            if ( selectedScheduleId.HasValue && listedSchedules.Any( s => s != null && s.Id == selectedScheduleId.Value ) )
            {
                hfSelectedScheduleId.Value = selectedScheduleId.ToString();
            }
            else
            {
                hfSelectedScheduleId.Value = "all";
            }
        }

        /// <summary>
        /// Gets the group schedules for the listed groups
        /// </summary>
        /// <param name="listedGroups">The listed groups.</param>
        /// <returns></returns>
        private static List<Schedule> GetGroupSchedules( List<Group> listedGroups )
        {
            if ( !listedGroups.Any() )
            {
                return new List<Schedule>();
            }

            var groupLocations = listedGroups.SelectMany( g => g.GroupLocations ).Distinct().ToList();

            var groupSchedules = groupLocations
                .Where( gl => gl.Location.IsActive )
                .SelectMany( gl => gl.Schedules )
                .Where( s => s.IsActive )
                .DistinctBy( a => a.Guid )
                .ToList();

            groupSchedules = groupSchedules.OrderByOrderAndNextScheduledDateTime();

            return groupSchedules;
        }

        /// <summary>
        /// Gets the currently selected group.
        /// </summary>
        /// <returns></returns>
        private Group GetCurrentlySelectedGroup()
        {
            var groupId = hfSelectedGroupId.Value.AsIntegerOrNull();
            var rockContext = new RockContext();
            Group group = null;
            if ( groupId.HasValue )
            {
                group = new GroupService( rockContext ).GetNoTracking( groupId.Value );
            }

            return group;
        }

        /// <summary>
        /// Gets the authorized listed groups, with an option to show a warning if there are some unauthorized groups
        /// </summary>
        /// <returns></returns>
        private List<Group> GetAuthorizedListedGroups( bool showWarnings )
        {
            string warning = string.Empty;

            // get the selected listed groups (not including ones determined from IncludeChildGroups)
            var listedGroupIds = ( hfListedGroupIds.Value ?? "" ).Split( ',' ).AsIntegerList();

            var rockContext = new RockContext();
            var groupService = new GroupService( rockContext );

            // if the ShowChildGroups option is enabled, also include those
            List<int> childGroupIds = new List<int>();
            if ( cbShowChildGroups.Checked )
            {
                childGroupIds = groupService.Queryable()
                    .Where( a =>
                        a.IsActive &&
                        a.ParentGroupId.HasValue &&
                        listedGroupIds.Contains( a.ParentGroupId.Value ) )
                    .Select( a => a.Id ).ToList();
            }

            List<int> groupIdsToQuery = listedGroupIds.Union( childGroupIds ).ToList();

            var listedGroups = new GroupService( rockContext ).GetByIds( groupIdsToQuery ).AsNoTracking().ToList();

            var authorizedGroups = listedGroups.Where( g =>
            {
                var isAuthorized = g.IsAuthorized( Authorization.EDIT, this.CurrentPerson ) || g.IsAuthorized( Authorization.SCHEDULE, this.CurrentPerson );
                return isAuthorized;
            } ).ToList();

            var authorizedGroupCount = authorizedGroups.Count();
            var listedGroupCount = listedGroups.Count();

            if ( listedGroupCount > 0 )
            {
                if ( authorizedGroupCount == 0 )
                {
                    warning = "You're not authorized to schedule resources for these groups";
                }
                else
                {
                    warning = "You're not authorized to schedule resources for some of these groups";
                }
            }

            if ( showWarnings && warning.IsNotNullOrWhiteSpace() )
            {
                nbNotice.Text = string.Format( "<p>{0}</p>", warning );
                nbNotice.NotificationBoxType = NotificationBoxType.Warning;
                nbNotice.Dismissable = true;
                nbNotice.Visible = true;
            }
            else
            {
                nbNotice.Visible = false;
            }

            return authorizedGroups;
        }

        /// <summary>
        /// Loads the filter from user preferences or the URL
        /// </summary>
        private void LoadFilterFromUserPreferencesOrURL()
        {
            DateTime selectedSundayDate = this.GetUrlSettingOrBlockUserPreference( UserPreferenceKey.SelectedDate ).AsDateTime() ?? RockDateTime.Now.SundayDate();
            if ( _listedSundayDates != null && _listedSundayDates.Contains( selectedSundayDate ) )
            {
                hfWeekSundayDate.Value = selectedSundayDate.ToISO8601DateString();
            }
            else
            {
                hfWeekSundayDate.Value = RockDateTime.Now.SundayDate().ToISO8601DateString();
            }

            // if there is a 'GroupIds' parameter, that defines what groups are shown.
            // However, only one group can be selected/active at a time
            List<int> pageParameterListedGroupIds = ( this.PageParameter( PageParameterKey.ListedGroupIds ) ?? string.Empty ).Split( ',' ).AsIntegerList();

            // if there is a 'GroupId' parameter that will define active/selected group
            int? pageParameterGroupId = this.PageParameter( PageParameterKey.GroupId ).AsIntegerOrNull();

            if ( !pageParameterGroupId.HasValue && pageParameterListedGroupIds.Any() )
            {
                // if there isn't a specific group specified, default to the first one
                pageParameterGroupId = pageParameterListedGroupIds.FirstOrDefault();
            }

            if ( pageParameterGroupId.HasValue )
            {
                // if there is a GroupId specified, but it isn't in the listed groups, add it to the list
                if ( !pageParameterListedGroupIds.Contains( pageParameterGroupId.Value ) )
                {
                    pageParameterListedGroupIds.Add( pageParameterGroupId.Value );
                }
            }

            int? selectedGroupId;
            List<int> listedGroupIds;

            if ( pageParameterGroupId.HasValue )
            {
                selectedGroupId = pageParameterGroupId;
                listedGroupIds = pageParameterListedGroupIds;

                // disable the groups picker if groupId(s) are specified in the URL
                gpListedGroups.Enabled = false;
                cbShowChildGroups.Enabled = false;
            }
            else
            {
                selectedGroupId = this.GetUrlSettingOrBlockUserPreference( UserPreferenceKey.SelectedGroupId ).AsIntegerOrNull();
                listedGroupIds = ( this.GetUrlSettingOrBlockUserPreference( UserPreferenceKey.ListedGroupIds ) ?? "" ).Split( ',' ).AsIntegerList();
                if ( selectedGroupId.HasValue && !listedGroupIds.Contains( selectedGroupId.Value ) )
                {
                    listedGroupIds.Add( selectedGroupId.Value );
                }
            }

            gpListedGroups.SetValues( listedGroupIds );

            hfSelectedGroupId.Value = selectedGroupId.ToString();
            hfListedGroupIds.Value = listedGroupIds.AsDelimited( "," );

            UpdateScheduleList();
            bool selectAllSchedules = this.GetUrlSettingOrBlockUserPreference( UserPreferenceKey.SelectAllSchedules ).AsBoolean();
            int? selectedIndividualScheduleId = this.GetUrlSettingOrBlockUserPreference( UserPreferenceKey.SelectedIndividualScheduleId ).AsIntegerOrNull();

            if ( selectAllSchedules )
            {
                hfSelectedScheduleId.Value = "all";
            }
            else
            {
                hfSelectedScheduleId.Value = selectedIndividualScheduleId.ToString();
            }

            UpdateGroupLocationList();
            hfSelectedGroupLocationIds.Value = this.GetUrlSettingOrBlockUserPreference( UserPreferenceKey.SelectedGroupLocationIds );

            var resourceListSourceType = this.GetUrlSettingOrBlockUserPreference( UserPreferenceKey.SelectedResourceListSourceType ).ConvertToEnumOrNull<SchedulerResourceListSourceType>() ?? SchedulerResourceListSourceType.GroupMembers;
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
            var authorizedListedGroups = this.GetAuthorizedListedGroups( false );

            List<int> listedGroupIds = authorizedListedGroups.Select( a => a.Id ).ToList();

            int? selectedGroupId = null;
            var selectedGroup = this.GetCurrentlySelectedGroup();
            if ( selectedGroup != null )
            {
                selectedGroupId = selectedGroup.Id;
            }

            List<int> scheduleIds = GetSelectedScheduleIds();

            var sundayDate = hfWeekSundayDate.Value.AsDateTime() ?? RockDateTime.Now.SundayDate();

            lWeekFilterText.Text = string.Format( "<i class='fa fa-calendar-alt'></i> Week: {0}", sundayDate.ToShortDateString() );

            this.SetBlockUserPreference( UserPreferenceKey.SelectedGroupId, selectedGroupId.ToString() );
            this.SetBlockUserPreference( UserPreferenceKey.ListedGroupIds, listedGroupIds.AsDelimited( "," ).ToString() );
            this.SetBlockUserPreference( UserPreferenceKey.SelectedDate, sundayDate.ToISO8601DateString() );

            this.SetBlockUserPreference( UserPreferenceKey.SelectedGroupLocationIds, hfSelectedGroupLocationIds.Value );
            bool selectAllSchedules = hfSelectedScheduleId.Value.AsIntegerOrNull() == null;
            int? selectedScheduleId = hfSelectedScheduleId.Value.AsIntegerOrNull();
            this.SetBlockUserPreference( UserPreferenceKey.SelectAllSchedules, selectAllSchedules.ToString() );
            this.SetBlockUserPreference( UserPreferenceKey.SelectedIndividualScheduleId, selectedScheduleId.ToString() );

            var rockContext = new RockContext();

            Schedule selectedSchedule = null;
            if ( selectedScheduleId.HasValue )
            {
                selectedSchedule = new ScheduleService( rockContext ).GetNoTracking( selectedScheduleId.Value );
            }

            string selectedSchedulesText;

            if ( selectAllSchedules || selectedSchedule == null )
            {
                selectedSchedulesText = "All Schedules";
            }
            else
            {
                if ( selectedSchedule.Name.IsNotNullOrWhiteSpace() )
                {
                    selectedSchedulesText = selectedSchedule.Name;
                }
                else
                {
                    selectedSchedulesText = selectedSchedule.ToFriendlyScheduleText();
                }
            }

            lScheduleFilterText.Text = string.Format( "<i class='fa fa-clock-o'></i>  {0}", selectedSchedulesText );

            var resourceListSourceType = ( SchedulerResourceListSourceType ) hfSchedulerResourceListSourceType.Value.AsInteger();
            var groupMemberFilterType = ( SchedulerResourceGroupMemberFilterType ) hfResourceGroupMemberFilterType.Value.AsInteger();

            List<SchedulerResourceListSourceType> schedulerResourceListSourceTypes = Enum.GetValues( typeof( SchedulerResourceListSourceType ) ).OfType<SchedulerResourceListSourceType>().ToList();

            if ( selectedGroup != null && selectedGroup.SchedulingMustMeetRequirements )
            {
                var sameGroupSourceTypes = new SchedulerResourceListSourceType[] { SchedulerResourceListSourceType.GroupMembers, SchedulerResourceListSourceType.GroupMatchingPreference };

                // don't show options for other groups or people if SchedulingMustMeetRequirements
                // this is because people from other groups wouldn't meet scheduling requirements (since they aren't in the same group as the Attendance Occurrence)
                schedulerResourceListSourceTypes = sameGroupSourceTypes.ToList();

                if ( !sameGroupSourceTypes.Contains( resourceListSourceType ) )
                {
                    resourceListSourceType = SchedulerResourceListSourceType.GroupMembers;
                    SetResourceListSourceType( resourceListSourceType, groupMemberFilterType );
                }
            }
            else
            {
                if ( selectedGroup == null || !selectedGroup.ParentGroupId.HasValue )
                {
                    schedulerResourceListSourceTypes = schedulerResourceListSourceTypes.Where( a => a != SchedulerResourceListSourceType.ParentGroup ).ToList();
                }

                pnlAddPerson.Visible = true;
                ppAddPerson.Visible = true;
            }

            rptSchedulerResourceListSourceType.DataSource = schedulerResourceListSourceTypes;
            rptSchedulerResourceListSourceType.DataBind();

            this.SetBlockUserPreference( UserPreferenceKey.SelectedResourceListSourceType, resourceListSourceType.ToString() );
            this.SetBlockUserPreference( UserPreferenceKey.GroupMemberFilterType, groupMemberFilterType.ToString() );
            this.SetBlockUserPreference( UserPreferenceKey.AlternateGroupId, gpResourceListAlternateGroup.SelectedValue );
            this.SetBlockUserPreference( UserPreferenceKey.DataViewId, dvpResourceListDataView.SelectedValue );

            pnlResourceFilterAlternateGroup.Visible = resourceListSourceType == SchedulerResourceListSourceType.AlternateGroup;
            pnlResourceFilterDataView.Visible = resourceListSourceType == SchedulerResourceListSourceType.DataView;

            var listedGroupLocations = GetListedGroupLocations( authorizedListedGroups, scheduleIds );
            var selectedGroupLocationIds = hfSelectedGroupLocationIds.Value.Split( ',' ).AsIntegerList();

            // fix up the list of selectedGroupLocationIds to only include ones that are listed
            selectedGroupLocationIds = selectedGroupLocationIds.Where( a => listedGroupLocations.Any( l => l.Id == a ) ).ToList();
            hfSelectedGroupLocationIds.Value = selectedGroupLocationIds.AsDelimited( "," );

            List<GroupLocation> selectedGroupLocations = new GroupLocationService( rockContext )
                .GetByIds( selectedGroupLocationIds )
                .OrderBy( a => a.Order )
                .ThenBy( a => a.Location.Name )
                .AsNoTracking()
                .ToList();

            bool selectAllLocations = false;

            string selectedGroupLocationFilterText;

            if ( !selectedGroupLocations.Any() )
            {
                selectedGroupLocationFilterText = "All Locations";
                selectedGroupLocationIds = listedGroupLocations.Where( a => a != null ).Select( a => a.Id ).ToList();
                hfSelectedGroupLocationIds.Value = selectedGroupLocationIds.AsDelimited( "," );
                selectAllLocations = true;
            }
            else if ( selectedGroupLocations.Count() == 1 )
            {
                selectedGroupLocationFilterText = selectedGroupLocations.First().Location.ToString();
            }
            else
            {
                selectedGroupLocationFilterText = string.Format(
                    "<span title='{0}'>{1} (+{2})</span>",
                    selectedGroupLocations.Select( a => a.Location.ToString() ).ToList().AsDelimited( ", " ).EncodeHtml(),
                    selectedGroupLocations.First().Location.ToString(),
                    selectedGroupLocations.Count() - 1 );
            }

            lSelectedGroupLocationFilterText.Text = string.Format( "<i class='fa fa-building'></i> {0}", selectedGroupLocationFilterText );

            var groupLocationButtons = rptGroupLocationSelector.ControlsOfTypeRecursive<LinkButton>().ToList();
            var groupLocationService = new GroupLocationService( rockContext );
            foreach ( var groupLocationButton in groupLocationButtons )
            {
                var groupLocationId = groupLocationButton.CommandArgument.AsIntegerOrNull();
                if ( groupLocationId.HasValue )
                {
                    var groupLocation = listedGroupLocations.Where( a => a != null ).Where( a => a.Id == groupLocationId.Value ).FirstOrDefault();
                    if ( groupLocation != null )
                    {
                        if ( selectedGroupLocationIds.Contains( groupLocationId.Value ) )
                        {
                            groupLocationButton.Text = string.Format( "<i class='fa fa-check'></i> {0}", groupLocation.Location.ToString().EncodeHtml() );
                        }
                        else
                        {
                            groupLocationButton.Text = string.Format( " {0}", groupLocation.Location.ToString() );
                        }
                    }
                }
                else
                {
                    // all locations button
                    if ( selectAllLocations )
                    {
                        groupLocationButton.Text = "<i class='fa fa-check'></i> All Locations";
                    }
                    else
                    {
                        groupLocationButton.Text = " All Locations";
                    }
                }
            }

            bool filterIsValid = authorizedListedGroups.Any() && scheduleIds.Any() && selectedGroupLocationIds.Any();
            ShowScheduler( filterIsValid && !selectedGroup.DisableScheduling );
            nbFilterInstructions.Visible = !filterIsValid;

            var disableSchedulingForSelectedGroup = selectedGroup != null && selectedGroup.DisableScheduling;
            nbSchedulingDisabled.Visible = disableSchedulingForSelectedGroup;

            if ( disableSchedulingForSelectedGroup )
            {
                nbSchedulingDisabled.Text = string.Format( "Scheduling is disabled for the {0} group.", selectedGroup.Name );
            }

            pnlGroupScheduleLocations.Visible = authorizedListedGroups.Any();

            if ( filterIsValid && !selectedGroup.DisableScheduling )
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
            var selectedScheduleId = hfSelectedScheduleId.Value.AsIntegerOrNull();
            if ( selectedScheduleId.HasValue )
            {
                var selectedScheduleIds = new List<int>();
                selectedScheduleIds.Add( selectedScheduleId.Value );
                return selectedScheduleIds;
            }

            var authorizedListedGroups = GetAuthorizedListedGroups( false );
            var scheduleList = GetGroupSchedules( authorizedListedGroups );

            if ( scheduleList != null )
            {
                return scheduleList.Where( a => a != null ).Select( a => a.Id ).ToList();
            }
            else
            {
                return new List<int>();
            }
        }

        /// <summary>
        /// Updates the list of group locations for the selected group
        /// </summary>
        private void UpdateGroupLocationList()
        {
            var authorizedListedGroups = GetAuthorizedListedGroups( true );

            if ( !authorizedListedGroups.Any() )
            {
                ShowScheduler( false );
                return;
            }

            if ( authorizedListedGroups.Any() )
            {
                ShowScheduler( true );
                List<int> scheduleIds = GetSelectedScheduleIds();

                var listedGroupLocations = GetListedGroupLocations( authorizedListedGroups, scheduleIds );

                if ( !listedGroupLocations.Any() && scheduleIds.Any() )
                {
                    if ( authorizedListedGroups.Count > 1 )
                    {
                        nbGroupWarning.Text = "The selected groups do not have any locations or schedules";
                    }
                    else
                    {
                        nbGroupWarning.Text = "The group does not have any locations or schedules";
                    }

                    nbGroupWarning.Visible = true;
                }
                else if ( scheduleIds.Any() )
                {
                    nbGroupWarning.Visible = false;
                }

                listedGroupLocations.Insert( 0, null );

                rptGroupLocationSelector.DataSource = listedGroupLocations;
                rptGroupLocationSelector.DataBind();
            }
        }

        /// <summary>
        /// Gets the listed group locations.
        /// </summary>
        /// <param name="group">The group.</param>
        /// <param name="scheduleIds">The schedule ids.</param>
        /// <returns></returns>
        private List<GroupLocation> GetListedGroupLocations( List<Group> listedGroups, List<int> scheduleIds )
        {
            var rockContext = new RockContext();
            var listedGroupIds = listedGroups.Select( a => a.Id ).ToList();

            var groupLocationsQuery = new GroupLocationService( rockContext ).Queryable()
                .Where( gl =>
                    listedGroupIds.Contains( gl.GroupId ) &&
                    gl.Schedules.Any( s => scheduleIds.Contains( s.Id ) ) &&
                    gl.Location.IsActive )
                .OrderBy( a => new { a.Order, a.Location.Name } )
                .AsNoTracking();

            return groupLocationsQuery.ToList();
        }

        /// <summary>
        /// Set the Resource List hidden fields which groupScheduler.js uses to populate the Resource List
        /// </summary>
        private void InitResourceList()
        {
            int groupId = hfSelectedGroupId.Value.AsInteger();
            int? resourceGroupId = null;
            int? resourceDataViewId = null;
            List<int> scheduleIds = GetSelectedScheduleIds();

            hfResourceAdditionalPersonIds.Value = string.Empty;

            var resourceListSourceType = hfSchedulerResourceListSourceType.Value.ConvertToEnum<SchedulerResourceListSourceType>();
            switch ( resourceListSourceType )
            {
                case SchedulerResourceListSourceType.GroupMembers:
                case SchedulerResourceListSourceType.GroupMatchingPreference:
                    {
                        resourceGroupId = groupId;
                        break;
                    }

                case SchedulerResourceListSourceType.AlternateGroup:
                    {
                        resourceGroupId = gpResourceListAlternateGroup.SelectedValue.AsInteger();
                        break;
                    }

                case SchedulerResourceListSourceType.ParentGroup:
                    {
                        var rockContext = new RockContext();
                        resourceGroupId = new GroupService( rockContext ).GetSelect( groupId, s => s.ParentGroupId );
                        break;
                    }

                case SchedulerResourceListSourceType.DataView:
                    {
                        resourceDataViewId = dvpResourceListDataView.SelectedValue.AsInteger();
                        break;
                    }
            }

            hfOccurrenceGroupId.Value = groupId.ToString();
            hfOccurrenceScheduleIds.Value = scheduleIds.AsDelimited( "," );
            hfOccurrenceSundayDate.Value = ( hfWeekSundayDate.Value.AsDateTime() ?? RockDateTime.Now.SundayDate() ).ToISO8601DateString();

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
            var selectedGroupLocationIds = hfSelectedGroupLocationIds.Value.Split( ',' ).AsIntegerList();

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

            var selectedGroupId = hfSelectedGroupId.Value.AsInteger();

            var attendanceOccurrencesOrderedList = attendanceOccurrencesList.OrderBy( a => a.ScheduledDateTime ).ThenBy( a => a.GroupLocationOrder ).ThenBy( a => a.LocationName ).ToList();

            // if there are any people that signed up with no location preference, add the to a special list of "No Location Preference" occurrences to the top of the list
            var unassignedLocationOccurrenceList = attendanceOccurrenceService.Queryable()
                .Where( a => occurrenceDateList.Contains( a.OccurrenceDate )
                    && a.ScheduleId.HasValue
                    && scheduleIds.Contains( a.ScheduleId.Value )
                    && a.GroupId == selectedGroupId
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
        protected void gpListedGroups_ValueChanged( object sender, EventArgs e )
        {
            // the glListedGroups picker selected the groups that we show in the Scheduler, but only one Group can be the active/selected one
            var listedGroupIds = gpListedGroups.SelectedValues.AsIntegerList();

            hfListedGroupIds.Value = listedGroupIds.AsDelimited( "," );
            var selectedGroupId = hfSelectedGroupId.Value.AsIntegerOrNull();
            if ( selectedGroupId.HasValue )
            {
                // if the currently selectedGroupId is not in the updated listedGroupIds, default the selectedGroupId to the first listed Groupid
                if ( !listedGroupIds.Contains( selectedGroupId.Value ) )
                {
                    selectedGroupId = listedGroupIds.FirstOrDefault();
                }
            }
            else
            {
                // if there isn't a currently selected group, default to the first one
                selectedGroupId = listedGroupIds.FirstOrDefault();
            }

            hfSelectedGroupId.Value = selectedGroupId.ToString();

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
            LinkButton btnResourceListSourceType = sender as LinkButton;

            SchedulerResourceListSourceType schedulerResourceListSourceType = ( SchedulerResourceListSourceType ) btnResourceListSourceType.CommandArgument.AsInteger();
            SchedulerResourceGroupMemberFilterType schedulerResourceGroupMemberFilterType = SchedulerResourceGroupMemberFilterType.ShowAllGroupMembers;

            if ( schedulerResourceListSourceType == SchedulerResourceListSourceType.GroupMatchingPreference )
            {
                schedulerResourceGroupMemberFilterType = SchedulerResourceGroupMemberFilterType.ShowMatchingPreference;
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
                case SchedulerResourceListSourceType.GroupMembers:
                case SchedulerResourceListSourceType.GroupMatchingPreference:
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

        /// <summary>
        /// Handles the ItemDataBound event of the rptSchedulerResourceListSourceType control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RepeaterItemEventArgs"/> instance containing the event data.</param>
        protected void rptSchedulerResourceListSourceType_ItemDataBound( object sender, RepeaterItemEventArgs e )
        {
            var btnResourceListSourceType = e.Item.FindControl( "btnResourceListSourceType" ) as LinkButton;
            SchedulerResourceListSourceType schedulerResourceListSourceType = ( SchedulerResourceListSourceType ) e.Item.DataItem;
            btnResourceListSourceType.Text = schedulerResourceListSourceType.GetDescription() ?? schedulerResourceListSourceType.ConvertToString( true );
            btnResourceListSourceType.CommandArgument = schedulerResourceListSourceType.ConvertToInt().ToString();
        }

        protected void rptWeekSelector_ItemDataBound( object sender, RepeaterItemEventArgs e )
        {
            var sundayDate = ( DateTime ) e.Item.DataItem;
            string weekTitle = string.Format( "Week of {0} to {1}", sundayDate.AddDays( -6 ).ToShortDateString(), sundayDate.ToShortDateString() );

            var btnSelectWeek = e.Item.FindControl( "btnSelectWeek" ) as LinkButton;
            btnSelectWeek.Text = weekTitle;
            btnSelectWeek.CommandArgument = sundayDate.ToISO8601DateString();
        }

        protected void btnSelectWeek_Click( object sender, EventArgs e )
        {
            var btnSelectWeek = sender as LinkButton;
            hfWeekSundayDate.Value = btnSelectWeek.CommandArgument;
            ApplyFilter();
        }

        protected void btnSelectSchedule_Click( object sender, EventArgs e )
        {
            var btnSelectSchedule = sender as LinkButton;
            hfSelectedScheduleId.Value = btnSelectSchedule.CommandArgument;

            UpdateGroupLocationList();
            ApplyFilter();
        }

        /// <summary>
        /// Handles the ItemDataBound event of the rptScheduleSelector control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RepeaterItemEventArgs"/> instance containing the event data.</param>
        protected void rptScheduleSelector_ItemDataBound( object sender, RepeaterItemEventArgs e )
        {
            LinkButton btnSelectSchedule = e.Item.FindControl( "btnSelectSchedule" ) as LinkButton;
            var schedule = e.Item.DataItem as Schedule;
            if ( schedule != null )
            {
                btnSelectSchedule.CommandArgument = schedule.Id.ToString();

                if ( schedule.Name.IsNotNullOrWhiteSpace() )
                {
                    btnSelectSchedule.Text = schedule.Name;
                }
                else
                {
                    btnSelectSchedule.Text = schedule.FriendlyScheduleText;
                }
            }
            else
            {
                btnSelectSchedule.CommandArgument = "all";
                btnSelectSchedule.Text = "All Schedules";
            }
        }

        /// <summary>
        /// Handles the Click event of the btnSelectGroupLocation control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnSelectGroupLocation_Click( object sender, EventArgs e )
        {
            var btnSelectGroupLocation = sender as LinkButton;

            var selectedGroupLocationId = btnSelectGroupLocation.CommandArgument.AsIntegerOrNull();

            if ( selectedGroupLocationId.HasValue )
            {
                var selectedGroupLocationIds = hfSelectedGroupLocationIds.Value.Split( ',' ).AsIntegerList();

                // toggle if the selected group location is in the selected group locations
                if ( selectedGroupLocationIds.Contains( selectedGroupLocationId.Value ) )
                {
                    selectedGroupLocationIds.Remove( selectedGroupLocationId.Value );
                }
                else
                {
                    selectedGroupLocationIds.Add( selectedGroupLocationId.Value );
                }

                hfSelectedGroupLocationIds.Value = selectedGroupLocationIds.AsDelimited( "," );
            }
            else
            {
                hfSelectedGroupLocationIds.Value = "all";
            }

            ApplyFilter();
        }

        /// <summary>
        /// Handles the ItemDataBound event of the rptGroupLocationSelector control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RepeaterItemEventArgs"/> instance containing the event data.</param>
        protected void rptGroupLocationSelector_ItemDataBound( object sender, RepeaterItemEventArgs e )
        {
            LinkButton btnSelectGroupLocation = e.Item.FindControl( "btnSelectGroupLocation" ) as LinkButton;
            GroupLocation groupLocation = e.Item.DataItem as GroupLocation;
            if ( groupLocation != null )
            {
                btnSelectGroupLocation.CommandArgument = groupLocation.Id.ToString();
                btnSelectGroupLocation.Text = groupLocation.Location.ToString();
            }
            else
            {
                btnSelectGroupLocation.CommandArgument = "all";
                btnSelectGroupLocation.Text = "All Locations";
            }
        }
    }
}