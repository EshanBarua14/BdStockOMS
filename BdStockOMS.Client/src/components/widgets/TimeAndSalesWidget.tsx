import { useState, useEffect, useRef, useCallback } from 'react';
import { subscribeMarket } from '@/hooks/useSignalR';
import { apiClient } from '@/api/client';

// ─── Types ────────────────────────────────────────────────────────────────────
type AggressorSide = 0 | 1 | -1; // Unknown | Buy | Sell

interface TimeAndSalesEntry {
  id: number;
  tradeMatchId: string;
  tradingCode: string;
  price: number;
  volume: number;
  value: number;
  executedAt: string;
  aggressor: AggressorSide;
  priceChange: number;
  previousClose?: number;
  changeFromClose?: number;
  changeFromClosePct?: number;
}

// ─── Aggressor indicator ──────────────────────────────────────────────────────
function AggressorBadge({ side }: { side: AggressorSide }) {
  if (side === 1)
    return (
      <span className="inline-flex items-center gap-0.5 text-[9px] font-bold text-emerald-400 bg-emerald-500/10 px-1.5 py-0.5 rounded font-mono">
        ▲ BUY
      </span>
    );
  if (side === -1)
    return (
      <span className="inline-flex items-center gap-0.5 text-[9px] font-bold text-red-400 bg-red-500/10 px-1.5 py-0.5 rounded font-mono">
        ▼ SELL
      </span>
    );
  return <span className="text-[9px] text-slate-600 font-mono">—</span>;
}

