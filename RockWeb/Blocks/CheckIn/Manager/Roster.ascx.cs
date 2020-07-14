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
using System.Text;
using System.Web.UI.WebControls;

using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Lava;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI.Controls;

namespace RockWeb.Blocks.CheckIn.Manager
{
    /// <summary>
    /// Block used to open and close classrooms, mark a person as 'present' in the classroom, Etc.
    /// </summary>
    [DisplayName( "Roster" )]
    [Category( "Check-in > Manager" )]
    [Description( "Block used to open and close classrooms, mark a person as 'present' in the classroom, Etc." )]

    #region Block Attributes

    [GroupTypeField( "Check-in Type",
        Key = AttributeKey.CheckInAreaGuid,
        Description = "The Check-in Area for the rooms to be managed by this Block. This value can also be overriden through the URL query string 'Area' key (e.g. when navigated to from the Check-in Type selection block).",
        IsRequired = false,
        GroupTypePurposeValueGuid = Rock.SystemGuid.DefinedValue.GROUPTYPE_PURPOSE_CHECKIN_TEMPLATE,
        Order = 1 )]

    [LinkedPage( "Person Page",
        Key = AttributeKey.PersonPage,
        Description = "The page used to display a selected person's details.",
        IsRequired = true,
        Order = 2 )]

    [LinkedPage( "Area Select Page",
        Key = AttributeKey.AreaSelectPage,
        Description = "The page to redirect user to if a Check-in Area has not been configured or selected.",
        IsRequired = true,
        Order = 3 )]

    #endregion Block Attributes

    public partial class Roster : Rock.Web.UI.RockBlock
    {
        #region Attribute Keys

        /// <summary>
        /// Keys to use for block attributes.
        /// </summary>
        private class AttributeKey
        {
            public const string CheckInAreaGuid = "CheckInAreaGuid";
            public const string PersonPage = "PersonPage";
            public const string AreaSelectPage = "AreaSelectPage";
        }

        #endregion Attribute Keys

        #region Page Parameter Keys

        private class PageParameterKey
        {
            public const string Area = "Area";
        }

        #endregion Page Parameter Keys

        #region ViewState Keys

        /// <summary>
        /// Keys to use for ViewState.
        /// </summary>
        private class ViewStateKey
        {
            public const string CurrentCampusId = "CurrentCampusId";
            public const string CurrentLocationId = "CurrentLocationId";
            public const string CheckInAreaGuid = "CheckInAreaGuid";
            public const string AllowCheckout = "AllowCheckout";
            public const string EnablePresence = "EnablePresence";
            public const string CurrentStatusFilter = "CurrentStatusFilter";
        }

        #endregion ViewState Keys

        #region User Preference Keys

        /// <summary>
        /// Keys to use for user preferences.
        /// </summary>
        private class UserPreferenceKey
        {
            public const string LocationId = "LocationId";
            public const string StatusFilter = "StatusFilter";
        }

        #endregion User Preference Keys

        #region Entity Attribute Value Keys

        /// <summary>
        /// Keys to use for entity attribute values.
        /// </summary>
        private class EntityAttributeValueKey
        {
            public const string GroupType_AllowCheckout = "core_checkin_AllowCheckout";
            public const string GroupType_EnablePresence = "core_checkin_EnablePresence";

            public const string Person_Allergy = "Allergy";
            public const string Person_LegalNotes = "LegalNotes";
        }

        #endregion Entity Attribute Value Keys

        #region Properties

        /// <summary>
        /// The current campus identitifier.
        /// </summary>
        public int CurrentCampusId
        {
            get
            {
                return ( ViewState[ViewStateKey.CurrentCampusId] as string ).AsInteger();
            }

            set
            {
                ViewState[ViewStateKey.CurrentCampusId] = value.ToString();
            }
        }

        /// <summary>
        /// The current location identifier.
        /// </summary>
        public int CurrentLocationId
        {
            get
            {
                return ( ViewState[ViewStateKey.CurrentLocationId] as string ).AsInteger();
            }

            set
            {
                ViewState[ViewStateKey.CurrentLocationId] = value.ToString();
            }
        }

