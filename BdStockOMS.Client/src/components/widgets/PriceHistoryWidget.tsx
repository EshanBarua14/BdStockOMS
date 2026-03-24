import { useState, useEffect, useCallback } from 'react';
import { apiClient } from '@/api/client';

// ─── Types ────────────────────────────────────────────────────────────────────
interface OhlcEntry {
  date: string;
  open: number;
  high: number;
  low: number;
  close: number;
  volume: number;
  value: number;
  trades: number;
  previousClose?: number;
  change: number;
  changePct: number;
  isUp: boolean;
}

interface PriceHistoryResult {
  tradingCode: string;
  companyName: string;
  board: string;
  data: OhlcEntry[];
  fiftyTwoWeekHigh?: number;
  fiftyTwoWeekLow?: number;
  averageVolume?: number;
  averageClose?: number;
}

// ─── Helpers ──────────────────────────────────────────────────────────────────
type Interval = 'daily' | 'weekly' | 'monthly';

const PRESETS: { label: string; days: number }[] = [
  { label: '1W', days: 7 },
  { label: '1M', days: 30 },
  { label: '3M', days: 90 },
  { label: '6M', days: 180 },
  { label: '1Y', days: 365 },
];

function toInputDate(d: Date): string {
  return d.toISOString().split('T')[0];
}

function formatDate(dateStr: string, interval: Interval): string {
  const d = new Date(dateStr);
  if (interval === 'monthly') return d.toLocaleDateString('en-GB', { month: 'short', year: '2-digit' });
  if (interval === 'weekly') return `W${getISOWeek(d)} '${d.toLocaleDateString('en-GB', { year: '2-digit' })}`;
  return d.toLocaleDateString('en-GB', { day: '2-digit', month: 'short' });
}

function getISOWeek(d: Date): number {
  const tmp = new Date(d);
  tmp.setHours(0, 0, 0, 0);
  tmp.setDate(tmp.getDate() + 3 - ((tmp.getDay() + 6) % 7));
  const week1 = new Date(tmp.getFullYear(), 0, 4);
  return 1 + Math.round(((tmp.getTime() - week1.getTime()) / 86400000 - 3 + ((week1.getDay() + 6) % 7)) / 7);
}

function formatVol(vol: number): string {
  if (vol >= 1_000_000) return `${(vol / 1_000_000).toFixed(2)}M`;
  if (vol >= 1_000) return `${(vol / 1_000).toFixed(1)}K`;
  return vol.toLocaleString();
}

// ─── Component ────────────────────────────────────────────────────────────────
interface Props {
  defaultTradingCode?: string;
}

