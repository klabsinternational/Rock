<%@ Control Language="C#" AutoEventWireup="true" CodeFile="ConnectionRequestBoard.ascx.cs" Inherits="RockWeb.Blocks.Connection.ConnectionRequestBoard" %>

<style>
    .main-content #page-content {
        min-height: 0;
    }

    .panel-collapsable {
        box-shadow: 0 1px 3px 0 rgba(21, 27, 38, 0.15);
    }

    .panel-collapsable .panel-drawer {
        border-bottom: 1px solid #dbdbdb;
    }

    .panel-toolbar {
        border-bottom: 1px solid #dbdbdb;
        padding: 4px 16px;
        font-size: 12px;
    }

    .panel-toolbar .btn {
        color: #6f7782;
        font-size: 12px;
        font-weight: 600;
        background: transparent;
    }

    .panel-toolbar .btn:hover,
    .panel-toolbar .btn:active,
    .open .panel-toolbar .btn {
        background: #E9ECEE;
    }

    .overflow-scroll {
        overflow-x: auto;
        overflow-y: hidden;
        min-height: 200px;
        height: calc(100vh - 290px);
    }

    .board-column-container:active,
    .board-column:active {
        cursor: move;
        cursor: -moz-grabbing;
        cursor: -webkit-grabbing;
        cursor: grabbing;
    }

    .board-column {
        -moz-box-sizing: border-box;
        box-sizing: border-box;
        flex: 0 0 312px;
        height: 100%;
        max-width: 312px;
        position: relative;
        width: 312px;
        border-radius: 6px;
        display: flex;
        flex-direction: column;
        padding-top: 5px;
        transition: box-shadow 250ms;
        overflow: hidden;
    }

    .board-heading {
        padding: 0 16px;
    }

    .board-heading-pill {
        height: 4px;
        border-radius: 2px;
    }

    .board-cards {
        overflow-x: hidden;
        overflow-y: scroll;
        padding: 1px 16px 64px;
    }

    .board-card {
        align-items: center;
        background: #fff;
        border: 1px solid #dbdbdb;
        border-radius: 4px;
        box-shadow: 0 1px 3px 0 rgba(0, 0, 0, 0.1), 0 1px 2px 0 rgba(0, 0, 0, 0.06);
        transition: box-shadow 100ms, transform 100ms, background-color 100ms, border-color 100ms;
        width: 280px;
        cursor: pointer;
        display: block;
        padding: 15px;
        margin-bottom: 12px;
    }

    .board-card:hover {
        box-shadow: 0 3px 5px 0 rgba(0, 0, 0, 0.1);
    }

    .board-card-photo {
        width: 24px;
        height: 24px;
        align-items: center;
        background: center/cover #cbd4db;
        border-radius: 50%;
        box-shadow: inset 0 0 0 1px rgba(0, 0, 0, 0.07);
        -moz-box-sizing: border-box;
        box-sizing: border-box;
        display: inline-flex;
        justify-content: center;
        position: relative;
        vertical-align: top;
    }

    .board-card-assigned {
        font-size: 12px;
    }

    .board-card-pills {
        align-content: space-between;
        display: flex;
        flex-flow: row wrap;
        margin: -5px -5px 0;
        margin-bottom: 10px;
    }

    .board-card-pill {
        border-radius: 3px;
        height: 6px;
        margin: 5px 5px 0;
        width: 42px;
        background-color: #7a6ff0;
    }

    .board-card-meta {
        font-size: 11px;
    }

    .dropdown-menu-mega {
        width: 300px;
        position: absolute;
        top: 0;
        bottom: 0;
        z-index: 2000;
        overflow-y: scroll;
    }

    .dropdown-menu-mega li {
        position: relative;
    }

    .dropdown-menu-mega .dropdown-header {
        margin: 24px 16px 4px;
        padding: 8px 0;
        font-size: 16px;
        color: #484848;
        border-bottom: 1px solid #E4E4E4;
        font-weight: 700;
    }

    .dropdown-menu-mega .dropdown-header:first-child {
        margin-top: 0;
    }

    .styled-scroll ::-webkit-scrollbar {
        width: 8px;
        height: 8px;
    }

    .styled-scroll ::-webkit-scrollbar-thumb {
        width: 8px;
        width: 8px;
    }