        /// <summary>
        /// The location identifier user preference key, taking into consideration the currently-selected campus.
        /// </summary>
        public string LocationIdUserPreferenceKey
        {
            get
            {
                return string.Format( "campus-{0}-{1}", CurrentCampusId, UserPreferenceKey.LocationId );
            }
        }

        /// <summary>
        /// The current area unique identifier.
        /// </summary>
        public Guid? CurrentAreaGuid
        {
            get
            {
                return ( ViewState[ViewStateKey.CheckInAreaGuid] as string ).AsGuidOrNull();
            }

            set
            {
                ViewState[ViewStateKey.CheckInAreaGuid] = value.ToString();
            }
        }

        /// <summary>
        /// Whether to allow checkout.
        /// </summary>
        public bool AllowCheckout
        {
            get
            {
                return ( ViewState[ViewStateKey.AllowCheckout] as string ).AsBoolean();
            }

            set
            {
                ViewState[ViewStateKey.AllowCheckout] = value.ToString();
            }
        }

        /// <summary>
        /// Whether to enable presence.
        /// </summary>
        public bool EnablePresence
        {
            get
            {
                return ( ViewState[ViewStateKey.EnablePresence] as string ).AsBoolean();
            }

            set
            {
                ViewState[ViewStateKey.EnablePresence] = value.ToString();
            }
        }

        /// <summary>
        /// The current status filter.
        /// </summary>
        public StatusFilter CurrentStatusFilter
        {
            get
            {
                StatusFilter statusFilter;
                Enum.TryParse( ViewState[ViewStateKey.CurrentStatusFilter] as string, out statusFilter );

                return statusFilter;
            }

            set
            {
                ViewState[ViewStateKey.CurrentStatusFilter] = value.ToString();
            }
        }

        /// <summary>
        /// The status filter user preference key, taking into consideration the currently-selected campus.
        /// </summary>
        public string StatusFilterUserPreferenceKey
        {
            get
            {
                //return string.Format( "campus-{0}-{1}", CurrentCampusId, UserPreferenceKey.StatusFilter );

                // While it makes sense for the location ID user preference to be per-campus, this preference should span campuses.
                return UserPreferenceKey.StatusFilter;
            }
        }

        #endregion Properties

        #region Base Control Methods

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

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

            BuildRoster();
        }

        #endregion Base Control Methods

        #region Control Events

        /// <summary>
        /// Handles the BlockUpdated event of the control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void Block_BlockUpdated( object sender, EventArgs e )
        {
            // Wipe out this value to trigger the reloading of the Area-related properties.
            CurrentAreaGuid = null;

            BuildRoster();
        }

        /// <summary>
        /// Handles the SelectLocation event of the lpLocation control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void lpLocation_SelectLocation( object sender, EventArgs e )
        {
            Location location = lpLocation.Location;
            if ( location != null )
            {
                SetBlockUserPreference( LocationIdUserPreferenceKey, location.Id.ToString(), true );
            }
            else
            {
                DeleteBlockUserPreference( LocationIdUserPreferenceKey );
            }
        }

        /// <summary>
        /// Handles the SelectedIndexChanged event of the bgStatus control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void bgStatus_SelectedIndexChanged( object sender, EventArgs e )
        {
            StatusFilter statusFilter = GetStatusFilterValueFromControl();
            SetBlockUserPreference( StatusFilterUserPreferenceKey, statusFilter.ToString( "d" ), true );
        }

