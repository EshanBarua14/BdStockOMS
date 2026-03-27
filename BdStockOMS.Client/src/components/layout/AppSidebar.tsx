// ============================================================
// BdStockOMS — Updated AppSidebar with full Settings submenu
// File: BdStockOMS.Client/src/components/layout/AppSidebar.tsx
//
// CHANGES FROM DAY 64:
//   - Settings group now has 15 sub-routes instead of placeholder
//   - Each sub-route matches AdminSettingsPage section IDs
//   - Active detection highlights the correct sub-item
// ============================================================
import { useState } from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import {
  LayoutDashboard, TrendingUp, Briefcase, BarChart2,
  Users, GitBranch, UserCheck, Wrench, ChevronDown,
  ChevronRight, Activity, FileText, Globe, Zap,
  DollarSign, Bell, Terminal, Archive, Shield, Key,
  Wifi, Database, Megaphone, Puzzle, Settings,
  AlertTriangle, Server,
} from 'lucide-react';

// ── Types ─────────────────────────────────────────────────────
interface NavItem {
  label: string;
  icon: any;
  path?: string;
  children?: NavItem[];
  badge?: string | number;
  badgeColor?: string;
}

// ── Full nav tree with Settings expanded ─────────────────────
const NAV: NavItem[] = [
  { label: 'Dashboard',    icon: LayoutDashboard, path: '/' },
  { label: 'Trading',      icon: TrendingUp,      path: '/trading' },
  { label: 'Portfolio',    icon: Briefcase,        path: '/portfolio' },
  { label: 'Market Watch', icon: BarChart2,        path: '/market' },
  {
    label: 'Admin',
    icon: Wrench,
    children: [
      { label: 'Brokers',    icon: Server,     path: '/admin/brokers' },
      { label: 'Branches',   icon: GitBranch,  path: '/admin/branches' },
      { label: 'BO Accounts',icon: UserCheck,  path: '/admin/bo-accounts' },
      { label: 'Users',      icon: Users,      path: '/admin/users' },
      { label: 'FIX Gateway',icon: Terminal,   path: '/admin/fix' },
      { label: 'Activities', icon: Activity,   path: '/admin/activities' },
    ],
  },
  {
    label: 'Reports',
    icon: FileText,
    children: [
      { label: 'Trade Report',     icon: TrendingUp, path: '/reports/trades' },
      { label: 'Portfolio Report', icon: Briefcase,  path: '/reports/portfolio' },
      { label: 'BO Report',        icon: UserCheck,  path: '/reports/bo' },
      { label: 'Audit Report',     icon: FileText,   path: '/reports/audit' },
    ],
  },
  {
    label: 'Settings',
    icon: Settings,
    children: [
      { label: 'General',           icon: Globe,       path: '/settings/general' },
      { label: 'Market',            icon: BarChart2,   path: '/settings/market' },
      { label: 'Trading Rules',     icon: Zap,         path: '/settings/trading-rules' },
      { label: 'Fee Structure',     icon: DollarSign,  path: '/settings/fees' },
      { label: 'Notifications',     icon: Bell,        path: '/settings/notifications' },
      { label: 'FIX Engine',        icon: Terminal,    path: '/settings/fix-engine' },
      { label: 'Backup & Restore',  icon: Archive,     path: '/settings/backup' },
      { label: 'System Health',     icon: Activity,    path: '/settings/health' },
      { label: 'Audit Log',         icon: FileText,    path: '/settings/audit-log' },
      { label: 'Roles & Permissions',icon: Shield,     path: '/settings/roles' },
      { label: 'API Keys',          icon: Key,         path: '/settings/api-keys' },
      { label: 'IP Whitelist',      icon: Wifi,        path: '/settings/ip-whitelist' },
      { label: 'Data Retention',    icon: Database,    path: '/settings/data-retention' },
      { label: 'Announcements',     icon: Megaphone,   path: '/settings/announcements' },
      { label: 'Integrations',      icon: Puzzle,      path: '/settings/integrations' },
    ],
  },
];

// ── Sidebar Component ─────────────────────────────────────────
interface Props {
  collapsed: boolean;
}

