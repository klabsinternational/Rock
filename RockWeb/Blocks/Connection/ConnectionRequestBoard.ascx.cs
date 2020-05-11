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
using System.Web.UI;
using System.Web.UI.WebControls;
using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI;
using Rock.Web.UI.Controls;

namespace RockWeb.Blocks.Connection
{
    /// <summary>
    /// Connect Request Board
    /// </summary>
    [DisplayName( "Connection Request Board" )]
    [Category( "Connection" )]
    [Description( "Display the Connection Requests for a selected Connection Opportunity as a list or board view." )]

    public partial class ConnectionRequestBoard : RockBlock
    {
        #region Keys

        /// <summary>
        /// User Preference Key
        /// </summary>
        private static class UserPreferenceKey
        {
            /// <summary>
            /// The sort by
            /// </summary>
            public const string SortBy = "SortBy";

            /// <summary>
            /// The campus filter
            /// </summary>
            public const string CampusFilter = "CampusFilter";

            /// <summary>
            /// The view mode
            /// </summary>
            public const string ViewMode = "ViewMode";

            /// <summary>
            /// Connector Person Alias Id
            /// </summary>
            public const string ConnectorPersonAliasId = "ConnectorPersonAliasId";
        }

        /// <summary>
        /// Filter Key
        /// </summary>
        private static class FilterKey
        {
            /// <summary>
            /// Date Range
            /// </summary>
            public const string DateRange = "DateRange";

            /// <summary>
            /// Requester
            /// </summary>
            public const string Requester = "Requester";

            /// <summary>
            /// Connector
            /// </summary>
            public const string Connector = "Connector";
        }

        #endregion Keys

        #region ViewState Properties

        /// <summary>
        /// Gets or sets the connection opportunity identifier.
        /// </summary>
        /// <value>
        /// The connection opportunity identifier.
        /// </value>
        private int? ConnectionOpportunityId
        {
            get
            {
                return ViewState["ConnectionOpportunityId"].ToStringSafe().AsIntegerOrNull();
            }
            set
            {
                ViewState["ConnectionOpportunityId"] = value;
            }
        }

        /// <summary>
        /// Connector Person Alias Id
        /// </summary>
        private int? ConnectorPersonAliasId
        {
            get
            {
                return ViewState["ConnectorPersonAliasId"].ToStringSafe().AsIntegerOrNull();
            }
            set
            {
                ViewState["ConnectorPersonAliasId"] = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is card view mode.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is card view mode; otherwise, <c>false</c>.
        /// </value>
        private bool IsCardViewMode
        {
            get
            {
                return ViewState["IsCardViewMode"].ToStringSafe().AsBooleanOrNull() ?? true;
            }
            set
            {
                ViewState["IsCardViewMode"] = value;
            }
        }

        /// <summary>
        /// Gets or sets the current sort property.
        /// </summary>
        /// <value>
        /// The current sort property.
        /// </value>
        private SortProperty CurrentSortProperty
        {
            get
            {
                var value = ViewState["CurrentSortProperty"].ToStringSafe();
                SortProperty sortProperty;

                if ( !value.IsNullOrWhiteSpace() && Enum.TryParse( value, out sortProperty ) )
                {
                    return sortProperty;
                }

                return SortProperty.Order;
            }
            set
            {
                ViewState["CurrentSortProperty"] = value;
            }
        }

        /// <summary>
        /// Gets or sets the campus identifier.
        /// </summary>
        /// <value>
        /// The campus identifier.
        /// </value>
        private int? CampusId
        {
            get
            {
                return ViewState["CampusId"].ToStringSafe().AsIntegerOrNull();
            }
            set
            {
                ViewState["CampusId"] = value;
            }
        }

        #endregion ViewState Properties

        #region Base Control Methods

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            // this event gets fired after block settings are updated. it's nice to repaint the screen if these settings would alter it
            BlockUpdated += Block_BlockUpdated;
            AddConfigurationUpdateTrigger( upnlContent );
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
                LoadSettings();
                BindUI();
            }
        }

        #endregion Base Control Methods

        #region Events

        /// <summary>
        /// Handles the BlockUpdated event of the control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void Block_BlockUpdated( object sender, EventArgs e )
        {
            BindUI();
        }

        /// <summary>
        /// Handles the ItemDataBound event of the rptConnnectionTypes control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Web.UI.WebControls.RepeaterItemEventArgs"/> instance containing the event data.</param>
        protected void rptConnnectionTypes_ItemDataBound( object sender, RepeaterItemEventArgs e )
        {
            if ( e.Item.ItemType != ListItemType.Item && e.Item.ItemType != ListItemType.AlternatingItem )
            {
                return;
            }

            var rptConnectionOpportunities = e.Item.FindControl( "rptConnectionOpportunities" ) as Repeater;
            var viewModel = e.Item.DataItem as ConnectionTypeViewModel;

            rptConnectionOpportunities.DataSource = viewModel.ConnectionOpportunities;
            rptConnectionOpportunities.DataBind();
        }