        /// <summary>
        /// Handles the RowDataBound event of the gAttendees control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="GridViewRowEventArgs"/> instance containing the event data.</param>
        protected void gAttendees_RowDataBound( object sender, GridViewRowEventArgs e )
        {
            if ( e.Row.RowType != DataControlRowType.DataRow )
            {
                return;
            }

            Attendee attendee = e.Row.DataItem as Attendee;

            var lImage = e.Row.FindControl( "lImage" ) as Literal;
            if ( lImage != null )
            {
                lImage.Text = Rock.Model.Person.GetPersonPhotoImageTag( attendee.PersonId, attendee.PhotoId, attendee.Age, attendee.Gender, null, 50, 50 );
            }

            var lName = e.Row.FindControl( "lName" ) as Literal;
            if ( lName != null )
            {
                lName.Text = string.Format( @"<b><span class=""js-checkin-person-name"">{0}</span></b><br>{1}", attendee.Name, attendee.ParentNames.IsNotNullOrWhiteSpace() ? attendee.ParentNames : "&nbsp;" );
            }

            var lIcons = e.Row.FindControl( "lIcons" ) as Literal;
            if ( lIcons != null )
            {
                var iconsSb = new StringBuilder();

                if ( attendee.IsBirthdayWeek )
                {
                    iconsSb.AppendFormat( @"<span class=""text-success""><i class=""fa fa-2x fa-birthday-cake""></i><br><span class=""tbd"">{0}</span></span>", attendee.Birthday );
                }

                if ( attendee.HasHealthNote )
                {
                    iconsSb.Append( @"<i class=""fa fa-2x fa-notes-medical text-danger""></i>" );
                }

                if ( attendee.HasLegalNote )
                {
                    iconsSb.Append( @"<i class=""fa fa-2x fa-clipboard""></i>" );
                }

                lIcons.Text = iconsSb.ToString();
            }

            var lCheckInTime = e.Row.FindControl( "lCheckInTime" ) as Literal;
            if ( lCheckInTime != null )
            {
                lCheckInTime.Text = RockFilters.HumanizeTimeSpan( attendee.CheckInTime, DateTime.Now, unit: "Second" );
            }
        }

        /// <summary>
        /// Handles the RowSelected event of the gAttendees control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="Rock.Web.UI.Controls.RowEventArgs"/> instance containing the event data.</param>
        protected void gAttendees_RowSelected( object sender, Rock.Web.UI.Controls.RowEventArgs e )
        {
            string personGuid = e.RowKeyValues[0].ToString();
            if ( !NavigateToLinkedPage( AttributeKey.PersonPage, new Dictionary<string, string> { { "Person", personGuid } } ) )
            {
                ShowWarningMessage( "The 'Person Page' Block Attribute must be defined.", true );
            }
        }

        /// <summary>
        /// Handles the Click event of the lbCancel control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="Rock.Web.UI.Controls.RowEventArgs"/> instance containing the event data.</param>
        protected void lbCancel_Click( object sender, RowEventArgs e )
        {
            var attendanceIds = e.RowKeyValues[1] as List<int>;
            if ( !attendanceIds.Any() )
            {
                return;
            }

            using ( var rockContext = new RockContext() )
            {
                var attendanceService = new AttendanceService( rockContext );
                foreach ( var attendance in attendanceService
                    .Queryable()
                    .Where( a => attendanceIds.Contains( a.Id ) ) )
                {
                    attendanceService.Delete( attendance );
                }

                rockContext.SaveChanges();

                // Reset the cache for this Location so the kiosk will show the correct counts.
                Rock.CheckIn.KioskLocationAttendance.Remove( CurrentLocationId );
            }

            ShowAttendees();
        }

        /// <summary>
        /// Handles the Click event of the lbCancel control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="Rock.Web.UI.Controls.RowEventArgs"/> instance containing the event data.</param>
        protected void lbPresent_Click( object sender, RowEventArgs e )
        {
            var attendanceIds = e.RowKeyValues[1] as List<int>;
            if ( !attendanceIds.Any() )
            {
                return;
            }

            using ( var rockContext = new RockContext() )
            {
                var now = RockDateTime.Now;
                var attendanceService = new AttendanceService( rockContext );
                foreach ( var attendee in attendanceService
                    .Queryable()
                    .Where( a => attendanceIds.Contains( a.Id ) ) )
                {
                    attendee.PresentDateTime = now;
                    attendee.PresentByPersonAliasId = CurrentPersonAliasId;
                }

                rockContext.SaveChanges();
            }

            ShowAttendees();
        }

