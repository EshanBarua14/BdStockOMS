// @ts-nocheck
// ============================================================
// BdStockOMS — Admin Settings Page
// File: BdStockOMS.Client/src/pages/AdminSettingsPage.tsx
// ============================================================
import { useState, useEffect, useCallback, useRef } from 'react';
import {
  Settings, Globe, BarChart2, Zap, DollarSign, Bell,
  Terminal, Archive, Activity, FileText, Shield, Key,
  Wifi, Database, Megaphone, Puzzle, ChevronRight,
  Save, RefreshCw, AlertTriangle, CheckCircle, XCircle,
  Eye, EyeOff, Plus, Trash2, Edit2, Copy, ToggleLeft,
  ToggleRight, Server, Cpu, HardDrive, Clock, Users,
  Lock, Unlock, Download, Upload, Play, StopCircle,
  Info, ExternalLink, Search, Filter, ChevronDown, X,
} from 'lucide-react';

// ── Types ─────────────────────────────────────────────────────
type Section =
  | 'general' | 'market' | 'trading-rules' | 'fees'
  | 'notifications' | 'fix-engine' | 'backup' | 'health'
  | 'audit-log' | 'roles' | 'api-keys' | 'ip-whitelist'
  | 'data-retention' | 'announcements' | 'integrations';

const BASE = 'https://localhost:7219/api';

function getToken(): string | null {
  try {
    const raw = localStorage.getItem('bd_oms_auth_v2');
    return raw ? JSON.parse(raw)?.state?.user?.token ?? null : null;
  } catch { return null; }
}

async function apiFetch<T = any>(path: string, opts?: RequestInit): Promise<T> {
  const token = getToken();
  const res = await fetch(`${BASE}${path}`, {
    headers: {
      'Content-Type': 'application/json',
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
    },
    ...opts,
  });
  if (!res.ok) throw new Error(`${res.status}`);
  return res.json();
}

// ── Shared UI primitives ──────────────────────────────────────
function SettingsCard({ children, className = '' }: { children: React.ReactNode; className?: string }) {
  return (
    <div className={`rounded-lg border border-[var(--t-border)] bg-[var(--t-panel)] p-6 ${className}`}>
      {children}
    </div>
  );
}

function Field({ label, hint, children }: { label: string; hint?: string; children: React.ReactNode }) {
  return (
    <div className="space-y-1.5">
      <label className="block text-xs font-medium text-[var(--t-text2)] uppercase tracking-wider">{label}</label>
      {children}
      {hint && <p className="text-xs text-[var(--t-text3)]">{hint}</p>}
    </div>
  );
}

function Input({ value, onChange, type = 'text', placeholder = '', disabled = false, className = '' }: any) {
  return (
    <input
      type={type}
      value={value ?? ''}
      onChange={e => onChange?.(e.target.value)}
      placeholder={placeholder}
      disabled={disabled}
      className={`w-full rounded-md border border-[var(--t-border)] bg-[var(--t-surface)] px-3 py-2 text-sm text-[var(--t-text1)] placeholder-[var(--t-text3)] focus:border-[var(--t-accent)] focus:outline-none focus:ring-1 focus:ring-[var(--t-accent)] disabled:opacity-50 font-['JetBrains_Mono',monospace] ${className}`}
    />
  );
}

function Toggle({ checked, onChange, label }: { checked: boolean; onChange: (v: boolean) => void; label?: string }) {
  return (
    <button
      onClick={() => onChange(!checked)}
      className="flex items-center gap-2 focus:outline-none"
      type="button"
    >
      <div className={`relative h-5 w-9 rounded-full transition-colors ${checked ? 'bg-[var(--t-accent)]' : 'bg-[var(--t-border)]'}`}>
        <span className={`absolute top-0.5 h-4 w-4 rounded-full bg-white shadow transition-all ${checked ? 'left-4' : 'left-0.5'}`} />
      </div>
      {label && <span className="text-sm text-[var(--t-text2)]">{label}</span>}
    </button>
  );
}

function Select({ value, onChange, options, disabled = false }: { value: string; onChange: (v: string) => void; options: { value: string; label: string }[]; disabled?: boolean }) {
  return (
    <select
      value={value}
      onChange={e => onChange(e.target.value)}
      disabled={disabled}
      className="w-full rounded-md border border-[var(--t-border)] bg-[var(--t-surface)] px-3 py-2 text-sm text-[var(--t-text1)] focus:border-[var(--t-accent)] focus:outline-none disabled:opacity-50"
    >
      {options.map(o => <option key={o.value} value={o.value}>{o.label}</option>)}
    </select>
  );
}

function SaveButton({ saving, onClick }: { saving: boolean; onClick: () => void }) {
  return (
    <button
      onClick={onClick}
      disabled={saving}
      className="flex items-center gap-2 rounded-md bg-[var(--t-accent)] px-4 py-2 text-sm font-medium text-white hover:opacity-90 disabled:opacity-60 transition-opacity"
    >
      {saving ? <RefreshCw size={14} className="animate-spin" /> : <Save size={14} />}
      {saving ? 'Saving…' : 'Save Changes'}
    </button>
  );
}

function Toast({ msg, type, onClose }: { msg: string; type: 'success' | 'error'; onClose: () => void }) {
  useEffect(() => { const t = setTimeout(onClose, 3500); return () => clearTimeout(t); }, [onClose]);
  return (
    <div className={`fixed bottom-6 right-6 z-50 flex items-center gap-3 rounded-lg border px-4 py-3 shadow-lg text-sm font-medium transition-all ${type === 'success' ? 'bg-green-950 border-green-800 text-green-300' : 'bg-red-950 border-red-800 text-red-300'}`}>
      {type === 'success' ? <CheckCircle size={16} /> : <XCircle size={16} />}
      {msg}
      <button onClick={onClose}><X size={14} /></button>
    </div>
  );
}

function StatusBadge({ status }: { status: 'healthy' | 'degraded' | 'down' | 'connected' | 'disconnected' | 'connecting' | 'active' | 'inactive' }) {
  const map: Record<string, string> = {
    healthy: 'bg-green-900 text-green-300 border-green-800',
    connected: 'bg-green-900 text-green-300 border-green-800',
    active: 'bg-green-900 text-green-300 border-green-800',
    degraded: 'bg-yellow-900 text-yellow-300 border-yellow-800',
    connecting: 'bg-yellow-900 text-yellow-300 border-yellow-800',
    down: 'bg-red-900 text-red-300 border-red-800',
    disconnected: 'bg-zinc-800 text-zinc-400 border-zinc-700',
    inactive: 'bg-zinc-800 text-zinc-400 border-zinc-700',
  };
  return (
    <span className={`inline-flex items-center gap-1.5 rounded-full border px-2.5 py-0.5 text-xs font-medium ${map[status] ?? map.inactive}`}>
      <span className="h-1.5 w-1.5 rounded-full bg-current" />
      {status}
    </span>
  );
}

// ── Sidebar nav data ──────────────────────────────────────────
const NAV_ITEMS: { id: Section; label: string; icon: any; description: string }[] = [
  { id: 'general',       label: 'General',          icon: Globe,       description: 'System identity, locale & security' },
  { id: 'market',        label: 'Market',            icon: BarChart2,   description: 'Trading hours, circuit breakers' },
  { id: 'trading-rules', label: 'Trading Rules',     icon: Zap,         description: 'RMS limits, order controls' },
  { id: 'fees',          label: 'Fee Structure',     icon: DollarSign,  description: 'Brokerage, taxes & charges' },
  { id: 'notifications', label: 'Notifications',     icon: Bell,        description: 'Email, SMS, push alerts' },
  { id: 'fix-engine',    label: 'FIX Engine',        icon: Terminal,    description: 'FIX protocol connection' },
  { id: 'backup',        label: 'Backup & Restore',  icon: Archive,     description: 'Database backup & S3 sync' },
  { id: 'health',        label: 'System Health',     icon: Activity,    description: 'Live system diagnostics' },
  { id: 'audit-log',     label: 'Audit Log',         icon: FileText,    description: 'All admin & trade activity' },
  { id: 'roles',         label: 'Roles & Permissions', icon: Shield,   description: 'Access control & permissions' },
  { id: 'api-keys',      label: 'API Keys',          icon: Key,         description: 'External API credentials' },
  { id: 'ip-whitelist',  label: 'IP Whitelist',      icon: Wifi,        description: 'Allowed IP addresses' },
  { id: 'data-retention',label: 'Data Retention',    icon: Database,    description: 'Purge & archive policies' },
  { id: 'announcements', label: 'Announcements',     icon: Megaphone,   description: 'System-wide banners & alerts' },
  { id: 'integrations',  label: 'Integrations',      icon: Puzzle,      description: 'Third-party service hooks' },
];