        /// <summary>
        /// Handles the ItemDataBound event of the rptColumns control.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void rptColumns_ItemDataBound( object sender, RepeaterItemEventArgs e )
        {
            if ( e.Item.ItemType != ListItemType.Item && e.Item.ItemType != ListItemType.AlternatingItem )
            {
                return;
            }

            var rptCards = e.Item.FindControl( "rptCards" ) as Repeater;
            var viewModel = e.Item.DataItem as ConnectionStatusViewModel;

            rptCards.DataSource = viewModel.Requests;
            rptCards.DataBind();
        }

        /// <summary>
        /// Handles the ItemCommand event of the rptConnectionOpportunities control.
        /// </summary>
        /// <param name="source">The source of the event.</param>
        /// <param name="e">The <see cref="RepeaterCommandEventArgs"/> instance containing the event data.</param>
        protected void rptConnectionOpportunities_ItemCommand( object source, RepeaterCommandEventArgs e )
        {
            ConnectionOpportunityId = e.CommandArgument.ToStringSafe().AsIntegerOrNull();
            LoadSettings();
            BindUI();
        }

        /// <summary>
        /// Handles the ItemCommand event of the rptCampuses control.
        /// </summary>
        /// <param name="source">The source of the event.</param>
        /// <param name="e">The <see cref="RepeaterCommandEventArgs"/> instance containing the event data.</param>
        protected void rptCampuses_ItemCommand( object source, RepeaterCommandEventArgs e )
        {
            var campusIdString = e.CommandArgument.ToStringSafe();
            CampusId = campusIdString.AsIntegerOrNull();
            SaveSettingByConnectionType( UserPreferenceKey.CampusFilter, campusIdString ?? string.Empty );
            BindUI();
        }

        /// <summary>
        /// Handles the Click event of the lbAllCampuses control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void lbAllCampuses_Click( object sender, EventArgs e )
        {
            CampusId = null;
            SaveSettingByConnectionType( UserPreferenceKey.CampusFilter, string.Empty );
            BindUI();
        }

        /// <summary>
        /// Handles the ItemCommand event of the rptSort control.
        /// </summary>
        /// <param name="source">The source of the event.</param>
        /// <param name="e">The <see cref="RepeaterCommandEventArgs"/> instance containing the event data.</param>
        protected void rptSort_ItemCommand( object source, RepeaterCommandEventArgs e )
        {
            var value = e.CommandArgument.ToStringSafe();
            SaveSettingByConnectionType( UserPreferenceKey.SortBy, value ?? string.Empty );
            SortProperty sortProperty;

            if ( !value.IsNullOrWhiteSpace() && Enum.TryParse( value, out sortProperty ) )
            {
                CurrentSortProperty = sortProperty;
            }
            else
            {
                CurrentSortProperty = SortProperty.Order;
            }

            BindUI();
        }

        /// <summary>
        /// Handles the Click event of the lbToggleViewMode control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void lbToggleViewMode_Click( object sender, EventArgs e )
        {
            IsCardViewMode = !IsCardViewMode;
            SaveSettingByConnectionType( UserPreferenceKey.ViewMode, IsCardViewMode.ToString() );
            BindUI();
        }

        /// <summary>
        /// Apply the filter
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void lbApplyFilter_Click( object sender, EventArgs e )
        {
            SaveSettingByConnectionType( FilterKey.DateRange, sdrpLastActivityDateRange.DelimitedValues );
            SaveSettingByConnectionType( FilterKey.Requester, ppRequester.PersonId.ToStringSafe() );
            SaveSettingByConnectionType( FilterKey.Connector, ppConnector.PersonId.ToStringSafe() );

            BindUI();
        }

        /// <summary>
        /// Clear the filter
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void lbClearFilter_Click( object sender, EventArgs e )
        {
            SaveSettingByConnectionType( FilterKey.DateRange, string.Empty );
            SaveSettingByConnectionType( FilterKey.Requester, string.Empty );
            SaveSettingByConnectionType( FilterKey.Connector, string.Empty );

            BindUI();
        }

        /// <summary>
        /// All connectors click
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void lbAllConnectors_Click( object sender, EventArgs e )
        {
            ConnectorPersonAliasId = null;
            SaveSettingByConnectionType( UserPreferenceKey.ConnectorPersonAliasId, string.Empty );
            BindUI();
        }

        /// <summary>
        /// My connections click
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void lbMyConnections_Click( object sender, EventArgs e )
        {
            ConnectorPersonAliasId = CurrentPersonAliasId;
            SaveSettingByConnectionType( UserPreferenceKey.ConnectorPersonAliasId, ConnectorPersonAliasId.ToStringSafe() );
            BindUI();
        }

