<%@ Control Language="C#" AutoEventWireup="true" CodeFile="Roster.ascx.cs" Inherits="RockWeb.Blocks.CheckIn.Manager.Roster" %>

<script type="text/javascript">
    Sys.Application.add_load(function () {
        $('.js-cancel-checkin').on('click', function (event) {
            event.stopImmediatePropagation();
            var personName = $(this).parent().siblings(".js-name-cell").find(".js-checkin-person-name").first().text();
            return Rock.dialogs.confirmDelete(event, 'Check-in for ' + personName);
        });
    });
</script>
<Rock:RockUpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>

        <Rock:NotificationBox ID="nbWarning" runat="server" NotificationBoxType="Warning" />

        <asp:Panel ID="pnlContent" runat="server" CssClass="checkin-roster">

            <div class="clearfix">
                <div class="pull-left">
                    <Rock:LocationPicker ID="lpLocation" runat="server" AllowedPickerModes="Named" CssClass="picker-lg" OnSelectLocation="lpLocation_SelectLocation" />
                </div>
                <div class="pull-right">
                    Sub Page Nav Goes Here
                </div>
            </div>

            <div class="panel panel-block">
                <div class="panel-heading clearfix">
                    <h1 class="panel-title pull-left">Room Roster</h1>
                    <div class="pull-right">
                        <Rock:ButtonGroup ID="bgStatus" runat="server" FormGroupCssClass="toggle-container" SelectedItemClass="btn btn-primary active" UnselectedItemClass="btn btn-default" AutoPostBack="true" OnSelectedIndexChanged="bgStatus_SelectedIndexChanged">
                            <asp:ListItem Text="All" Value="1" />
                            <asp:ListItem Text="Checked-in" Value="2" />
                            <asp:ListItem Text="Present" Value="3" />
                        </Rock:ButtonGroup>
                    </div>
                </div>
                <div class="panel-body">
                    <div class="grid grid-panel">
                        <Rock:Grid ID="gAttendees" runat="server" DisplayType="Light" UseFullStylesForLightGrid="true" OnRowDataBound="gAttendees_RowDataBound" OnRowSelected="gAttendees_RowSelected" DataKeyNames="PersonGuid,AttendanceIds">
                            <Columns>
                                <Rock:RockLiteralField ID="lImage" />
                                <Rock:RockLiteralField ID="lName" HeaderText="Name" ItemStyle-CssClass="js-name-cell" />
                                <Rock:RockLiteralField ID="lIcons" />
                                <Rock:RockBoundField DataField="Tag" HeaderText="Tag" />
                                <Rock:RockBoundField DataField="ServiceTimes" HeaderText="Service Times" />
                                <Rock:RockBoundField DataField="StatusString" />
                                <Rock:RockLiteralField ID="lCheckInTime" HeaderText="Check-in Time" />
                                <Rock:LinkButtonField ID="lbCancel" ItemStyle-CssClass="" CssClass="js-cancel-checkin btn btn-default" OnClick="lbCancel_Click" />
                                <Rock:LinkButtonField ID="lbPresent" ItemStyle-CssClass="" CssClass="btn btn-success" OnClick="lbPresent_Click" />
                                <Rock:LinkButtonField ID="lbCheckOut" ItemStyle-CssClass="" CssClass="btn btn-primary" OnClick="lbCheckOut_Click" />
                            </Columns>
                        </Rock:Grid>
                    </div>
                </div>
            </div>

        </asp:Panel>

    </ContentTemplate>
</Rock:RockUpdatePanel>