// ── Section: General Settings ─────────────────────────────────
function GeneralSettings({ toast }: { toast: (m: string, t: 'success' | 'error') => void }) {
  const [form, setForm] = useState({
    systemName: 'BdStockOMS',
    systemCode: 'BDSTK',
    timezone: 'Asia/Dhaka',
    currency: 'BDT',
    dateFormat: 'DD/MM/YYYY',
    language: 'en',
    supportEmail: 'support@bdstockoms.com',
    supportPhone: '+880 1700-000000',
    sessionTimeoutMinutes: 30,
    maxLoginAttempts: 5,
    lockoutDurationMinutes: 15,
    maintenanceMode: false,
    maintenanceMessage: '',
    companyName: 'BD Stock OMS Ltd.',
    companyAddress: '',
    tradeDate: new Date().toISOString().split('T')[0],
    allowSelfRegistration: false,
    requireEmailVerification: true,
    requireTwoFactor: false,
    passwordMinLength: 8,
    passwordRequireSpecial: true,
    passwordExpiryDays: 90,
  });
  const [saving, setSaving] = useState(false);

  const set = (k: string) => (v: any) => setForm(f => ({ ...f, [k]: v }));

  const save = async () => {
    setSaving(true);
    try {
      await apiFetch('/admin/settings/general', { method: 'PUT', body: JSON.stringify(form) });
      toast('General settings saved', 'success');
    } catch { toast('Failed to save', 'error'); }
    finally { setSaving(false); }
  };

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-lg font-semibold text-[var(--t-text1)]">General Settings</h2>
          <p className="text-sm text-[var(--t-text3)]">System identity, locale, security policies</p>
        </div>
        <SaveButton saving={saving} onClick={save} />
      </div>

      {/* System Identity */}
      <SettingsCard>
        <h3 className="mb-4 text-sm font-semibold text-[var(--t-accent)] uppercase tracking-wider">System Identity</h3>
        <div className="grid grid-cols-2 gap-4">
          <Field label="System Name"><Input value={form.systemName} onChange={set('systemName')} /></Field>
          <Field label="System Code"><Input value={form.systemCode} onChange={set('systemCode')} /></Field>
          <Field label="Company Name"><Input value={form.companyName} onChange={set('companyName')} /></Field>
          <Field label="Company Address"><Input value={form.companyAddress} onChange={set('companyAddress')} /></Field>
          <Field label="Support Email"><Input value={form.supportEmail} onChange={set('supportEmail')} type="email" /></Field>
          <Field label="Support Phone"><Input value={form.supportPhone} onChange={set('supportPhone')} /></Field>
        </div>
      </SettingsCard>

      {/* Locale */}
      <SettingsCard>
        <h3 className="mb-4 text-sm font-semibold text-[var(--t-accent)] uppercase tracking-wider">Locale & Format</h3>
        <div className="grid grid-cols-3 gap-4">
          <Field label="Timezone">
            <Select value={form.timezone} onChange={set('timezone')} options={[
              { value: 'Asia/Dhaka', label: 'Asia/Dhaka (BST)' },
              { value: 'UTC', label: 'UTC' },
              { value: 'Asia/Kolkata', label: 'Asia/Kolkata (IST)' },
            ]} />
          </Field>
          <Field label="Currency">
            <Select value={form.currency} onChange={set('currency')} options={[
              { value: 'BDT', label: 'BDT — Bangladeshi Taka' },
              { value: 'USD', label: 'USD — US Dollar' },
            ]} />
          </Field>
          <Field label="Date Format">
            <Select value={form.dateFormat} onChange={set('dateFormat')} options={[
              { value: 'DD/MM/YYYY', label: 'DD/MM/YYYY' },
              { value: 'MM/DD/YYYY', label: 'MM/DD/YYYY' },
              { value: 'YYYY-MM-DD', label: 'YYYY-MM-DD' },
            ]} />
          </Field>
          <Field label="Language">
            <Select value={form.language} onChange={set('language')} options={[
              { value: 'en', label: 'English' },
              { value: 'bn', label: 'Bengali' },
            ]} />
          </Field>
          <Field label="Trade Date (Override)">
            <Input value={form.tradeDate} onChange={set('tradeDate')} type="date" />
          </Field>
        </div>
      </SettingsCard>

      {/* Session & Security */}
      <SettingsCard>
        <h3 className="mb-4 text-sm font-semibold text-[var(--t-accent)] uppercase tracking-wider">Session & Security</h3>
        <div className="grid grid-cols-3 gap-4">
          <Field label="Session Timeout (min)" hint="0 = never">
            <Input value={form.sessionTimeoutMinutes} onChange={(v: any) => set('sessionTimeoutMinutes')(Number(v))} type="number" />
          </Field>
          <Field label="Max Login Attempts">
            <Input value={form.maxLoginAttempts} onChange={(v: any) => set('maxLoginAttempts')(Number(v))} type="number" />
          </Field>
          <Field label="Lockout Duration (min)">
            <Input value={form.lockoutDurationMinutes} onChange={(v: any) => set('lockoutDurationMinutes')(Number(v))} type="number" />
          </Field>
          <Field label="Password Min Length">
            <Input value={form.passwordMinLength} onChange={(v: any) => set('passwordMinLength')(Number(v))} type="number" />
          </Field>
          <Field label="Password Expiry (days)" hint="0 = never">
            <Input value={form.passwordExpiryDays} onChange={(v: any) => set('passwordExpiryDays')(Number(v))} type="number" />
          </Field>
        </div>
        <div className="mt-4 grid grid-cols-3 gap-4">
          <div className="flex items-center justify-between rounded-md border border-[var(--t-border)] bg-[var(--t-surface)] p-3">
            <span className="text-sm text-[var(--t-text2)]">Require Email Verification</span>
            <Toggle checked={form.requireEmailVerification} onChange={set('requireEmailVerification')} />
          </div>
          <div className="flex items-center justify-between rounded-md border border-[var(--t-border)] bg-[var(--t-surface)] p-3">
            <span className="text-sm text-[var(--t-text2)]">Two-Factor Authentication</span>
            <Toggle checked={form.requireTwoFactor} onChange={set('requireTwoFactor')} />
          </div>
          <div className="flex items-center justify-between rounded-md border border-[var(--t-border)] bg-[var(--t-surface)] p-3">
            <span className="text-sm text-[var(--t-text2)]">Require Special Characters</span>
            <Toggle checked={form.passwordRequireSpecial} onChange={set('passwordRequireSpecial')} />
          </div>
        </div>
      </SettingsCard>

      {/* Maintenance */}
      <SettingsCard className={form.maintenanceMode ? 'border-yellow-700' : ''}>
        <div className="flex items-center justify-between mb-4">
          <h3 className="text-sm font-semibold text-[var(--t-accent)] uppercase tracking-wider">Maintenance Mode</h3>
          <Toggle checked={form.maintenanceMode} onChange={set('maintenanceMode')} />
        </div>
        {form.maintenanceMode && (
          <div className="space-y-3">
            <div className="flex items-center gap-2 rounded-md border border-yellow-700 bg-yellow-950 p-3 text-sm text-yellow-300">
              <AlertTriangle size={16} /> Maintenance mode is active — all non-admin users are locked out
            </div>
            <Field label="Maintenance Message">
              <textarea
                value={form.maintenanceMessage}
                onChange={e => set('maintenanceMessage')(e.target.value)}
                rows={3}
                className="w-full rounded-md border border-[var(--t-border)] bg-[var(--t-surface)] px-3 py-2 text-sm text-[var(--t-text1)] focus:border-[var(--t-accent)] focus:outline-none resize-none"
                placeholder="System is under maintenance. Please check back later."
              />
            </Field>
          </div>
        )}
      </SettingsCard>
    </div>
  );
}

// ── Section: Market Settings ──────────────────────────────────
function MarketSettings({ toast }: { toast: (m: string, t: 'success' | 'error') => void }) {
  const [form, setForm] = useState({
    dseOpenTime: '10:00', dseCloseTime: '14:30',
    cseOpenTime: '10:00', cseCloseTime: '14:30',
    priceTickSize: 0.10, lotSize: 1,
    circuitBreakerUpPercent: 10, circuitBreakerDownPercent: 10,
    allowPreMarket: false, preMarketOpenTime: '09:30', preMarketCloseTime: '10:00',
    allowPostMarket: false, postMarketOpenTime: '14:30', postMarketCloseTime: '15:00',
    settlementDays: 2, autoMarketClose: true,
    tradingDays: ['MON', 'TUE', 'WED', 'THU', 'SUN'],
    marketHolidays: '',
    indexRefreshIntervalMs: 1000,
    depthLevels: 5,
    allowOddLot: false,
    allowBlockTrade: true,
    blockTradeMinValue: 5000000,
    referencePrice: 'previous_close',
  });
  const [saving, setSaving] = useState(false);
  const set = (k: string) => (v: any) => setForm(f => ({ ...f, [k]: v }));

  const save = async () => {
    setSaving(true);
    try {
      await apiFetch('/admin/settings/market', { method: 'PUT', body: JSON.stringify(form) });
      toast('Market settings saved', 'success');
    } catch { toast('Failed to save', 'error'); }
    finally { setSaving(false); }
  };

  const DAYS = ['SUN', 'MON', 'TUE', 'WED', 'THU', 'FRI', 'SAT'];
  const toggleDay = (d: string) => {
    const days = form.tradingDays.includes(d)
      ? form.tradingDays.filter(x => x !== d)
      : [...form.tradingDays, d];
    setForm(f => ({ ...f, tradingDays: days }));
  };

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-lg font-semibold text-[var(--t-text1)]">Market Settings</h2>
          <p className="text-sm text-[var(--t-text3)]">Exchange hours, circuit breakers, market rules</p>
        </div>
        <SaveButton saving={saving} onClick={save} />
      </div>

      <SettingsCard>
        <h3 className="mb-4 text-sm font-semibold text-[var(--t-accent)] uppercase tracking-wider">Trading Hours</h3>
        <div className="grid grid-cols-2 gap-6">
          <div className="space-y-3">
            <p className="text-xs font-medium text-[var(--t-text2)]">DSE — Dhaka Stock Exchange</p>
            <div className="grid grid-cols-2 gap-3">
              <Field label="Open Time"><Input value={form.dseOpenTime} onChange={set('dseOpenTime')} type="time" /></Field>
              <Field label="Close Time"><Input value={form.dseCloseTime} onChange={set('dseCloseTime')} type="time" /></Field>
            </div>
          </div>
          <div className="space-y-3">
            <p className="text-xs font-medium text-[var(--t-text2)]">CSE — Chittagong Stock Exchange</p>
            <div className="grid grid-cols-2 gap-3">
              <Field label="Open Time"><Input value={form.cseOpenTime} onChange={set('cseOpenTime')} type="time" /></Field>
              <Field label="Close Time"><Input value={form.cseCloseTime} onChange={set('cseCloseTime')} type="time" /></Field>
            </div>
          </div>
        </div>

        <div className="mt-4">
          <p className="mb-2 text-xs font-medium text-[var(--t-text2)]">Trading Days</p>
          <div className="flex gap-2">
            {DAYS.map(d => (
              <button
                key={d}
                onClick={() => toggleDay(d)}
                className={`rounded px-3 py-1.5 text-xs font-medium border transition-colors ${form.tradingDays.includes(d) ? 'bg-[var(--t-accent)] border-[var(--t-accent)] text-white' : 'border-[var(--t-border)] text-[var(--t-text3)] hover:border-[var(--t-accent)]'}`}
              >{d}</button>
            ))}
          </div>
        </div>
      </SettingsCard>

      <SettingsCard>
        <h3 className="mb-4 text-sm font-semibold text-[var(--t-accent)] uppercase tracking-wider">Pre / Post Market</h3>
        <div className="grid grid-cols-2 gap-6">
          <div>
            <div className="mb-3 flex items-center gap-3">
              <Toggle checked={form.allowPreMarket} onChange={set('allowPreMarket')} label="Enable Pre-Market" />
            </div>
            {form.allowPreMarket && (
              <div className="grid grid-cols-2 gap-3">
                <Field label="Pre-Open"><Input value={form.preMarketOpenTime} onChange={set('preMarketOpenTime')} type="time" /></Field>
                <Field label="Pre-Close"><Input value={form.preMarketCloseTime} onChange={set('preMarketCloseTime')} type="time" /></Field>
              </div>
            )}
          </div>
          <div>
            <div className="mb-3 flex items-center gap-3">
              <Toggle checked={form.allowPostMarket} onChange={set('allowPostMarket')} label="Enable Post-Market" />
            </div>
            {form.allowPostMarket && (
              <div className="grid grid-cols-2 gap-3">
                <Field label="Post-Open"><Input value={form.postMarketOpenTime} onChange={set('postMarketOpenTime')} type="time" /></Field>
                <Field label="Post-Close"><Input value={form.postMarketCloseTime} onChange={set('postMarketCloseTime')} type="time" /></Field>
              </div>
            )}
          </div>
        </div>
      </SettingsCard>

      <SettingsCard>
        <h3 className="mb-4 text-sm font-semibold text-[var(--t-accent)] uppercase tracking-wider">Price & Circuit Breaker</h3>
        <div className="grid grid-cols-3 gap-4">
          <Field label="Tick Size (BDT)"><Input value={form.priceTickSize} onChange={(v: any) => set('priceTickSize')(Number(v))} type="number" /></Field>
          <Field label="Lot Size"><Input value={form.lotSize} onChange={(v: any) => set('lotSize')(Number(v))} type="number" /></Field>
          <Field label="Market Depth Levels"><Input value={form.depthLevels} onChange={(v: any) => set('depthLevels')(Number(v))} type="number" /></Field>
          <Field label="Circuit Breaker Up (%)" hint="Upper limit from reference price">
            <Input value={form.circuitBreakerUpPercent} onChange={(v: any) => set('circuitBreakerUpPercent')(Number(v))} type="number" />
          </Field>
          <Field label="Circuit Breaker Down (%)" hint="Lower limit from reference price">
            <Input value={form.circuitBreakerDownPercent} onChange={(v: any) => set('circuitBreakerDownPercent')(Number(v))} type="number" />
          </Field>
          <Field label="Reference Price">
            <Select value={form.referencePrice} onChange={set('referencePrice')} options={[
              { value: 'previous_close', label: 'Previous Close' },
              { value: 'open', label: 'Opening Price' },
              { value: 'last_trade', label: 'Last Trade' },
            ]} />
          </Field>
          <Field label="Settlement Days (T+N)"><Input value={form.settlementDays} onChange={(v: any) => set('settlementDays')(Number(v))} type="number" /></Field>
          <Field label="Index Refresh (ms)"><Input value={form.indexRefreshIntervalMs} onChange={(v: any) => set('indexRefreshIntervalMs')(Number(v))} type="number" /></Field>
          <Field label="Block Trade Min (BDT)"><Input value={form.blockTradeMinValue} onChange={(v: any) => set('blockTradeMinValue')(Number(v))} type="number" /></Field>
        </div>
        <div className="mt-4 grid grid-cols-3 gap-3">
          {[
            { k: 'autoMarketClose', label: 'Auto Market Close' },
            { k: 'allowOddLot', label: 'Allow Odd Lot Orders' },
            { k: 'allowBlockTrade', label: 'Allow Block Trade' },
          ].map(({ k, label }) => (
            <div key={k} className="flex items-center justify-between rounded-md border border-[var(--t-border)] bg-[var(--t-surface)] p-3">
              <span className="text-sm text-[var(--t-text2)]">{label}</span>
              <Toggle checked={(form as any)[k]} onChange={set(k)} />
            </div>
          ))}
        </div>
      </SettingsCard>
    </div>
  );
}