        /// <summary>
        /// Handles the ItemCommand event of the rConnectors control.
        /// </summary>
        /// <param name="source">The source of the event.</param>
        /// <param name="e">The <see cref="RepeaterCommandEventArgs"/> instance containing the event data.</param>
        protected void rConnectors_ItemCommand( object source, RepeaterCommandEventArgs e )
        {
            ConnectorPersonAliasId = e.CommandArgument.ToStringSafe().AsIntegerOrNull();
            SaveSettingByConnectionType( UserPreferenceKey.ConnectorPersonAliasId, ConnectorPersonAliasId.ToStringSafe() );
            BindUI();
        }

        #endregion Events

        #region UI Bindings

        /// <summary>
        /// Binds all UI.
        /// </summary>
        private void BindUI()
        {
            if ( !ConnectionOpportunityId.HasValue )
            {
                GetConnectionOpportunity();
            }

            if ( !ConnectionOpportunityId.HasValue )
            {
                pnlView.Visible = false;
                ShowError( "At least one connection opportunity is required before this block can be used" );
                return;
            }

            BindHeader();
            BindViewModeToggle();
            BindFilterControls();
            BindSortOptions();
            BindConnectorOptions();
            BindCampuses();
            BindConnectionTypesRepeater();

            if (IsCardViewMode)
            {
                divBoardPanel.Visible = true;
                divListPanel.Visible = false;
                BindColumnsRepeater();
            }
            else
            {
                divBoardPanel.Visible = false;
                divListPanel.Visible = true;
            }
        }


        /// <summary>
        /// Bind the filter controls
        /// </summary>
        private void BindFilterControls()
        {
            sdrpLastActivityDateRange.DelimitedValues = LoadSettingByConnectionType( FilterKey.DateRange );
            ppRequester.PersonId = LoadSettingByConnectionType( FilterKey.Requester ).AsIntegerOrNull();
            ppConnector.PersonId = LoadSettingByConnectionType( FilterKey.Connector ).AsIntegerOrNull();
        }

        /// <summary>
        /// Binds the view mode toggle.
        /// </summary>
        private void BindViewModeToggle()
        {
            if ( IsCardViewMode )
            {
                lbToggleViewMode.Text = @"<i class=""fa fa-list""></i> List";
            }
            else
            {
                lbToggleViewMode.Text = @"<i class=""fa fa-th-large""></i> Board";
            }
        }

        /// <summary>
        /// Binds the campuses.
        /// </summary>
        private void BindCampuses()
        {
            var campuseViewModels = GetCampusViewModels();

            // If there is only 1 campus, then we don't show campus controls throughout Rock
            if ( campuseViewModels.Count <= 1 )
            {
                CampusId = null;
                divCampusBtnGroup.Visible = false;
                return;
            }

            var currentCampusViewModel = CampusId.HasValue ?
                campuseViewModels.FirstOrDefault( c => c.Id == CampusId.Value ) :
                null;

            lCurrentCampusName.Text = currentCampusViewModel == null ?
                "All Campuses" :
                string.Format( "Campus: {0}", currentCampusViewModel.Name );

            rptCampuses.DataSource = campuseViewModels;
            rptCampuses.DataBind();
        }

        /// <summary>
        /// Binds the connector options.
        /// </summary>
        private void BindConnectorOptions()
        {
            var connectorViewModels = GetConnectors();

            if ( !ConnectorPersonAliasId.HasValue )
            {
                lConnectorText.Text = "All Connectors";
            }
            else if ( ConnectorPersonAliasId == CurrentPersonAliasId )
            {
                lConnectorText.Text = "My Requests";
            }
            else
            {
                var connector = connectorViewModels.FirstOrDefault( c => c.PersonAliasId == ConnectorPersonAliasId );

                if ( connector != null )
                {
                    lConnectorText.Text = string.Format( "Connector: {0}", connector.Fullname );
                }
                else
                {
                    lConnectorText.Text = string.Format( "Connector: Person Alias {0}", ConnectorPersonAliasId );
                }
            }

            rConnectors.DataSource = connectorViewModels;
            rConnectors.DataBind();
        }

        /// <summary>
        /// Binds the sort options.
        /// </summary>
        private void BindSortOptions()
        {
            switch ( CurrentSortProperty )
            {
                case SortProperty.Requestor:
                    lSortText.Text = "Sort: Requestor";
                    break;
                case SortProperty.Connector:
                    lSortText.Text = "Sort: Connector";
                    break;
                case SortProperty.DateAdded:
                case SortProperty.DateAddedDesc:
                    lSortText.Text = "Sort: Date Added";
                    break;
                case SortProperty.LastActivity:
                case SortProperty.LastActivityDesc:
                    lSortText.Text = "Sort: Last Activity";
                    break;
                default:
                    lSortText.Text = "Sort";
                    break;
            }

            var sortOptionViewModels = GetSortOptions();
            rptSort.DataSource = sortOptionViewModels;
            rptSort.DataBind();
        }

