import { useState, useEffect, useRef, useCallback } from 'react';
import * as signalR from '@microsoft/signalr';
import { apiClient } from '@/api/client';

// ─── Types ────────────────────────────────────────────────────────────────────
interface NewsItem {
  id: number;
  title: string;
  summary: string;
  category: string;
  board: string;
  tradingCode?: string;
  source: string;
  sourceUrl?: string;
  publishedAt: string;
  isPriceSensitive: boolean;
  keywords: string[];
}

interface NewsPagedResult {
  items: NewsItem[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

// ─── Constants ────────────────────────────────────────────────────────────────
const BOARDS = ['ALL', 'A', 'B', 'N', 'Z', 'SME'];

const CATEGORIES = [
  { value: 'all', label: 'All' },
  { value: 'price-sensitive', label: 'Price Sensitive' },
  { value: 'regulatory', label: 'Regulatory' },
  { value: 'corporate', label: 'Corporate' },
  { value: 'general', label: 'General' },
];

const CATEGORY_STYLES: Record<string, string> = {
  'price-sensitive': 'bg-red-500/15 text-red-400 border-red-500/30',
  regulatory: 'bg-blue-500/15 text-blue-400 border-blue-500/30',
  corporate: 'bg-purple-500/15 text-purple-400 border-purple-500/30',
  general: 'bg-slate-500/15 text-slate-400 border-slate-500/30',
};

function formatRelativeTime(dateStr: string): string {
  const diff = Date.now() - new Date(dateStr).getTime();
  const mins = Math.floor(diff / 60000);
  if (mins < 1) return 'just now';
  if (mins < 60) return `${mins}m ago`;
  const hrs = Math.floor(mins / 60);
  if (hrs < 24) return `${hrs}h ago`;
  return new Date(dateStr).toLocaleDateString('en-GB', { day: '2-digit', month: 'short' });
}

// ─── Component ────────────────────────────────────────────────────────────────
export function NewsFeedWidget() {
  const [items, setItems] = useState<NewsItem[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [totalPages, setTotalPages] = useState(1);
  const [page, setPage] = useState(1);

  // Filter state
  const [keyword, setKeyword] = useState('');
  const [debouncedKeyword, setDebouncedKeyword] = useState('');
  const [board, setBoard] = useState('ALL');
  const [category, setCategory] = useState('all');

  const [loading, setLoading] = useState(false);
  const [liveCount, setLiveCount] = useState(0);
  const [connected, setConnected] = useState(false);
  const [expandedId, setExpandedId] = useState<number | null>(null);
  const [newItemIds, setNewItemIds] = useState<Set<number>>(new Set());

  const hubRef = useRef<signalR.HubConnection | null>(null);
  const keywordDebounceRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  // ── Debounce keyword input ────────────────────────────────────────────────
  useEffect(() => {
    if (keywordDebounceRef.current) clearTimeout(keywordDebounceRef.current);
    keywordDebounceRef.current = setTimeout(() => {
      setDebouncedKeyword(keyword);
      setPage(1);
    }, 400);
    return () => { if (keywordDebounceRef.current) clearTimeout(keywordDebounceRef.current); };
  }, [keyword]);

  // ── Fetch news ────────────────────────────────────────────────────────────
  const fetchNews = useCallback(async () => {
    setLoading(true);
    try {
      const params = new URLSearchParams({ page: String(page), pageSize: '15' });
      if (debouncedKeyword) params.set('keyword', debouncedKeyword);
      if (board !== 'ALL') params.set('board', board);
      if (category !== 'all') params.set('category', category);

      const res = await apiClient.get(`/api/news?${params}`).then(r => r.data as NewsPagedResult);
      setItems(res.items);
      setTotalCount(res.totalCount);
      setTotalPages(res.totalPages);
    } catch (err) {
      console.error('Failed to fetch news', err);
    } finally {
      setLoading(false);
    }
  }, [page, debouncedKeyword, board, category]);

  useEffect(() => { fetchNews(); }, [fetchNews]);

  // ── SignalR connection ────────────────────────────────────────────────────
  useEffect(() => {
    const hub = new signalR.HubConnectionBuilder()
      .withUrl('/hubs/news', {
        accessTokenFactory: () => { try { const r = localStorage.getItem('bd_oms_auth_v2'); if (r) return JSON.parse(r)?.state?.user?.token ?? ''; } catch {} return localStorage.getItem('token') ?? ''; },
      })
      .withAutomaticReconnect()
      .build();

    hub.on('ReceiveNews', (item: NewsItem) => {
      setItems(prev => {
        // Only inject if matches current filters
        const kw = debouncedKeyword.toLowerCase();
        if (kw && !item.title.toLowerCase().includes(kw) && !item.keywords.some(k => k.includes(kw))) return prev;
        if (board !== 'ALL' && item.board !== board) return prev;
        if (category !== 'all' && item.category !== category) return prev;

        setLiveCount(c => c + 1);
        setNewItemIds(s => new Set([...s, item.id]));
        setTimeout(() => setNewItemIds(s => { const n = new Set(s); n.delete(item.id); return n; }), 3000);
        return [item, ...prev.slice(0, 14)];
      });
    });

    hub.onclose(() => setConnected(false));
    hub.onreconnecting(() => setConnected(false));
    hub.onreconnected(() => setConnected(true));

    hub.start().then(() => {
      setConnected(true);
      hubRef.current = hub;
    }).catch(console.error);

    return () => { hub.stop(); };
  }, []); // intentionally omit filter deps — we filter in the handler above

  // ── Reset page on filter change ───────────────────────────────────────────
  useEffect(() => { setPage(1); }, [board, category]);

  return (
    <div className="flex flex-col h-full bg-[var(--t-bg)] rounded-lg border border-slate-800 overflow-hidden">

      {/* ── Header ──────────────────────────────────────────────────────── */}
      <div className="flex items-center justify-between px-4 py-2.5 border-b border-slate-800 bg-[var(--t-panel)]">
        <div className="flex items-center gap-2">
          <div className="w-1.5 h-4 rounded-sm bg-gradient-to-b from-amber-400 to-orange-500" />
          <span className="text-sm font-semibold text-slate-100 tracking-wide">Market News</span>
          {totalCount > 0 && (
            <span className="text-xs text-slate-500">({totalCount})</span>
          )}
        </div>
        <div className="flex items-center gap-3">
          {liveCount > 0 && (
            <span className="text-[10px] bg-emerald-500/20 text-emerald-400 border border-emerald-500/30 px-1.5 py-0.5 rounded">
              +{liveCount} live
            </span>
          )}
          <div className={`flex items-center gap-1.5 text-[10px] ${connected ? 'text-emerald-400' : 'text-slate-500'}`}>
            <div className={`w-1.5 h-1.5 rounded-full ${connected ? 'bg-emerald-400 animate-pulse' : 'bg-slate-600'}`} />
            {connected ? 'LIVE' : 'OFFLINE'}
          </div>
        </div>
      </div>

      {/* ── Filters ─────────────────────────────────────────────────────── */}
      <div className="px-3 py-2 border-b border-slate-800/60 bg-[var(--t-panel)] space-y-2">
        {/* Keyword */}
        <div className="relative">
          <svg className="absolute left-2.5 top-1/2 -translate-y-1/2 w-3 h-3 text-slate-500" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
          </svg>
          <input
            type="text"
            placeholder="Search keyword or trading code…"
            value={keyword}
            onChange={e => setKeyword(e.target.value)}
            className="w-full bg-slate-800/50 border border-slate-700/50 rounded text-xs text-slate-300 placeholder-slate-600
                       pl-7 pr-3 py-1.5 focus:outline-none focus:border-amber-500/50 focus:ring-1 focus:ring-amber-500/20 transition-all"
          />
          {keyword && (
            <button
              onClick={() => setKeyword('')}
              className="absolute right-2 top-1/2 -translate-y-1/2 text-slate-500 hover:text-slate-300"
            >
              ×
            </button>
          )}
        </div>

        {/* Board + Category */}
        <div className="flex gap-2">
          {/* Board chips */}
          <div className="flex gap-1 flex-wrap">
            {BOARDS.map(b => (
              <button
                key={b}
                onClick={() => setBoard(b)}
                className={`text-[10px] px-2 py-0.5 rounded font-mono font-semibold border transition-all
                  ${board === b
                    ? 'bg-amber-500/20 text-amber-400 border-amber-500/40'
                    : 'bg-slate-800/40 text-slate-500 border-slate-700/40 hover:text-slate-300'}`}
              >
                {b}
              </button>
            ))}
          </div>

          {/* Category select */}
          <select
            value={category}
            onChange={e => setCategory(e.target.value)}
            className="ml-auto text-[10px] bg-slate-800/50 border border-slate-700/50 text-slate-400
                       rounded px-2 py-0.5 focus:outline-none focus:border-amber-500/50"
          >
            {CATEGORIES.map(c => (
              <option key={c.value} value={c.value}>{c.label}</option>
            ))}
          </select>
        </div>
      </div>

      {/* ── News List ────────────────────────────────────────────────────── */}
      <div className="flex-1 overflow-y-auto">
        {loading && items.length === 0 && (
          <div className="flex items-center justify-center h-20 text-slate-600 text-xs">Loading…</div>
        )}
        {!loading && items.length === 0 && (
          <div className="flex items-center justify-center h-20 text-slate-600 text-xs">No news found</div>
        )}
        {items.map(item => (
          <div
            key={item.id}
            onClick={() => setExpandedId(expandedId === item.id ? null : item.id)}
            className={`border-b border-slate-800/40 cursor-pointer transition-all duration-200 px-4 py-2.5
              ${newItemIds.has(item.id) ? 'bg-amber-500/5 border-l-2 border-l-amber-500' : 'hover:bg-slate-800/20'}`}
          >
            <div className="flex items-start justify-between gap-2 mb-1">
              <div className="flex items-center gap-1.5 flex-wrap">
                <span className={`text-[9px] px-1.5 py-0.5 rounded border ${CATEGORY_STYLES[item.category] ?? 'bg-slate-700 text-slate-400'}`}>
                  {item.category}
                </span>
                {item.isPriceSensitive && (
                  <span className="text-[9px] px-1.5 py-0.5 rounded border bg-red-500/10 text-red-400 border-red-500/20">
                    PS
                  </span>
                )}
                {item.tradingCode && (
                  <span className="text-[9px] font-mono font-bold text-amber-400 bg-amber-500/10 px-1.5 py-0.5 rounded">
                    {item.tradingCode}
                  </span>
                )}
                <span className="text-[9px] font-mono text-slate-600 bg-slate-800/60 px-1.5 py-0.5 rounded">
                  {item.board}
                </span>
              </div>
              <span className="text-[9px] text-slate-600 whitespace-nowrap shrink-0">
                {formatRelativeTime(item.publishedAt)}
              </span>
            </div>

            <p className={`text-xs text-slate-200 leading-snug font-medium ${expandedId === item.id ? '' : 'line-clamp-2'}`}>
              {item.title}
            </p>

            {expandedId === item.id && (
              <div className="mt-2 space-y-1.5">
                <p className="text-[11px] text-slate-400 leading-relaxed">{item.summary}</p>
                <div className="flex items-center justify-between pt-1">
                  <span className="text-[9px] text-slate-600">Source: {item.source}</span>
                  {item.sourceUrl && (
                    <a
                      href={item.sourceUrl}
                      target="_blank"
                      rel="noopener noreferrer"
                      onClick={e => e.stopPropagation()}
                      className="text-[9px] text-amber-400/70 hover:text-amber-400 underline"
                    >
                      Full Article →
                    </a>
                  )}
                </div>
                {item.keywords.length > 0 && (
                  <div className="flex gap-1 flex-wrap">
                    {item.keywords.map(k => (
                      <button
                        key={k}
                        onClick={e => { e.stopPropagation(); setKeyword(k); }}
                        className="text-[9px] text-slate-500 hover:text-amber-400 transition-colors"
                      >
                        #{k}
                      </button>
                    ))}
                  </div>
                )}
              </div>
            )}
          </div>
        ))}
      </div>

      {/* ── Pagination ───────────────────────────────────────────────────── */}
      {totalPages > 1 && (
        <div className="flex items-center justify-between px-4 py-2 border-t border-slate-800 bg-[var(--t-panel)]">
          <button
            disabled={page <= 1}
            onClick={() => setPage(p => p - 1)}
            className="text-[10px] text-slate-400 disabled:text-slate-700 hover:text-slate-200 transition-colors px-2 py-0.5 rounded border border-slate-700/50 disabled:border-slate-800"
          >
            ← Prev
          </button>
          <span className="text-[10px] text-slate-500">
            Page {page} / {totalPages}
          </span>
          <button
            disabled={page >= totalPages}
            onClick={() => setPage(p => p + 1)}
            className="text-[10px] text-slate-400 disabled:text-slate-700 hover:text-slate-200 transition-colors px-2 py-0.5 rounded border border-slate-700/50 disabled:border-slate-800"
          >
            Next →
          </button>
        </div>
      )}
    </div>
  );
}

export default NewsFeedWidget;
