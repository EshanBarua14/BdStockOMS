// @ts-nocheck
// ============================================================
// BdStockOMS — Admin Settings API Hooks
// File: BdStockOMS.Client/src/hooks/useAdminSettings.ts
// ============================================================
import { useState, useEffect, useCallback } from 'react';

const BASE = 'https://localhost:7219/api';

function authHeader() {
  const raw = localStorage.getItem('bd_oms_auth_v2');
  if (!raw) return {};
  try {
    const token = JSON.parse(raw)?.state?.user?.token;
    return token ? { Authorization: `Bearer ${token}` } : {};
  } catch {
    return {};
  }
}

async function apiFetch<T>(path: string, options?: RequestInit): Promise<T> {
  const res = await fetch(`${BASE}${path}`, {
    headers: { 'Content-Type': 'application/json', ...authHeader() },
    ...options,
  });
  if (!res.ok) throw new Error(`API ${res.status}: ${res.statusText}`);
  return res.json();
}

// ── General Settings ────────────────────────────────────────
export function useGeneralSettings() {
  const [data, setData] = useState<any>(null);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);

  const fetch_ = useCallback(async () => {
    setLoading(true);
    try { setData(await apiFetch('/admin/settings/general')); }
    catch { /* use fallback */ }
    finally { setLoading(false); }
  }, []);

  const save = useCallback(async (payload: any) => {
    setSaving(true);
    try {
      await apiFetch('/admin/settings/general', { method: 'PUT', body: JSON.stringify(payload) });
      setData(payload);
      return true;
    } catch { return false; }
    finally { setSaving(false); }
  }, []);

  useEffect(() => { fetch_(); }, [fetch_]);
  return { data, loading, saving, save, refresh: fetch_ };
}

// ── Market Settings ──────────────────────────────────────────
export function useMarketSettings() {
  const [data, setData] = useState<any>(null);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);

  const fetch_ = useCallback(async () => {
    setLoading(true);
    try { setData(await apiFetch('/admin/settings/market')); }
    catch { /* fallback */ }
    finally { setLoading(false); }
  }, []);

  const save = useCallback(async (payload: any) => {
    setSaving(true);
    try {
      await apiFetch('/admin/settings/market', { method: 'PUT', body: JSON.stringify(payload) });
      setData(payload);
      return true;
    } catch { return false; }
    finally { setSaving(false); }
  }, []);

  useEffect(() => { fetch_(); }, [fetch_]);
  return { data, loading, saving, save, refresh: fetch_ };
}

// ── Trading Rules ─────────────────────────────────────────────
export function useTradingRules() {
  const [data, setData] = useState<any>(null);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);

  const fetch_ = useCallback(async () => {
    setLoading(true);
    try { setData(await apiFetch('/admin/settings/trading-rules')); }
    catch { /* fallback */ }
    finally { setLoading(false); }
  }, []);

  const save = useCallback(async (payload: any) => {
    setSaving(true);
    try {
      await apiFetch('/admin/settings/trading-rules', { method: 'PUT', body: JSON.stringify(payload) });
      setData(payload); return true;
    } catch { return false; }
    finally { setSaving(false); }
  }, []);

  useEffect(() => { fetch_(); }, [fetch_]);
  return { data, loading, saving, save, refresh: fetch_ };
}

// ── Fee Structure ─────────────────────────────────────────────
export function useFeeStructure() {
  const [data, setData] = useState<any[]>([]);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);

  const fetch_ = useCallback(async () => {
    setLoading(true);
    try { setData(await apiFetch('/admin/fees')); }
    catch { /* fallback */ }
    finally { setLoading(false); }
  }, []);

  const create = useCallback(async (payload: any) => {
    setSaving(true);
    try {
      const created = await apiFetch('/admin/fees', { method: 'POST', body: JSON.stringify(payload) });
      setData(prev => [...prev, created]); return true;
    } catch { return false; }
    finally { setSaving(false); }
  }, []);

  const update = useCallback(async (id: string, payload: any) => {
    setSaving(true);
    try {
      await apiFetch(`/admin/fees/${id}`, { method: 'PUT', body: JSON.stringify(payload) });
      setData(prev => prev.map(f => f.id === id ? { ...f, ...payload } : f)); return true;
    } catch { return false; }
    finally { setSaving(false); }
  }, []);

  const remove = useCallback(async (id: string) => {
    try {
      await apiFetch(`/admin/fees/${id}`, { method: 'DELETE' });
      setData(prev => prev.filter(f => f.id !== id)); return true;
    } catch { return false; }
  }, []);

  useEffect(() => { fetch_(); }, [fetch_]);
  return { data, loading, saving, create, update, remove, refresh: fetch_ };
}