        /// <summary>
        /// Binds the header.
        /// </summary>
        private void BindHeader()
        {
            var connectionOpportunity = GetConnectionOpportunity();
            var icon = connectionOpportunity.IconCssClass.IsNullOrWhiteSpace() ?
                "fa fa-arrow-circle-right" :
                connectionOpportunity.IconCssClass;
            var text = connectionOpportunity.Name;
            lTitle.Text = string.Format( @"<i class=""{0}""></i> {1}", icon, text );
        }

        /// <summary>
        /// Binds the connection types repeater.
        /// </summary>
        private void BindConnectionTypesRepeater()
        {
            rptConnnectionTypes.DataSource = GetConnectionTypeViewModels();
            rptConnnectionTypes.DataBind();
        }

        /// <summary>
        /// Binds the columns repeater.
        /// </summary>
        private void BindColumnsRepeater()
        {
            rptColumns.DataSource = GetConnectionStatusViewModels();
            rptColumns.DataBind();
        }

        #endregion UI Bindings

        #region Notification Box

        /// <summary>
        /// Shows the error.
        /// </summary>
        /// <param name="text">The text.</param>
        private void ShowError( string text )
        {
            nbNotificationBox.Title = "Oops";
            nbNotificationBox.NotificationBoxType = NotificationBoxType.Danger;
            nbNotificationBox.Text = text;
            nbNotificationBox.Visible = true;
        }

        #endregion Notification Box

        #region Data Access

        /// <summary>
        /// Loads the settings.
        /// </summary>
        private void LoadSettings()
        {
            // Make sure the connection opportunity id and record are in sync
            GetConnectionOpportunity();

            // Load the view mode
            IsCardViewMode = LoadSettingByConnectionType( UserPreferenceKey.ViewMode ).AsBooleanOrNull() ?? true;

            // Load the sort property
            SortProperty sortProperty;

            if ( Enum.TryParse( LoadSettingByConnectionType( UserPreferenceKey.SortBy ), out sortProperty ) )
            {
                CurrentSortProperty = sortProperty;
            }
            else
            {
                CurrentSortProperty = SortProperty.Order;
            }

            // Load the campus id
            CampusId = LoadSettingByConnectionType( UserPreferenceKey.CampusFilter ).AsIntegerOrNull();

            // Load the connector filter
            ConnectorPersonAliasId = LoadSettingByConnectionType( UserPreferenceKey.ConnectorPersonAliasId ).AsIntegerOrNull();
        }

        /// <summary>
        /// Loads the type of the setting by connection.
        /// </summary>
        /// <param name="subKey">The sub key.</param>
        /// <returns></returns>
        private string LoadSettingByConnectionType( string subKey )
        {
            var connectionOpportunity = GetConnectionOpportunity();

            if ( connectionOpportunity == null )
            {
                return string.Empty;
            }

            var key = string.Format( "{0}-{1}", connectionOpportunity.ConnectionTypeId, subKey );
            return GetBlockUserPreference( key );
        }

        /// <summary>
        /// Saves the type of the setting by connection.
        /// </summary>
        /// <param name="subKey">The sub key.</param>
        /// <param name="value">The value.</param>
        private void SaveSettingByConnectionType( string subKey, string value )
        {
            var key = string.Format( "{0}-{1}", ConnectionOpportunityId ?? 0, subKey );
            SetBlockUserPreference( key, value );
        }

        /// <summary>
        /// Gets the sort options.
        /// </summary>
        /// <returns></returns>
        private List<SortOptionViewModel> GetSortOptions() {
            return new List<SortOptionViewModel> {
                new SortOptionViewModel { SortBy = SortProperty.Order, Title = string.Empty },
                new SortOptionViewModel { SortBy = SortProperty.Requestor, Title = "Requestor" },
                new SortOptionViewModel { SortBy = SortProperty.Connector, Title = "Connector" },
                new SortOptionViewModel { SortBy = SortProperty.DateAdded, Title = "Date Added", SubTitle = "Oldest First" },
                new SortOptionViewModel { SortBy = SortProperty.DateAddedDesc, Title = "Date Added", SubTitle = "Newest First" },
                new SortOptionViewModel { SortBy = SortProperty.LastActivity, Title = "Last Activity", SubTitle = "Oldest First" },
                new SortOptionViewModel { SortBy = SortProperty.LastActivityDesc, Title = "Last Activity", SubTitle = "Newest First" }
            };
        }

        /// <summary>
        /// Gets the connection opportunity.
        /// </summary>
        /// <returns></returns>
        private ConnectionOpportunity GetConnectionOpportunity()
        {
            // Do not make a db call if the id and current record are in sync
            if ( _connectionOpportunity != null && _connectionOpportunity.Id == ConnectionOpportunityId )
            {
                return _connectionOpportunity;
            }

            var rockContext = new RockContext();
            var connectionOpportunityService = new ConnectionOpportunityService( rockContext );
            var query = connectionOpportunityService.Queryable().AsNoTracking();

            _connectionOpportunity = ConnectionOpportunityId.HasValue ?
                query.FirstOrDefault( co => co.Id == ConnectionOpportunityId.Value ) :
                query.FirstOrDefault();

            // Select the first record if one is not explicitly selected
            if ( !ConnectionOpportunityId.HasValue && _connectionOpportunity != null )
            {
                ConnectionOpportunityId = _connectionOpportunity.Id;
            }

            return _connectionOpportunity;
        }
        private ConnectionOpportunity _connectionOpportunity = null;

