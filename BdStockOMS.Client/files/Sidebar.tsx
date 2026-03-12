/* ═══════════════════════════════════════════════════════════════
   BdStockOMS — Sidebar
   RBAC dynamic side menu — collapses to icons, expands with labels
   ═══════════════════════════════════════════════════════════════ */

import React, { useState } from 'react';
import { useAuth } from '../stores/AuthStore';
import './Sidebar.css';

interface SidebarProps {
  expanded: boolean;
  onToggle: () => void;
}

interface MenuItem {
  id: string;
  icon: string;
  label: string;
  roles?: string[];  // If not specified, available to all
  badge?: number;
}

const menuItems: MenuItem[] = [
  { id: 'watchlist', icon: '☰', label: 'Watchlists' },
  { id: 'chart', icon: '📈', label: 'Charts' },
  { id: 'order', icon: '📋', label: 'Order Console' },
  { id: 'depth', icon: '📊', label: 'Market Depth' },
  { id: 'orderbook', icon: '📖', label: 'Order Book' },
  { id: 'executions', icon: '⚡', label: 'Executions' },
  { id: 'portfolio', icon: '💼', label: 'Portfolio' },
  { id: 'movers', icon: '🔥', label: 'Top Movers' },
  { id: 'heatmap', icon: '🗺', label: 'Sector Heatmap' },
  { id: 'ai', icon: '🤖', label: 'AI Sentiment' },
  { id: 'scanner', icon: '🔍', label: 'Scanner' },
  { id: 'notif', icon: '🔔', label: 'Alerts' },
  { id: 'news', icon: '📰', label: 'News' },
];

const adminMenuItems: MenuItem[] = [
  { id: 'clients', icon: '👥', label: 'Client Manager', roles: ['SuperAdmin', 'Admin'] },
  { id: 'limits', icon: '🔒', label: 'Limit Requests', roles: ['SuperAdmin', 'Admin', 'RiskOfficer'], badge: 3 },
  { id: 'rms', icon: '⚠', label: 'Risk Monitor', roles: ['SuperAdmin', 'Admin', 'RiskOfficer'] },
  { id: 'settlement', icon: '🏦', label: 'Settlement', roles: ['SuperAdmin', 'Admin'] },
  { id: 'reports', icon: '📑', label: 'Broker Reports', roles: ['SuperAdmin', 'Admin'] },
];

export default function Sidebar({ expanded, onToggle }: SidebarProps) {
  const { user, hasRole } = useAuth();
  const [activeItem, setActiveItem] = useState('watchlist');

  const filteredAdminItems = adminMenuItems.filter(item => {
    if (!item.roles) return true;
    return item.roles.some(r => hasRole(r as any));
  });

  const showAdmin = filteredAdminItems.length > 0;

  return (
    <aside className={`sidebar ${expanded ? 'sidebar--expanded' : ''}`}>
      <nav className="sidebar__nav">
        {/* Trading section */}
        <div className="sidebar__section">
          {expanded && <div className="sidebar__section-label">TRADING</div>}
          {menuItems.map(item => (
            <button
              key={item.id}
              className={`sidebar__item ${activeItem === item.id ? 'active' : ''}`}
              onClick={() => setActiveItem(item.id)}
              title={!expanded ? item.label : undefined}
            >
              <span className="sidebar__item-icon">{item.icon}</span>
              {expanded && <span className="sidebar__item-label">{item.label}</span>}
              {item.badge && item.badge > 0 && (
                <span className="sidebar__item-badge">{item.badge}</span>
              )}
            </button>
          ))}
        </div>

        {/* Admin section */}
        {showAdmin && (
          <div className="sidebar__section">
            {expanded && <div className="sidebar__section-label">ADMIN</div>}
            <div className="sidebar__divider" />
            {filteredAdminItems.map(item => (
              <button
                key={item.id}
                className={`sidebar__item ${activeItem === item.id ? 'active' : ''}`}
                onClick={() => setActiveItem(item.id)}
                title={!expanded ? item.label : undefined}
              >
                <span className="sidebar__item-icon">{item.icon}</span>
                {expanded && <span className="sidebar__item-label">{item.label}</span>}
                {item.badge && item.badge > 0 && (
                  <span className="sidebar__item-badge">{item.badge}</span>
                )}
              </button>
            ))}
          </div>
        )}
      </nav>
    </aside>
  );
}