// ─── Formatters ───────────────────────────────────────────────────────────────
function formatTime(dateStr: string): string {
  return new Date(dateStr).toLocaleTimeString('en-GB', {
    hour: '2-digit', minute: '2-digit', second: '2-digit', hour12: false
  });
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

export function TimeAndSalesWidget({ defaultTradingCode = 'BRACBANK' }: Props) {
  const [tradingCode, setTradingCode] = useState(defaultTradingCode);
  const [inputCode, setInputCode] = useState(defaultTradingCode);
  const [entries, setEntries] = useState<TimeAndSalesEntry[]>([]);
  const [loading, setLoading] = useState(false);
  const [showMatchId, setShowMatchId] = useState(false);
  const [aggressorFilter, setAggressorFilter] = useState<AggressorSide | 'all'>('all');
  const [connected, setConnected] = useState(false);
  const [flashIds, setFlashIds] = useState<Set<number>>(new Set());

  const tableRef = useRef<HTMLDivElement>(null);

  // ── Fetch history ──────────────────────────────────────────────────────────
  const fetchData = useCallback(async (code: string) => {
    setLoading(true);
    try {
      const params = new URLSearchParams({ count: '80' });
      if (aggressorFilter !== 'all') params.set('aggressorFilter', String(aggressorFilter));
      const res = await apiClient.get(`/api/timeandsales/${code}?${params}`).then(r => r.data as TimeAndSalesEntry[]);
      setEntries(res);
    } catch (err) {
      console.error('Failed to fetch T&S', err);
    } finally {
      setLoading(false);
    }
  }, [aggressorFilter]);

  useEffect(() => { fetchData(tradingCode); }, [tradingCode, fetchData]);

  // ── SignalR — reuses global market hub (started in main.tsx) ──────────────────
  useEffect(() => {
    const unsub = subscribeMarket("ReceiveTimeAndSales", (entry: TimeAndSalesEntry) => {
      if (entry.tradingCode !== tradingCode) return;
      if (aggressorFilter !== "all" && entry.aggressor !== aggressorFilter) return;
      setFlashIds(s => new Set([...s, entry.id]));
      setTimeout(() => setFlashIds(s => { const n = new Set(s); n.delete(entry.id); return n; }), 800);
      setEntries(prev => [entry, ...prev.slice(0, 199)]);
      tableRef.current?.scrollTo({ top: 0, behavior: "smooth" });
    });
    setConnected(true);
    return () => unsub();
  }, [tradingCode, aggressorFilter]);

  const handleSearch = () => {
    const code = inputCode.trim().toUpperCase();
    if (code) setTradingCode(code);
  };

  const filteredEntries = aggressorFilter === 'all'
    ? entries
    : entries.filter(e => e.aggressor === aggressorFilter);

  // ── Stats ──────────────────────────────────────────────────────────────────
  const buyCount = entries.filter(e => e.aggressor === 1).length;
  const sellCount = entries.filter(e => e.aggressor === -1).length;
  const totalVol = entries.reduce((s, e) => s + e.volume, 0);

  return (
    <div className="flex flex-col h-full bg-[#0f1117] rounded-lg border border-slate-800 overflow-hidden">

      {/* ── Header ──────────────────────────────────────────────────────── */}
      <div className="flex items-center justify-between px-4 py-2.5 border-b border-slate-800 bg-[#141620]">
        <div className="flex items-center gap-2">
          <div className="w-1.5 h-4 rounded-sm bg-gradient-to-b from-cyan-400 to-blue-500" />
          <span className="text-sm font-semibold text-slate-100 tracking-wide">Time & Sales</span>
          <span className="text-xs font-mono font-bold text-cyan-400">{tradingCode}</span>
        </div>
        <div className="flex items-center gap-2">
          <button
            onClick={() => setShowMatchId(v => !v)}
            title="Toggle Trade Match ID column"
            className={`text-[9px] px-1.5 py-0.5 rounded border transition-all font-mono
              ${showMatchId
                ? 'bg-cyan-500/20 text-cyan-400 border-cyan-500/40'
                : 'bg-slate-800/40 text-slate-500 border-slate-700/40 hover:text-slate-300'}`}
          >
            ID
          </button>
          <div className={`flex items-center gap-1 text-[10px] ${connected ? 'text-emerald-400' : 'text-slate-500'}`}>
            <div className={`w-1.5 h-1.5 rounded-full ${connected ? 'bg-emerald-400 animate-pulse' : 'bg-slate-600'}`} />
            LIVE
          </div>
        </div>
      </div>

      {/* ── Search + Filter Bar ─────────────────────────────────────────── */}
      <div className="px-3 py-2 border-b border-slate-800/60 bg-[#12151d] flex items-center gap-2">
        <input
          type="text"
          value={inputCode}
          onChange={e => setInputCode(e.target.value.toUpperCase())}
          onKeyDown={e => e.key === 'Enter' && handleSearch()}
          placeholder="Trading code…"
          className="w-28 bg-slate-800/50 border border-slate-700/50 rounded text-xs font-mono text-slate-300
                     placeholder-slate-600 px-2 py-1 focus:outline-none focus:border-cyan-500/50 focus:ring-1 focus:ring-cyan-500/20"
        />
        <button
          onClick={handleSearch}
          className="text-xs px-2.5 py-1 bg-cyan-600/20 text-cyan-400 border border-cyan-500/30 rounded hover:bg-cyan-600/30 transition-colors"
        >
          Go
        </button>

        <div className="flex gap-1 ml-auto">
          {(['all', 1, -1] as const).map(side => (
            <button
              key={String(side)}
              onClick={() => setAggressorFilter(side)}
              className={`text-[9px] px-2 py-0.5 rounded border font-semibold transition-all
                ${aggressorFilter === side
                  ? side === 1 ? 'bg-emerald-500/20 text-emerald-400 border-emerald-500/40'
                    : side === -1 ? 'bg-red-500/20 text-red-400 border-red-500/40'
                    : 'bg-slate-600/30 text-slate-300 border-slate-500/40'
                  : 'bg-slate-800/30 text-slate-600 border-slate-700/30 hover:text-slate-400'}`}
            >
              {side === 'all' ? 'ALL' : side === 1 ? '▲ BUY' : '▼ SELL'}
            </button>
          ))}
        </div>
      </div>

      {/* ── Stats Bar ─────────────────────────────────────────────────── */}
      {entries.length > 0 && (
        <div className="flex items-center gap-4 px-4 py-1.5 border-b border-slate-800/40 bg-[#10131a] text-[10px]">
          <span className="text-slate-500">
            Vol: <span className="text-slate-300 font-mono">{formatVol(totalVol)}</span>
          </span>
          <span className="text-emerald-400/80">
            ▲ {buyCount} <span className="text-slate-600">buys</span>
          </span>
          <span className="text-red-400/80">
            ▼ {sellCount} <span className="text-slate-600">sells</span>
          </span>
          <span className="text-slate-500 ml-auto">
            {filteredEntries.length} trades
          </span>
        </div>
      )}

      {/* ── Table ────────────────────────────────────────────────────────── */}
      <div className="flex-1 overflow-hidden flex flex-col">
        {/* Column Headers */}
        <div className={`grid text-[9px] text-slate-500 font-semibold uppercase tracking-wider
                         border-b border-slate-800/60 px-3 py-1.5 bg-[#0d1016]
                         ${showMatchId ? 'grid-cols-[1fr_5rem_4.5rem_4.5rem_4.5rem_4.5rem]' : 'grid-cols-[1fr_5rem_4.5rem_4.5rem_4.5rem]'}`}>
          <span>Time</span>
          {showMatchId && <span>Match ID</span>}
          <span className="text-right">Price</span>
          <span className="text-right">Volume</span>
          <span className="text-right">Value</span>
          <span className="text-center">Side</span>
        </div>

        <div ref={tableRef} className="flex-1 overflow-y-auto">
          {loading && entries.length === 0 && (
            <div className="flex items-center justify-center h-20 text-slate-600 text-xs">Loading…</div>
          )}
          {filteredEntries.map(entry => {
            const isUp = entry.priceChange > 0;
            const isDown = entry.priceChange < 0;
            const isFlash = flashIds.has(entry.id);

            return (
              <div
                key={entry.id}
                className={`grid items-center px-3 py-[4px] border-b border-slate-800/20 text-[11px] font-mono
                             transition-all duration-500
                             ${isFlash ? 'bg-cyan-500/10' : 'hover:bg-slate-800/20'}
                             ${showMatchId
                               ? 'grid-cols-[1fr_5rem_4.5rem_4.5rem_4.5rem_4.5rem]'
                               : 'grid-cols-[1fr_5rem_4.5rem_4.5rem_4.5rem]'}`}
              >
                {/* Time */}
                <span className="text-slate-500 text-[9px]">{formatTime(entry.executedAt)}</span>

                {/* Trade Match ID */}
                {showMatchId && (
                  <span
                    className="text-[8px] text-slate-600 truncate"
                    title={entry.tradeMatchId}
                  >
                    {entry.tradeMatchId.split('-')[1]}
                  </span>
                )}

                {/* Price */}
                <span className={`text-right font-bold
                  ${isUp ? 'text-emerald-400' : isDown ? 'text-red-400' : 'text-slate-300'}`}>
                  {entry.price.toFixed(2)}
                  {isUp && <span className="ml-0.5 text-[8px]">▲</span>}
                  {isDown && <span className="ml-0.5 text-[8px]">▼</span>}
                </span>

                {/* Volume */}
                <span className="text-right text-slate-400">{formatVol(entry.volume)}</span>

                {/* Value (in BDT thousand) */}
                <span className="text-right text-slate-500 text-[9px]">
                  {(entry.value / 1000).toFixed(1)}K
                </span>

                {/* Aggressor */}
                <div className="flex justify-center">
                  <AggressorBadge side={entry.aggressor} />
                </div>
              </div>
            );
          })}
        </div>
      </div>
    </div>
  );
}

export default TimeAndSalesWidget;