        /// <summary>
        /// Gets the connection type view models.
        /// </summary>
        /// <returns></returns>
        private List<ConnectionTypeViewModel> GetConnectionTypeViewModels()
        {
            if ( _connectionTypeViewModels == null )
            {
                var rockContext = new RockContext();
                var connectionTypeService = new ConnectionTypeService( rockContext );

                _connectionTypeViewModels = connectionTypeService.Queryable().AsNoTracking()
                    .Include( ct => ct.ConnectionOpportunities )
                    .Where( ct => ct.IsActive )
                    .Select( ct => new ConnectionTypeViewModel
                    {
                        Name = ct.Name,
                        IconCssClass = ct.IconCssClass,
                        ConnectionOpportunities = ct.ConnectionOpportunities
                            .Where( co => co.IsActive )
                            .Select( co => new ConnectionOpportunityViewModel
                            {
                                Id = co.Id,
                                PublicName = co.PublicName,
                                IconCssClass = co.IconCssClass
                            } ).ToList()
                    } )
                    .ToList();
            }

            return _connectionTypeViewModels;
        }
        private List<ConnectionTypeViewModel> _connectionTypeViewModels = null;

        /// <summary>
        /// Gets a list of connectors
        /// </summary>
        /// <returns></returns>
        private List<ConnectorViewModel> GetConnectors()
        {
            var rockContext = new RockContext();
            var connectionRequestService = new ConnectionRequestService( rockContext );

            return connectionRequestService.Queryable()
                .AsNoTracking()
                .Where( cr => cr.ConnectorPersonAliasId.HasValue )
                .Where( cr => cr.ConnectorPersonAliasId.Value != CurrentPersonAliasId )
                .Select( cr => new ConnectorViewModel
                {
                    LastName = cr.ConnectorPersonAlias.Person.LastName,
                    NickName = cr.ConnectorPersonAlias.Person.NickName,
                    PersonAliasId = cr.ConnectorPersonAliasId.Value
                } )
                .OrderBy( c => c.LastName )
                .Distinct()
                .ToList();
        }

