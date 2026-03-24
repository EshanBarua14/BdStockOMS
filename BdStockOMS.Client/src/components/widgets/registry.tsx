// @ts-nocheck
import { MarketTickerStrip }    from "./MarketTickerStrip"
import { WatchlistWidget }      from "./WatchlistWidget"
import { OrderEntryWidget }     from "./OrderEntryWidget"
import { OrderBookWidget }      from "./OrderBookWidget"
import { TopMoversWidget }      from "./TopMoversWidget"
import { MarketMapWidget }      from "./MarketMapWidget"
import { MarketDepthWidget }    from "./MarketDepthWidget"
import { BuySellPressureWidget } from "./BuySellPressureWidget"
import { PortfolioWidget }      from "./PortfolioWidget"
import { PriceChartWidget }     from "./PriceChartWidget"
import { NotificationsWidget }  from "./NotificationsWidget"
import { AIPredictionWidget }   from "./AIPredictionWidget"
import { IndexSummaryWidget }   from "./IndexSummaryWidget"
import { NewsFeedWidget }       from "./NewsFeedWidget"
import { RMSLimitsWidget }      from "./RMSLimitsWidget"
import { OrdersWidget, ExecutionsWidget } from "./OrdersWidget"
import { BuySellConsole, BuySellConsoleEvents, BuySellConsoleInline } from "@/components/trading/BuySellConsole"
// Day 61 — Market Analytics
import { ScoreBoardWidget }    from "./ScoreBoardWidget"
import { TimeAndSalesWidget }  from "./TimeAndSalesWidget"
import { MostActiveWidget }    from "./MostActiveWidget"
import { PriceHistoryWidget }   from "./PriceHistoryWidget"

export interface WidgetDef {
  id:         string
  title:      string
  label:      string
  icon:       string
  minW:       number
  minH:       number
  defaultW:   number
  defaultH:   number
  category:   string
  component:  React.ComponentType<any>
}

export const WIDGET_REGISTRY_LIST: WidgetDef[] = [
  { id:"buysell",    title:"Buy/Sell Console",  label:"Buy/Sell Console",  icon:"⚡", minW:2, minH:2, defaultW:8,  defaultH:14, category:"Trading",   component: BuySellConsoleInline },
  { id:"ticker",     title:"Market Ticker",     label:"Market Ticker",     icon:"📈", minW:2, minH:2, defaultW:24, defaultH:3,  category:"Market",    component: MarketTickerStrip },
  { id:"watchlist",  title:"Watchlist",         label:"Watchlist",         icon:"👁",  minW:2, minH:2, defaultW:10, defaultH:16, category:"Market",    component: WatchlistWidget },
  { id:"order",      title:"Order Entry",       label:"Order Entry",       icon:"⚡", minW:2, minH:2, defaultW:9,  defaultH:14, category:"Trading",   component: (p) => <OrdersWidget ordersData={p.ordersData} /> },
  { id:"orderbook",  title:"Order Book",        label:"Order Book",        icon:"📋", minW:2, minH:2, defaultW:10, defaultH:14, category:"Trading",   component: OrderBookWidget },
  { id:"executions", title:"Executions",        label:"Executions",        icon:"✅", minW:2, minH:2, defaultW:10, defaultH:10, category:"Trading",   component: (p) => <ExecutionsWidget ordersData={p.ordersData} /> },
  { id:"movers",     title:"Top Movers",        label:"Top Movers",        icon:"🚀", minW:2, minH:2, defaultW:10, defaultH:14, category:"Market",    component: TopMoversWidget },
  { id:"heatmap",    title:"Market Map",        label:"Market Map",        icon:"🗺", minW:2, minH:2, defaultW:10, defaultH:12, category:"Market",    component: MarketMapWidget },
  { id:"depth",      title:"Market Depth",      label:"Market Depth",      icon:"📊", minW:2, minH:2, defaultW:10, defaultH:14, category:"Market",    component: MarketDepthWidget },
  { id:"pressure",   title:"Buy/Sell Pressure", label:"Buy/Sell Pressure", icon:"⚖️", minW:2, minH:2, defaultW:9,  defaultH:10, category:"Market",    component: BuySellPressureWidget },
  { id:"portfolio",  title:"Portfolio",         label:"Portfolio",         icon:"💼", minW:2, minH:2, defaultW:10, defaultH:14, category:"Portfolio", component: PortfolioWidget },
  { id:"chart",      title:"Price Chart",       label:"Price Chart",       icon:"📉", minW:2, minH:2, defaultW:10, defaultH:12, category:"Market",    component: PriceChartWidget },
  { id:"notif",      title:"Notifications",     label:"Notifications",     icon:"🔔", minW:2, minH:2, defaultW:9,  defaultH:10, category:"System",    component: NotificationsWidget },
  { id:"ai",         title:"AI Prediction",     label:"AI Prediction",     icon:"🤖", minW:2, minH:2, defaultW:10, defaultH:14, category:"AI",        component: AIPredictionWidget },
  { id:"index",      title:"Index Summary",     label:"Index Summary",     icon:"🏦", minW:2, minH:2, defaultW:9,  defaultH:10, category:"Market",    component: IndexSummaryWidget },
  { id:"news",       title:"News Feed",         label:"News Feed",         icon:"📰", minW:2, minH:2, defaultW:9,  defaultH:10, category:"News",      component: NewsFeedWidget },
  { id:"rms",        title:"RMS Limits",        label:"RMS Limits",        icon:"🛡️", minW:2, minH:2, defaultW:9,  defaultH:10, category:"Risk",      component: RMSLimitsWidget },
  // ── Day 61: Market Analytics ──────────────────────────────────────────────
  { id:"scoreboard", title:"Score Board",       label:"Score Board",       icon:"🏆", minW:2, minH:2, defaultW:14, defaultH:16, category:"Analytics", component: ScoreBoardWidget },
  { id:"timesales",  title:"Time & Sales",      label:"Time & Sales",      icon:"⏱",  minW:2, minH:2, defaultW:10, defaultH:16, category:"Analytics", component: TimeAndSalesWidget },
  { id:"mostactive", title:"Most Active",       label:"Most Active",       icon:"🔥", minW:2, minH:2, defaultW:10, defaultH:16, category:"Analytics", component: MostActiveWidget },
  { id:"pricehistory", title:"Price History", label:"Price History", icon:"📅", minW:3, minH:3, defaultW:14, defaultH:18, category:"Analytics", component: PriceHistoryWidget },
]

// Object map keyed by id — used by DashboardPage
export const WIDGET_REGISTRY: Record<string, WidgetDef> = Object.fromEntries(
  WIDGET_REGISTRY_LIST.map(w => [w.id, w])
)

export const CATEGORIES = [...new Set(WIDGET_REGISTRY_LIST.map(w => w.category))]
