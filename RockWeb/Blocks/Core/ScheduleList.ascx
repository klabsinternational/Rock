<%@ Control Language="C#" AutoEventWireup="true" CodeFile="ScheduleList.ascx.cs" Inherits="RockWeb.Blocks.Administration.ScheduleList" %>

<asp:UpdatePanel ID="upScheduleList" runat="server">
    <ContentTemplate>
        <asp:Panel ID="pnlScheduleList" runat="server">
            <Rock:ModalAlert ID="mdGridWarning" runat="server" />
            <asp:HiddenField ID="hfCategoryId" runat="server" />

            <div class="panel panel-block">
                <div class="panel-heading">
                    <h1 class="panel-title">
                        <i class="fa fa-calendar"></i>
                        Schedules
                    </h1>
                </div>
                <div class="panel-body">

                    <div class="grid grid-panel">
                        <Rock:Grid ID="gSchedules" runat="server" AllowSorting="false" OnRowSelected="gSchedules_Edit" CssClass="js-grid-schedule-list">
                            <Columns>
                                <Rock:ReorderField />
                                <Rock:RockBoundField DataField="Name" HeaderText="Name" />
                                <Rock:RockBoundField DataField="Category.Name" HeaderText="Category" />
                                <Rock:DeleteField OnClick="gSchedules_Delete" />
                            </Columns>
                        </Rock:Grid>
                    </div>

                </div>
            </div>
        </asp:Panel>

    </ContentTemplate>
</asp:UpdatePanel>
