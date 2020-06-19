<%@ Control Language="C#" AutoEventWireup="true" CodeFile="GroupScheduleRoster.ascx.cs" Inherits="RockWeb.Blocks.GroupScheduling.GroupScheduleRoster" %>

<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>

        <asp:Panel ID="pnlView" runat="server" CssClass="panel panel-block">

            <div class="panel-heading">
                <h1 class="panel-title">
                    <i class="fa fa-calendar-check-o"></i>
                    Group Schedule Roster
                </h1>

                <div class="panel-labels">
                    <asp:Literal ID="lLiveUpdateEnabled" runat="server" Visible="false"><i class='fa fa-check-square-o'></i></asp:Literal>
                    <asp:Literal ID="lLiveUpdateDisabled" runat="server" Visible="true"><i class='fa fa-square-o'></i></asp:Literal>
                    <asp:Literal ID="lLiveUpdateLabel" runat="server" Text="Live Update" />
                    <asp:LinkButton ID="btnConfiguration" runat="server" CssClass="btn btn-default btn-square btn-xs" OnClick="btnConfiguration_Click"><i class="fa fa-gear"></i></asp:LinkButton>
                </div>
            </div>
            <div class="locations js-scheduled-occurrences">
                <asp:Repeater ID="rptAttendanceOccurrences" runat="server" OnItemDataBound="rptAttendanceOccurrences_ItemDataBound">
                    <ItemTemplate>
                    </ItemTemplate>
                </asp:Repeater>
            </div>

        </asp:Panel>

    </ContentTemplate>
</asp:UpdatePanel>