        /// <summary>
        /// Handles the Click event of the lbCheckOut control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="Rock.Web.UI.Controls.RowEventArgs"/> instance containing the event data.</param>
        protected void lbCheckOut_Click( object sender, RowEventArgs e )
        {
            var attendanceIds = e.RowKeyValues[1] as List<int>;
            if ( !attendanceIds.Any() )
            {
                return;
            }

            using ( var rockContext = new RockContext() )
            {
                var now = RockDateTime.Now;
                var attendanceService = new AttendanceService( rockContext );
                foreach ( var attendee in attendanceService
                    .Queryable()
                    .Where( a => attendanceIds.Contains( a.Id ) ) )
                {
                    attendee.EndDateTime = now;
                    attendee.CheckedOutByPersonAliasId = CurrentPersonAliasId;
                }

                rockContext.SaveChanges();
            }

            ShowAttendees();
        }

        #endregion Control Events

        #region Internal Methods

        /// <summary>
        /// Builds the roster for the selected campus and location.
        /// </summary>
        private void BuildRoster()
        {
            nbWarning.Visible = false;

            if ( !SetArea() )
            {
                return;
            }

            CampusCache campus = GetCampusFromContext();
            if ( campus == null )
            {
                ShowWarningMessage( "Please select a Campus.", true );
                return;
            }

            // If the Campus selection has changed, we need to reload the LocationItemPicker with the Locations specific to that Campus.
            if ( campus.Id != CurrentCampusId )
            {
                CurrentCampusId = campus.Id;
                lpLocation.NamedPickerRootLocationId = campus.LocationId.GetValueOrDefault();
            }

            // Check the LocationPicker for the Location ID.
            int locationId = lpLocation.Location != null
                ? lpLocation.Location.Id
                : 0;

            if ( locationId <= 0 )
            {
                // If not defined on the LocationPicker, check for a Block user preference.
                locationId = GetBlockUserPreference( LocationIdUserPreferenceKey ).AsInteger();

                if ( locationId <= 0 )
                {
                    ShowWarningMessage( "Please select a Location.", false );
                    return;
                }

                SetLocation( locationId );
            }

            // Check the ButtonGroup for the StatusFilter value.
            StatusFilter statusFilter = GetStatusFilterValueFromControl();
            if ( statusFilter == StatusFilter.Unknown )
            {
                // If not defined on the ButtonGroup, check for a Block user preference.
                Enum.TryParse( GetBlockUserPreference( StatusFilterUserPreferenceKey ), out statusFilter );

                if ( statusFilter == StatusFilter.Unknown )
                {
                    // If we still don't know the value, set it to 'All'.
                    statusFilter = StatusFilter.All;
                }

                SetStatusFilter( statusFilter );
            }

            // If the Location or StatusFilter selections have changed, we need to reload the attendees.
            if ( locationId != CurrentLocationId || statusFilter != CurrentStatusFilter )
            {
                CurrentLocationId = locationId;
                CurrentStatusFilter = statusFilter;

                ShowAttendees();
            }
        }

