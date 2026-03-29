namespace BdStockOMS.API.Authorization;

public static class Permissions
{
    // ── Orders ──────────────────────────────────────────────────────
    public const string OrdersPlace        = "orders.place";
    public const string OrdersCancel       = "orders.cancel";
    public const string OrdersAmend        = "orders.amend";
    public const string OrdersViewOwn      = "orders.view.own";
    public const string OrdersViewAll      = "orders.view.all";
    public const string OrdersApprove      = "orders.approve";
    public const string OrdersReject       = "orders.reject";
    public const string OrdersExport       = "orders.export";

    // ── Portfolio ────────────────────────────────────────────────────
    public const string PortfolioViewOwn   = "portfolio.view.own";
    public const string PortfolioViewAll   = "portfolio.view.all";
    public const string PortfolioExport    = "portfolio.export";

    // ── KYC ──────────────────────────────────────────────────────────
    public const string KycView            = "kyc.view";
    public const string KycApprove         = "kyc.approve";
    public const string KycReject          = "kyc.reject";
    public const string KycUpload          = "kyc.upload";

    // ── Reports ──────────────────────────────────────────────────────
    public const string ReportsView        = "reports.view";
    public const string ReportsExport      = "reports.export";
    public const string ReportsCommission  = "reports.commission";
    public const string ReportsSettlement  = "reports.settlement";
    public const string ReportsAudit       = "reports.audit";

    // ── RMS ──────────────────────────────────────────────────────────
    public const string RmsView            = "rms.view";
    public const string RmsSetLimits       = "rms.set_limits";
    public const string RmsOverride        = "rms.override";
    public const string RmsViewBreach      = "rms.view_breach";

    // ── Accounts / BO ────────────────────────────────────────────────
    public const string AccountsView       = "accounts.view";
    public const string AccountsCreate     = "accounts.create";
    public const string AccountsEdit       = "accounts.edit";
    public const string AccountsDeactivate = "accounts.deactivate";
    public const string AccountsViewBalance= "accounts.view_balance";

    // ── Fund Requests ────────────────────────────────────────────────
    public const string FundsView          = "funds.view";
    public const string FundsApprove       = "funds.approve";
    public const string FundsReject        = "funds.reject";
    public const string FundsCreate        = "funds.create";

    // ── Market Data ──────────────────────────────────────────────────
    public const string MarketView         = "market.view";
    public const string MarketExport       = "market.export";
    public const string MarketAdmin        = "market.admin";

    // ── BOS / Reconciliation ─────────────────────────────────────────
    public const string BosView            = "bos.view";
    public const string BosImport          = "bos.import";
    public const string BosExport          = "bos.export";
    public const string BosReconcile       = "bos.reconcile";

    // ── Admin Settings ───────────────────────────────────────────────
    public const string AdminSettings      = "admin.settings";
    public const string AdminUsers         = "admin.users";
    public const string AdminRoles         = "admin.roles";
    public const string AdminPermissions   = "admin.permissions";
    public const string AdminAudit         = "admin.audit";
    public const string AdminBranches      = "admin.branches";
    public const string AdminFees          = "admin.fees";
    public const string AdminIpWhitelist   = "admin.ip_whitelist";
    public const string AdminAnnouncements = "admin.announcements";
    public const string AdminBackup        = "admin.backup";

    // ── Trading ──────────────────────────────────────────────────────
    public const string TradeMonitor       = "trade.monitor";
    public const string TradeApprove       = "trade.approve";
    public const string TradeBlock         = "trade.block";
    public const string TradeViewAll       = "trade.view_all";

    // ── Compliance ───────────────────────────────────────────────────
    public const string ComplianceView     = "compliance.view";
    public const string ComplianceReport   = "compliance.report";
    public const string ComplianceAudit    = "compliance.audit";
    public const string ComplianceFreeze   = "compliance.freeze";

    // ── IPO / T-Bond ─────────────────────────────────────────────────
    public const string IpoView            = "ipo.view";
    public const string IpoApply           = "ipo.apply";
    public const string IpoAdmin           = "ipo.admin";
    public const string TbondView          = "tbond.view";
    public const string TbondApply         = "tbond.apply";
    public const string TbondAdmin         = "tbond.admin";

    // ── Notifications ────────────────────────────────────────────────
    public const string NotificationsView  = "notifications.view";
    public const string NotificationsSend  = "notifications.send";
    public const string NotificationsAdmin = "notifications.admin";

    // ── SuperAdmin only ──────────────────────────────────────────────
    public const string TenantProvision    = "tenant.provision";
    public const string TenantFeatureFlags = "tenant.feature_flags";
    public const string SystemHealth       = "system.health";
    public const string SystemBackup       = "system.backup";

    // ── Helper: all permission keys ──────────────────────────────────
    public static IEnumerable<string> All() =>
        typeof(Permissions)
            .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Where(f => f.FieldType == typeof(string))
            .Select(f => (string)f.GetValue(null)!);

    // ── Default permissions by role ──────────────────────────────────
    public static IEnumerable<string> DefaultsForRole(string roleName) => roleName switch
    {
        "Investor"   => new[] { OrdersPlace, OrdersCancel, OrdersAmend, OrdersViewOwn,
                                PortfolioViewOwn, MarketView, FundsCreate,
                                NotificationsView, IpoView, IpoApply, TbondView, TbondApply },
        "Trader"     => new[] { OrdersPlace, OrdersCancel, OrdersAmend, OrdersViewAll,
                                PortfolioViewAll, PortfolioExport, MarketView, MarketExport,
                                FundsView, FundsApprove, RmsView, TradeMonitor, TradeViewAll,
                                AccountsView, NotificationsView, ReportsView },
        "Admin"      => new[] { OrdersViewAll, OrdersApprove, OrdersReject, OrdersExport,
                                PortfolioViewAll, PortfolioExport, KycView, KycApprove, KycReject,
                                ReportsView, ReportsExport, ReportsCommission, ReportsSettlement,
                                RmsView, RmsSetLimits, AccountsView, AccountsCreate, AccountsEdit,
                                FundsView, FundsApprove, FundsReject, BosView, BosImport, BosExport,
                                BosReconcile, AdminUsers, AdminBranches, AdminFees, AdminAudit,
                                AdminAnnouncements, TradeMonitor, TradeViewAll, ComplianceView,
                                NotificationsView, NotificationsSend, IpoAdmin, TbondAdmin },
        "CCD"        => new[] { AccountsView, AccountsCreate, AccountsEdit, AccountsViewBalance,
                                FundsView, FundsApprove, FundsReject, PortfolioViewAll, ReportsView,
                                BosView, BosImport, BosExport, BosReconcile, NotificationsView },
        "Compliance" => new[] { ComplianceView, ComplianceReport, ComplianceAudit, ComplianceFreeze,
                                ReportsView, ReportsAudit, TradeViewAll, OrdersViewAll, AdminAudit },
        "SuperAdmin" => All().ToArray(),
        _            => Array.Empty<string>()
    };
}