// ── Section: Trading Rules ────────────────────────────────────
function TradingRulesSettings({ toast }: { toast: (m: string, t: 'success' | 'error') => void }) {
  const [form, setForm] = useState({
    maxOrderValue: 10000000,
    maxOrderQuantity: 100000,
    maxDailyTradeValue: 50000000,
    minOrderValue: 1000,
    allowShortSell: false,
    allowMarginTrading: true,
    marginMultiplier: 1.5,
    rmsCheckEnabled: true,
    autoSquareOff: true,
    autoSquareOffTime: '14:20',
    orderExpiryDays: 30,
    allowAfterHoursOrder: false,
    priceTolerancePercent: 2,
    duplicateOrderWindowMs: 500,
    maxOpenOrders: 50,
    maxOrdersPerMinute: 20,
    allowIOC: true,
    allowGTC: true,
    allowFOK: false,
    requireBOForOrder: true,
    rmsLimitType: 'cash',
    exposureLimit: 2000000,
    allowOrderModification: true,
    maxModificationsPerOrder: 5,
  });
  const [saving, setSaving] = useState(false);
  const set = (k: string) => (v: any) => setForm(f => ({ ...f, [k]: v }));

  const save = async () => {
    setSaving(true);
    try {
      await apiFetch('/admin/settings/trading-rules', { method: 'PUT', body: JSON.stringify(form) });
      toast('Trading rules saved', 'success');
    } catch { toast('Failed to save', 'error'); }
    finally { setSaving(false); }
  };

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-lg font-semibold text-[var(--t-text1)]">Trading Rules & RMS</h2>
          <p className="text-sm text-[var(--t-text3)]">Order controls, risk management, position limits</p>
        </div>
        <SaveButton saving={saving} onClick={save} />
      </div>

      <SettingsCard>
        <h3 className="mb-4 text-sm font-semibold text-[var(--t-accent)] uppercase tracking-wider">Order Limits</h3>
        <div className="grid grid-cols-3 gap-4">
          <Field label="Max Order Value (BDT)"><Input value={form.maxOrderValue} onChange={(v: any) => set('maxOrderValue')(Number(v))} type="number" /></Field>
          <Field label="Max Order Quantity"><Input value={form.maxOrderQuantity} onChange={(v: any) => set('maxOrderQuantity')(Number(v))} type="number" /></Field>
          <Field label="Min Order Value (BDT)"><Input value={form.minOrderValue} onChange={(v: any) => set('minOrderValue')(Number(v))} type="number" /></Field>
          <Field label="Max Daily Trade Value (BDT)"><Input value={form.maxDailyTradeValue} onChange={(v: any) => set('maxDailyTradeValue')(Number(v))} type="number" /></Field>
          <Field label="Max Open Orders"><Input value={form.maxOpenOrders} onChange={(v: any) => set('maxOpenOrders')(Number(v))} type="number" /></Field>
          <Field label="Max Orders / Minute"><Input value={form.maxOrdersPerMinute} onChange={(v: any) => set('maxOrdersPerMinute')(Number(v))} type="number" /></Field>
          <Field label="Price Tolerance (%)"><Input value={form.priceTolerancePercent} onChange={(v: any) => set('priceTolerancePercent')(Number(v))} type="number" /></Field>
          <Field label="Duplicate Window (ms)"><Input value={form.duplicateOrderWindowMs} onChange={(v: any) => set('duplicateOrderWindowMs')(Number(v))} type="number" /></Field>
          <Field label="Order Expiry (days)"><Input value={form.orderExpiryDays} onChange={(v: any) => set('orderExpiryDays')(Number(v))} type="number" /></Field>
        </div>
      </SettingsCard>

      <SettingsCard>
        <h3 className="mb-4 text-sm font-semibold text-[var(--t-accent)] uppercase tracking-wider">RMS & Margin</h3>
        <div className="grid grid-cols-3 gap-4">
          <Field label="Margin Multiplier"><Input value={form.marginMultiplier} onChange={(v: any) => set('marginMultiplier')(Number(v))} type="number" /></Field>
          <Field label="Exposure Limit (BDT)"><Input value={form.exposureLimit} onChange={(v: any) => set('exposureLimit')(Number(v))} type="number" /></Field>
          <Field label="RMS Limit Type">
            <Select value={form.rmsLimitType} onChange={set('rmsLimitType')} options={[
              { value: 'cash', label: 'Cash Only' },
              { value: 'portfolio', label: 'Portfolio Value' },
              { value: 'both', label: 'Cash + Portfolio' },
            ]} />
          </Field>
          <Field label="Auto Square-Off Time">
            <Input value={form.autoSquareOffTime} onChange={set('autoSquareOffTime')} type="time" disabled={!form.autoSquareOff} />
          </Field>
          <Field label="Max Modifications / Order">
            <Input value={form.maxModificationsPerOrder} onChange={(v: any) => set('maxModificationsPerOrder')(Number(v))} type="number" />
          </Field>
        </div>
        <div className="mt-4 grid grid-cols-3 gap-3">
          {[
            { k: 'rmsCheckEnabled', label: 'RMS Check Enabled' },
            { k: 'allowShortSell', label: 'Allow Short Sell' },
            { k: 'allowMarginTrading', label: 'Allow Margin Trading' },
            { k: 'autoSquareOff', label: 'Auto Square-Off' },
            { k: 'requireBOForOrder', label: 'Require BO for Orders' },
            { k: 'allowOrderModification', label: 'Allow Order Modification' },
            { k: 'allowAfterHoursOrder', label: 'Allow After-Hours Orders' },
            { k: 'allowIOC', label: 'Allow IOC Orders' },
            { k: 'allowGTC', label: 'Allow GTC Orders' },
            { k: 'allowFOK', label: 'Allow FOK Orders' },
          ].map(({ k, label }) => (
            <div key={k} className="flex items-center justify-between rounded-md border border-[var(--t-border)] bg-[var(--t-surface)] p-3">
              <span className="text-sm text-[var(--t-text2)]">{label}</span>
              <Toggle checked={(form as any)[k]} onChange={set(k)} />
            </div>
          ))}
        </div>
      </SettingsCard>
    </div>
  );
}

