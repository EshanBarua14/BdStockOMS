/* ═══════════════════════════════════════════════════════════════
   BdStockOMS — PriceTicker
   Scrolling price ribbon at the very top — real-time SignalR data
   ═══════════════════════════════════════════════════════════════ */

import React, { useMemo, useRef, useEffect } from 'react';
import { useMarketData } from '../stores/MarketDataStore';
import './PriceTicker.css';

export default function PriceTicker() {
  const { state } = useMarketData();
  const scrollRef = useRef<HTMLDivElement>(null);

  const stocks = useMemo(() => {
    return Object.values(state.stocks).sort((a, b) => {
      // Sort by absolute change% descending — most active first
      return Math.abs(b.changePercent) - Math.abs(a.changePercent);
    });
  }, [state.stocks]);

  // Auto-scroll animation
  useEffect(() => {
    const el = scrollRef.current;
    if (!el) return;

    let animFrame: number;
    let scrollPos = 0;
    const speed = 0.5; // px per frame

    function step() {
      scrollPos += speed;
      if (scrollPos >= el!.scrollWidth / 2) {
        scrollPos = 0;
      }
      el!.scrollLeft = scrollPos;
      animFrame = requestAnimationFrame(step);
    }

    animFrame = requestAnimationFrame(step);

    // Pause on hover
    const pause = () => cancelAnimationFrame(animFrame);
    const resume = () => { animFrame = requestAnimationFrame(step); };
    el.addEventListener('mouseenter', pause);
    el.addEventListener('mouseleave', resume);

    return () => {
      cancelAnimationFrame(animFrame);
      el.removeEventListener('mouseenter', pause);
      el.removeEventListener('mouseleave', resume);
    };
  }, [stocks.length]);

  if (stocks.length === 0) {
    return (
      <div className="price-ticker">
        <div className="price-ticker__empty">
          <span className="price-ticker__dot" />
          Connecting to market data...
        </div>
      </div>
    );
  }

  // Duplicate items for seamless loop
  const tickerItems = [...stocks, ...stocks];

  return (
    <div className="price-ticker">
      <div className="price-ticker__track" ref={scrollRef}>
        {tickerItems.map((stock, idx) => {
          const isUp = stock.changePercent > 0;
          const isDown = stock.changePercent < 0;
          const dirClass = isUp ? 'up' : isDown ? 'down' : 'flat';
          const flashClass = stock.priceDirection === 'up'
            ? 'flash-green'
            : stock.priceDirection === 'down'
              ? 'flash-red'
              : '';

          return (
            <div
              key={`${stock.symbol}-${idx}`}
              className={`price-ticker__item ${dirClass} ${flashClass}`}
            >
              <span className="price-ticker__symbol">{stock.symbol}</span>
              <span className="price-ticker__exchange">{stock.exchange}</span>
              <span className="price-ticker__price">
                {stock.lastTradePrice.toFixed(2)}
              </span>
              <span className={`price-ticker__change ${dirClass}`}>
                {isUp ? '▲' : isDown ? '▼' : '•'}
                {Math.abs(stock.changePercent).toFixed(2)}%
              </span>
            </div>
          );
        })}
      </div>
    </div>
  );
}
