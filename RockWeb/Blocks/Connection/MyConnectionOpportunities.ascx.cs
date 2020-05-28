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
using System.Data.Entity.SqlServer;
using System.Linq;
using System.Text;
using System.Web.UI;
using System.Web.UI.WebControls;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Security;
using Rock.Web.Cache;
using Rock.Web.UI.Controls;

namespace RockWeb.Blocks.Connection
{
    /// <summary>
    /// Block to display the connectionOpportunities that user is authorized to view, and the activities that are currently assigned to the user.
    /// </summary>
    [DisplayName( "My Connection Opportunities" )]
    [Category( "Connection" )]
    [Description( "Block to display the connection opportunities that user is authorized to view, and the opportunities that are currently assigned to the user." )]

    #region Block Attributes

    [LinkedPage(
        "Configuration Page",
        Description = "Page used to modify and create connection opportunities.",
        IsRequired = true,
        Order = 0,
        Key = AttributeKey.ConfigurationPage )]

    [LinkedPage(
        "Detail Page",
        Description = "Page used to view all requests for a selected opportunity.",
        IsRequired = true,
        Order = 1,
        Key = AttributeKey.DetailPage )]

    [ConnectionTypesField(
        "Connection Types",
        Description = "Optional list of connection types to limit the display to (All will be displayed by default).",
        IsRequired = false,
        Order = 2,
        Key = AttributeKey.ConnectionTypes )]

    [BooleanField(
        "Show Request Total",
        Description = "If enabled, the block will show the total number of requests.",
        DefaultBooleanValue = true,
        Order = 3,
        Key = AttributeKey.ShowRequestTotal )]

    [BooleanField(
        "Show Last Activity Note",
        Description = "If enabled, the block will show the last activity note for each request in the list.",
        DefaultBooleanValue = false,
        Order = 4,
        Key = AttributeKey.ShowLastActivityNote )]

    [CodeEditorField(
        "Status Template",
        Description = "Lava Template that can be used to customize what is displayed in the status bar. Includes common merge fields plus ConnectionOpportunities, ConnectionTypes and the default IdleTooltip.",
        EditorMode = CodeEditorMode.Lava,
        EditorTheme = CodeEditorTheme.Rock,
        DefaultValue = StatusTemplateDefaultValue,
        Order = 5,
        Key = AttributeKey.StatusTemplate )]

    [CodeEditorField(
        "Opportunity Summary Template",
        Description = "Lava Template that can be used to customize what is displayed in each Opportunity Summary. Includes common merge fields plus the OpportunitySummary, ConnectionOpportunity, and its ConnectionRequests.",
        EditorMode = CodeEditorMode.Lava,
        EditorTheme = CodeEditorTheme.Rock,
        DefaultValue = OpportunitySummaryTemplateDefaultValue,
        Key = AttributeKey.OpportunitySummaryTemplate,
        Order = 6 )]

    [BooleanField(
        "Enable Request Security",
        DefaultBooleanValue = false,
        Description = "When enabled, the the security column for request would be displayed.",
        Key = AttributeKey.EnableRequestSecurity,
        IsRequired = true,
        Order = 8
    )]

    #endregion Block Attributes

    public partial class MyConnectionOpportunities : Rock.Web.UI.RockBlock
    {
        #region Keys

        private static class AttributeKey
        {
            public const string ConfigurationPage = "ConfigurationPage";
            public const string DetailPage = "DetailPage";
            public const string EnableRequestSecurity = "EnableRequestSecurity";
            public const string ConnectionTypes = "ConnectionTypes";
            public const string ShowRequestTotal = "ShowRequestTotal";
            public const string ShowLastActivityNote = "ShowLastActivityNote";
            public const string StatusTemplate = "StatusTemplate";
            public const string OpportunitySummaryTemplate = "OpportunitySummaryTemplate";
        }

        /// <summary>
        /// Keys to use for Page Parameters
        /// </summary>
        private static class PageParameterKey
        {
            public const string ConnectionRequestId = "ConnectionRequestId";
            public const string ConnectionOpportunityId = "ConnectionOpportunityId";
        }

        #endregion Keys

        #region Attribute Default values