        /// <summary>
        /// Sets the area.
        /// </summary>
        private bool SetArea()
        {
            if ( CurrentAreaGuid.HasValue )
            {
                // We have already set the Area-related properties on initial page load and placed them in ViewState.
                return true;
            }

            // If a query string parameter is defined, it takes precedence.
            Guid? areaGuid = PageParameter( PageParameterKey.Area ).AsGuidOrNull();

            if ( !areaGuid.HasValue )
            {
                // Next, check the Block AttributeValue.
                areaGuid = this.GetAttributeValue( AttributeKey.CheckInAreaGuid ).AsGuidOrNull();
            }

            if ( !areaGuid.HasValue )
            {
                // Finally, fall back to the Weekly Service Check-in system Guid.
                areaGuid = Rock.SystemGuid.GroupType.GROUPTYPE_WEEKLY_SERVICE_CHECKIN_AREA.AsGuidOrNull();
            }

            // TODO(JH) - Discuss with Nick, as this is no longer needed based on the fallback above.
            if ( !areaGuid.HasValue )
            {
                if ( !NavigateToLinkedPage( AttributeKey.AreaSelectPage ) )
                {
                    ShowWarningMessage( "The 'Area Select Page' Block Attribute must be defined.", true );
                }

                return false;
            }

            // Save the Area Guid in ViewState.
            CurrentAreaGuid = areaGuid;

            // Get the GroupType represented by the Check-in Area Guid Block Attribute so we can set the related runtime properties.
            using ( var rockContext = new RockContext() )
            {
                GroupType area = new GroupTypeService( rockContext ).Get( areaGuid.Value );
                if ( area == null )
                {
                    ShowWarningMessage( "The specified Check-in Area is not valid.", true );
                    return false;
                }

                area.LoadAttributes( rockContext );

                // Save the following Area-related values in ViewState.
                EnablePresence = area.GetAttributeValue( EntityAttributeValueKey.GroupType_EnablePresence ).AsBoolean();
                AllowCheckout = area.GetAttributeValue( EntityAttributeValueKey.GroupType_AllowCheckout ).AsBoolean();
            }

            return true;
        }

        /// <summary>
        /// Gets the campus from the current context.
        /// </summary>
        private CampusCache GetCampusFromContext()
        {
            CampusCache campus = null;

            var campusEntityType = EntityTypeCache.Get( "Rock.Model.Campus" );
            if ( campusEntityType != null )
            {
                var campusContext = RockPage.GetCurrentContext( campusEntityType ) as Campus;

                // TODO(JH) - Do we want to fall back to the first campus?
                //campus = campusContext != null
                //    ? CampusCache.Get( campusContext )
                //    : CampusCache.All().FirstOrDefault();

                campus = CampusCache.Get( campusContext );
            }

            return campus;
        }

        /// <summary>
        /// Shows a warning message, and optionally hides the main content panel.
        /// </summary>
        /// <param name="warningMessage">The warning message to show.</param>
        /// <param name="hideContentPanel">Whether to hide the main content panel.</param>
        private void ShowWarningMessage( string warningMessage, bool hideContentPanel )
        {
            nbWarning.Text = warningMessage;
            nbWarning.Visible = true;
            pnlContent.Visible = !hideContentPanel;
        }

        /// <summary>
        /// Sets the value of the lpLocation control.
        /// </summary>
        /// <param name="locationId">The identifier of the location.</param>
        private void SetLocation( int locationId )
        {
            using ( var rockContext = new RockContext() )
            {
                Location location = new LocationService( rockContext ).Get( locationId );
                if ( location != null )
                {
                    lpLocation.Location = location;
                }
            }
        }

        /// <summary>
        /// Gets the status filter value from the bgStatus control.
        /// </summary>
        /// <returns></returns>
        private StatusFilter GetStatusFilterValueFromControl()
        {
            StatusFilter statusFilter;
            Enum.TryParse( bgStatus.SelectedValue, out statusFilter );

            return statusFilter;
        }

        /// <summary>
        /// Sets the value of the bgStatus control.
        /// </summary>
        /// <param name="statusFilter">The status filter.</param>
        private void SetStatusFilter( StatusFilter statusFilter )
        {
            bgStatus.SelectedValue = statusFilter.ToString( "d" );
        }

        /// <summary>
        /// Shows the attendees.
        /// </summary>
        private void ShowAttendees()
        {
            IList<Attendee> attendees = null;

            using ( var rockContext = new RockContext() )
            {
                RemoveDisabledStatusFilters();

                attendees = GetAttendees( rockContext );
            }

            ToggleColumnVisibility();

            gAttendees.DataSource = attendees;
            gAttendees.DataBind();
        }