        /// <summary>
        /// Gets the connection status view models.
        /// </summary>
        /// <returns></returns>
        private List<ConnectionStatusViewModel> GetConnectionStatusViewModels()
        {
            var rockContext = new RockContext();
            var connectionRequestService = new ConnectionRequestService( rockContext );
            var connectionOpportunityService = new ConnectionOpportunityService( rockContext );

            // Query the statuses and requests in such a way that we get all statuses, even if there
            // are no requests in that column at this time
            var connectionRequestsQuery = connectionRequestService.Queryable()
                .AsNoTracking()
                .Where( cr =>
                    cr.ConnectionOpportunityId == ConnectionOpportunityId &&
                    ( !CampusId.HasValue || CampusId.Value == cr.CampusId.Value ) )
                .Select( cr => new ConnectionRequestViewModel
                {
                    ConnectionStatusId = cr.ConnectionStatusId,
                    PersonId = cr.PersonAlias.PersonId,
                    PersonNickName = cr.PersonAlias.Person.NickName,
                    PersonLastName = cr.PersonAlias.Person.LastName,
                    PersonPhotoId = cr.PersonAlias.Person.PhotoId,
                    CampusName = cr.Campus.Name,
                    CampusCode = cr.Campus.ShortCode,
                    ConnectorPersonNickName = cr.ConnectorPersonAlias.Person.NickName,
                    ConnectorPersonLastName = cr.ConnectorPersonAlias.Person.LastName,
                    ConnectorPersonId = cr.ConnectorPersonAlias.PersonId,
                    ConnectorPersonAliasId = cr.ConnectorPersonAliasId,
                    ActivityCount = cr.ConnectionRequestActivities.Count,
                    DateOpened = cr.CreatedDateTime,
                    Order = cr.Order,
                    LastActivityDate = cr.ConnectionRequestActivities
                        .Select( cra => cra.CreatedDateTime )
                        .OrderByDescending( d => d )
                        .FirstOrDefault()
                } );

            // Filter by connector
            if ( ConnectorPersonAliasId.HasValue )
            {
                connectionRequestsQuery = connectionRequestsQuery.Where( cr => cr.ConnectorPersonAliasId == ConnectorPersonAliasId );
            }

            // Filter by date range
            var minDate = sdrpLastActivityDateRange.SelectedDateRange.Start;
            if (minDate.HasValue)
            {
                connectionRequestsQuery = connectionRequestsQuery.Where( cr => cr.LastActivityDate >= minDate.Value );
            }

            var maxDate = sdrpLastActivityDateRange.SelectedDateRange.End;
            if ( maxDate.HasValue )
            {
                connectionRequestsQuery = connectionRequestsQuery.Where( cr => cr.LastActivityDate <= maxDate.Value );
            }

            // Filter requester
            var requesterId = ppRequester.PersonId;
            if (requesterId.HasValue)
            {
                connectionRequestsQuery = connectionRequestsQuery.Where( cr => cr.PersonId == requesterId.Value );
            }

            // Filter requester
            var connectorId = ppConnector.PersonId;
            if ( connectorId.HasValue )
            {
                connectionRequestsQuery = connectionRequestsQuery.Where( cr => cr.ConnectorPersonId == connectorId.Value );
            }

            // Sort by the selected sorting property
            switch ( CurrentSortProperty )
            {
                case SortProperty.Requestor:
                    connectionRequestsQuery = connectionRequestsQuery
                        .OrderBy( cr => cr.PersonLastName )
                        .ThenBy( cr => cr.PersonNickName )
                        .ThenBy( cr => cr.DateOpened );
                    break;
                case SortProperty.Connector:
                    connectionRequestsQuery = connectionRequestsQuery
                        .OrderBy( cr => cr.ConnectorPersonLastName )
                        .ThenBy( cr => cr.ConnectorPersonNickName )
                        .ThenBy( cr => cr.DateOpened );
                    break;
                case SortProperty.DateAdded:
                    connectionRequestsQuery = connectionRequestsQuery
                        .OrderBy( cr => cr.DateOpened )
                        .ThenBy( cr => cr.PersonLastName )
                        .ThenBy( cr => cr.PersonNickName );
                    break;
                case SortProperty.DateAddedDesc:
                    connectionRequestsQuery = connectionRequestsQuery
                        .OrderByDescending( cr => cr.DateOpened )
                        .ThenBy( cr => cr.PersonLastName )
                        .ThenBy( cr => cr.PersonNickName );
                    break;
                case SortProperty.LastActivityDesc:
                    connectionRequestsQuery = connectionRequestsQuery
                        .OrderByDescending( cr => cr.LastActivityDate )
                        .ThenBy( cr => cr.PersonLastName )
                        .ThenBy( cr => cr.PersonNickName );
                    break;
                case SortProperty.LastActivity:
                    connectionRequestsQuery = connectionRequestsQuery
                        .OrderBy( cr => cr.LastActivityDate )
                        .ThenBy( cr => cr.PersonLastName )
                        .ThenBy( cr => cr.PersonNickName );
                    break;
                case SortProperty.Order:
                default:
                    connectionRequestsQuery = connectionRequestsQuery
                        .OrderBy( cr => cr.Order )
                        .ThenBy( cr => cr.LastActivityDate )
                        .ThenBy( cr => cr.PersonLastName )
                        .ThenBy( cr => cr.PersonNickName );
                    break;
            }

            var connectionRequestsByStatus = connectionRequestsQuery
                .ToList()
                .GroupBy( cr => cr.ConnectionStatusId )
                .ToDictionary( g => g.Key, g => g.ToList() );

            var viewModels = connectionOpportunityService.Queryable()
                .AsNoTracking()
                .Where( co => co.Id == ConnectionOpportunityId )
                .SelectMany( co => co.ConnectionType.ConnectionStatuses )
                .Where( cs => cs.IsActive )
                .OrderBy( cs => cs.Order )
                .ThenBy( cs => cs.Name )
                .Select( cs => new ConnectionStatusViewModel
                {
                    Id = cs.Id,
                    Name = cs.Name
                } )
                .ToList();

            foreach ( var viewModel in viewModels )
            {
                viewModel.Requests = connectionRequestsByStatus.GetValueOrDefault( viewModel.Id, new List<ConnectionRequestViewModel>() );
            }

            return viewModels;
        }

        /// <summary>
        /// Gets the campus view models.
        /// </summary>
        /// <returns></returns>
        private List<CampusViewModel> GetCampusViewModels()
        {
            return CampusCache.All()
                .Where( c => c.IsActive != false )
                .OrderBy( c => c.Order )
                .ThenBy( c => c.Name )
                .Select( c => new CampusViewModel
                {
                    Id = c.Id,
                    Name = c.ShortCode.IsNullOrWhiteSpace() ?
                        c.Name :
                        c.ShortCode
                } )
                .ToList();
        }

        #endregion

        #region View Models

        /// <summary>
        /// Connection Type View Model (Opportunities sidebar)
        /// </summary>
        private class ConnectionTypeViewModel
        {
            /// <summary>
            /// Gets or sets the name.
            /// </summary>
            /// <value>
            /// The name.
            /// </value>
            public string Name { get; set; }

            /// <summary>
            /// Gets or sets the icon CSS class.
            /// </summary>
            /// <value>
            /// The icon CSS class.
            /// </value>
            public string IconCssClass { get; set; }