export default function AppSidebar({ collapsed }: Props) {
  const navigate = useNavigate();
  const location = useLocation();

  // Track which groups are expanded
  const [expanded, setExpanded] = useState<Record<string, boolean>>({
    Settings: location.pathname.startsWith('/settings'),
    Admin: location.pathname.startsWith('/admin'),
    Reports: location.pathname.startsWith('/reports'),
  });

  const toggle = (label: string) =>
    setExpanded(e => ({ ...e, [label]: !e[label] }));

  const isActive = (path?: string) =>
    path ? location.pathname === path || location.pathname.startsWith(path + '/') : false;

  const isGroupActive = (item: NavItem) =>
    item.children?.some(c => isActive(c.path)) ?? false;

  return (
    <aside
      className="flex h-full flex-col border-r border-[var(--t-border)] bg-[var(--t-panel)] transition-all duration-200 overflow-hidden"
      style={{ width: collapsed ? 'var(--oms-sidebar-c)' : 'var(--oms-sidebar-w)' }}
    >
      {/* Logo */}
      <div className="flex h-14 shrink-0 items-center border-b border-[var(--t-border)] px-3">
        {collapsed ? (
          <span className="text-lg font-bold text-[var(--t-accent)]">BD</span>
        ) : (
          <span className="text-sm font-bold tracking-tight text-[var(--t-text1)]">
            <span className="text-[var(--t-accent)]">Bd</span>StockOMS
          </span>
        )}
      </div>

      {/* Nav */}
      <nav className="flex-1 overflow-y-auto py-3 space-y-0.5 px-2 scrollbar-thin">
        {NAV.map(item => {
          const Icon = item.icon;
          const hasChildren = !!item.children?.length;
          const groupActive = isGroupActive(item);
          const open = expanded[item.label] ?? false;

          if (!hasChildren) {
            const active = isActive(item.path);
            return (
              <button
                key={item.label}
                onClick={() => item.path && navigate(item.path)}
                title={collapsed ? item.label : undefined}
                className={`flex w-full items-center gap-2.5 rounded-md px-2.5 py-2 text-sm transition-colors ${active ? 'bg-[var(--t-accent)]/15 text-[var(--t-accent)]' : 'text-[var(--t-text2)] hover:bg-[var(--t-hover)] hover:text-[var(--t-text1)]'}`}
              >
                <Icon size={16} className="shrink-0" />
                {!collapsed && <span className="truncate">{item.label}</span>}
                {!collapsed && item.badge && (
                  <span className={`ml-auto rounded-full px-1.5 py-0.5 text-xs font-medium ${item.badgeColor ?? 'bg-[var(--t-accent)]/20 text-[var(--t-accent)]'}`}>{item.badge}</span>
                )}
              </button>
            );
          }

          return (
            <div key={item.label}>
              {/* Group header */}
              <button
                onClick={() => !collapsed && toggle(item.label)}
                title={collapsed ? item.label : undefined}
                className={`flex w-full items-center gap-2.5 rounded-md px-2.5 py-2 text-sm transition-colors ${groupActive ? 'text-[var(--t-accent)]' : 'text-[var(--t-text2)] hover:bg-[var(--t-hover)] hover:text-[var(--t-text1)]'}`}
              >
                <Icon size={16} className="shrink-0" />
                {!collapsed && (
                  <>
                    <span className="flex-1 truncate text-left">{item.label}</span>
                    {open
                      ? <ChevronDown size={13} className="shrink-0 opacity-60" />
                      : <ChevronRight size={13} className="shrink-0 opacity-60" />
                    }
                  </>
                )}
              </button>

              {/* Children — show when expanded (or always in collapsed for tooltips) */}
              {!collapsed && open && (
                <div className="ml-3 mt-0.5 space-y-0.5 border-l border-[var(--t-border)] pl-3">
                  {item.children!.map(child => {
                    const CIcon = child.icon;
                    const active = isActive(child.path);
                    return (
                      <button
                        key={child.label}
                        onClick={() => child.path && navigate(child.path)}
                        className={`flex w-full items-center gap-2 rounded-md px-2 py-1.5 text-xs transition-colors ${active ? 'bg-[var(--t-accent)]/15 text-[var(--t-accent)] font-medium' : 'text-[var(--t-text3)] hover:bg-[var(--t-hover)] hover:text-[var(--t-text1)]'}`}
                      >
                        <CIcon size={13} className="shrink-0" />
                        <span className="truncate">{child.label}</span>
                        {active && <span className="ml-auto h-1.5 w-1.5 rounded-full bg-[var(--t-accent)]" />}
                      </button>
                    );
                  })}
                </div>
              )}
            </div>
          );
        })}
      </nav>

      {/* Bottom: user/version */}
      {!collapsed && (
        <div className="shrink-0 border-t border-[var(--t-border)] px-3 py-3">
          <p className="text-xs text-[var(--t-text3)]">BdStockOMS v1.0 · Day 65</p>
        </div>
      )}
    </aside>
  );
}