export function PriceHistoryWidget({ defaultTradingCode = 'BRACBANK' }: Props) {
  const [tradingCode, setTradingCode] = useState(defaultTradingCode);
  const [inputCode, setInputCode] = useState(defaultTradingCode);
  const [interval, setInterval] = useState<Interval>('daily');
  const [fromDate, setFromDate] = useState(toInputDate(new Date(Date.now() - 90 * 86400000)));
  const [toDate, setToDate] = useState(toInputDate(new Date()));
  const [result, setResult] = useState<PriceHistoryResult | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [sortField, setSortField] = useState<keyof OhlcEntry>('date');
  const [sortAsc, setSortAsc] = useState(false);

  const fetchData = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const params = new URLSearchParams({ fromDate, toDate, interval });
      const res = await apiClient.get(`/api/pricehistory/${tradingCode}?${params}`).then(r => r.data as PriceHistoryResult);
      setResult(res);
    } catch (err: any) {
      setError(err?.message || 'Failed to load price history');
    } finally {
      setLoading(false);
    }
  }, [tradingCode, fromDate, toDate, interval]);

  useEffect(() => { fetchData(); }, [fetchData]);

  const applyPreset = (days: number) => {
    setFromDate(toInputDate(new Date(Date.now() - days * 86400000)));
    setToDate(toInputDate(new Date()));
    setInterval(days <= 30 ? 'daily' : days <= 180 ? 'weekly' : 'monthly');
  };

  const handleSort = (field: keyof OhlcEntry) => {
    if (sortField === field) setSortAsc(a => !a);
    else { setSortField(field); setSortAsc(false); }
  };

  const sortedData = result?.data ? [...result.data].sort((a, b) => {
    const av = a[sortField] as any;
    const bv = b[sortField] as any;
    const cmp = av < bv ? -1 : av > bv ? 1 : 0;
    return sortAsc ? cmp : -cmp;
  }) : [];

  const SortIcon = ({ field }: { field: keyof OhlcEntry }) => (
    <span className="ml-0.5 opacity-40">
      {sortField === field ? (sortAsc ? '▲' : '▼') : '⇅'}
    </span>
  );

  return (
    <div className="flex flex-col h-full bg-[#0f1117] rounded-lg border border-slate-800 overflow-hidden">

      {/* ── Header ──────────────────────────────────────────────────────── */}
      <div className="flex items-center justify-between px-4 py-2.5 border-b border-slate-800 bg-[#141620]">
        <div className="flex items-center gap-2">
          <div className="w-1.5 h-4 rounded-sm bg-gradient-to-b from-violet-400 to-indigo-500" />
          <span className="text-sm font-semibold text-slate-100 tracking-wide">Price History</span>
          {result && (
            <span className="text-xs font-mono font-bold text-violet-400">{result.tradingCode}</span>
          )}
        </div>
        {result && (
          <span className="text-[10px] text-slate-600">{result.companyName}</span>
        )}
      </div>

      {/* ── Controls ─────────────────────────────────────────────────────── */}
      <div className="px-3 py-2 border-b border-slate-800/60 bg-[#12151d] space-y-2">
        {/* Code search + interval */}
        <div className="flex items-center gap-2">
          <input
            type="text"
            value={inputCode}
            onChange={e => setInputCode(e.target.value.toUpperCase())}
            onKeyDown={e => e.key === 'Enter' && setTradingCode(inputCode.trim().toUpperCase())}
            placeholder="Trading code…"
            className="w-28 bg-slate-800/50 border border-slate-700/50 rounded text-xs font-mono text-slate-300
                       placeholder-slate-600 px-2 py-1 focus:outline-none focus:border-violet-500/50 focus:ring-1 focus:ring-violet-500/20"
          />
          <button
            onClick={() => setTradingCode(inputCode.trim().toUpperCase())}
            className="text-xs px-2.5 py-1 bg-violet-600/20 text-violet-400 border border-violet-500/30 rounded hover:bg-violet-600/30 transition-colors"
          >
            Go
          </button>

          <div className="flex gap-1 ml-auto">
            {(['daily', 'weekly', 'monthly'] as Interval[]).map(iv => (
              <button
                key={iv}
                onClick={() => setInterval(iv)}
                className={`text-[9px] px-2 py-0.5 rounded border font-semibold capitalize transition-all
                  ${interval === iv
                    ? 'bg-violet-500/20 text-violet-400 border-violet-500/40'
                    : 'bg-slate-800/40 text-slate-500 border-slate-700/40 hover:text-slate-300'}`}
              >
                {iv[0].toUpperCase() + iv.slice(1, 1)}
                {iv === 'daily' ? 'D' : iv === 'weekly' ? 'W' : 'M'}
              </button>
            ))}
          </div>
        </div>

        {/* Date range + presets */}
        <div className="flex items-center gap-2 flex-wrap">
          <input
            type="date"
            value={fromDate}
            max={toDate}
            onChange={e => setFromDate(e.target.value)}
            className="bg-slate-800/50 border border-slate-700/50 rounded text-[10px] text-slate-400 px-2 py-0.5
                       focus:outline-none focus:border-violet-500/50 [color-scheme:dark]"
          />
          <span className="text-slate-600 text-[10px]">→</span>
          <input
            type="date"
            value={toDate}
            min={fromDate}
            onChange={e => setToDate(e.target.value)}
            className="bg-slate-800/50 border border-slate-700/50 rounded text-[10px] text-slate-400 px-2 py-0.5
                       focus:outline-none focus:border-violet-500/50 [color-scheme:dark]"
          />
          <div className="flex gap-1 ml-auto">
            {PRESETS.map(p => (
              <button
                key={p.label}
                onClick={() => applyPreset(p.days)}
                className="text-[9px] px-1.5 py-0.5 rounded bg-slate-800/50 text-slate-500 border border-slate-700/40 hover:text-violet-400 hover:border-violet-500/40 transition-all"
              >
                {p.label}
              </button>
            ))}
          </div>
        </div>
      </div>

      {/* ── 52W Stats Bar ────────────────────────────────────────────────── */}
      {result && (
        <div className="flex items-center gap-5 px-4 py-1.5 border-b border-slate-800/40 bg-[#10131a] text-[10px] font-mono">
          <span className="text-slate-500">
            52W H: <span className="text-emerald-400">{result.fiftyTwoWeekHigh?.toFixed(2) ?? '—'}</span>
          </span>
          <span className="text-slate-500">
            52W L: <span className="text-red-400">{result.fiftyTwoWeekLow?.toFixed(2) ?? '—'}</span>
          </span>
          <span className="text-slate-500">
            Avg Vol: <span className="text-slate-300">{result.averageVolume ? formatVol(Math.round(result.averageVolume)) : '—'}</span>
          </span>
          <span className="text-slate-500 ml-auto">
            <span className="text-slate-400">{sortedData.length}</span> {interval} bars
          </span>
        </div>
      )}

      {/* ── Table ─────────────────────────────────────────────────────────── */}
      <div className="flex-1 overflow-hidden flex flex-col">
        {/* Headers */}
        <div className="grid grid-cols-[5rem_4.5rem_4.5rem_4.5rem_4.5rem_5rem_4rem_4rem]
                        text-[9px] text-slate-500 font-semibold uppercase tracking-wider
                        border-b border-slate-800/60 px-3 py-1.5 bg-[#0d1016]">
          {([
            ['date', 'Date'],
            ['open', 'Open'],
            ['high', 'High'],
            ['low', 'Low'],
            ['close', 'Close'],
            ['volume', 'Volume'],
            ['trades', 'Trades'],
            ['changePct', 'Chg%'],
          ] as [keyof OhlcEntry, string][]).map(([f, label]) => (
            <button
              key={f}
              onClick={() => handleSort(f)}
              className="text-left hover:text-slate-300 transition-colors"
            >
              {label}<SortIcon field={f} />
            </button>
          ))}
        </div>

        <div className="flex-1 overflow-y-auto">
          {loading && (
            <div className="flex items-center justify-center h-20 text-slate-600 text-xs">Loading…</div>
          )}
          {error && (
            <div className="flex items-center justify-center h-16 text-red-500/70 text-xs px-4 text-center">{error}</div>
          )}
          {!loading && !error && sortedData.map((entry, i) => (
            <div
              key={entry.date}
              className={`grid grid-cols-[5rem_4.5rem_4.5rem_4.5rem_4.5rem_5rem_4rem_4rem]
                           items-center px-3 py-[3px] border-b border-slate-800/20 text-[11px] font-mono
                           ${i % 2 === 0 ? 'bg-transparent' : 'bg-slate-900/20'} hover:bg-slate-800/25 transition-colors`}
            >
              {/* Date */}
              <span className="text-slate-500 text-[9px]">{formatDate(entry.date, interval)}</span>

              {/* Open */}
              <span className="text-slate-400">{entry.open.toFixed(2)}</span>

              {/* High */}
              <span className="text-emerald-400/80">{entry.high.toFixed(2)}</span>

              {/* Low */}
              <span className="text-red-400/80">{entry.low.toFixed(2)}</span>

              {/* Close */}
              <span className={`font-bold ${entry.isUp ? 'text-emerald-400' : 'text-red-400'}`}>
                {entry.close.toFixed(2)}
              </span>

              {/* Volume */}
              <span className="text-slate-400">{formatVol(entry.volume)}</span>

              {/* Trades */}
              <span className="text-slate-500">{entry.trades.toLocaleString()}</span>

              {/* Change % */}
              <span className={`font-semibold text-[10px] ${entry.isUp ? 'text-emerald-400' : entry.changePct < 0 ? 'text-red-400' : 'text-slate-400'}`}>
                {entry.isUp ? '+' : ''}{entry.changePct.toFixed(2)}%
              </span>
            </div>
          ))}
          {!loading && !error && sortedData.length === 0 && (
            <div className="flex items-center justify-center h-16 text-slate-600 text-xs">No data in range</div>
          )}
        </div>
      </div>
    </div>
  );
}

export default PriceHistoryWidget;