        private const string StatusTemplateDefaultValue = @"
<div class='pull-left badge-legend padding-r-md'>
    <span class='pull-left badge badge-info badge-circle js-legend-badge' data-toggle='tooltip' data-original-title='Assigned To You'><span class='sr-only'>Assigned To You</span></span>
    <span class='pull-left badge badge-warning badge-circle js-legend-badge' data-toggle='tooltip' data-original-title='Unassigned Item'><span class='sr-only'>Unassigned Item</span></span>
    <span class='pull-left badge badge-critical badge-circle js-legend-badge' data-toggle='tooltip' data-original-title='Critical Status'><span class='sr-only'>Critical Status</span></span>
    <span class='pull-left badge badge-danger badge-circle js-legend-badge' data-toggle='tooltip' data-original-title='{{ IdleTooltip }}'><span class='sr-only'>{{ IdleTooltip }}</span></span>
</div>";

        private const string OpportunitySummaryTemplateDefaultValue = @"
<span class=""item-count"" title=""There are {{ 'active connection' | ToQuantity:OpportunitySummary.TotalRequests }} in this opportunity."">{{ OpportunitySummary.TotalRequests | Format:'#,###,##0' }}</span>
<i class='{{ OpportunitySummary.IconCssClass }}'></i>
<h3>{{ OpportunitySummary.Name }}</h3>
<div class='status-list'>
    <span class='badge badge-info'>{{ OpportunitySummary.AssignedToYou | Format:'#,###,###' }}</span>
    <span class='badge badge-warning'>{{ OpportunitySummary.UnassignedCount | Format:'#,###,###' }}</span>
    <span class='badge badge-critical'>{{ OpportunitySummary.CriticalCount | Format:'#,###,###' }}</span>
    <span class='badge badge-danger'>{{ OpportunitySummary.IdleCount | Format:'#,###,###' }}</span>
</div>
";

        private const string ConnectionRequestStatusIconsTemplateDefaultValue = @"
<div class='status-list'>
    {% if ConnectionRequestStatusIcons.IsAssignedToYou %}
    <span class='badge badge-info js-legend-badge' data-toggle='tooltip' data-original-title='Assigned To You'><span class='sr-only'>Assigned To You</span></span>
    {% endif %}
    {% if ConnectionRequestStatusIcons.IsUnassigned %}
    <span class='badge badge-warning js-legend-badge' data-toggle='tooltip' data-original-title='Unassigned'><span class='sr-only'>Unassigned</span></span>
    {% endif %}
    {% if ConnectionRequestStatusIcons.IsCritical %}
    <span class='badge badge-critical js-legend-badge' data-toggle='tooltip' data-original-title='Critical'><span class='sr-only'>Critical</span></span>
    {% endif %}
    {% if ConnectionRequestStatusIcons.IsIdle %}
    <span class='badge badge-danger js-legend-badge' data-toggle='tooltip' data-original-title='{{ IdleTooltip }}'><span class='sr-only'>{{ IdleTooltip }}</span></span>
    {% endif %}
</div>
";

        #endregion Attribute Default values

        #region Fields

        private const string TOGGLE_ACTIVE_SETTING = "MyConnectionOpportunities_ToggleShowActive";
        private const string TOGGLE_SETTING = "MyConnectionOpportunities_Toggle";
        private const string SELECTED_OPPORTUNITY_SETTING = "MyConnectionOpportunities_SelectedOpportunity";
        private const string CAMPUS_SETTING = "MyConnectionOpportunities_SelectedCampus";
        DateTime _midnightToday = RockDateTime.Today.AddDays( 1 );

        #endregion Fields

        #region Properties

        protected int? SelectedOpportunityId { get; set; }
        protected List<ConnectionTypeSummary> SummaryState { get; set; }

        #endregion Properties

        #region Base Control Methods