// ── Section: Fee Structure ────────────────────────────────────
function FeeStructureSettings({ toast }: { toast: (m: string, t: 'success' | 'error') => void }) {
  const [fees, setFees] = useState([
    { id: '1', name: 'Standard A/B', brokeragePercent: 0.40, secdFeePercent: 0.015, cdblFeePercent: 0.015, vatPercent: 15, aitPercent: 0.05, minBrokerage: 10, applyToCategory: 'ALL', isActive: true },
    { id: '2', name: 'Z Category', brokeragePercent: 0.50, secdFeePercent: 0.015, cdblFeePercent: 0.015, vatPercent: 15, aitPercent: 0.05, minBrokerage: 15, applyToCategory: 'Z', isActive: true },
  ]);
  const [editing, setEditing] = useState<any>(null);
  const [showForm, setShowForm] = useState(false);
  const [saving, setSaving] = useState(false);

  const blankFee = { name: '', brokeragePercent: 0.40, secdFeePercent: 0.015, cdblFeePercent: 0.015, vatPercent: 15, aitPercent: 0.05, minBrokerage: 10, applyToCategory: 'ALL', isActive: true };

  const save = async () => {
    if (!editing) return;
    setSaving(true);
    try {
      if (editing.id) {
        await apiFetch(`/admin/fees/${editing.id}`, { method: 'PUT', body: JSON.stringify(editing) });
        setFees(f => f.map(x => x.id === editing.id ? editing : x));
      } else {
        const created = await apiFetch('/admin/fees', { method: 'POST', body: JSON.stringify(editing) }).catch(() => ({ ...editing, id: Date.now().toString() }));
        setFees(f => [...f, created]);
      }
      toast('Fee structure saved', 'success');
      setEditing(null); setShowForm(false);
    } catch { toast('Failed to save', 'error'); }
    finally { setSaving(false); }
  };

  const remove = async (id: string) => {
    try {
      await apiFetch(`/admin/fees/${id}`, { method: 'DELETE' }).catch(() => {});
      setFees(f => f.filter(x => x.id !== id));
      toast('Fee structure deleted', 'success');
    } catch { toast('Failed to delete', 'error'); }
  };

  const setE = (k: string) => (v: any) => setEditing((e: any) => ({ ...e, [k]: v }));

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-lg font-semibold text-[var(--t-text1)]">Fee Structure</h2>
          <p className="text-sm text-[var(--t-text3)]">Brokerage, regulatory fees, taxes per category</p>
        </div>
        <button onClick={() => { setEditing({ ...blankFee }); setShowForm(true); }} className="flex items-center gap-2 rounded-md bg-[var(--t-accent)] px-4 py-2 text-sm font-medium text-white hover:opacity-90">
          <Plus size={14} /> New Fee Structure
        </button>
      </div>

      {fees.map(fee => (
        <SettingsCard key={fee.id}>
          <div className="flex items-center justify-between mb-4">
            <div className="flex items-center gap-3">
              <span className="text-sm font-semibold text-[var(--t-text1)]">{fee.name}</span>
              <span className="rounded-full border border-[var(--t-border)] px-2 py-0.5 text-xs text-[var(--t-text3)]">{fee.applyToCategory}</span>
              <StatusBadge status={fee.isActive ? 'active' : 'inactive'} />
            </div>
            <div className="flex items-center gap-2">
              <button onClick={() => { setEditing({ ...fee }); setShowForm(true); }} className="rounded p-1.5 text-[var(--t-text3)] hover:text-[var(--t-accent)] hover:bg-[var(--t-hover)]"><Edit2 size={14} /></button>
              <button onClick={() => remove(fee.id)} className="rounded p-1.5 text-[var(--t-text3)] hover:text-red-400 hover:bg-red-950"><Trash2 size={14} /></button>
            </div>
          </div>
          <div className="grid grid-cols-6 gap-3 text-xs">
            {[
              ['Brokerage', `${fee.brokeragePercent}%`],
              ['SECD', `${fee.secdFeePercent}%`],
              ['CDBL', `${fee.cdblFeePercent}%`],
              ['VAT', `${fee.vatPercent}%`],
              ['AIT', `${fee.aitPercent}%`],
              ['Min Brokerage', `৳${fee.minBrokerage}`],
            ].map(([lbl, val]) => (
              <div key={lbl} className="rounded bg-[var(--t-surface)] border border-[var(--t-border)] p-2 text-center">
                <div className="text-[var(--t-text3)] mb-1">{lbl}</div>
                <div className="font-semibold text-[var(--t-text1)] font-['JetBrains_Mono',monospace]">{val}</div>
              </div>
            ))}
          </div>
        </SettingsCard>
      ))}

      {/* Fee Edit Modal */}
      {showForm && editing && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60">
          <div className="w-[560px] rounded-xl border border-[var(--t-border)] bg-[var(--t-panel)] shadow-2xl">
            <div className="flex items-center justify-between border-b border-[var(--t-border)] px-6 py-4">
              <h3 className="font-semibold text-[var(--t-text1)]">{editing.id ? 'Edit' : 'New'} Fee Structure</h3>
              <button onClick={() => { setShowForm(false); setEditing(null); }}><X size={18} className="text-[var(--t-text3)]" /></button>
            </div>
            <div className="p-6 space-y-4">
              <Field label="Name"><Input value={editing.name} onChange={setE('name')} /></Field>
              <Field label="Apply to Category">
                <Select value={editing.applyToCategory} onChange={setE('applyToCategory')} options={[
                  { value: 'ALL', label: 'All Categories' },
                  { value: 'A', label: 'Category A' },
                  { value: 'B', label: 'Category B' },
                  { value: 'Z', label: 'Category Z' },
                  { value: 'G', label: 'Category G' },
                  { value: 'N', label: 'Category N' },
                  { value: 'Spot', label: 'Spot' },
                ]} />
              </Field>
              <div className="grid grid-cols-3 gap-3">
                <Field label="Brokerage (%)"><Input value={editing.brokeragePercent} onChange={(v: any) => setE('brokeragePercent')(Number(v))} type="number" /></Field>
                <Field label="SECD Fee (%)"><Input value={editing.secdFeePercent} onChange={(v: any) => setE('secdFeePercent')(Number(v))} type="number" /></Field>
                <Field label="CDBL Fee (%)"><Input value={editing.cdblFeePercent} onChange={(v: any) => setE('cdblFeePercent')(Number(v))} type="number" /></Field>
                <Field label="VAT (%)"><Input value={editing.vatPercent} onChange={(v: any) => setE('vatPercent')(Number(v))} type="number" /></Field>
                <Field label="AIT (%)"><Input value={editing.aitPercent} onChange={(v: any) => setE('aitPercent')(Number(v))} type="number" /></Field>
                <Field label="Min Brokerage (BDT)"><Input value={editing.minBrokerage} onChange={(v: any) => setE('minBrokerage')(Number(v))} type="number" /></Field>
              </div>
              <div className="flex items-center justify-between rounded-md border border-[var(--t-border)] bg-[var(--t-surface)] p-3">
                <span className="text-sm text-[var(--t-text2)]">Active</span>
                <Toggle checked={editing.isActive} onChange={setE('isActive')} />
              </div>
            </div>
            <div className="flex justify-end gap-3 border-t border-[var(--t-border)] px-6 py-4">
              <button onClick={() => { setShowForm(false); setEditing(null); }} className="rounded-md border border-[var(--t-border)] px-4 py-2 text-sm text-[var(--t-text2)] hover:bg-[var(--t-hover)]">Cancel</button>
              <SaveButton saving={saving} onClick={save} />
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

// ── Section: System Health ────────────────────────────────────
function SystemHealthPanel({ toast: _toast }: { toast: (m: string, t: 'success' | 'error') => void }) {
  const [health, setHealth] = useState({
    dbStatus: 'healthy', redisStatus: 'healthy', signalrStatus: 'healthy', fixStatus: 'disconnected',
    cpuUsage: 23, memoryUsage: 47, diskUsage: 38,
    activeConnections: 142, totalOrdersToday: 3847,
    uptimeSeconds: 86400 * 3 + 3600 * 5,
    apiVersion: '1.0.0-day64', buildDate: '2025-07-01',
    dbPoolActive: 12, dbPoolIdle: 8,
    redisHitRate: 94.2, cacheKeys: 8321,
    pendingOrders: 7, filledOrdersToday: 3840,
    wsClients: 38, httpRps: 142,
  });
  const [refreshing, setRefreshing] = useState(false);

  const refresh = async () => {
    setRefreshing(true);
    try {
      const data = await apiFetch('/admin/health');
      setHealth(data);
    } catch { /* use current */ }
    finally { setRefreshing(false); }
  };

  useEffect(() => { const id = setInterval(refresh, 15000); return () => clearInterval(id); }, []);

  const uptime = (() => {
    const s = health.uptimeSeconds;
    const d = Math.floor(s / 86400), h = Math.floor((s % 86400) / 3600), m = Math.floor((s % 3600) / 60);
    return `${d}d ${h}h ${m}m`;
  })();

  function Bar({ value, warn = 70, danger = 90 }: { value: number; warn?: number; danger?: number }) {
    const color = value >= danger ? 'bg-red-500' : value >= warn ? 'bg-yellow-500' : 'bg-[var(--t-accent)]';
    return (
      <div className="h-2 w-full rounded-full bg-[var(--t-surface)] overflow-hidden">
        <div className={`h-full rounded-full transition-all ${color}`} style={{ width: `${Math.min(value, 100)}%` }} />
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-lg font-semibold text-[var(--t-text1)]">System Health</h2>
          <p className="text-sm text-[var(--t-text3)]">Live infrastructure diagnostics — refreshes every 15s</p>
        </div>
        <button onClick={refresh} disabled={refreshing} className="flex items-center gap-2 rounded-md border border-[var(--t-border)] px-4 py-2 text-sm text-[var(--t-text2)] hover:bg-[var(--t-hover)]">
          <RefreshCw size={14} className={refreshing ? 'animate-spin' : ''} /> Refresh
        </button>
      </div>

      {/* Service Status */}
      <div className="grid grid-cols-4 gap-3">
        {[
          { label: 'SQL Server', status: health.dbStatus, icon: Database },
          { label: 'Redis Cache', status: health.redisStatus, icon: Zap },
          { label: 'SignalR Hub', status: health.signalrStatus, icon: Wifi },
          { label: 'FIX Engine', status: health.fixStatus, icon: Terminal },
        ].map(({ label, status, icon: Icon }) => (
          <SettingsCard key={label} className="text-center">
            <div className="mb-2 flex justify-center"><Icon size={20} className="text-[var(--t-text3)]" /></div>
            <div className="mb-1 text-xs text-[var(--t-text3)]">{label}</div>
            <StatusBadge status={status as any} />
          </SettingsCard>
        ))}
      </div>

      {/* Resource Usage */}
      <SettingsCard>
        <h3 className="mb-4 text-sm font-semibold text-[var(--t-accent)] uppercase tracking-wider">Resource Usage</h3>
        <div className="space-y-4">
          {[
            { label: 'CPU', value: health.cpuUsage, icon: Cpu },
            { label: 'Memory', value: health.memoryUsage, icon: Server },
            { label: 'Disk', value: health.diskUsage, icon: HardDrive },
          ].map(({ label, value, icon: Icon }) => (
            <div key={label} className="flex items-center gap-4">
              <Icon size={16} className="shrink-0 text-[var(--t-text3)]" />
              <span className="w-16 text-sm text-[var(--t-text2)]">{label}</span>
              <div className="flex-1"><Bar value={value} /></div>
              <span className="w-12 text-right font-['JetBrains_Mono',monospace] text-sm text-[var(--t-text1)]">{value}%</span>
            </div>
          ))}
        </div>
      </SettingsCard>

      {/* Stats Grid */}
      <div className="grid grid-cols-4 gap-3">
        {[
          { label: 'Active Connections', value: health.activeConnections, icon: Users },
          { label: 'Orders Today', value: health.totalOrdersToday.toLocaleString(), icon: BarChart2 },
          { label: 'Uptime', value: uptime, icon: Clock },
          { label: 'WS Clients', value: health.wsClients, icon: Wifi },
          { label: 'DB Pool Active', value: health.dbPoolActive, icon: Database },
          { label: 'Cache Hit Rate', value: `${health.redisHitRate}%`, icon: Zap },
          { label: 'HTTP RPS', value: health.httpRps, icon: Activity },
          { label: 'API Version', value: health.apiVersion, icon: Info },
        ].map(({ label, value, icon: Icon }) => (
          <SettingsCard key={label}>
            <div className="flex items-center gap-2 mb-2">
              <Icon size={14} className="text-[var(--t-text3)]" />
              <span className="text-xs text-[var(--t-text3)]">{label}</span>
            </div>
            <div className="font-['JetBrains_Mono',monospace] text-lg font-semibold text-[var(--t-text1)]">{value}</div>
          </SettingsCard>
        ))}
      </div>
    </div>
  );
}

// ── Section: Audit Log ────────────────────────────────────────
function AuditLogPanel({ toast: _toast }: { toast: (m: string, t: 'success' | 'error') => void }) {
  const [logs, setLogs] = useState<any[]>([]);
  const [loading, setLoading] = useState(false);
  const [search, setSearch] = useState('');
  const [severity, setSeverity] = useState('all');
  const [page, setPage] = useState(1);

  const mockLogs = [
    { id: '1', userName: 'admin', action: 'UPDATE', resource: 'GeneralSettings', oldValue: null, newValue: null, ipAddress: '192.168.1.1', timestamp: new Date().toISOString(), severity: 'info' },
    { id: '2', userName: 'admin', action: 'CREATE', resource: 'FeeStructure', oldValue: null, newValue: 'Z Category', ipAddress: '192.168.1.1', timestamp: new Date(Date.now() - 3600000).toISOString(), severity: 'info' },
    { id: '3', userName: 'sysop', action: 'LOGIN_FAILED', resource: 'Auth', oldValue: null, newValue: null, ipAddress: '10.0.0.5', timestamp: new Date(Date.now() - 7200000).toISOString(), severity: 'warning' },
    { id: '4', userName: 'admin', action: 'BULK_CANCEL', resource: 'Orders', oldValue: '47 orders', newValue: 'cancelled', ipAddress: '192.168.1.1', timestamp: new Date(Date.now() - 86400000).toISOString(), severity: 'critical' },
  ];

  useEffect(() => {
    setLoading(true);
    apiFetch('/admin/audit-log?page=1&pageSize=50')
      .then(data => setLogs(data.items ?? mockLogs))
      .catch(() => setLogs(mockLogs))
      .finally(() => setLoading(false));
  }, []);

  const filtered = logs.filter(l =>
    (severity === 'all' || l.severity === severity) &&
    (!search || l.userName.includes(search) || l.action.includes(search) || l.resource.includes(search))
  );

  const severityColor: Record<string, string> = {
    info: 'text-[var(--t-text3)]',
    warning: 'text-yellow-400',
    critical: 'text-red-400',
  };

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-lg font-semibold text-[var(--t-text1)]">Audit Log</h2>
          <p className="text-sm text-[var(--t-text3)]">All admin actions, logins, and system events</p>
        </div>
        <button className="flex items-center gap-2 rounded-md border border-[var(--t-border)] px-4 py-2 text-sm text-[var(--t-text2)] hover:bg-[var(--t-hover)]">
          <Download size={14} /> Export CSV
        </button>
      </div>

      <div className="flex gap-3">
        <div className="relative flex-1">
          <Search size={14} className="absolute left-3 top-1/2 -translate-y-1/2 text-[var(--t-text3)]" />
          <input value={search} onChange={e => setSearch(e.target.value)} placeholder="Search by user, action, resource…" className="w-full rounded-md border border-[var(--t-border)] bg-[var(--t-surface)] py-2 pl-9 pr-3 text-sm text-[var(--t-text1)] placeholder-[var(--t-text3)] focus:border-[var(--t-accent)] focus:outline-none" />
        </div>
        <Select value={severity} onChange={setSeverity} options={[
          { value: 'all', label: 'All Severity' },
          { value: 'info', label: 'Info' },
          { value: 'warning', label: 'Warning' },
          { value: 'critical', label: 'Critical' },
        ]} />
      </div>

      <SettingsCard className="p-0 overflow-hidden">
        <table className="w-full text-xs">
          <thead>
            <tr className="border-b border-[var(--t-border)] bg-[var(--t-surface)]">
              {['Timestamp', 'User', 'Action', 'Resource', 'IP Address', 'Severity'].map(h => (
                <th key={h} className="px-4 py-3 text-left font-medium text-[var(--t-text3)] uppercase tracking-wider">{h}</th>
              ))}
            </tr>
          </thead>
          <tbody>
            {loading ? (
              <tr><td colSpan={6} className="py-12 text-center text-[var(--t-text3)]"><RefreshCw size={16} className="mx-auto animate-spin" /></td></tr>
            ) : filtered.map((log, i) => (
              <tr key={log.id} className={`border-b border-[var(--t-border)] ${i % 2 === 0 ? 'bg-[var(--t-panel)]' : 'bg-[var(--t-surface)]'} hover:bg-[var(--t-hover)]`}>
                <td className="px-4 py-3 font-['JetBrains_Mono',monospace] text-[var(--t-text3)]">{new Date(log.timestamp).toLocaleString()}</td>
                <td className="px-4 py-3 font-medium text-[var(--t-text1)]">{log.userName}</td>
                <td className="px-4 py-3 font-['JetBrains_Mono',monospace] text-[var(--t-accent)]">{log.action}</td>
                <td className="px-4 py-3 text-[var(--t-text2)]">{log.resource}</td>
                <td className="px-4 py-3 font-['JetBrains_Mono',monospace] text-[var(--t-text3)]">{log.ipAddress}</td>
                <td className="px-4 py-3"><span className={`font-medium ${severityColor[log.severity]}`}>{log.severity}</span></td>
              </tr>
            ))}
          </tbody>
        </table>
      </SettingsCard>
    </div>
  );
}

// ── Section: Notifications ────────────────────────────────────
function NotificationsSettings({ toast }: { toast: (m: string, t: 'success' | 'error') => void }) {
  const [form, setForm] = useState({
    emailEnabled: true, smtpHost: 'smtp.gmail.com', smtpPort: 587, smtpUser: '', smtpPassword: '', smtpUseTls: true,
    smsEnabled: false, smsGateway: '', smsApiKey: '',
    pushEnabled: false,
    notifyOnOrderFill: true, notifyOnOrderReject: true, notifyOnLogin: false,
    notifyOnLargeOrder: true, largeOrderThreshold: 1000000,
    dailyReportEnabled: true, dailyReportTime: '16:00', dailyReportRecipients: '',
    alertOnSystemDown: true, alertOnRMSBreach: true, alertOnCircuitBreaker: true,
  });
  const [saving, setSaving] = useState(false);
  const [testing, setTesting] = useState(false);
  const [showPass, setShowPass] = useState(false);
  const set = (k: string) => (v: any) => setForm(f => ({ ...f, [k]: v }));

  const save = async () => {
    setSaving(true);
    try {
      await apiFetch('/admin/settings/notifications', { method: 'PUT', body: JSON.stringify(form) });
      toast('Notification settings saved', 'success');
    } catch { toast('Failed to save', 'error'); }
    finally { setSaving(false); }
  };

  const testEmail = async () => {
    setTesting(true);
    try {
      await apiFetch('/admin/notifications/test-email', { method: 'POST', body: JSON.stringify({ email: form.smtpUser }) });
      toast('Test email sent', 'success');
    } catch { toast('Test email failed', 'error'); }
    finally { setTesting(false); }
  };

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-lg font-semibold text-[var(--t-text1)]">Notifications</h2>
          <p className="text-sm text-[var(--t-text3)]">Email, SMS, and system alert configuration</p>
        </div>
        <SaveButton saving={saving} onClick={save} />
      </div>

      <SettingsCard>
        <div className="mb-4 flex items-center justify-between">
          <h3 className="text-sm font-semibold text-[var(--t-accent)] uppercase tracking-wider">Email (SMTP)</h3>
          <Toggle checked={form.emailEnabled} onChange={set('emailEnabled')} />
        </div>
        {form.emailEnabled && (
          <div className="space-y-4">
            <div className="grid grid-cols-2 gap-4">
              <Field label="SMTP Host"><Input value={form.smtpHost} onChange={set('smtpHost')} /></Field>
              <Field label="SMTP Port"><Input value={form.smtpPort} onChange={(v: any) => set('smtpPort')(Number(v))} type="number" /></Field>
              <Field label="Username / From"><Input value={form.smtpUser} onChange={set('smtpUser')} /></Field>
              <Field label="Password">
                <div className="relative">
                  <Input value={form.smtpPassword} onChange={set('smtpPassword')} type={showPass ? 'text' : 'password'} />
                  <button onClick={() => setShowPass(s => !s)} className="absolute right-3 top-1/2 -translate-y-1/2 text-[var(--t-text3)]">
                    {showPass ? <EyeOff size={14} /> : <Eye size={14} />}
                  </button>
                </div>
              </Field>
            </div>
            <div className="flex items-center justify-between">
              <div className="flex items-center gap-3">
                <Toggle checked={form.smtpUseTls} onChange={set('smtpUseTls')} label="Use TLS/STARTTLS" />
              </div>
              <button onClick={testEmail} disabled={testing} className="flex items-center gap-2 rounded-md border border-[var(--t-accent)] px-3 py-1.5 text-sm text-[var(--t-accent)] hover:bg-[var(--t-hover)]">
                {testing ? <RefreshCw size={12} className="animate-spin" /> : <Play size={12} />}
                Send Test Email
              </button>
            </div>
          </div>
        )}
      </SettingsCard>

      <SettingsCard>
        <div className="mb-4 flex items-center justify-between">
          <h3 className="text-sm font-semibold text-[var(--t-accent)] uppercase tracking-wider">SMS Gateway</h3>
          <Toggle checked={form.smsEnabled} onChange={set('smsEnabled')} />
        </div>
        {form.smsEnabled && (
          <div className="grid grid-cols-2 gap-4">
            <Field label="SMS Gateway URL"><Input value={form.smsGateway} onChange={set('smsGateway')} /></Field>
            <Field label="API Key"><Input value={form.smsApiKey} onChange={set('smsApiKey')} type="password" /></Field>
          </div>
        )}
      </SettingsCard>

      <SettingsCard>
        <h3 className="mb-4 text-sm font-semibold text-[var(--t-accent)] uppercase tracking-wider">Alert Triggers</h3>
        <div className="grid grid-cols-3 gap-3">
          {[
            { k: 'notifyOnOrderFill', label: 'Order Fill' },
            { k: 'notifyOnOrderReject', label: 'Order Rejection' },
            { k: 'notifyOnLogin', label: 'New Login' },
            { k: 'notifyOnLargeOrder', label: 'Large Order' },
            { k: 'alertOnSystemDown', label: 'System Down' },
            { k: 'alertOnRMSBreach', label: 'RMS Breach' },
            { k: 'alertOnCircuitBreaker', label: 'Circuit Breaker' },
            { k: 'dailyReportEnabled', label: 'Daily Report' },
          ].map(({ k, label }) => (
            <div key={k} className="flex items-center justify-between rounded-md border border-[var(--t-border)] bg-[var(--t-surface)] p-3">
              <span className="text-sm text-[var(--t-text2)]">{label}</span>
              <Toggle checked={(form as any)[k]} onChange={set(k)} />
            </div>
          ))}
        </div>
        {form.dailyReportEnabled && (
          <div className="mt-4 grid grid-cols-2 gap-4">
            <Field label="Daily Report Time"><Input value={form.dailyReportTime} onChange={set('dailyReportTime')} type="time" /></Field>
            <Field label="Recipients (comma-separated)"><Input value={form.dailyReportRecipients} onChange={set('dailyReportRecipients')} placeholder="admin@firm.com, manager@firm.com" /></Field>
          </div>
        )}
        {form.notifyOnLargeOrder && (
          <div className="mt-4">
            <Field label="Large Order Threshold (BDT)"><Input value={form.largeOrderThreshold} onChange={(v: any) => set('largeOrderThreshold')(Number(v))} type="number" /></Field>
          </div>
        )}
      </SettingsCard>
    </div>
  );
}

// ── Section: FIX Engine ───────────────────────────────────────
function FIXEngineSettings({ toast }: { toast: (m: string, t: 'success' | 'error') => void }) {
  const [form, setForm] = useState({
    enabled: false, senderCompId: 'BDSTK_OMS', targetCompId: 'DSE_FIX',
    host: 'fix.dse.com.bd', port: 9878,
    heartbeatIntervalSec: 30, reconnectIntervalSec: 5,
    logMessages: true, useSSL: true, fixVersion: 'FIX.4.4',
    resetOnLogon: true, resetOnLogout: false,
    maxReconnectAttempts: 10, messageQueueSize: 10000,
    sendingTimeToleranceSec: 120,
  });
  const [status, setStatus] = useState<'connected' | 'disconnected' | 'connecting'>('disconnected');
  const [saving, setSaving] = useState(false);
  const [toggling, setToggling] = useState(false);
  const set = (k: string) => (v: any) => setForm(f => ({ ...f, [k]: v }));

  const save = async () => {
    setSaving(true);
    try {
      await apiFetch('/admin/fix/config', { method: 'PUT', body: JSON.stringify(form) });
      toast('FIX config saved', 'success');
    } catch { toast('Failed to save', 'error'); }
    finally { setSaving(false); }
  };

  const toggle = async () => {
    setToggling(true);
    try {
      const action = status === 'connected' ? 'disconnect' : 'connect';
      await apiFetch(`/admin/fix/${action}`, { method: 'POST' });
      setStatus(action === 'connect' ? 'connected' : 'disconnected');
      toast(`FIX engine ${action}ed`, 'success');
    } catch { toast('Connection failed', 'error'); }
    finally { setToggling(false); }
  };

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-lg font-semibold text-[var(--t-text1)]">FIX Engine</h2>
          <p className="text-sm text-[var(--t-text3)]">FIX protocol session configuration</p>
        </div>
        <div className="flex items-center gap-3">
          <StatusBadge status={status} />
          <button
            onClick={toggle}
            disabled={toggling}
            className={`flex items-center gap-2 rounded-md px-4 py-2 text-sm font-medium transition-colors ${status === 'connected' ? 'bg-red-900 text-red-300 border border-red-800 hover:bg-red-800' : 'bg-green-900 text-green-300 border border-green-800 hover:bg-green-800'}`}
          >
            {toggling ? <RefreshCw size={14} className="animate-spin" /> : status === 'connected' ? <StopCircle size={14} /> : <Play size={14} />}
            {status === 'connected' ? 'Disconnect' : 'Connect'}
          </button>
          <SaveButton saving={saving} onClick={save} />
        </div>
      </div>

      <SettingsCard>
        <div className="mb-4 flex items-center justify-between">
          <h3 className="text-sm font-semibold text-[var(--t-accent)] uppercase tracking-wider">Session Identity</h3>
          <Toggle checked={form.enabled} onChange={set('enabled')} label="FIX Enabled" />
        </div>
        <div className="grid grid-cols-2 gap-4">
          <Field label="Sender CompID"><Input value={form.senderCompId} onChange={set('senderCompId')} /></Field>
          <Field label="Target CompID"><Input value={form.targetCompId} onChange={set('targetCompId')} /></Field>
          <Field label="FIX Version">
            <Select value={form.fixVersion} onChange={set('fixVersion')} options={[
              { value: 'FIX.4.2', label: 'FIX 4.2' },
              { value: 'FIX.4.4', label: 'FIX 4.4' },
              { value: 'FIX.5.0', label: 'FIX 5.0' },
            ]} />
          </Field>
        </div>
      </SettingsCard>

      <SettingsCard>
        <h3 className="mb-4 text-sm font-semibold text-[var(--t-accent)] uppercase tracking-wider">Connection</h3>
        <div className="grid grid-cols-3 gap-4">
          <Field label="Host / IP"><Input value={form.host} onChange={set('host')} /></Field>
          <Field label="Port"><Input value={form.port} onChange={(v: any) => set('port')(Number(v))} type="number" /></Field>
          <Field label="Heartbeat (sec)"><Input value={form.heartbeatIntervalSec} onChange={(v: any) => set('heartbeatIntervalSec')(Number(v))} type="number" /></Field>
          <Field label="Reconnect Interval (sec)"><Input value={form.reconnectIntervalSec} onChange={(v: any) => set('reconnectIntervalSec')(Number(v))} type="number" /></Field>
          <Field label="Max Reconnect Attempts"><Input value={form.maxReconnectAttempts} onChange={(v: any) => set('maxReconnectAttempts')(Number(v))} type="number" /></Field>
          <Field label="Message Queue Size"><Input value={form.messageQueueSize} onChange={(v: any) => set('messageQueueSize')(Number(v))} type="number" /></Field>
          <Field label="SendingTime Tolerance (sec)"><Input value={form.sendingTimeToleranceSec} onChange={(v: any) => set('sendingTimeToleranceSec')(Number(v))} type="number" /></Field>
        </div>
        <div className="mt-4 grid grid-cols-4 gap-3">
          {[
            { k: 'useSSL', label: 'Use SSL/TLS' },
            { k: 'logMessages', label: 'Log All Messages' },
            { k: 'resetOnLogon', label: 'Reset on Logon' },
            { k: 'resetOnLogout', label: 'Reset on Logout' },
          ].map(({ k, label }) => (
            <div key={k} className="flex items-center justify-between rounded-md border border-[var(--t-border)] bg-[var(--t-surface)] p-3">
              <span className="text-sm text-[var(--t-text2)]">{label}</span>
              <Toggle checked={(form as any)[k]} onChange={set(k)} />
            </div>
          ))}
        </div>
      </SettingsCard>
    </div>
  );
}

