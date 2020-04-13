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
using DocumentFormat.OpenXml.Office2010.ExcelAc;
using Rock;
using Rock.Data;
using Rock.Model;
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
        #region ViewState Properties

        /// <summary>
        /// Page Parameter Keys
        /// </summary>
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
            BindUI();
        }

        #endregion Events

        #region UI Bindings

        /// <summary>
        /// Binds all UI.
        /// </summary>
        private void BindUI()
        {
            var connectionOpportunity = GetConnectionOpportunity();

            if ( connectionOpportunity == null )
            {
                pnlView.Visible = false;
                ShowError( "At least one connection opportunity is required before this block can be used" );
                return;
            }

            BindHeader();
            BindConnectionTypesRepeater();
            BindColumnsRepeater();
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
        /// Gets the connection opportunity.
        /// </summary>
        /// <returns></returns>
        private ConnectionOpportunity GetConnectionOpportunity()
        {
            var rockContext = new RockContext();
            var connectionOpportunityService = new ConnectionOpportunityService( rockContext );
            var query = connectionOpportunityService.Queryable().AsNoTracking();

            var connectionOpportunity = ConnectionOpportunityId.HasValue ?
                query.FirstOrDefault( co => co.Id == ConnectionOpportunityId.Value ) :
                query.FirstOrDefault();

            if ( !ConnectionOpportunityId.HasValue && connectionOpportunity != null )
            {
                ConnectionOpportunityId = connectionOpportunity.Id;
            }

            return connectionOpportunity;
        }

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
            var connectionRequestsByStatus = connectionRequestService.Queryable()
                .AsNoTracking()
                .Where( cr => cr.ConnectionOpportunityId == ConnectionOpportunityId )
                .Select( cr => new ConnectionRequestViewModel
                {
                    ConnectionStatusId = cr.ConnectionStatusId,
                    PersonNickName = cr.PersonAlias.Person.NickName,
                    PersonLastName = cr.PersonAlias.Person.LastName,
                    PersonPhotoId = cr.PersonAlias.Person.PhotoId,
                    CampusName = cr.Campus.Name,
                    CampusCode = cr.Campus.ShortCode,
                    ConnectorPersonNickName = cr.ConnectorPersonAlias.Person.NickName,
                    ConnectorPersonLastName = cr.ConnectorPersonAlias.Person.LastName,
                    ActivityCount = cr.ConnectionRequestActivities.Count,
                    DateOpened = cr.CreatedDateTime,
                    LastActivityDate = cr.ConnectionRequestActivities
                        .Select( cra => cra.CreatedDateTime )
                        .OrderByDescending( d => d )
                        .FirstOrDefault()
                } )
                .ToList()
                .GroupBy( cr => cr.ConnectionStatusId )
                .ToDictionary( g => g.Key, g => g.ToList() );

            var viewModels = connectionOpportunityService.Queryable()
                .AsNoTracking()
                .Where( co => co.Id == ConnectionOpportunityId )
                .SelectMany( co => co.ConnectionType.ConnectionStatuses )
                .Where( cs => cs.IsActive )
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

        #endregion View Models
    }
}