        /// <summary>
        /// Restores the view-state information from a previous user control request that was saved by the <see cref="M:System.Web.UI.UserControl.SaveViewState" /> method.
        /// </summary>
        /// <param name="savedState">An <see cref="T:System.Object" /> that represents the user control state to be restored.</param>
        protected override void LoadViewState( object savedState )
        {
            base.LoadViewState( savedState );

            SelectedOpportunityId = ViewState["SelectedOpportunityId"] as int?;
            SummaryState = ViewState["SummaryState"] as List<ConnectionTypeSummary>;
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            lbConnectionTypes.Visible = UserCanAdministrate;

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
                tglMyOpportunities.Checked = GetUserPreference( TOGGLE_SETTING ).AsBoolean( true );
                tglShowActive.Checked = GetUserPreference( TOGGLE_ACTIVE_SETTING ).AsBoolean( true );
                SelectedOpportunityId = GetUserPreference( SELECTED_OPPORTUNITY_SETTING ).AsIntegerOrNull();

                // NOTE: Don't include Inactive Campuses for the "Campus Filter for Page"
                cpCampusFilterForPage.Campuses = CampusCache.All( false );
                cpCampusFilterForPage.Items[0].Text = "All";

                cpCampusFilterForPage.SelectedCampusId = GetUserPreference( CAMPUS_SETTING ).AsIntegerOrNull();

                GetSummaryData();

                RockPage.AddScriptLink( "~/Scripts/jquery.visible.min.js" );
            }
        }

        /// <summary>
        /// Saves any user control view-state changes that have occurred since the last page postback.
        /// </summary>
        /// <returns>
        /// Returns the user control's current view state. If there is no view state associated with the control, it returns null.
        /// </returns>
        protected override object SaveViewState()
        {
            ViewState["SelectedOpportunityId"] = SelectedOpportunityId;
            ViewState["SummaryState"] = SummaryState;

            return base.SaveViewState();
        }

        /// <summary>
        /// Handles the BlockUpdated event of the control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void Block_BlockUpdated( object sender, EventArgs e )
        {
            GetSummaryData();
        }

        #endregion Base Control Methods

        #region Events

        /// <summary>
        /// Handles the SelectedIndexChanged event of the cpCampusPickerForPage control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void cpCampusPickerForPage_SelectedIndexChanged( object sender, EventArgs e )
        {
            SetUserPreference( CAMPUS_SETTING, cpCampusFilterForPage.SelectedCampusId.ToString() );
            GetSummaryData();
        }

        /// <summary>
        /// Handles the CheckedChanged event of the tgl control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void tglMyOpportunities_CheckedChanged( object sender, EventArgs e )
        {
            SetUserPreference( TOGGLE_SETTING, tglMyOpportunities.Checked.ToString() );
            BindSummaryData();
        }

        /// <summary>
        /// Handles the CheckedChanged event of the tglShowActive control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void tglShowActive_CheckedChanged( object sender, EventArgs e )
        {
            SetUserPreference( TOGGLE_ACTIVE_SETTING, tglShowActive.Checked.ToString() );
            SummaryState = null;
            BindSummaryData();
        }

        /// <summary>
        /// Handles the Click event of the lbConnectionTypes control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void lbConnectionTypes_Click( object sender, EventArgs e )
        {
            NavigateToLinkedPage( AttributeKey.ConfigurationPage );
        }

        /// <summary>
        /// Handles the ItemDataBound event of the rptConnnectionTypes control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RepeaterItemEventArgs"/> instance containing the event data.</param>
        protected void rptConnnectionTypes_ItemDataBound( object sender, RepeaterItemEventArgs e )
        {
            var rptConnectionOpportunities = e.Item.FindControl( "rptConnectionOpportunities" ) as Repeater;
            var lConnectionTypeName = e.Item.FindControl( "lConnectionTypeName" ) as Literal;
            var connectionType = e.Item.DataItem as ConnectionTypeSummary;
            if ( rptConnectionOpportunities != null && lConnectionTypeName != null && connectionType != null )
            {
                if ( tglMyOpportunities.Checked )
                {
                    // if 'My Opportunities' is selected, only include the opportunities that have active requests with current person as the connector
                    rptConnectionOpportunities.DataSource = connectionType.Opportunities.Where( o => o.HasActiveRequestsForConnector ).OrderBy( c => c.Name ).ToList();
                }
                else
                {
                    // if 'All Opportunities' is selected, show all the opportunities for the type
                    rptConnectionOpportunities.DataSource = connectionType.Opportunities.OrderBy( c => c.Name );
                }
                rptConnectionOpportunities.DataBind();
                //rptConnectionOpportunities.ItemCommand += rptConnectionOpportunities_ItemCommand;

                lConnectionTypeName.Text = String.Format( "<h4 class='block-title'>{0}</h4>", connectionType.Name );
            }
        }