        /// <summary>
        /// Removes the disabled status filters.
        /// </summary>
        private void RemoveDisabledStatusFilters()
        {
            if ( !EnablePresence )
            {
                // When EnablePresence is false for a given Check-in Area, the [Attendance].[PresentDateTime] value will have already been set upon check-in.

                if ( !AllowCheckout )
                {
                    // When both EnablePresence and AllowCheckout are false, it doesn't make sense to show the status filters at all.
                    bgStatus.Visible = false;
                    CurrentStatusFilter = StatusFilter.Present;

                    return;
                }

                // Reset the visibility, just in case the control was previously hidden.
                bgStatus.Visible = true;

                // If EnablePresence is false, it doesn't make sense to show the 'Checked-in' filter.
                var checkedInItem = bgStatus.Items.FindByValue( StatusFilter.CheckedIn.ToString( "d" ) );
                if ( checkedInItem != null )
                {
                    bgStatus.Items.Remove( checkedInItem );
                }

                if ( CurrentStatusFilter == StatusFilter.CheckedIn )
                {
                    CurrentStatusFilter = StatusFilter.Present;
                    SetStatusFilter( CurrentStatusFilter );
                }
            }
        }

        /// <summary>
        /// Gets the attendees.
        /// </summary>
        private IList<Attendee> GetAttendees( RockContext rockContext )
        {
            var attendees = new List<Attendee>();

            var startDateTime = RockDateTime.Today;
            var now = GetCampusTime();

            // Get all Attendance records for the current day and location.
            var attendanceQuery = new AttendanceService( rockContext )
                .Queryable( "AttendanceCode,PersonAlias.Person,Occurrence.Schedule" )
                .AsNoTracking()
                .Where( a => a.StartDateTime >= startDateTime )
                .Where( a => a.StartDateTime <= now )
                .Where( a => a.PersonAliasId.HasValue )
                .Where( a => a.Occurrence.LocationId == CurrentLocationId )
                .Where( a => a.Occurrence.ScheduleId.HasValue );

            /*
                If StatusFilter == All, no further filtering is needed.
                If StatusFilter == Checked-in, only retrieve records that have neither a EndDateTime nor a PresentDateTime value.
                If StatusFilter == Present, only retrieve records that have a PresentDateTime value but don't have a EndDateTime value.
            */

            if ( CurrentStatusFilter == StatusFilter.CheckedIn )
            {
                attendanceQuery = attendanceQuery
                    .Where( a => !a.PresentDateTime.HasValue )
                    .Where( a => !a.EndDateTime.HasValue );
            }
            else if ( CurrentStatusFilter == StatusFilter.Present )
            {
                attendanceQuery = attendanceQuery
                    .Where( a => a.PresentDateTime.HasValue )
                    .Where( a => !a.EndDateTime.HasValue );
            }

            List<Attendance> attendances = attendanceQuery.ToList();

            // Remove all whose schedules (and check-ins) are not currently active.
            attendances = attendances.Where( a => a.Occurrence.Schedule.WasScheduleOrCheckInActive( RockDateTime.Now ) ).ToList();

            foreach ( var attendance in attendances.Where( a => a.PersonAlias != null && a.PersonAlias.Person != null ) )
            {
                // Create an Attendee for each unique Person within the Attendance records.
                var person = attendance.PersonAlias.Person;

                Attendee attendee = attendees.FirstOrDefault( a => a.PersonGuid == person.Guid );
                if ( attendee == null )
                {
                    attendee = CreateAttendee( rockContext, person );
                    attendees.Add( attendee );
                }

                // Add the attendance-specific property values.
                SetAttendanceInfo( attendance, attendee );
            }

            return attendees;
        }

        /// <summary>
        /// Gets the current campus time.
        /// </summary>
        private DateTime GetCampusTime()
        {
            CampusCache campusCache = CampusCache.Get( CurrentCampusId );
            return campusCache != null
                ? campusCache.CurrentDateTime
                : RockDateTime.Now;
        }

