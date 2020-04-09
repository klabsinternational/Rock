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
                            <button type="button" class="btn btn-xs dropdown-toggle" data-toggle="dropdown" aria-haspopup="true" aria-expanded="false"><i class="fa fa-user"></i>All Connectors</button>
                            <ul class="dropdown-menu">
                                <li><a href="#">All Connectors</a></li>
                                <li><a href="#">My Connections</a></li>
                                <li role="separator" class="divider"></li>
                                <li><a href="#">Ted Decker</a></li>
                                <li><a href="#">Phil Coffee</a></li>
                            </ul>
                        </div>
                    </div>
                    <div class="d-block">
                        <div class="btn-group">
                            <button type="button" class="btn btn-xs dropdown-toggle" data-toggle="dropdown" aria-haspopup="true" aria-expanded="false"><i class="fa fa-sort"></i>Sort</button>
                            <ul class="dropdown-menu">
                                <li><a href="#">Requestor</a></li>
                                <li><a href="#">Connector</a></li>
                                <li><a href="#">Date Added <small class="text-muted">Oldest First</small></a></li>
                                <li><a href="#">Date Added <small class="text-muted">Newest First</small></a></li>
                                <li><a href="#">Last Activity <small class="text-muted">Oldest First</small></a></li>
                                <li><a href="#">Last Activity <small class="text-muted">Newest First</small></a></li>
                            </ul>
                        </div>
                        <a href="javascript:void(0);" onclick="$('#filter-drawer').slideToggle()" class="btn btn-xs"><i class="fa fa-filter"></i>Filters</a>
                        <div class="btn-group">
                            <button type="button" class="btn btn-xs dropdown-toggle" data-toggle="dropdown" aria-haspopup="true" aria-expanded="false"><i class="fa fa-building"></i>All Campuses</button>
                            <ul class="dropdown-menu">
                                <li><a href="#">All Campuses</a></li>
                                <li><a href="#">Campus 1</a></li>
                                <li><a href="#">Campus 2</a></li>
                            </ul>
                        </div>
                        <a href="#" class="btn btn-xs"><i class="fa fa-list"></i>List</a>
                    </div>
                </div>

                <div id="filter-drawer" class="panel-drawer" style="display: none;">
                    <div data-note="REPLACE ME" style="display: grid; place-items: center; height: 200px;">Rock Filter Controls Here</div>
                </div>
            </div>

            <div class="panel-body p-0 overflow-scroll board-column-container cursor-grab">
                <div class="d-flex flex-row w-100 h-100">

                    <asp:Repeater ID="rptColumns" runat="server">
                        <ItemTemplate>
                            <div class="board-column">
                                <div class="board-heading mt-3">
                                    <div class="d-flex justify-content-between">
                                        <span class="board-column-title">No Contact</span>
                                        <span class="board-count">4</span>
                                    </div>
                                    <div class="board-heading-pill mt-2 mb-3" style="background: #009CE3"></div>
                                </div>
                                <div class="board-cards">

                                    <div class="board-card">
                                        <div class="d-flex justify-content-between">
                                            <div class="board-card-pills">
                                                <div class="board-card-pill badge-danger"></div>
                                            </div>
                                            <span class="badge badge-info font-weight-normal" title="Phoenix Campus">PHX</span>
                                        </div>
                                        <div class="board-card-main d-flex">
                                            <div class="flex-grow-1 mb-2">
                                                <div class="board-card-photo mb-1" style="background-image: url('/GetImage.ashx?id=74');" title="Ted Decker Profile Photo"></div>
                                                <div class="board-card-name">
                                                    Ted Decker
                                                </div>
                                                <span class="board-card-assigned d-block text-muted">Ted Decker</span>
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
                                            <span class="text-muted" title="4 Activities - 2 Days Since Last Activity"><i class="fa fa-list"></i> 4 - 2d</span>
                                            <span class="text-muted" title="Opened 3 Days Ago (Aug 12, 2020)"><i class="fa fa-calendar"></i> 3d</span>
                                        </div>
                                    </div>

                                    <div class="board-card">
                                        <div class="d-flex justify-content-between">
                                            <div class="board-card-pills">
                                                <div class="board-card-pill badge-danger"></div>
                                            </div>
                                            <span class="badge badge-info font-weight-normal" title="Phoenix Campus">PHX</span>
                                        </div>
                                        <div class="board-card-main d-flex">
                                            <div class="flex-grow-1 mb-2">
                                                <div class="board-card-photo mb-1" style="background-image: url('/GetImage.ashx?id=74');" title="Ted Decker Profile Photo"></div>
                                                <div class="board-card-name">
                                                    Ted Decker
                                                </div>
                                                <span class="board-card-assigned d-block text-muted">Ted Decker</span>
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
                                            <span class="text-muted" title="4 Activities - 2 Days Since Last Activity"><i class="fa fa-list"></i> 4 - 2d</span>
                                            <span class="text-muted" title="Opened 3 Days Ago (Aug 12, 2020)"><i class="fa fa-calendar"></i> 3d</span>
                                        </div>
                                    </div>

                                </div>
                            </div>
                        </ItemTemplate>
                    </asp:Repeater>

                    <div class="board-column">
                        <div class="board-heading mt-3">
                            <div class="d-flex justify-content-between">
                                <span class="board-column-title">No Contact</span>
                                <span class="board-count">4</span>
                            </div>
                            <div class="board-heading-pill mt-2 mb-3" style="background: #009CE3"></div>
                        </div>
                        <div class="board-cards">

                            <div class="board-card">
                                <div class="d-flex justify-content-between">
                                    <div class="board-card-pills">
                                        <div class="board-card-pill badge-danger"></div>
                                    </div>
                                    <span class="badge badge-info font-weight-normal" title="Phoenix Campus">PHX</span>
                                </div>
                                <div class="board-card-main d-flex">
                                    <div class="flex-grow-1 mb-2">
                                        <div class="board-card-photo mb-1" style="background-image: url('/GetImage.ashx?id=74');" title="Ted Decker Profile Photo"></div>
                                        <div class="board-card-name">
                                            Ted Decker
                                        </div>
                                        <span class="board-card-assigned d-block text-muted">Ted Decker</span>
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
                                    <span class="text-muted" title="4 Activities - 2 Days Since Last Activity"><i class="fa fa-list"></i> 4 - 2d</span>
                                    <span class="text-muted" title="Opened 3 Days Ago (Aug 12, 2020)"><i class="fa fa-calendar"></i> 3d</span>
                                </div>
                            </div>

                            <div class="board-card">
                                <div class="d-flex justify-content-between">
                                    <div class="board-card-pills">
                                        <div class="board-card-pill badge-danger"></div>
                                    </div>
                                    <span class="badge badge-info font-weight-normal" title="Phoenix Campus">PHX</span>
                                </div>
                                <div class="board-card-main d-flex">
                                    <div class="flex-grow-1 mb-2">
                                        <div class="board-card-photo mb-1" style="background-image: url('/GetImage.ashx?id=74');" title="Ted Decker Profile Photo"></div>
                                        <div class="board-card-name">
                                            Ted Decker
                                        </div>
                                        <span class="board-card-assigned d-block text-muted">Ted Decker</span>
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
                                    <span class="text-muted" title="4 Activities - 2 Days Since Last Activity"><i class="fa fa-list"></i> 4 - 2d</span>
                                    <span class="text-muted" title="Opened 3 Days Ago (Aug 12, 2020)"><i class="fa fa-calendar"></i> 3d</span>
                                </div>
                            </div>

                        </div>
                    </div>

                    <div class="board-column">
                        <div class="board-heading mt-3">
                            <div class="d-flex justify-content-between">
                                <span class="board-column-title">No Contact</span>
                                <span class="board-count">4</span>
                            </div>
                            <div class="board-heading-pill mt-2 mb-3" style="background: #009CE3"></div>
                        </div>
                        <div class="board-cards">

                            <div class="board-card">
                                <div class="d-flex justify-content-between">
                                    <div class="board-card-pills">
                                        <div class="board-card-pill badge-danger"></div>
                                    </div>
                                    <span class="badge badge-info font-weight-normal" title="Phoenix Campus">PHX</span>
                                </div>
                                <div class="board-card-main d-flex">
                                    <div class="flex-grow-1 mb-2">
                                        <div class="board-card-photo mb-1" style="background-image: url('/GetImage.ashx?id=74');" title="Ted Decker Profile Photo"></div>
                                        <div class="board-card-name">
                                            Ted Decker
                                        </div>
                                        <span class="board-card-assigned d-block text-muted">Ted Decker</span>
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
                                    <span class="text-muted" title="4 Activities - 2 Days Since Last Activity"><i class="fa fa-list"></i> 4 - 2d</span>
                                    <span class="text-muted" title="Opened 3 Days Ago (Aug 12, 2020)"><i class="fa fa-calendar"></i> 3d</span>
                                </div>
                            </div>

                            <div class="board-card">
                                <div class="d-flex justify-content-between">
                                    <div class="board-card-pills">
                                        <div class="board-card-pill badge-danger"></div>
                                    </div>
                                    <span class="badge badge-info font-weight-normal" title="Phoenix Campus">PHX</span>
                                </div>
                                <div class="board-card-main d-flex">
                                    <div class="flex-grow-1 mb-2">
                                        <div class="board-card-photo mb-1" style="background-image: url('/GetImage.ashx?id=74');" title="Ted Decker Profile Photo"></div>
                                        <div class="board-card-name">
                                            Ted Decker
                                        </div>
                                        <span class="board-card-assigned d-block text-muted">Ted Decker</span>
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
                                    <span class="text-muted" title="4 Activities - 2 Days Since Last Activity"><i class="fa fa-list"></i> 4 - 2d</span>
                                    <span class="text-muted" title="Opened 3 Days Ago (Aug 12, 2020)"><i class="fa fa-calendar"></i> 3d</span>
                                </div>
                            </div>

                            <div class="board-card">
                                <div class="d-flex justify-content-between">
                                    <div class="board-card-pills">
                                        <div class="board-card-pill badge-danger"></div>
                                    </div>
                                    <span class="badge badge-info font-weight-normal" title="Phoenix Campus">PHX</span>
                                </div>
                                <div class="board-card-main d-flex">
                                    <div class="flex-grow-1 mb-2">
                                        <div class="board-card-photo mb-1" style="background-image: url('/GetImage.ashx?id=74');" title="Ted Decker Profile Photo"></div>
                                        <div class="board-card-name">
                                            Ted Decker
                                        </div>
                                        <span class="board-card-assigned d-block text-muted">Ted Decker</span>
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
                                    <span class="text-muted" title="4 Activities - 2 Days Since Last Activity"><i class="fa fa-list"></i> 4 - 2d</span>
                                    <span class="text-muted" title="Opened 3 Days Ago (Aug 12, 2020)"><i class="fa fa-calendar"></i> 3d</span>
                                </div>
                            </div>

                            <div class="board-card">
                                <div class="d-flex justify-content-between">
                                    <div class="board-card-pills">
                                        <div class="board-card-pill badge-danger"></div>
                                    </div>
                                    <span class="badge badge-info font-weight-normal" title="Phoenix Campus">PHX</span>
                                </div>
                                <div class="board-card-main d-flex">
                                    <div class="flex-grow-1 mb-2">
                                        <div class="board-card-photo mb-1" style="background-image: url('/GetImage.ashx?id=74');" title="Ted Decker Profile Photo"></div>
                                        <div class="board-card-name">
                                            Ted Decker
                                        </div>
                                        <span class="board-card-assigned d-block text-muted">Ted Decker</span>
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
                                    <span class="text-muted" title="4 Activities - 2 Days Since Last Activity"><i class="fa fa-list"></i> 4 - 2d</span>
                                    <span class="text-muted" title="Opened 3 Days Ago (Aug 12, 2020)"><i class="fa fa-calendar"></i> 3d</span>
                                </div>
                            </div>

                            <div class="board-card">
                                <div class="d-flex justify-content-between">
                                    <div class="board-card-pills">
                                        <div class="board-card-pill badge-danger"></div>
                                    </div>
                                    <span class="badge badge-info font-weight-normal" title="Phoenix Campus">PHX</span>
                                </div>
                                <div class="board-card-main d-flex">
                                    <div class="flex-grow-1 mb-2">
                                        <div class="board-card-photo mb-1" style="background-image: url('/GetImage.ashx?id=74');" title="Ted Decker Profile Photo"></div>
                                        <div class="board-card-name">
                                            Ted Decker
                                        </div>
                                        <span class="board-card-assigned d-block text-muted">Ted Decker</span>
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
                                    <span class="text-muted" title="4 Activities - 2 Days Since Last Activity"><i class="fa fa-list"></i> 4 - 2d</span>
                                    <span class="text-muted" title="Opened 3 Days Ago (Aug 12, 2020)"><i class="fa fa-calendar"></i> 3d</span>
                                </div>
                            </div>

                            <div class="board-card">
                                <div class="d-flex justify-content-between">
                                    <div class="board-card-pills">
                                        <div class="board-card-pill badge-danger"></div>
                                    </div>
                                    <span class="badge badge-info font-weight-normal" title="Phoenix Campus">PHX</span>
                                </div>
                                <div class="board-card-main d-flex">
                                    <div class="flex-grow-1 mb-2">
                                        <div class="board-card-photo mb-1" style="background-image: url('/GetImage.ashx?id=74');" title="Ted Decker Profile Photo"></div>
                                        <div class="board-card-name">
                                            Ted Decker
                                        </div>
                                        <span class="board-card-assigned d-block text-muted">Ted Decker</span>
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
                                    <span class="text-muted" title="4 Activities - 2 Days Since Last Activity"><i class="fa fa-list"></i> 4 - 2d</span>
                                    <span class="text-muted" title="Opened 3 Days Ago (Aug 12, 2020)"><i class="fa fa-calendar"></i> 3d</span>
                                </div>
                            </div>

                            <div class="board-card">
                                <div class="d-flex justify-content-between">
                                    <div class="board-card-pills">
                                        <div class="board-card-pill badge-danger"></div>
                                    </div>
                                    <span class="badge badge-info font-weight-normal" title="Phoenix Campus">PHX</span>
                                </div>
                                <div class="board-card-main d-flex">
                                    <div class="flex-grow-1 mb-2">
                                        <div class="board-card-photo mb-1" style="background-image: url('/GetImage.ashx?id=74');" title="Ted Decker Profile Photo"></div>
                                        <div class="board-card-name">
                                            Ted Decker
                                        </div>
                                        <span class="board-card-assigned d-block text-muted">Ted Decker</span>
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
                                    <span class="text-muted" title="4 Activities - 2 Days Since Last Activity"><i class="fa fa-list"></i> 4 - 2d</span>
                                    <span class="text-muted" title="Opened 3 Days Ago (Aug 12, 2020)"><i class="fa fa-calendar"></i> 3d</span>
                                </div>
                            </div>

                            <div class="board-card">
                                <div class="d-flex justify-content-between">
                                    <div class="board-card-pills">
                                        <div class="board-card-pill badge-danger"></div>
                                    </div>
                                    <span class="badge badge-info font-weight-normal" title="Phoenix Campus">PHX</span>
                                </div>
                                <div class="board-card-main d-flex">
                                    <div class="flex-grow-1 mb-2">
                                        <div class="board-card-photo mb-1" style="background-image: url('/GetImage.ashx?id=74');" title="Ted Decker Profile Photo"></div>
                                        <div class="board-card-name">
                                            Ted Decker
                                        </div>
                                        <span class="board-card-assigned d-block text-muted">Ted Decker</span>
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
                                    <span class="text-muted" title="4 Activities - 2 Days Since Last Activity"><i class="fa fa-list"></i> 4 - 2d</span>
                                    <span class="text-muted" title="Opened 3 Days Ago (Aug 12, 2020)"><i class="fa fa-calendar"></i> 3d</span>
                                </div>
                            </div>

                        </div>
                    </div>

                    <div class="board-column">
                        <div class="board-heading mt-3">
                            <div class="d-flex justify-content-between">
                                <span class="board-column-title">No Contact</span>
                                <span class="board-count">4</span>
                            </div>
                            <div class="board-heading-pill mt-2 mb-3" style="background: #009CE3"></div>
                        </div>
                        <div class="board-cards">

                            <div class="board-card">
                                <div class="d-flex justify-content-between">
                                    <div class="board-card-pills">
                                        <div class="board-card-pill badge-danger"></div>
                                    </div>
                                    <span class="badge badge-info font-weight-normal" title="Phoenix Campus">PHX</span>
                                </div>
                                <div class="board-card-main d-flex">
                                    <div class="flex-grow-1 mb-2">
                                        <div class="board-card-photo mb-1" style="background-image: url('/GetImage.ashx?id=74');" title="Ted Decker Profile Photo"></div>
                                        <div class="board-card-name">
                                            Ted Decker
                                        </div>
                                        <span class="board-card-assigned d-block text-muted">Ted Decker</span>
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
                                    <span class="text-muted" title="4 Activities - 2 Days Since Last Activity"><i class="fa fa-list"></i> 4 - 2d</span>
                                    <span class="text-muted" title="Opened 3 Days Ago (Aug 12, 2020)"><i class="fa fa-calendar"></i> 3d</span>
                                </div>
                            </div>

                            <div class="board-card">
                                <div class="d-flex justify-content-between">
                                    <div class="board-card-pills">
                                        <div class="board-card-pill badge-danger"></div>
                                    </div>
                                    <span class="badge badge-info font-weight-normal" title="Phoenix Campus">PHX</span>
                                </div>
                                <div class="board-card-main d-flex">
                                    <div class="flex-grow-1 mb-2">
                                        <div class="board-card-photo mb-1" style="background-image: url('/GetImage.ashx?id=74');" title="Ted Decker Profile Photo"></div>
                                        <div class="board-card-name">
                                            Ted Decker
                                        </div>
                                        <span class="board-card-assigned d-block text-muted">Ted Decker</span>
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
                                    <span class="text-muted" title="4 Activities - 2 Days Since Last Activity"><i class="fa fa-list"></i> 4 - 2d</span>
                                    <span class="text-muted" title="Opened 3 Days Ago (Aug 12, 2020)"><i class="fa fa-calendar"></i> 3d</span>
                                </div>
                            </div>

                        </div>
                    </div>

                    <div class="board-column">
                        <div class="board-heading mt-3">
                            <div class="d-flex justify-content-between">
                                <span class="board-column-title">No Contact</span>
                                <span class="board-count">4</span>
                            </div>
                            <div class="board-heading-pill mt-2 mb-3" style="background: #009CE3"></div>
                        </div>
                        <div class="board-cards">

                            <div class="board-card">
                                <div class="d-flex justify-content-between">
                                    <div class="board-card-pills">
                                        <div class="board-card-pill badge-danger"></div>
                                    </div>
                                    <span class="badge badge-info font-weight-normal" title="Phoenix Campus">PHX</span>
                                </div>
                                <div class="board-card-main d-flex">
                                    <div class="flex-grow-1 mb-2">
                                        <div class="board-card-photo mb-1" style="background-image: url('/GetImage.ashx?id=74');" title="Ted Decker Profile Photo"></div>
                                        <div class="board-card-name">
                                            Ted Decker
                                        </div>
                                        <span class="board-card-assigned d-block text-muted">Ted Decker</span>
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
                                    <span class="text-muted" title="4 Activities - 2 Days Since Last Activity"><i class="fa fa-list"></i> 4 - 2d</span>
                                    <span class="text-muted" title="Opened 3 Days Ago (Aug 12, 2020)"><i class="fa fa-calendar"></i> 3d</span>
                                </div>
                            </div>

                            <div class="board-card">
                                <div class="d-flex justify-content-between">
                                    <div class="board-card-pills">
                                        <div class="board-card-pill badge-danger"></div>
                                    </div>
                                    <span class="badge badge-info font-weight-normal" title="Phoenix Campus">PHX</span>
                                </div>
                                <div class="board-card-main d-flex">
                                    <div class="flex-grow-1 mb-2">
                                        <div class="board-card-photo mb-1" style="background-image: url('/GetImage.ashx?id=74');" title="Ted Decker Profile Photo"></div>
                                        <div class="board-card-name">
                                            Ted Decker
                                        </div>
                                        <span class="board-card-assigned d-block text-muted">Ted Decker</span>
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
                                    <span class="text-muted" title="4 Activities - 2 Days Since Last Activity"><i class="fa fa-list"></i> 4 - 2d</span>
                                    <span class="text-muted" title="Opened 3 Days Ago (Aug 12, 2020)"><i class="fa fa-calendar"></i> 3d</span>
                                </div>
                            </div>

                        </div>
                    </div>

                    <div class="board-column">
                        <div class="board-heading mt-3">
                            <div class="d-flex justify-content-between">
                                <span class="board-column-title">No Contact</span>
                                <span class="board-count">4</span>
                            </div>
                            <div class="board-heading-pill mt-2 mb-3" style="background: #009CE3"></div>
                        </div>
                        <div class="board-cards">

                            <div class="board-card">
                                <div class="d-flex justify-content-between">
                                    <div class="board-card-pills">
                                        <div class="board-card-pill badge-danger"></div>
                                    </div>
                                    <span class="badge badge-info font-weight-normal" title="Phoenix Campus">PHX</span>
                                </div>
                                <div class="board-card-main d-flex">
                                    <div class="flex-grow-1 mb-2">
                                        <div class="board-card-photo mb-1" style="background-image: url('/GetImage.ashx?id=74');" title="Ted Decker Profile Photo"></div>
                                        <div class="board-card-name">
                                            Ted Decker
                                        </div>
                                        <span class="board-card-assigned d-block text-muted">Ted Decker</span>
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
                                    <span class="text-muted" title="4 Activities - 2 Days Since Last Activity"><i class="fa fa-list"></i> 4 - 2d</span>
                                    <span class="text-muted" title="Opened 3 Days Ago (Aug 12, 2020)"><i class="fa fa-calendar"></i> 3d</span>
                                </div>
                            </div>

                            <div class="board-card">
                                <div class="d-flex justify-content-between">
                                    <div class="board-card-pills">
                                        <div class="board-card-pill badge-danger"></div>
                                    </div>
                                    <span class="badge badge-info font-weight-normal" title="Phoenix Campus">PHX</span>
                                </div>
                                <div class="board-card-main d-flex">
                                    <div class="flex-grow-1 mb-2">
                                        <div class="board-card-photo mb-1" style="background-image: url('/GetImage.ashx?id=74');" title="Ted Decker Profile Photo"></div>
                                        <div class="board-card-name">
                                            Ted Decker
                                        </div>
                                        <span class="board-card-assigned d-block text-muted">Ted Decker</span>
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
                                    <span class="text-muted" title="4 Activities - 2 Days Since Last Activity"><i class="fa fa-list"></i> 4 - 2d</span>
                                    <span class="text-muted" title="Opened 3 Days Ago (Aug 12, 2020)"><i class="fa fa-calendar"></i> 3d</span>
                                </div>
                            </div>

                        </div>
                    </div>

                </div>
            </div>
        
        </asp:Panel>

    </ContentTemplate>
</asp:UpdatePanel>