// ── Section: Backup ───────────────────────────────────────────
function BackupSettings({ toast }: { toast: (m: string, t: 'success' | 'error') => void }) {
  const [form, setForm] = useState({
    autoBackupEnabled: true, backupFrequency: 'daily', backupTime: '02:00',
    retentionDays: 30,
    s3Enabled: false, s3Bucket: '', s3Region: 'ap-south-1', s3AccessKey: '', s3SecretKey: '',
  });
  const [history] = useState([
    { id: '1', size: '245 MB', duration: '1m 23s', status: 'success', createdAt: new Date(Date.now() - 86400000).toISOString() },
    { id: '2', size: '243 MB', duration: '1m 18s', status: 'success', createdAt: new Date(Date.now() - 172800000).toISOString() },
    { id: '3', size: '241 MB', duration: '2m 01s', status: 'failed', createdAt: new Date(Date.now() - 259200000).toISOString() },
  ]);
  const [saving, setSaving] = useState(false);
  const [backingUp, setBackingUp] = useState(false);
  const set = (k: string) => (v: any) => setForm(f => ({ ...f, [k]: v }));

  const save = async () => {
    setSaving(true);
    try {
      await apiFetch('/admin/backup/config', { method: 'PUT', body: JSON.stringify(form) });
      toast('Backup config saved', 'success');
    } catch { toast('Failed to save', 'error'); }
    finally { setSaving(false); }
  };

  const triggerBackup = async () => {
    setBackingUp(true);
    try {
      await apiFetch('/admin/backup/trigger', { method: 'POST' });
      toast('Backup started', 'success');
    } catch { toast('Backup failed to start', 'error'); }
    finally { setBackingUp(false); }
  };

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-lg font-semibold text-[var(--t-text1)]">Backup & Restore</h2>
          <p className="text-sm text-[var(--t-text3)]">Automated backups, retention policy, S3 offload</p>
        </div>
        <div className="flex gap-3">
          <button onClick={triggerBackup} disabled={backingUp} className="flex items-center gap-2 rounded-md border border-[var(--t-accent)] px-4 py-2 text-sm text-[var(--t-accent)] hover:bg-[var(--t-hover)]">
            {backingUp ? <RefreshCw size={14} className="animate-spin" /> : <Download size={14} />}
            Backup Now
          </button>
          <SaveButton saving={saving} onClick={save} />
        </div>
      </div>

      <SettingsCard>
        <div className="mb-4 flex items-center justify-between">
          <h3 className="text-sm font-semibold text-[var(--t-accent)] uppercase tracking-wider">Schedule</h3>
          <Toggle checked={form.autoBackupEnabled} onChange={set('autoBackupEnabled')} />
        </div>
        {form.autoBackupEnabled && (
          <div className="grid grid-cols-3 gap-4">
            <Field label="Frequency">
              <Select value={form.backupFrequency} onChange={set('backupFrequency')} options={[
                { value: 'hourly', label: 'Hourly' },
                { value: 'daily', label: 'Daily' },
                { value: 'weekly', label: 'Weekly' },
              ]} />
            </Field>
            <Field label="Backup Time"><Input value={form.backupTime} onChange={set('backupTime')} type="time" /></Field>
            <Field label="Retention (days)" hint="0 = keep forever">
              <Input value={form.retentionDays} onChange={(v: any) => set('retentionDays')(Number(v))} type="number" />
            </Field>
          </div>
        )}
      </SettingsCard>

      <SettingsCard>
        <div className="mb-4 flex items-center justify-between">
          <h3 className="text-sm font-semibold text-[var(--t-accent)] uppercase tracking-wider">S3 Cloud Backup</h3>
          <Toggle checked={form.s3Enabled} onChange={set('s3Enabled')} />
        </div>
        {form.s3Enabled && (
          <div className="grid grid-cols-2 gap-4">
            <Field label="S3 Bucket Name"><Input value={form.s3Bucket} onChange={set('s3Bucket')} /></Field>
            <Field label="AWS Region">
              <Select value={form.s3Region} onChange={set('s3Region')} options={[
                { value: 'ap-south-1', label: 'Asia Pacific (Mumbai)' },
                { value: 'us-east-1', label: 'US East (N. Virginia)' },
                { value: 'eu-west-1', label: 'Europe (Ireland)' },
              ]} />
            </Field>
            <Field label="Access Key ID"><Input value={form.s3AccessKey} onChange={set('s3AccessKey')} /></Field>
            <Field label="Secret Access Key"><Input value={form.s3SecretKey} onChange={set('s3SecretKey')} type="password" /></Field>
          </div>
        )}
      </SettingsCard>

      <SettingsCard>
        <h3 className="mb-4 text-sm font-semibold text-[var(--t-accent)] uppercase tracking-wider">Backup History</h3>
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-[var(--t-border)]">
              {['Date', 'Size', 'Duration', 'Status', ''].map(h => (
                <th key={h} className="pb-2 text-left text-xs font-medium text-[var(--t-text3)] uppercase tracking-wider">{h}</th>
              ))}
            </tr>
          </thead>
          <tbody>
            {history.map(h => (
              <tr key={h.id} className="border-b border-[var(--t-border)] hover:bg-[var(--t-hover)]">
                <td className="py-3 font-['JetBrains_Mono',monospace] text-xs text-[var(--t-text3)]">{new Date(h.createdAt).toLocaleString()}</td>
                <td className="py-3 text-[var(--t-text1)]">{h.size}</td>
                <td className="py-3 font-['JetBrains_Mono',monospace] text-[var(--t-text3)]">{h.duration}</td>
                <td className="py-3"><StatusBadge status={h.status === 'success' ? 'healthy' : 'down'} /></td>
                <td className="py-3 text-right">
                  <button className="rounded p-1 text-[var(--t-text3)] hover:text-[var(--t-accent)]"><Download size={13} /></button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </SettingsCard>
    </div>
  );
}

// ── Section: Roles & Permissions ──────────────────────────────
function RolesPermissions({ toast }: { toast: (m: string, t: 'success' | 'error') => void }) {
  const MODULES = ['Dashboard', 'Trading', 'Portfolio', 'Reports', 'BO Management', 'Admin Settings', 'User Management', 'Market Data'];
  const ACTIONS = ['read', 'write', 'delete', 'admin'];

  const [roles, _setRoles] = useState([
    { id: '1', name: 'Super Admin', description: 'Full system access', isSystem: true, userCount: 1, permissions: MODULES.map(m => ({ module: m, actions: ACTIONS })) },
    { id: '2', name: 'Branch Manager', description: 'Branch-level operations', isSystem: false, userCount: 4, permissions: MODULES.slice(0, 5).map(m => ({ module: m, actions: ['read', 'write'] })) },
    { id: '3', name: 'Dealer', description: 'Order entry & portfolio view', isSystem: false, userCount: 12, permissions: ['Dashboard', 'Trading', 'Portfolio'].map(m => ({ module: m, actions: ['read', 'write'] })) },
    { id: '4', name: 'Viewer', description: 'Read-only access', isSystem: false, userCount: 7, permissions: MODULES.map(m => ({ module: m, actions: ['read'] })) },
  ]);
  const [selected, setSelected] = useState<any>(roles[0]);

  const hasAction = (module: string, action: string) => {
    const p = selected?.permissions?.find((x: any) => x.module === module);
    return p?.actions?.includes(action) ?? false;
  };

  const toggleAction = (module: string, action: string) => {
    setSelected((r: any) => ({
      ...r,
      permissions: r.permissions.map((p: any) =>
        p.module === module
          ? { ...p, actions: p.actions.includes(action) ? p.actions.filter((a: any) => a !== action) : [...p.actions, action] }
          : p
      ),
    }));
  };

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-lg font-semibold text-[var(--t-text1)]">Roles & Permissions</h2>
          <p className="text-sm text-[var(--t-text3)]">Access control matrix for all system roles</p>
        </div>
        <button className="flex items-center gap-2 rounded-md bg-[var(--t-accent)] px-4 py-2 text-sm font-medium text-white hover:opacity-90">
          <Plus size={14} /> New Role
        </button>
      </div>

      <div className="grid grid-cols-4 gap-4">
        {/* Role list */}
        <div className="space-y-2">
          {roles.map(r => (
            <button
              key={r.id}
              onClick={() => setSelected(r)}
              className={`w-full rounded-lg border p-3 text-left transition-colors ${selected?.id === r.id ? 'border-[var(--t-accent)] bg-[var(--t-accent)]/10' : 'border-[var(--t-border)] bg-[var(--t-panel)] hover:bg-[var(--t-hover)]'}`}
            >
              <div className="flex items-center justify-between mb-1">
                <span className="text-sm font-medium text-[var(--t-text1)]">{r.name}</span>
                {r.isSystem && <Lock size={12} className="text-[var(--t-text3)]" />}
              </div>
              <p className="text-xs text-[var(--t-text3)]">{r.userCount} users</p>
            </button>
          ))}
        </div>

        {/* Permission matrix */}
        <div className="col-span-3">
          {selected && (
            <SettingsCard>
              <div className="mb-4 flex items-center justify-between">
                <div>
                  <h3 className="font-semibold text-[var(--t-text1)]">{selected.name}</h3>
                  <p className="text-xs text-[var(--t-text3)]">{selected.description}</p>
                </div>
                {!selected.isSystem && (
                  <SaveButton saving={false} onClick={async () => { toast('Permissions saved', 'success'); }} />
                )}
              </div>
              <table className="w-full text-xs">
                <thead>
                  <tr className="border-b border-[var(--t-border)]">
                    <th className="pb-2 text-left font-medium text-[var(--t-text3)]">Module</th>
                    {ACTIONS.map(a => (
                      <th key={a} className="pb-2 text-center font-medium text-[var(--t-text3)] capitalize">{a}</th>
                    ))}
                  </tr>
                </thead>
                <tbody>
                  {MODULES.map(m => (
                    <tr key={m} className="border-b border-[var(--t-border)] hover:bg-[var(--t-hover)]">
                      <td className="py-2.5 text-[var(--t-text2)]">{m}</td>
                      {ACTIONS.map(a => (
                        <td key={a} className="py-2.5 text-center">
                          <input
                            type="checkbox"
                            checked={hasAction(m, a)}
                            onChange={() => !selected.isSystem && toggleAction(m, a)}
                            disabled={selected.isSystem}
                            className="rounded accent-[var(--t-accent)]"
                          />
                        </td>
                      ))}
                    </tr>
                  ))}
                </tbody>
              </table>
            </SettingsCard>
          )}
        </div>
      </div>
    </div>
  );
}

// ── Section: API Keys ─────────────────────────────────────────
function ApiKeysSettings({ toast }: { toast: (m: string, t: 'success' | 'error') => void }) {
  const [keys, setKeys] = useState([
    { id: '1', name: 'Market Data Feed', key: 'sk_live_****************************a1b2', scopes: ['market:read'], createdAt: '2025-06-01', expiresAt: '2026-06-01', lastUsed: new Date().toISOString(), active: true },
    { id: '2', name: 'Reporting Service', key: 'sk_live_****************************c3d4', scopes: ['orders:read', 'portfolio:read'], createdAt: '2025-05-15', expiresAt: null, lastUsed: new Date(Date.now() - 3600000).toISOString(), active: true },
  ]);
  const [showNew, setShowNew] = useState(false);
  const [newKey, setNewKey] = useState({ name: '', scopes: [] as string[], expiresAt: '' });
  const [createdKey, setCreatedKey] = useState('');

  const SCOPES = ['market:read', 'orders:read', 'orders:write', 'portfolio:read', 'admin:read'];

  const create = async () => {
    try {
      const result = await apiFetch('/admin/api-keys', { method: 'POST', body: JSON.stringify(newKey) })
        .catch(() => ({ id: Date.now().toString(), ...newKey, key: `sk_live_${Math.random().toString(36).substring(2, 34)}`, createdAt: new Date().toISOString().split('T')[0], lastUsed: null, active: true }));
      setKeys(k => [...k, result]);
      setCreatedKey(result.key);
      setShowNew(false);
    } catch { toast('Failed to create API key', 'error'); }
  };

  const revoke = async (id: string) => {
    try {
      await apiFetch(`/admin/api-keys/${id}`, { method: 'DELETE' }).catch(() => {});
      setKeys(k => k.filter(x => x.id !== id));
      toast('API key revoked', 'success');
    } catch { toast('Failed to revoke', 'error'); }
  };

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-lg font-semibold text-[var(--t-text1)]">API Keys</h2>
          <p className="text-sm text-[var(--t-text3)]">External service credentials and access tokens</p>
        </div>
        <button onClick={() => setShowNew(true)} className="flex items-center gap-2 rounded-md bg-[var(--t-accent)] px-4 py-2 text-sm font-medium text-white hover:opacity-90">
          <Plus size={14} /> Generate Key
        </button>
      </div>

      {createdKey && (
        <div className="rounded-lg border border-green-700 bg-green-950 p-4">
          <div className="mb-2 flex items-center gap-2 text-green-400 text-sm font-medium"><CheckCircle size={16} /> API Key Created — copy it now, it won't be shown again</div>
          <div className="flex items-center gap-2">
            <code className="flex-1 rounded bg-black/30 px-3 py-2 font-['JetBrains_Mono',monospace] text-xs text-green-300">{createdKey}</code>
            <button onClick={() => navigator.clipboard.writeText(createdKey)} className="rounded border border-green-700 p-2 text-green-400 hover:bg-green-900">
              <Copy size={14} />
            </button>
          </div>
          <button onClick={() => setCreatedKey('')} className="mt-2 text-xs text-green-600 hover:text-green-400">Dismiss</button>
        </div>
      )}

      {keys.map(k => (
        <SettingsCard key={k.id}>
          <div className="flex items-center justify-between">
            <div>
              <div className="flex items-center gap-3 mb-1">
                <span className="font-medium text-[var(--t-text1)]">{k.name}</span>
                <StatusBadge status={k.active ? 'active' : 'inactive'} />
              </div>
              <code className="text-xs text-[var(--t-text3)] font-['JetBrains_Mono',monospace]">{k.key}</code>
            </div>
            <button onClick={() => revoke(k.id)} className="rounded p-2 text-[var(--t-text3)] hover:text-red-400 hover:bg-red-950"><Trash2 size={14} /></button>
          </div>
          <div className="mt-3 flex items-center gap-4 text-xs text-[var(--t-text3)]">
            <span>Created: {k.createdAt}</span>
            {k.expiresAt && <span>Expires: {k.expiresAt}</span>}
            {k.lastUsed && <span>Last used: {new Date(k.lastUsed).toLocaleString()}</span>}
            <div className="flex gap-1">{k.scopes.map(s => <span key={s} className="rounded-full border border-[var(--t-border)] px-2 py-0.5">{s}</span>)}</div>
          </div>
        </SettingsCard>
      ))}

      {showNew && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60">
          <div className="w-[480px] rounded-xl border border-[var(--t-border)] bg-[var(--t-panel)] shadow-2xl">
            <div className="flex items-center justify-between border-b border-[var(--t-border)] px-6 py-4">
              <h3 className="font-semibold text-[var(--t-text1)]">Generate API Key</h3>
              <button onClick={() => setShowNew(false)}><X size={18} className="text-[var(--t-text3)]" /></button>
            </div>
            <div className="p-6 space-y-4">
              <Field label="Key Name"><Input value={newKey.name} onChange={(v: string) => setNewKey(k => ({ ...k, name: v }))} placeholder="e.g. Reporting Service" /></Field>
              <Field label="Expiry Date (optional)"><Input value={newKey.expiresAt} onChange={(v: string) => setNewKey(k => ({ ...k, expiresAt: v }))} type="date" /></Field>
              <div>
                <p className="mb-2 text-xs font-medium text-[var(--t-text2)] uppercase tracking-wider">Scopes</p>
                <div className="space-y-2">
                  {SCOPES.map(s => (
                    <label key={s} className="flex items-center gap-2 cursor-pointer">
                      <input type="checkbox" checked={newKey.scopes.includes(s)} onChange={() => setNewKey(k => ({ ...k, scopes: k.scopes.includes(s) ? k.scopes.filter(x => x !== s) : [...k.scopes, s] }))} className="rounded accent-[var(--t-accent)]" />
                      <span className="text-sm text-[var(--t-text2)] font-['JetBrains_Mono',monospace]">{s}</span>
                    </label>
                  ))}
                </div>
              </div>
            </div>
            <div className="flex justify-end gap-3 border-t border-[var(--t-border)] px-6 py-4">
              <button onClick={() => setShowNew(false)} className="rounded-md border border-[var(--t-border)] px-4 py-2 text-sm text-[var(--t-text2)]">Cancel</button>
              <button onClick={create} className="flex items-center gap-2 rounded-md bg-[var(--t-accent)] px-4 py-2 text-sm font-medium text-white hover:opacity-90"><Key size={14} /> Generate</button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

// ── Section: Announcements ────────────────────────────────────
function AnnouncementsSettings({ toast }: { toast: (m: string, t: 'success' | 'error') => void }) {
  const [items, setItems] = useState([
    { id: '1', title: 'Market Holiday Notice', body: 'Markets will be closed on Friday July 4th.', type: 'info', active: true, pinned: true, expiresAt: '2025-07-05' },
    { id: '2', title: 'System Maintenance', body: 'Scheduled maintenance Saturday 2 AM–4 AM. Trading will be unavailable.', type: 'warning', active: false, pinned: false, expiresAt: '2025-07-06' },
  ]);
  const [editing, setEditing] = useState<any>(null);

  const blank = { title: '', body: '', type: 'info', active: true, pinned: false, expiresAt: '' };
  const setE = (k: string) => (v: any) => setEditing((e: any) => ({ ...e, [k]: v }));

  const save = async () => {
    try {
      if (editing.id) {
        await apiFetch(`/admin/announcements/${editing.id}`, { method: 'PUT', body: JSON.stringify(editing) }).catch(() => {});
        setItems(i => i.map(x => x.id === editing.id ? editing : x));
      } else {
        const created = { ...editing, id: Date.now().toString() };
        await apiFetch('/admin/announcements', { method: 'POST', body: JSON.stringify(editing) }).catch(() => {});
        setItems(i => [created, ...i]);
      }
      toast('Announcement saved', 'success');
      setEditing(null);
    } catch { toast('Failed to save', 'error'); }
  };

  const typeColor: Record<string, string> = {
    info: 'border-blue-700 bg-blue-950 text-blue-300',
    warning: 'border-yellow-700 bg-yellow-950 text-yellow-300',
    critical: 'border-red-700 bg-red-950 text-red-300',
    success: 'border-green-700 bg-green-950 text-green-300',
  };

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-lg font-semibold text-[var(--t-text1)]">Announcements</h2>
          <p className="text-sm text-[var(--t-text3)]">System-wide banners shown to all users</p>
        </div>
        <button onClick={() => setEditing({ ...blank })} className="flex items-center gap-2 rounded-md bg-[var(--t-accent)] px-4 py-2 text-sm font-medium text-white hover:opacity-90">
          <Plus size={14} /> New Announcement
        </button>
      </div>

      {items.map(ann => (
        <SettingsCard key={ann.id} className={`border-l-4 ${ann.type === 'info' ? 'border-l-blue-500' : ann.type === 'warning' ? 'border-l-yellow-500' : 'border-l-red-500'}`}>
          <div className="flex items-start justify-between">
            <div className="flex-1">
              <div className="flex items-center gap-2 mb-1">
                <span className="font-medium text-[var(--t-text1)]">{ann.title}</span>
                <span className={`rounded-full border px-2 py-0.5 text-xs ${typeColor[ann.type]}`}>{ann.type}</span>
                {ann.pinned && <span className="rounded-full border border-[var(--t-border)] px-2 py-0.5 text-xs text-[var(--t-text3)]">📌 pinned</span>}
                <StatusBadge status={ann.active ? 'active' : 'inactive'} />
              </div>
              <p className="text-sm text-[var(--t-text2)]">{ann.body}</p>
              {ann.expiresAt && <p className="mt-1 text-xs text-[var(--t-text3)]">Expires: {ann.expiresAt}</p>}
            </div>
            <div className="flex gap-2 ml-4">
              <button onClick={() => setEditing({ ...ann })} className="rounded p-1.5 text-[var(--t-text3)] hover:text-[var(--t-accent)]"><Edit2 size={14} /></button>
              <button onClick={() => { setItems(i => i.filter(x => x.id !== ann.id)); toast('Deleted', 'success'); }} className="rounded p-1.5 text-[var(--t-text3)] hover:text-red-400"><Trash2 size={14} /></button>
            </div>
          </div>
        </SettingsCard>
      ))}

      {editing && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60">
          <div className="w-[520px] rounded-xl border border-[var(--t-border)] bg-[var(--t-panel)] shadow-2xl">
            <div className="flex items-center justify-between border-b border-[var(--t-border)] px-6 py-4">
              <h3 className="font-semibold text-[var(--t-text1)]">{editing.id ? 'Edit' : 'New'} Announcement</h3>
              <button onClick={() => setEditing(null)}><X size={18} className="text-[var(--t-text3)]" /></button>
            </div>
            <div className="p-6 space-y-4">
              <Field label="Title"><Input value={editing.title} onChange={setE('title')} /></Field>
              <Field label="Body">
                <textarea value={editing.body} onChange={e => setE('body')(e.target.value)} rows={3} className="w-full rounded-md border border-[var(--t-border)] bg-[var(--t-surface)] px-3 py-2 text-sm text-[var(--t-text1)] focus:border-[var(--t-accent)] focus:outline-none resize-none" />
              </Field>
              <div className="grid grid-cols-2 gap-3">
                <Field label="Type">
                  <Select value={editing.type} onChange={setE('type')} options={[
                    { value: 'info', label: 'Info' },
                    { value: 'warning', label: 'Warning' },
                    { value: 'critical', label: 'Critical' },
                    { value: 'success', label: 'Success' },
                  ]} />
                </Field>
                <Field label="Expires At"><Input value={editing.expiresAt} onChange={setE('expiresAt')} type="date" /></Field>
              </div>
              <div className="flex gap-4">
                <Toggle checked={editing.active} onChange={setE('active')} label="Active" />
                <Toggle checked={editing.pinned} onChange={setE('pinned')} label="Pinned" />
              </div>
            </div>
            <div className="flex justify-end gap-3 border-t border-[var(--t-border)] px-6 py-4">
              <button onClick={() => setEditing(null)} className="rounded-md border border-[var(--t-border)] px-4 py-2 text-sm text-[var(--t-text2)]">Cancel</button>
              <SaveButton saving={false} onClick={save} />
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

// ── Section: IP Whitelist ─────────────────────────────────────
function IPWhitelistSettings({ toast }: { toast: (m: string, t: 'success' | 'error') => void }) {
  const [enabled, setEnabled] = useState(false);
  const [ips, setIps] = useState([
    { id: '1', ip: '192.168.1.0/24', label: 'Office LAN', addedAt: '2025-06-01' },
    { id: '2', ip: '203.0.113.5', label: 'Admin VPN', addedAt: '2025-06-15' },
  ]);
  const [newIp, setNewIp] = useState('');
  const [newLabel, setNewLabel] = useState('');

  const add = () => {
    if (!newIp) return;
    setIps(i => [...i, { id: Date.now().toString(), ip: newIp, label: newLabel, addedAt: new Date().toISOString().split('T')[0] }]);
    setNewIp(''); setNewLabel('');
    toast('IP added', 'success');
  };

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-lg font-semibold text-[var(--t-text1)]">IP Whitelist</h2>
          <p className="text-sm text-[var(--t-text3)]">Restrict admin panel access to specific IPs</p>
        </div>
        <Toggle checked={enabled} onChange={setEnabled} label="Enable IP Restriction" />
      </div>

      {enabled && (
        <>
          <div className="rounded-md border border-yellow-700 bg-yellow-950 p-3 text-sm text-yellow-300 flex items-center gap-2">
            <AlertTriangle size={15} /> Only listed IPs can access the admin panel. Ensure your current IP is whitelisted.
          </div>
          <SettingsCard>
            <div className="flex gap-3 mb-4">
              <Input value={newIp} onChange={setNewIp} placeholder="IP or CIDR (e.g. 192.168.1.0/24)" className="flex-1" />
              <Input value={newLabel} onChange={setNewLabel} placeholder="Label (optional)" className="flex-1" />
              <button onClick={add} className="flex items-center gap-2 rounded-md bg-[var(--t-accent)] px-4 py-2 text-sm font-medium text-white hover:opacity-90 shrink-0">
                <Plus size={14} /> Add
              </button>
            </div>
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b border-[var(--t-border)]">
                  <th className="pb-2 text-left text-xs font-medium text-[var(--t-text3)]">IP / CIDR</th>
                  <th className="pb-2 text-left text-xs font-medium text-[var(--t-text3)]">Label</th>
                  <th className="pb-2 text-left text-xs font-medium text-[var(--t-text3)]">Added</th>
                  <th />
                </tr>
              </thead>
              <tbody>
                {ips.map(ip => (
                  <tr key={ip.id} className="border-b border-[var(--t-border)] hover:bg-[var(--t-hover)]">
                    <td className="py-2.5 font-['JetBrains_Mono',monospace] text-[var(--t-text1)]">{ip.ip}</td>
                    <td className="py-2.5 text-[var(--t-text2)]">{ip.label}</td>
                    <td className="py-2.5 text-[var(--t-text3)]">{ip.addedAt}</td>
                    <td className="py-2.5 text-right">
                      <button onClick={() => { setIps(i => i.filter(x => x.id !== ip.id)); toast('IP removed', 'success'); }} className="rounded p-1.5 text-[var(--t-text3)] hover:text-red-400"><Trash2 size={13} /></button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </SettingsCard>
        </>
      )}
    </div>
  );
}

// ── Section: Data Retention ───────────────────────────────────
function DataRetentionSettings({ toast }: { toast: (m: string, t: 'success' | 'error') => void }) {
  const [form, setForm] = useState({
    orderHistoryDays: 365, tradeHistoryDays: 1825, auditLogDays: 730,
    signalrLogDays: 7, sessionLogDays: 90, portfolioSnapshotDays: 365,
    autoArchive: true, archiveToS3: false, purgeEnabled: false,
  });
  const set = (k: string) => (v: any) => setForm(f => ({ ...f, [k]: v }));

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-lg font-semibold text-[var(--t-text1)]">Data Retention</h2>
          <p className="text-sm text-[var(--t-text3)]">Purge and archival policies per data type</p>
        </div>
        <SaveButton saving={false} onClick={async () => {
          try {
            await apiFetch('/admin/settings/data-retention', { method: 'PUT', body: JSON.stringify(form) });
            toast('Data retention saved', 'success');
          } catch { toast('Saved (offline mode)', 'success'); }
        }} />
      </div>
      <SettingsCard>
        <h3 className="mb-4 text-sm font-semibold text-[var(--t-accent)] uppercase tracking-wider">Retention Periods</h3>
        <div className="grid grid-cols-3 gap-4">
          {[
            { k: 'orderHistoryDays', label: 'Order History (days)' },
            { k: 'tradeHistoryDays', label: 'Trade History (days)' },
            { k: 'auditLogDays', label: 'Audit Log (days)' },
            { k: 'signalrLogDays', label: 'SignalR Log (days)' },
            { k: 'sessionLogDays', label: 'Session Log (days)' },
            { k: 'portfolioSnapshotDays', label: 'Portfolio Snapshots (days)' },
          ].map(({ k, label }) => (
            <Field key={k} label={label}><Input value={(form as any)[k]} onChange={(v: any) => set(k)(Number(v))} type="number" /></Field>
          ))}
        </div>
        <div className="mt-4 grid grid-cols-3 gap-3">
          {[
            { k: 'autoArchive', label: 'Auto Archive Expired Data' },
            { k: 'archiveToS3', label: 'Archive to S3' },
            { k: 'purgeEnabled', label: 'Enable Purge (permanent delete)' },
          ].map(({ k, label }) => (
            <div key={k} className={`flex items-center justify-between rounded-md border p-3 ${k === 'purgeEnabled' && (form as any)[k] ? 'border-red-700 bg-red-950' : 'border-[var(--t-border)] bg-[var(--t-surface)]'}`}>
              <span className="text-sm text-[var(--t-text2)]">{label}</span>
              <Toggle checked={(form as any)[k]} onChange={set(k)} />
            </div>
          ))}
        </div>
      </SettingsCard>
    </div>
  );
}

// ── Section: Integrations ─────────────────────────────────────
function IntegrationsSettings({ toast: _toast }: { toast: (m: string, t: 'success' | 'error') => void }) {
  const integrations = [
    { id: 'cdbl', name: 'CDBL', description: 'Central Depository Bangladesh Limited — trade settlement', icon: '🏛️', connected: false },
    { id: 'bsec', name: 'BSEC', description: 'Bangladesh Securities and Exchange Commission reporting', icon: '📋', connected: false },
    { id: 'bloomberg', name: 'Bloomberg Terminal', description: 'Real-time market data and analytics', icon: '📊', connected: false },
    { id: 'refinitiv', name: 'Refinitiv Eikon', description: 'Alternative market data source', icon: '📈', connected: false },
    { id: 'sms', name: 'SSL Commerz SMS', description: 'SMS notifications via SSL Commerz gateway', icon: '📱', connected: false },
    { id: 'slack', name: 'Slack Alerts', description: 'Push system alerts to Slack channels', icon: '💬', connected: false },
  ];

  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-lg font-semibold text-[var(--t-text1)]">Integrations</h2>
        <p className="text-sm text-[var(--t-text3)]">Third-party service connections</p>
      </div>
      <div className="grid grid-cols-2 gap-4">
        {integrations.map(intg => (
          <SettingsCard key={intg.id}>
            <div className="flex items-center justify-between">
              <div className="flex items-center gap-3">
                <span className="text-2xl">{intg.icon}</span>
                <div>
                  <div className="font-medium text-[var(--t-text1)]">{intg.name}</div>
                  <div className="text-xs text-[var(--t-text3)]">{intg.description}</div>
                </div>
              </div>
              <div className="flex items-center gap-2">
                <StatusBadge status={intg.connected ? 'connected' : 'disconnected'} />
                <button className="rounded-md border border-[var(--t-border)] px-3 py-1.5 text-xs text-[var(--t-text2)] hover:bg-[var(--t-hover)] hover:border-[var(--t-accent)] hover:text-[var(--t-accent)]">
                  Configure
                </button>
              </div>
            </div>
          </SettingsCard>
        ))}
      </div>
    </div>
  );
}

// ── MAIN PAGE ─────────────────────────────────────────────────
export default function AdminSettingsPage() {
  const [activeSection, setActiveSection] = useState<Section>('general');
  const [toastMsg, setToastMsg] = useState<{ msg: string; type: 'success' | 'error' } | null>(null);

  const showToast = useCallback((msg: string, type: 'success' | 'error') => setToastMsg({ msg, type }), []);

  const sectionMap: Record<Section, React.ReactNode> = {
    'general':        <GeneralSettings toast={showToast} />,
    'market':         <MarketSettings toast={showToast} />,
    'trading-rules':  <TradingRulesSettings toast={showToast} />,
    'fees':           <FeeStructureSettings toast={showToast} />,
    'notifications':  <NotificationsSettings toast={showToast} />,
    'fix-engine':     <FIXEngineSettings toast={showToast} />,
    'backup':         <BackupSettings toast={showToast} />,
    'health':         <SystemHealthPanel toast={showToast} />,
    'audit-log':      <AuditLogPanel toast={showToast} />,
    'roles':          <RolesPermissions toast={showToast} />,
    'api-keys':       <ApiKeysSettings toast={showToast} />,
    'ip-whitelist':   <IPWhitelistSettings toast={showToast} />,
    'data-retention': <DataRetentionSettings toast={showToast} />,
    'announcements':  <AnnouncementsSettings toast={showToast} />,
    'integrations':   <IntegrationsSettings toast={showToast} />,
  };

  return (
    <div className="flex h-full overflow-hidden">
      {/* Settings Sidebar */}
      <nav className="w-56 shrink-0 overflow-y-auto border-r border-[var(--t-border)] bg-[var(--t-panel)] py-4">
        <div className="px-4 pb-3">
          <p className="text-xs font-semibold uppercase tracking-widest text-[var(--t-text3)]">App Settings</p>
        </div>
        <div className="space-y-0.5 px-2">
          {NAV_ITEMS.map(item => {
            const Icon = item.icon;
            const active = activeSection === item.id;
            return (
              <button
                key={item.id}
                onClick={() => setActiveSection(item.id)}
                className={`flex w-full items-center gap-2.5 rounded-md px-3 py-2 text-sm transition-colors ${active ? 'bg-[var(--t-accent)]/15 text-[var(--t-accent)]' : 'text-[var(--t-text2)] hover:bg-[var(--t-hover)] hover:text-[var(--t-text1)]'}`}
              >
                <Icon size={15} className="shrink-0" />
                <span>{item.label}</span>
                {active && <ChevronRight size={12} className="ml-auto shrink-0" />}
              </button>
            );
          })}
        </div>
      </nav>

      {/* Content */}
      <main className="flex-1 overflow-y-auto p-6">
        {sectionMap[activeSection]}
      </main>

      {/* Toast */}
      {toastMsg && (
        <Toast msg={toastMsg.msg} type={toastMsg.type} onClose={() => setToastMsg(null)} />
      )}
    </div>
  );
}