        /// <summary>
        /// Creates an attendee.
        /// </summary>
        /// <param name="rockContext">The rock context.</param>
        /// <param name="person">The person.</param>
        private Attendee CreateAttendee( RockContext rockContext, Rock.Model.Person person )
        {
            person.LoadAttributes( rockContext );

            var attendee = new Attendee
            {
                PersonId = person.Id,
                PersonGuid = person.Guid,
                Name = person.FullName,
                ParentNames = Rock.Model.Person.GetFamilySalutation( person, finalSeparator: "and" ),
                PhotoId = person.PhotoId,
                Age = person.Age,
                Gender = person.Gender,
                Birthday = GetBirthday( person ),
                HasHealthNote = GetHasHealthNote( person ),
                HasLegalNote = GetHasLegalNote( person )
            };

            return attendee;
        }

        /// <summary>
        /// Gets the birthday (abbreviated day of week).
        /// </summary>
        /// <param name="person">The person.</param>
        private string GetBirthday( Rock.Model.Person person )
        {
            // If this Person's bday is today, simply return "Today".
            int daysToBirthday = person.DaysToBirthday;
            if ( daysToBirthday == 0 )
            {
                return "Today";
            }

            // Otherwise, if their bday falls within the next 6 days, return the abbreviated day of the week (Mon-Sun) on which their bday falls.
            if ( daysToBirthday < 7 )
            {
                return person.BirthdayDayOfWeekShort;
            }

            return null;
        }

        /// <summary>
        /// Gets whether the person has a health note.
        /// </summary>
        /// <param name="person">The person.</param>
        private bool GetHasHealthNote( Rock.Model.Person person )
        {
            string attributeValue = person.GetAttributeValue( EntityAttributeValueKey.Person_Allergy );
            return attributeValue.IsNotNullOrWhiteSpace();
        }

        /// <summary>
        /// Gets whether the person has a legal note.
        /// </summary>
        /// <param name="person">The person.</param>
        private bool GetHasLegalNote( Rock.Model.Person person )
        {
            string attributeValue = person.GetAttributeValue( EntityAttributeValueKey.Person_LegalNotes );
            return attributeValue.IsNotNullOrWhiteSpace();
        }

        /// <summary>
        /// Sets the attendance-specific properties.
        /// </summary>
        /// <param name="attendance">The attendance.</param>
        /// <param name="attendee">The attendee.</param>
        private void SetAttendanceInfo( Attendance attendance, Attendee attendee )
        {
            // Keep track of each Attendance ID tied to this Attendee so we can manage them all as a group.
            attendee.AttendanceIds.Add( attendance.Id );

            // Tag(s).
            string tag = attendance.AttendanceCode != null
                ? attendance.AttendanceCode.Code
                : null;

            if ( tag.IsNotNullOrWhiteSpace() && !attendee.UniqueTags.Contains( tag, StringComparer.OrdinalIgnoreCase ) )
            {
                attendee.UniqueTags.Add( tag );
            }

            // Service Time(s).
            string serviceTime = attendance.Occurrence != null && attendance.Occurrence.Schedule != null
                ? attendance.Occurrence.Schedule.Name
                : null;

            if ( serviceTime.IsNotNullOrWhiteSpace() && !attendee.UniqueServiceTimes.Contains( serviceTime, StringComparer.OrdinalIgnoreCase ) )
            {
                attendee.UniqueServiceTimes.Add( serviceTime );
            }

            // Status: if this Attendee has multiple AttendanceOccurrences, the highest AttendeeStatus value among them wins.
            AttendeeStatus attendeeStatus = AttendeeStatus.CheckedIn;
            if ( attendance.EndDateTime.HasValue )
            {
                attendeeStatus = AttendeeStatus.CheckedOut;
            }
            else if ( attendance.PresentDateTime.HasValue )
            {
                attendeeStatus = AttendeeStatus.Present;
            }

            if ( attendeeStatus > attendee.Status )
            {
                attendee.Status = attendeeStatus;
            }

            // Check-in Time: if this Attendee has multiple AttendanceOccurrences, the latest StartDateTime value among them wins.
            if ( attendance.StartDateTime > attendee.CheckInTime )
            {
                attendee.CheckInTime = attendance.StartDateTime;
            }
        }