        /// <summary>
        /// Gets the opportunity summary HTML.
        /// </summary>
        /// <param name="opportunitySummaryId">The opportunity summary identifier.</param>
        /// <returns></returns>
        public string GetOpportunitySummaryHtml( OpportunitySummary opportunitySummary )
        {
            var template = this.GetAttributeValue( AttributeKey.OpportunitySummaryTemplate );

            var mergeFields = Rock.Lava.LavaHelper.GetCommonMergeFields( this.RockPage, this.CurrentPerson, new Rock.Lava.CommonMergeFieldsOptions { GetLegacyGlobalMergeFields = false } );
            mergeFields.Add( "OpportunitySummary", DotLiquid.Hash.FromAnonymousObject( opportunitySummary ) );

            string result = null;
            using ( var rockContext = new RockContext() )
            {
                var connectionOpportunity = new ConnectionOpportunityService( rockContext ).Queryable().AsNoTracking().FirstOrDefault( a => a.Id == opportunitySummary.Id );
                mergeFields.Add( "ConnectionOpportunity", connectionOpportunity );

                result = template.ResolveMergeFields( mergeFields );
            }

            return result;
        }

        /// <summary>
        /// Handles the ItemCommand event of the rptConnectionOpportunities control.
        /// </summary>
        /// <param name="source">The source of the event.</param>
        /// <param name="e">The <see cref="RepeaterCommandEventArgs"/> instance containing the event data.</param>
        protected void rptConnectionOpportunities_ItemCommand( object source, RepeaterCommandEventArgs e )
        {
            string selectedOpportunityValue = e.CommandArgument.ToString();
            SetUserPreference( SELECTED_OPPORTUNITY_SETTING, selectedOpportunityValue );

            SelectedOpportunityId = selectedOpportunityValue.AsIntegerOrNull();

            BindSummaryData();

            ScriptManager.RegisterStartupScript(
                Page,
                GetType(),
                "ScrollToGrid",
                "scrollToGrid();",
                true );
        }

        #endregion Events

        #region Methods