            /// <summary>
            /// Gets or sets the connection opportunities.
            /// </summary>
            /// <value>
            /// The connection opportunities.
            /// </value>
            public List<ConnectionOpportunityViewModel> ConnectionOpportunities { get; set; }
        }

        /// <summary>
        /// Connection Opportunity View Model (Opportunities sidebar)
        /// </summary>
        private class ConnectionOpportunityViewModel
        {
            /// <summary>
            /// Gets or sets the identifier.
            /// </summary>
            /// <value>
            /// The identifier.
            /// </value>
            public int Id { get; set; }

            /// <summary>
            /// Gets or sets the name.
            /// </summary>
            /// <value>
            /// The name.
            /// </value>
            public string PublicName { get; set; }

            /// <summary>
            /// Gets or sets the icon CSS class.
            /// </summary>
            /// <value>
            /// The icon CSS class.
            /// </value>
            public string IconCssClass { get; set; }
        }

        /// <summary>
        /// Connection Status View Model (columns)
        /// </summary>
        private class ConnectionStatusViewModel
        {
            /// <summary>
            /// Gets or sets the identifier.
            /// </summary>
            /// <value>
            /// The identifier.
            /// </value>
            public int Id { get; set; }

            /// <summary>
            /// Gets or sets the name.
            /// </summary>
            /// <value>
            /// The name.
            /// </value>
            public string Name { get; set; }

            /// <summary>
            /// Gets or sets the requests.
            /// </summary>
            /// <value>
            /// The requests.
            /// </value>
            public List<ConnectionRequestViewModel> Requests { get; set; }
        }

        /// <summary>
        /// Connection Request View Model (cards)
        /// </summary>
        private class ConnectionRequestViewModel
        {
            /// <summary>
            /// Requester Person Id
            /// </summary>
            public int? PersonId { get; set; }

            /// <summary>
            /// Gets or sets the name of the person.
            /// </summary>
            /// <value>
            /// The name of the person.
            /// </value>
            public string PersonNickName { get; set; }

            /// <summary>
            /// Person Last Name
            /// </summary>
            public string PersonLastName { get; set; }

            /// <summary>
            /// Person Photo Id
            /// </summary>
            public int? PersonPhotoId { get; set; }

            /// <summary>
            /// Campus Name
            /// </summary>
            public string CampusName { get; set; }

            /// <summary>
            /// Campus Code
            /// </summary>
            public string CampusCode { get; set; }

            /// <summary>
            /// Gets or sets the name of the connector person.
            /// </summary>
            /// <value>
            /// The name of the connector person.
            /// </value>
            public string ConnectorPersonNickName { get; set; }

            /// <summary>
            /// Gets or sets the last name of the connector person.
            /// </summary>
            public string ConnectorPersonLastName { get; set; }

            /// <summary>
            /// Connector Person Id
            /// </summary>
            public int? ConnectorPersonId { get; set; }

            /// <summary>
            /// Connector person alias id
            /// </summary>
            public int? ConnectorPersonAliasId { get; set; }

            /// <summary>
            /// Connection Status Id
            /// </summary>
            public int ConnectionStatusId { get; set; }

            /// <summary>
            /// Activity count
            /// </summary>
            public int ActivityCount { get; set; }

            /// <summary>
            /// Last activity date
            /// </summary>
            public DateTime? LastActivityDate { get; set; }

            /// <summary>
            /// Date Opened
            /// </summary>
            public DateTime? DateOpened { get; set; }

            /// <summary>
            /// Gets or sets the order.
            /// </summary>
            /// <value>
            /// The order.
            /// </value>
            public int Order { get; set; }

            /// <summary>
            /// Activity Count Text
            /// </summary>
            public string ActivityCountText
            {
                get
                {
                    if ( ActivityCount == 1 )
                    {
                        return "1 Activity";
                    }

                    return string.Format( "{0} Activities", ActivityCount );
                }
            }

            /// <summary>
            /// Connector Person Fullname
            /// </summary>
            public string ConnectorPersonFullname
            {
                get
                {
                    return string.Format( "{0} {1}", ConnectorPersonNickName, ConnectorPersonLastName );
                }
            }

            /// <summary>
            /// Person Fullname
            /// </summary>
            public string PersonFullname
            {
                get
                {
                    return string.Format( "{0} {1}", PersonNickName, PersonLastName );
                }
            }

            /// <summary>
            /// Person Photo Html
            /// </summary>
            public string PersonPhotoUrl
            {
                get
                {
                    return PersonPhotoId.HasValue ?
                        string.Format( "/GetImage.ashx?id={0}", PersonPhotoId.Value ) :
                        "/Assets/Images/person-no-photo-unknown.svg";
                }
            }

            /// <summary>
            /// Has Campus
            /// </summary>
            public string CampusHtml
            {
                get
                {
                    if ( CampusCode.IsNullOrWhiteSpace() )
                    {
                        return string.Empty;
                    }

                    return string.Format( @"<span class=""badge badge-info font-weight-normal"" title=""{0}"">{1}</span>",
                        CampusName,
                        CampusCode );
                }
            }