</style>

<script type="text/javascript" src="https://cdn.jsdelivr.net/gh/mvlandys/jquery.dragscrollable@master/dragscrollable.min.js"></script>
<script type="text/javascript">
    $(document).ready(function () {
        $('.board-column-container').dragscrollable({ allowY: false });

        $(".btn-group-mega").each(function () {
            var bottom = $(this).children('.dropdown-toggle').first().position().top + $(this).height();
            $(this).children('.dropdown-menu-mega').first().css('top', bottom);
        });
    });
</script>

<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>

        <Rock:NotificationBox runat="server" ID="nbNotificationBox" Visible="false" />

        <asp:Panel ID="pnlView" runat="server" CssClass="panel panel-block">
        
            <div class="panel-heading">
                <h2 class="panel-title">
                    <asp:Literal ID="lTitle" runat="server" />
                </h2>
            </div>

            <div class="panel-collapsable">
                <div class="panel-toolbar d-flex flex-wrap flex-sm-nowrap justify-content-between">
                    <div class="d-block">
                        <div class="d-inline-block btn-group-mega">
                            <button type="button" class="btn btn-xs dropdown-toggle" data-toggle="dropdown" aria-haspopup="true" aria-expanded="false"><i class="fa fa-plug"></i>Opportunities</button>
                            <ul class="dropdown-menu dropdown-menu-mega">
                                <li class="dropdown-header"><i class="fa fa-star"></i> Favorites</li>
                                <li>
                                    <a href="#">
                                        <i class="fa fa-child"></i>
                                        TODO
                                        <span class="pull-right text-muted small">Involvement</span>
                                    </a>
                                </li>

                                <asp:Repeater ID="rptConnnectionTypes" runat="server" OnItemDataBound="rptConnnectionTypes_ItemDataBound">
                                    <ItemTemplate>
                                        <li class="dropdown-header">
                                            <i class="<%# Eval("IconCssClass") %>"></i>
                                            <%# Eval("Name") %>
                                        </li>
                                        <asp:Repeater ID="rptConnectionOpportunities" runat="server" OnItemCommand="rptConnectionOpportunities_ItemCommand">
                                            <ItemTemplate>
                                                <li>
                                                    <asp:LinkButton runat="server" CommandArgument='<%# Eval("Id") %>'>
                                                        <i class="<%# Eval("IconCssClass") %>"></i>
                                                        <%# Eval("PublicName") %>
                                                    </asp:LinkButton>
                                                </li>
                                            </ItemTemplate>
                                        </asp:Repeater>
                                    </ItemTemplate>
                                </asp:Repeater>
                            </ul>
                        </div>
                        <a href="#" class="btn btn-xs"><i class="fa fa-plus"></i>Add Request</a>
                    </div>
                    <div class="d-block">
                        <div class="btn-group">
                            <button type="button" class="btn btn-xs dropdown-toggle" data-toggle="dropdown" aria-haspopup="true" aria-expanded="false">
                                <i class="fa fa-user"></i>
                                <asp:Literal runat="server" ID="lConnectorText" />
                            </button>
                            <ul class="dropdown-menu">
                                <li>
                                    <asp:LinkButton runat="server" ID="lbAllConnectors" OnClick="lbAllConnectors_Click">
                                        All Connectors
                                    </asp:LinkButton>
                                </li>
                                <li>
                                    <asp:LinkButton runat="server" ID="lbMyConnections" OnClick="lbMyConnections_Click">
                                        My Connections
                                    </asp:LinkButton>
                                </li>
                                <li role="separator" class="divider"></li>
                                <asp:Repeater ID="rConnectors" runat="server" OnItemCommand="rConnectors_ItemCommand">
                                    <ItemTemplate>
                                        <li>
                                            <asp:LinkButton runat="server" CommandArgument='<%# Eval("PersonAliasId") %>'>
                                                <%# Eval("FullName") %>
                                            </asp:LinkButton>
                                        </li>
                                    </ItemTemplate>
                                </asp:Repeater>
                            </ul>
                        </div>
                    </div>
                    <div class="d-block">
                        <div class="btn-group">
                            <button type="button" class="btn btn-xs dropdown-toggle" data-toggle="dropdown" aria-haspopup="true" aria-expanded="false">
                                <i class="fa fa-sort"></i>
                                <asp:Literal runat="server" ID="lSortText" />
                            </button>
                            <ul class="dropdown-menu">
                                <asp:Repeater ID="rptSort" runat="server" OnItemCommand="rptSort_ItemCommand">
                                    <ItemTemplate>
                                        <li>
                                            <asp:LinkButton runat="server" CommandArgument='<%# Eval("SortBy") %>'>
                                                <%# Eval("Title") %>
                                                <small class="text-muted"><%# Eval("SubTitle") %></small>
                                            </asp:LinkButton>
                                        </li>
                                    </ItemTemplate>
                                </asp:Repeater>
                            </ul>
                        </div>
                        <a href="javascript:void(0);" onclick="$('#filter-drawer').slideToggle()" class="btn btn-xs"><i class="fa fa-filter"></i>Filters</a>
                        <div runat="server" id="divCampusBtnGroup" class="btn-group">
                            <button type="button" class="btn btn-xs dropdown-toggle" data-toggle="dropdown" aria-haspopup="true" aria-expanded="false">
                                <i class="fa fa-building"></i>
                                <asp:Literal runat="server" ID="lCurrentCampusName" />
                            </button>
                            <ul class="dropdown-menu">
                                <li>
                                    <asp:LinkButton runat="server" ID="lbAllCampuses" OnClick="lbAllCampuses_Click"></asp:LinkButton>
                                </li>
                                <asp:Repeater ID="rptCampuses" runat="server" OnItemCommand="rptCampuses_ItemCommand">
                                    <ItemTemplate>
                                        <li>
                                            <asp:LinkButton runat="server" CommandArgument='<%# Eval("Id") %>'>
                                                <%# Eval("Name") %>
                                            </asp:LinkButton>
                                        </li>
                                    </ItemTemplate>
                                </asp:Repeater>
                            </ul>
                        </div>
                        <asp:LinkButton ID="lbToggleViewMode" runat="server" CssClass="btn btn-xs" OnClick="lbToggleViewMode_Click" />
                    </div>
                </div>

                <div id="filter-drawer" class="panel-drawer" style="display: none;">
                    <div class="container-fluid padding-t-md padding-b-md">
                        <div class="row">
                            <div class="col-md-4">
                                <Rock:SlidingDateRangePicker ID="sdrpLastActivityDateRange" runat="server" Label="Last Activity Date Range" EnabledSlidingDateRangeUnits="Day, Week, Month, Year" EnabledSlidingDateRangeTypes="Previous, Last, Current, DateRange" />
                            </div>
                            <div class="col-md-4">
                                <Rock:PersonPicker ID="ppRequester" runat="server" Label="Requester" />
                            </div>
                        </div>
                        <div class="row">
                            <div class="col-sm-12">
                                <asp:LinkButton runat="server" ID="lbApplyFilter" CssClass="btn btn-primary btn-xs" OnClick="lbApplyFilter_Click">
                                    Apply
                                </asp:LinkButton>
                                <asp:LinkButton runat="server" ID="lbClearFilter" CssClass="btn btn-link btn-xs" OnClick="lbClearFilter_Click">
                                    Clear
                                </asp:LinkButton>
                            </div>
                        </div>
                    </div>
                </div>
            </div>

            <div id="divListPanel" runat="server" class="panel-body p-0">
                <Rock:Grid ID="gRequests" runat="server" AllowSorting="true" OnSorting="gRequests_Sorting">
                    <Columns>
                        <Rock:SelectField></Rock:SelectField>
                        <Rock:RockLiteralField ID="lStatusColors" HeaderText="" />
                        <Rock:RockBoundField DataField="PersonFullname" HeaderText="Name" SortExpression="Requestor" />
                    </Columns>
                </Rock:Grid>
            </div>

            <div id="divBoardPanel" runat="server" class="panel-body p-0 overflow-scroll board-column-container cursor-grab">
                <div class="d-flex flex-row w-100 h-100">

                    <asp:Repeater ID="rptColumns" runat="server" OnItemDataBound="rptColumns_ItemDataBound">
                        <ItemTemplate>
                            <div class="board-column">
                                <div class="board-heading mt-3">
                                    <div class="d-flex justify-content-between">
                                        <span class="board-column-title"><%# Eval("Name") %></span>
                                        <span class="board-count"><%# Eval("Requests.Count") %></span>
                                    </div>
                                    <div class="board-heading-pill mt-2 mb-3" style="background: #009CE3"></div>
                                </div>
                                <div class="board-cards">
                                    <asp:Repeater ID="rptCards" runat="server">
                                        <ItemTemplate>
                                            <div class="board-card">
                                                <div class="d-flex justify-content-between">
                                                    <div class="board-card-pills">
                                                        <div class="board-card-pill badge-danger"></div>
                                                    </div>
                                                    <%# Eval("CampusHtml") %>
                                                </div>
                                                <div class="board-card-main d-flex">
                                                    <div class="flex-grow-1 mb-2">
                                                        <div class="board-card-photo mb-1" style="background-image: url( '<%# Eval("PersonPhotoUrl") %>' );" title="<%# Eval("PersonFullname") %> Profile Photo"></div>
                                                        <div class="board-card-name">
                                                            <%# Eval("PersonFullname") %>
                                                        </div>
                                                        <span class="board-card-assigned d-block text-muted">
                                                            <%# Eval("ConnectorPersonFullname") %>
                                                        </span>
                                                    </div>
                                                    <div>
                                                        <div class="btn-group dropdown-right">
                                                            <button type="button" class="btn btn-sm text-muted bg-white dropdown-toggle pr-0" data-toggle="dropdown" aria-haspopup="true" aria-expanded="false">
                                                                <i class="fa fa-ellipsis-h"></i>
                                                            </button>
                                                            <ul class="dropdown-menu">
                                                                <li><a href="#">View Details</a></li>
                                                                <li><a href="#">Mark connected</a></li>
                                                                <li role="separator" class="divider"></li>
                                                                <li><a href="#" class="dropdown-item-danger">Delete</a></li>
                                                            </ul>
                                                        </div>
                                                    </div>
                                                </div>
                                                <div class="board-card-meta d-flex justify-content-between">
                                                    <span class="text-muted" title="<%# Eval("ActivityCountText") %> - <%# Eval("DaysSinceLastActivityLongText") %>">
                                                        <i class="fa fa-list"></i>
                                                        <%# Eval("ActivityCount") %> - <%# Eval("DaysSinceLastActivityShortText") %>
                                                    </span>
                                                    <span class="text-muted" title="<%# Eval("DaysSinceOpeningLongText") %>">
                                                        <i class="fa fa-calendar"></i>
                                                        <%# Eval("DaysSinceOpeningShortText") %>
                                                    </span>
                                                </div>
                                            </div>
                                        </ItemTemplate>
                                    </asp:Repeater>
                                </div>
                            </div>
                        </ItemTemplate>
                    </asp:Repeater>

                </div>
            </div>
        
        </asp:Panel>

    </ContentTemplate>
</asp:UpdatePanel>