        /// <summary>
        /// Gets the summary data.
        /// </summary>
        private void GetSummaryData()
        {
            SummaryState = new List<ConnectionTypeSummary>();

            var rockContext = new RockContext();
            var opportunities = new ConnectionOpportunityService( rockContext )
                .Queryable().AsNoTracking();

            var typeFilter = GetAttributeValue( AttributeKey.ConnectionTypes ).SplitDelimitedValues().AsGuidList();
            if ( typeFilter.Any() )
            {
                opportunities = opportunities.Where( o => typeFilter.Contains( o.ConnectionType.Guid ) );
            }

            if ( tglShowActive.Checked )
            {
                opportunities = opportunities.Where( a => a.ConnectionType.IsActive );

            }

            var selfAssignedOpportunities = new List<int>();
            bool isSelfAssignedOpportunitiesQueried = false;

            // Loop through opportunities
            foreach ( var opportunity in opportunities )
            {
                // Check to see if person can edit the opportunity because of edit rights to this block or edit rights to
                // the opportunity
                bool canEdit = UserCanEdit || opportunity.IsAuthorized( Authorization.EDIT, CurrentPerson );
                bool campusSpecificConnector = false;
                var campusIds = new List<int>();

                if ( CurrentPersonId.HasValue )
                {
                    // Check to see if person belongs to any connector group that is not campus specific
                    if ( !canEdit )
                    {
                        canEdit = opportunity
                            .ConnectionOpportunityConnectorGroups
                            .Any( g =>
                                !g.CampusId.HasValue &&
                                g.ConnectorGroup != null &&
                                g.ConnectorGroup.Members.Any( m => m.PersonId == CurrentPersonId.Value ) );
                    }

                    // If user is not yet authorized to edit the opportunity, check to see if they are a member of one of the
                    // campus-specific connector groups for the opportunity, and note the campus
                    if ( !canEdit )
                    {
                        foreach ( var groupCampus in opportunity
                            .ConnectionOpportunityConnectorGroups
                            .Where( g =>
                                g.CampusId.HasValue &&
                                g.ConnectorGroup != null &&
                                g.ConnectorGroup.Members.Any( m => m.PersonId == CurrentPersonId.Value ) ) )
                        {
                            campusSpecificConnector = true;
                            canEdit = true;
                            campusIds.Add( groupCampus.CampusId.Value );
                        }
                    }
                }

                if ( opportunity.ConnectionType.EnableRequestSecurity && !isSelfAssignedOpportunitiesQueried )
                {
                    isSelfAssignedOpportunitiesQueried = true;
                    selfAssignedOpportunities = new ConnectionRequestService( rockContext )
                        .Queryable()
                        .Where( a => a.ConnectorPersonAlias.PersonId == CurrentPersonId.Value )
                        .Select( a => a.ConnectionOpportunityId )
                        .Distinct()
                        .ToList();
                }

                var canView = canEdit ||
                                opportunity.IsAuthorized( Authorization.VIEW, CurrentPerson ) ||
                                ( opportunity.ConnectionType.EnableRequestSecurity && selfAssignedOpportunities.Contains( opportunity.Id ) );

                // Is user is authorized to view this opportunity type...
                if ( canView )
                {
                    // Check if the opportunity's type has been added to summary yet, and if not, add it
                    var connectionTypeSummary = SummaryState.Where( c => c.Id == opportunity.ConnectionTypeId ).FirstOrDefault();
                    if ( connectionTypeSummary == null )
                    {
                        connectionTypeSummary = new ConnectionTypeSummary
                        {
                            Id = opportunity.ConnectionTypeId,
                            Name = opportunity.ConnectionType.Name,
                            EnableRequestSecurity = opportunity.ConnectionType.EnableRequestSecurity,
                            Opportunities = new List<OpportunitySummary>()
                        };
                        SummaryState.Add( connectionTypeSummary );
                    }

                    // get list of idle requests (no activity in past X days)

                    var connectionRequestsQry = new ConnectionRequestService( rockContext ).Queryable().Where( a => a.ConnectionOpportunityId == opportunity.Id );
                    if ( cpCampusFilterForPage.SelectedCampusId.HasValue )
                    {
                        connectionRequestsQry = connectionRequestsQry.Where( a => a.CampusId.HasValue && a.CampusId == cpCampusFilterForPage.SelectedCampusId );
                    }

                    var currentDateTime = RockDateTime.Now;
                    int activeRequestCount = connectionRequestsQry
                        .Where( cr =>
                                cr.ConnectionState == ConnectionState.Active
                                || ( cr.ConnectionState == ConnectionState.FutureFollowUp && cr.FollowupDate.HasValue && cr.FollowupDate.Value < _midnightToday )
                        )
                        .Count();

                    // only show if the opportunity is active and there are active requests
                    if ( opportunity.IsActive || ( !opportunity.IsActive && activeRequestCount > 0 ) )
                    {
                        // idle count is:
                        //  (the request is active OR future follow-up who's time has come)
                        //  AND
                        //  (where the activity is more than DaysUntilRequestIdle days old OR no activity but created more than DaysUntilRequestIdle days ago)
                        List<int> idleConnectionRequests = connectionRequestsQry
                                            .Where( cr =>
                                                (
                                                    cr.ConnectionState == ConnectionState.Active
                                                    || ( cr.ConnectionState == ConnectionState.FutureFollowUp && cr.FollowupDate.HasValue && cr.FollowupDate.Value < _midnightToday )
                                                )
                                                &&
                                                (
                                                    ( cr.ConnectionRequestActivities.Any() && cr.ConnectionRequestActivities.Max( ra => ra.CreatedDateTime ) < SqlFunctions.DateAdd( "day", -cr.ConnectionOpportunity.ConnectionType.DaysUntilRequestIdle, currentDateTime ) )
                                                    || ( !cr.ConnectionRequestActivities.Any() && cr.CreatedDateTime < SqlFunctions.DateAdd( "day", -cr.ConnectionOpportunity.ConnectionType.DaysUntilRequestIdle, currentDateTime ) )
                                                )
                                            )
                                            .Select( a => a.Id ).ToList();

                        // get list of requests that have a status that is considered critical.
                        List<int> criticalConnectionRequests = connectionRequestsQry
                                                    .Where( r =>
                                                        r.ConnectionStatus.IsCritical
                                                        && (
                                                                r.ConnectionState == ConnectionState.Active
                                                                || ( r.ConnectionState == ConnectionState.FutureFollowUp && r.FollowupDate.HasValue && r.FollowupDate.Value < _midnightToday )
                                                           )
                                                    )
                                                    .Select( a => a.Id ).ToList();

                        // Add the opportunity
                        var opportunitySummary = new OpportunitySummary
                        {
                            Id = opportunity.Id,
                            Name = opportunity.Name,
                            IsActive = opportunity.IsActive,
                            IconCssClass = opportunity.IconCssClass,
                            IdleConnectionRequests = idleConnectionRequests,
                            CriticalConnectionRequests = criticalConnectionRequests,
                            DaysUntilRequestIdle = opportunity.ConnectionType.DaysUntilRequestIdle,
                            CanEdit = canEdit
                        };

                        // If the user is limited requests with specific campus(es) set the list, otherwise leave it to be null
                        opportunitySummary.CampusSpecificConnector = campusSpecificConnector;
                        opportunitySummary.ConnectorCampusIds = campusIds.Distinct().ToList();

                        connectionTypeSummary.Opportunities.Add( opportunitySummary );
                    }
                }
            }

            // Get a list of all the authorized opportunity ids
            var allOpportunities = SummaryState.SelectMany( s => s.Opportunities ).Select( o => o.Id ).Distinct().ToList();

            // Get all the active and past-due future followup request ids, and include the campus id and personid of connector
            var midnightToday = RockDateTime.Today.AddDays( 1 );
            var activeRequestsQry = new ConnectionRequestService( rockContext )
                .Queryable().AsNoTracking()
                .Where( r =>
                    allOpportunities.Contains( r.ConnectionOpportunityId ) &&
                    ( r.ConnectionState == ConnectionState.Active ||
                        ( r.ConnectionState == ConnectionState.FutureFollowUp && r.FollowupDate.HasValue && r.FollowupDate.Value < midnightToday ) ) )
                .AsEnumerable()
                .Select( r => new
                {
                    r.Id,
                    r.ConnectionOpportunityId,
                    r.CampusId,
                    ConnectorPersonId = r.ConnectorPersonAlias != null ? r.ConnectorPersonAlias.PersonId : -1
                } );


            if ( cpCampusFilterForPage.SelectedCampusId.HasValue )
            {
                activeRequestsQry = activeRequestsQry.Where( a => a.CampusId.HasValue && a.CampusId == cpCampusFilterForPage.SelectedCampusId );
            }

            var activeRequests = activeRequestsQry.ToList();

            // Based on the active requests, set additional properties for each opportunity
            foreach ( var opportunity in SummaryState.SelectMany( s => s.Opportunities ) )
            {
                // Get the active requests for this opportunity that user is authorized to view (based on campus connector)
                var opportunityRequests = activeRequests
                    .Where( r =>
                        r.ConnectionOpportunityId == opportunity.Id &&
                        (
                            !opportunity.CampusSpecificConnector ||
                            ( r.CampusId.HasValue && opportunity.ConnectorCampusIds.Contains( r.CampusId.Value ) )
                        ) )
                    .ToList();

                // The active requests assigned to the current person
                opportunity.AssignedToYouConnectionRequests = opportunityRequests.Where( r => r.ConnectorPersonId == CurrentPersonId ).Select( a => a.Id ).ToList();

                // The active requests that are unassigned
                opportunity.UnassignedConnectionRequests = opportunityRequests.Where( r => r.ConnectorPersonId == -1 ).Select( a => a.Id ).ToList();

                // Flag indicating if current user is connector for any of the active types
                opportunity.HasActiveRequestsForConnector = opportunityRequests.Any( r => r.ConnectorPersonId == CurrentPersonId );

                // Total number of requests for opportunity/campus/connector
                opportunity.TotalRequests = opportunityRequests.Count();
            }

            //Set the Idle tooltip
            var connectionTypes = opportunities.Where( o => allOpportunities.Contains( o.Id ) ).Select( o => o.ConnectionType ).Distinct().ToList();
            StringBuilder sb = new StringBuilder();
            if ( connectionTypes.Select( t => t.DaysUntilRequestIdle ).Distinct().Count() == 1 )
            {
                sb.Append( String.Format( "Idle (no activity in {0} days)", connectionTypes.Select( t => t.DaysUntilRequestIdle ).Distinct().First() ) );
            }
            else
            {
                sb.Append( "Idle (no activity in several days)<br/><ul class='list-unstyled'>" );
                foreach ( var connectionType in connectionTypes )
                {
                    sb.Append( String.Format( "<li>{0}: {1} days</li>", connectionType.Name, connectionType.DaysUntilRequestIdle ) );
                }
                sb.Append( "</ul>" );
            }

            var statusTemplate = this.GetAttributeValue( AttributeKey.StatusTemplate );
            var statusMergeFields = Rock.Lava.LavaHelper.GetCommonMergeFields( this.RockPage );
            statusMergeFields.Add( "ConnectionOpportunities", allOpportunities );
            statusMergeFields.Add( "ConnectionTypes", connectionTypes );
            statusMergeFields.Add( "IdleTooltip", sb.ToString().EncodeHtml() );
            lStatusBarContent.Text = statusTemplate.ResolveMergeFields( statusMergeFields );
            BindSummaryData();

            if ( GetAttributeValue( AttributeKey.ShowRequestTotal ).AsBoolean( true ) )
            {
                lTotal.Visible = true;
                lTotal.Text = string.Format( "Total Requests: {0:N0}", SummaryState.SelectMany( s => s.Opportunities ).Sum( o => o.TotalRequests ) );
            }
            else
            {
                lTotal.Visible = false;
            }
        }