// ── Notifications ─────────────────────────────────────────────
export function useNotificationSettings() {
  const [data, setData] = useState<any>(null);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [testing, setTesting] = useState(false);

  const fetch_ = useCallback(async () => {
    setLoading(true);
    try { setData(await apiFetch('/admin/settings/notifications')); }
    catch { /* fallback */ }
    finally { setLoading(false); }
  }, []);

  const save = useCallback(async (payload: any) => {
    setSaving(true);
    try {
      await apiFetch('/admin/settings/notifications', { method: 'PUT', body: JSON.stringify(payload) });
      setData(payload); return true;
    } catch { return false; }
    finally { setSaving(false); }
  }, []);

  const testEmail = useCallback(async (email: string) => {
    setTesting(true);
    try {
      await apiFetch('/admin/notifications/test-email', { method: 'POST', body: JSON.stringify({ email }) });
      return true;
    } catch { return false; }
    finally { setTesting(false); }
  }, []);

  useEffect(() => { fetch_(); }, [fetch_]);
  return { data, loading, saving, testing, save, testEmail, refresh: fetch_ };
}

// ── FIX Engine ────────────────────────────────────────────────
export function useFIXConfig() {
  const [data, setData] = useState<any>(null);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [status, setStatus] = useState<'connected' | 'disconnected' | 'connecting'>('disconnected');

  const fetch_ = useCallback(async () => {
    setLoading(true);
    try {
      const [config, st] = await Promise.all([
        apiFetch('/admin/fix/config'),
        apiFetch<any>('/admin/fix/status'),
      ]);
      setData(config);
      setStatus(st.status ?? 'disconnected');
    } catch { /* fallback */ }
    finally { setLoading(false); }
  }, []);

  const save = useCallback(async (payload: any) => {
    setSaving(true);
    try {
      await apiFetch('/admin/fix/config', { method: 'PUT', body: JSON.stringify(payload) });
      setData(payload); return true;
    } catch { return false; }
    finally { setSaving(false); }
  }, []);

  const toggleConnection = useCallback(async () => {
    try {
      const action = status === 'connected' ? 'disconnect' : 'connect';
      await apiFetch(`/admin/fix/${action}`, { method: 'POST' });
      setStatus(action === 'connect' ? 'connected' : 'disconnected');
    } catch { /* ignore */ }
  }, [status]);

  useEffect(() => { fetch_(); }, [fetch_]);
  return { data, loading, saving, status, save, toggleConnection, refresh: fetch_ };
}

// ── Backup ────────────────────────────────────────────────────
export function useBackupConfig() {
  const [config, setConfig] = useState<any>(null);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [backingUp, setBackingUp] = useState(false);
  const [restoring, setRestoring] = useState(false);
  const [backupHistory, setBackupHistory] = useState<any[]>([]);

  const fetch_ = useCallback(async () => {
    setLoading(true);
    try {
      const [cfg, hist] = await Promise.all([
        apiFetch('/admin/backup/config'),
        apiFetch<any[]>('/admin/backup/history'),
      ]);
      setConfig(cfg);
      setBackupHistory(hist);
    } catch { /* fallback */ }
    finally { setLoading(false); }
  }, []);

  const save = useCallback(async (payload: any) => {
    setSaving(true);
    try {
      await apiFetch('/admin/backup/config', { method: 'PUT', body: JSON.stringify(payload) });
      setConfig(payload); return true;
    } catch { return false; }
    finally { setSaving(false); }
  }, []);

  const triggerBackup = useCallback(async () => {
    setBackingUp(true);
    try {
      await apiFetch('/admin/backup/trigger', { method: 'POST' });
      await fetch_(); return true;
    } catch { return false; }
    finally { setBackingUp(false); }
  }, [fetch_]);

  useEffect(() => { fetch_(); }, [fetch_]);
  return { config, loading, saving, backingUp, restoring, backupHistory, save, triggerBackup, refresh: fetch_ };
}

// ── System Health ─────────────────────────────────────────────
export function useSystemHealth() {
  const [data, setData] = useState<any>(null);
  const [loading, setLoading] = useState(true);

  const fetch_ = useCallback(async () => {
    try { setData(await apiFetch('/admin/health')); }
    catch { /* fallback */ }
    finally { setLoading(false); }
  }, []);

  useEffect(() => {
    fetch_();
    const id = setInterval(fetch_, 15_000);
    return () => clearInterval(id);
  }, [fetch_]);

  return { data, loading, refresh: fetch_ };
}

