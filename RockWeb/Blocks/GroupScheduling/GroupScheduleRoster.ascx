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
                    <asp:LinkButton ID="btnConfiguration" runat="server" CssClass="btn btn-default btn-square btn-xs" CausesValidation="false" OnClick="btnConfiguration_Click"><i class="fa fa-gear"></i></asp:LinkButton>
                </div>
            </div>
            <div class="locations js-scheduled-occurrences">
                <asp:Repeater ID="rptAttendanceOccurrences" runat="server" OnItemDataBound="rptAttendanceOccurrences_ItemDataBound">
                    <ItemTemplate>
                        <asp:Literal ID="lOccurrenceRosterHTML" runat="server" />
                    </ItemTemplate>
                </asp:Repeater>
            </div>

        </asp:Panel>

        <%-- Roster Configuration (User preferences) --%>
        <asp:Panel ID="pnlConfiguration" runat="server">
            <Rock:ModalDialog ID="mdRosterConfiguration" runat="server" Title="Configuration" CssClass=".js-configuration-modal" ValidationGroup="vgRosterConfiguration" OnSaveClick="mdRosterConfiguration_SaveClick">
                <Content>
                    <div class="row">
                        <div class="col-md-6">
                            <Rock:GroupPicker ID="gpGroups" runat="server" AllowMultiSelect="true" Label="Groups" Required="true" OnSelectItem="gpGroups_SelectItem" ValidationGroup="vgRosterConfiguration" LimitToSchedulingEnabledGroups="true" />
                            <Rock:NotificationBox ID="nbGroupWarning" runat="server" NotificationBoxType="Warning" />
                            <Rock:RockListBox ID="lbSchedules" runat="server" Label="Schedules" ValidationGroup="vgRosterConfiguration" AutoPostBack="true" OnSelectedIndexChanged="lbSchedules_SelectedIndexChanged"/>
                            <Rock:RockCheckBox ID="cbDisplayRole" runat="server" Label="Display Role" ValidationGroup="vgRosterConfiguration"/>
                        </div>
                        <div class="col-md-6">
                            <Rock:RockCheckBox ID="cbIncludeChildGroups" runat="server" Label="Include Child Groups" AutoPostBack="true" OnCheckedChanged="cbIncludeChildGroups_CheckedChanged" ValidationGroup="vgRosterConfiguration"/>
                            <Rock:RockCheckBoxList ID="cblLocations" runat="server" Label="Locations" ValidationGroup="vgRosterConfiguration"/>
                            <Rock:NotificationBox ID="nbLocationsWarning" runat="server" NotificationBoxType="Warning" />
                        </div>
                    </div>
                </Content>
            </Rock:ModalDialog>
        </asp:Panel>

    </ContentTemplate>
</asp:UpdatePanel>