        /// <summary>
        /// Binds the summary data.
        /// </summary>
        private void BindSummaryData()
        {
            if ( SummaryState == null )
            {
                GetSummaryData();
            }

            var viewableOpportunityIds = SummaryState
                .SelectMany( c => c.Opportunities )
                .Where( o => !tglMyOpportunities.Checked || o.HasActiveRequestsForConnector )
                .Select( o => o.Id )
                .ToList();

            // Make sure that the selected opportunity is actually one that is being displayed
            if ( SelectedOpportunityId.HasValue && !viewableOpportunityIds.Contains( SelectedOpportunityId.Value ) )
            {
                SelectedOpportunityId = null;
            }

            nbNoOpportunities.Visible = !viewableOpportunityIds.Any();

            rptConnnectionTypes.DataSource = SummaryState.Where( t => t.Opportunities.Any( o => viewableOpportunityIds.Contains( o.Id ) ) );
            rptConnnectionTypes.DataBind();
        }

        #endregion Methods

        #region Helper Classes

        [Serializable]
        public class ConnectionTypeSummary
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public bool EnableRequestSecurity { get; set; }
            public List<OpportunitySummary> Opportunities { get; set; }
        }

        [Serializable]
        public class OpportunitySummary
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string IconCssClass { get; set; }
            public bool IsActive { get; set; }
            public bool CampusSpecificConnector { get; set; }
            public List<int> ConnectorCampusIds { get; set; }  // Will be null if user is a connector for all campuses
            public int DaysUntilRequestIdle { get; set; }
            public bool CanEdit { get; set; }
            public int AssignedToYou
            {
                get
                {
                    return AssignedToYouConnectionRequests.Count();
                }
            }

            public int UnassignedCount
            {
                get
                {
                    return UnassignedConnectionRequests.Count();
                }
            }

            public int CriticalCount
            {
                get
                {
                    return CriticalConnectionRequests.Count();
                }
            }

            public int IdleCount
            {
                get
                {
                    return IdleConnectionRequests.Count();
                }
            }

            public bool HasActiveRequestsForConnector { get; set; }
            public List<int> AssignedToYouConnectionRequests { get; internal set; }
            public List<int> UnassignedConnectionRequests { get; internal set; }
            public List<int> IdleConnectionRequests { get; internal set; }
            public List<int> CriticalConnectionRequests { get; internal set; }
            public int TotalRequests { get; internal set; }
        }

        #endregion Helper Classes
    }
}