// ── Audit Log ─────────────────────────────────────────────────
export function useAuditLog(filters?: { page?: number; pageSize?: number; severity?: string; userId?: string; from?: string; to?: string }) {
  const [data, setData] = useState<any[]>([]);
  const [total, setTotal] = useState(0);
  const [loading, setLoading] = useState(true);

  const fetch_ = useCallback(async () => {
    setLoading(true);
    try {
      const params = new URLSearchParams({
        page: String(filters?.page ?? 1),
        pageSize: String(filters?.pageSize ?? 50),
        ...(filters?.severity ? { severity: filters.severity } : {}),
        ...(filters?.userId ? { userId: filters.userId } : {}),
        ...(filters?.from ? { from: filters.from } : {}),
        ...(filters?.to ? { to: filters.to } : {}),
      });
      const res = await apiFetch<any>(`/admin/audit-log?${params}`);
      setData(res.items ?? []);
      setTotal(res.total ?? 0);
    } catch { /* fallback */ }
    finally { setLoading(false); }
  }, [JSON.stringify(filters)]);

  useEffect(() => { fetch_(); }, [fetch_]);
  return { data, total, loading, refresh: fetch_ };
}

// ── Roles & Permissions ───────────────────────────────────────
export function useRoles() {
  const [data, setData] = useState<any[]>([]);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);

  const fetch_ = useCallback(async () => {
    setLoading(true);
    try { setData(await apiFetch('/admin/roles')); }
    catch { /* fallback */ }
    finally { setLoading(false); }
  }, []);

  const createRole = useCallback(async (payload: any) => {
    setSaving(true);
    try {
      const created = await apiFetch('/admin/roles', { method: 'POST', body: JSON.stringify(payload) });
      setData(prev => [...prev, created]); return true;
    } catch { return false; }
    finally { setSaving(false); }
  }, []);

  const updateRole = useCallback(async (id: string, payload: any) => {
    setSaving(true);
    try {
      await apiFetch(`/admin/roles/${id}`, { method: 'PUT', body: JSON.stringify(payload) });
      setData(prev => prev.map(r => r.id === id ? { ...r, ...payload } : r)); return true;
    } catch { return false; }
    finally { setSaving(false); }
  }, []);

  const deleteRole = useCallback(async (id: string) => {
    try {
      await apiFetch(`/admin/roles/${id}`, { method: 'DELETE' });
      setData(prev => prev.filter(r => r.id !== id)); return true;
    } catch { return false; }
  }, []);

  useEffect(() => { fetch_(); }, [fetch_]);
  return { data, loading, saving, createRole, updateRole, deleteRole, refresh: fetch_ };
}

// ── API Keys ──────────────────────────────────────────────────
export function useApiKeys() {
  const [data, setData] = useState<any[]>([]);
  const [loading, setLoading] = useState(true);

  const fetch_ = useCallback(async () => {
    setLoading(true);
    try { setData(await apiFetch('/admin/api-keys')); }
    catch { /* fallback */ }
    finally { setLoading(false); }
  }, []);

  const create = useCallback(async (payload: any) => {
    try {
      const created = await apiFetch('/admin/api-keys', { method: 'POST', body: JSON.stringify(payload) });
      setData(prev => [...prev, created]); return created;
    } catch { return null; }
  }, []);

  const revoke = useCallback(async (id: string) => {
    try {
      await apiFetch(`/admin/api-keys/${id}`, { method: 'DELETE' });
      setData(prev => prev.filter(k => k.id !== id)); return true;
    } catch { return false; }
  }, []);

  useEffect(() => { fetch_(); }, [fetch_]);
  return { data, loading, create, revoke, refresh: fetch_ };
}

// ── Announcements ─────────────────────────────────────────────
export function useAnnouncements() {
  const [data, setData] = useState<any[]>([]);
  const [loading, setLoading] = useState(true);

  const fetch_ = useCallback(async () => {
    setLoading(true);
    try { setData(await apiFetch('/admin/announcements')); }
    catch { /* fallback */ }
    finally { setLoading(false); }
  }, []);

  const create = useCallback(async (payload: any) => {
    try {
      const created = await apiFetch('/admin/announcements', { method: 'POST', body: JSON.stringify(payload) });
      setData(prev => [created, ...prev]); return true;
    } catch { return false; }
  }, []);

  const update = useCallback(async (id: string, payload: any) => {
    try {
      await apiFetch(`/admin/announcements/${id}`, { method: 'PUT', body: JSON.stringify(payload) });
      setData(prev => prev.map(a => a.id === id ? { ...a, ...payload } : a)); return true;
    } catch { return false; }
  }, []);

  const remove = useCallback(async (id: string) => {
    try {
      await apiFetch(`/admin/announcements/${id}`, { method: 'DELETE' });
      setData(prev => prev.filter(a => a.id !== id)); return true;
    } catch { return false; }
  }, []);

  useEffect(() => { fetch_(); }, [fetch_]);
  return { data, loading, create, update, remove, refresh: fetch_ };
}