            /// <summary>
            /// Days Since Opening
            /// </summary>
            public int? DaysSinceOpening
            {
                get
                {
                    if ( !DateOpened.HasValue )
                    {
                        return null;
                    }

                    return ( RockDateTime.Now - DateOpened.Value ).Days;
                }
            }

            /// <summary>
            /// Days Since Opening Short Text
            /// </summary>
            public string DaysSinceOpeningShortText
            {
                get
                {
                    if ( !DaysSinceOpening.HasValue )
                    {
                        return "No Opening";
                    }

                    return string.Format( "{0}d", DaysSinceOpening.Value );
                }
            }

            /// <summary>
            /// Days Since Opening Long Text
            /// </summary>
            public string DaysSinceOpeningLongText
            {
                get
                {
                    if ( !DaysSinceOpening.HasValue )
                    {
                        return "No Opening";
                    }

                    if ( DaysSinceOpening.Value == 1 )
                    {
                        return string.Format( "Opened 1 Day Ago ({0})", DateOpened.Value.ToShortDateString() );
                    }

                    return string.Format( "Opened {0} Days Ago ({1})", DaysSinceOpening.Value, DateOpened.Value.ToShortDateString() );
                }
            }

            /// <summary>
            /// Days Since Last Activity
            /// </summary>
            public int? DaysSinceLastActivity
            {
                get
                {
                    if ( !LastActivityDate.HasValue )
                    {
                        return null;
                    }

                    return ( RockDateTime.Now - LastActivityDate.Value ).Days;
                }
            }

            /// <summary>
            /// Days Since Last Activity Short Text
            /// </summary>
            public string DaysSinceLastActivityShortText
            {
                get
                {
                    if ( !DaysSinceLastActivity.HasValue )
                    {
                        return "No Activity";
                    }

                    return string.Format( "{0}d", DaysSinceLastActivity.Value );
                }
            }

            /// <summary>
            /// Days Since Last Activity Long Text
            /// </summary>
            public string DaysSinceLastActivityLongText
            {
                get
                {
                    if ( !DaysSinceLastActivity.HasValue )
                    {
                        return "No Activity";
                    }

                    if ( DaysSinceLastActivity.Value == 1 )
                    {
                        return "1 Day Since Last Activity";
                    }

                    return string.Format( "{0} Days Since Last Activity", DaysSinceLastActivity.Value );
                }
            }
        }

        /// <summary>
        /// Campus View Model
        /// </summary>
        private class ConnectorViewModel
        {
            /// <summary>
            /// Gets or sets the person alias identifier.
            /// </summary>
            /// <value>
            /// The identifier.
            /// </value>
            public int PersonAliasId { get; set; }

            /// <summary>
            /// Gets or sets the nick name.
            /// </summary>
            /// <value>
            /// The name.
            /// </value>
            public string NickName { get; set; }

            /// <summary>
            /// Gets or sets the last name.
            /// </summary>
            /// <value>
            /// The name.
            /// </value>
            public string LastName { get; set; }

            /// <summary>
            /// Person Fullname
            /// </summary>
            public string Fullname
            {
                get
                {
                    return string.Format( "{0} {1}", NickName, LastName );
                }
            }
        }

        /// <summary>
        /// Campus View Model
        /// </summary>
        private class CampusViewModel
        {
            /// <summary>
            /// Gets or sets the identifier.
            /// </summary>
            /// <value>
            /// The identifier.
            /// </value>
            public int Id { get; set; }

            /// <summary>
            /// Gets or sets the name.
            /// </summary>
            /// <value>
            /// The name.
            /// </value>
            public string Name { get; set; }
        }

        /// <summary>
        /// Sort Option View Model
        /// </summary>
        private class SortOptionViewModel
        {
            /// <summary>
            /// Gets or sets the sort by.
            /// </summary>
            /// <value>
            /// The sort by.
            /// </value>
            public SortProperty SortBy { get; set; }

            /// <summary>
            /// Gets or sets the title.
            /// </summary>
            /// <value>
            /// The title.
            /// </value>
            public string Title { get; set; }

            /// <summary>
            /// Gets or sets the sub title.
            /// </summary>
            /// <value>
            /// The sub title.
            /// </value>
            public string SubTitle { get; set; }
        }

        /// <summary>
        /// Connector Option View Model
        /// </summary>
        private class ConnectorOptionViewModel
        {
            /// <summary>
            /// Person Alias Id
            /// </summary>
            public int PersonAliasId { get; set; }

            /// <summary>
            /// Name
            /// </summary>
            public string Name { get; set; }
        }

        #endregion View Models

        #region Enums

        /// <summary>
        /// The sort property
        /// </summary>
        private enum SortProperty
        {
            Requestor,
            Connector,
            DateAdded,
            DateAddedDesc,
            LastActivity,
            LastActivityDesc,
            Order
        }

        #endregion Enums
    }
}