        /// <summary>
        /// Toggles the column visibility within the gAttendees grid.
        /// </summary>
        private void ToggleColumnVisibility()
        {
            // All.
            var serviceTimes = gAttendees.ColumnsOfType<RockBoundField>().First( c => c.DataField == "ServiceTimes" );
            var statusString = gAttendees.ColumnsOfType<RockBoundField>().First( c => c.DataField == "StatusString" );

            // Checked-in.
            var checkInTime = gAttendees.ColumnsOfType<RockLiteralField>().First( c => c.ID == "lCheckInTime" );
            var cancel = gAttendees.ColumnsOfType<LinkButtonField>().First( c => c.ID == "lbCancel" );
            cancel.Text = @"Cancel <i class=""fa fa-times""></i>";

            var present = gAttendees.ColumnsOfType<LinkButtonField>().First( c => c.ID == "lbPresent" );
            present.Text = @"Present <i class=""fa fa-user-check""></i>";

            // Present.
            var checkOut = gAttendees.ColumnsOfType<LinkButtonField>().First( c => c.ID == "lbCheckOut" );
            checkOut.Text = @"Check-out <i class=""fa fa-user-minus""></i>";

            switch ( CurrentStatusFilter )
            {
                case StatusFilter.All:
                    serviceTimes.Visible = true;
                    statusString.Visible = true;

                    checkInTime.Visible = false;
                    cancel.Visible = false;
                    present.Visible = false;

                    checkOut.Visible = false;

                    break;
                case StatusFilter.CheckedIn:
                    serviceTimes.Visible = false;
                    statusString.Visible = false;

                    checkInTime.Visible = true;
                    cancel.Visible = true;
                    present.Visible = true;

                    checkOut.Visible = false;

                    break;
                case StatusFilter.Present:
                    serviceTimes.Visible = true;
                    statusString.Visible = false;

                    checkInTime.Visible = false;
                    cancel.Visible = false;
                    present.Visible = false;

                    checkOut.Visible = AllowCheckout;

                    break;
            }
        }

        #endregion Internal Methods

        #region Helper Classes

        /// <summary>
        /// A class to represent an attendee.
        /// </summary>
        protected class Attendee
        {
            public int PersonId { get; set; }

            public Guid PersonGuid { get; set; }

            private readonly List<int> _attendanceIds = new List<int>();

            public List<int> AttendanceIds
            {
                get
                {
                    return _attendanceIds;
                }
            }

            public string Name { get; set; }

            public string ParentNames { get; set; }

            public int? PhotoId { get; set; }

            public int? Age { get; set; }

            public Gender Gender { get; set; }

            public string Birthday { get; set; }

            public bool IsBirthdayWeek
            {
                get
                {
                    return Birthday.IsNotNullOrWhiteSpace();
                }
            }

            public bool HasHealthNote { get; set; }

            public bool HasLegalNote { get; set; }

            private readonly List<string> _uniqueTags = new List<string>();

            public List<string> UniqueTags
            {
                get
                {
                    return _uniqueTags;
                }
            }

            public string Tag
            {
                get
                {
                    return string.Join( ", ", UniqueTags );
                }
            }

            private readonly List<string> _uniqueServiceTimes = new List<string>();

            public List<string> UniqueServiceTimes
            {
                get
                {
                    return _uniqueServiceTimes;
                }
            }

            public string ServiceTimes
            {
                get
                {
                    return string.Join( ", ", UniqueServiceTimes );
                }
            }

            public AttendeeStatus Status { get; set; }

            public string StatusString
            {
                get
                {
                    return Status.GetDescription();
                }
            }

            public DateTime CheckInTime { get; set; }
        }

        /// <summary>
        /// The status of an attendee.
        /// </summary>
        protected enum AttendeeStatus
        {
            [Description( "Checked-in" )]
            CheckedIn = 1,

            [Description( "Present" )]
            Present = 2,

            [Description( "Checked-out" )]
            CheckedOut = 3
        }

        /// <summary>
        /// The status filter to be applied to attendees displayed.
        /// </summary>
        public enum StatusFilter
        {
            Unknown = 0,
            All = 1,
            CheckedIn = 2,
            Present = 3
        }

        #endregion Helper Classes
    }
}