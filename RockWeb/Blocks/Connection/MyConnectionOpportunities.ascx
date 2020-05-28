<%@ Control Language="C#" AutoEventWireup="true" CodeFile="MyConnectionOpportunities.ascx.cs" Inherits="RockWeb.Blocks.Connection.MyConnectionOpportunities" %>
<%@ Import namespace="Rock" %>
<script>
    Sys.Application.add_load(function () {
        $('.js-legend-badge').tooltip({ html: true, container: 'body', delay: { show: 200, hide: 100 } });
    });

    //Sys.WebForms.PageRequestManager.getInstance().add_endRequest(scrollToGrid);
    function scrollToGrid() {
        if (!$('.js-grid-header').visible(true)) {
            $('html, body').animate({
                scrollTop: $('.js-grid-header').offset().top + 'px'
            }, 'fast');
        }
    }
</script>

<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>
        <div class="row">
            <div class="col-xs-12">
                <div class="form-horizontal label-auto">
                    <Rock:CampusPicker ID="cpCampusFilterForPage" runat="server" CssClass="input-width-lg" AutoPostBack="true" OnSelectedIndexChanged="cpCampusPickerForPage_SelectedIndexChanged" />
                </div>
            </div>
        </div>
        <div class="panel panel-block">
            <div class="panel-heading">
                <h1 class="panel-title">
                    <i class='fa fa-plug'></i>
                    My Connection Requests</h1>

                <div class="pull-right">
                    <asp:Literal ID="lStatusBarContent" runat="server" />
                    <Rock:Toggle ID="tglShowActive" CssClass="margin-r-sm pull-left" runat="server" OffText="All Types" ActiveButtonCssClass="btn-primary" ButtonSizeCssClass="btn-xs" OnText="Active Types" AutoPostBack="true" OnCheckedChanged="tglShowActive_CheckedChanged" Checked="true" />
                    <Rock:Toggle ID="tglMyOpportunities" CssClass="margin-r-sm pull-left" runat="server" OnText="My Requests" OnCssClass="btn-primary" OffCssClass="btn-outline-primary" ActiveButtonCssClass="btn-primary" ButtonSizeCssClass="btn-xs" OffText="All Requests" AutoPostBack="true" OnCheckedChanged="tglMyOpportunities_CheckedChanged" Checked="true" />
                    <asp:Label ID="lTotal" runat="server" CssClass="margin-r-sm pull-left label label-default" Style="line-height:1.6;" />
                    <asp:LinkButton ID="lbConnectionTypes" runat="server" CssClass="btn btn-xs btn-square btn-default pull-right pull-right" OnClick="lbConnectionTypes_Click" CausesValidation="false"> <i title="Options" class="fa fa-gear"></i></asp:LinkButton>
                </div>

            </div>

            <div class="panel-body">
                <Rock:NotificationBox ID="nbNoOpportunities" runat="server" NotificationBoxType="Info" Text="There are no current connection requests." />

                <asp:Repeater ID="rptConnnectionTypes" runat="server" OnItemDataBound="rptConnnectionTypes_ItemDataBound">
                    <ItemTemplate>
                        <asp:Literal ID="lConnectionTypeName" runat="server" />
                        <div class="list-as-blocks has-count clearfix">
                            <ul>
                                <asp:Repeater ID="rptConnectionOpportunities" runat="server" OnItemCommand="rptConnectionOpportunities_ItemCommand">
                                    <ItemTemplate>
                                        <li class='<%# SelectedOpportunityId.HasValue && (int)Eval("Id") == SelectedOpportunityId.Value ? "selected" : "" %> block-status <%# (bool)Eval("IsActive") ? "" : "inactive-item"  %>' title='<%# (bool)Eval("IsActive") ? "" : "This opportunity is inactive."  %>' >
                                            <asp:LinkButton ID="lbConnectionOpportunity" runat="server" CommandArgument='<%# Eval("Id") %>' CommandName="Display">
                                                <%# this.GetOpportunitySummaryHtml( Container.DataItem as OpportunitySummary ) %>
                                            </asp:LinkButton>
                                        </li>
                                    </ItemTemplate>
                                </asp:Repeater>
                            </ul>
                        </div>
                    </ItemTemplate>
                </asp:Repeater>

            </div>
        </div>
    </ContentTemplate>
</asp:UpdatePanel>
