// @ts-nocheck
import { MarketTickerStrip }    from "./MarketTickerStrip"
import { WatchlistWidget }      from "./WatchlistWidget"
import { OrderEntryWidget }     from "./OrderEntryWidget"
import { OrderBookWidget }      from "./OrderBookWidget"
import { ExecutionListWidget }  from "./ExecutionListWidget"
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

export interface WidgetDef {
  id:         string
  label:      string
  icon:       string
  minW:       number
  minH:       number
  defaultW:   number
  defaultH:   number
  category:   string
  component:  React.ComponentType<any>
}

export const WIDGET_REGISTRY: WidgetDef[] = [
  { id:"ticker",    label:"Market Ticker",     icon:"📈", minW:4, minH:1, defaultW:12, defaultH:1,  category:"Market",    component: MarketTickerStrip },
  { id:"watchlist", label:"Watchlist",         icon:"👁",  minW:2, minH:3, defaultW:3,  defaultH:6,  category:"Market",    component: WatchlistWidget },
  { id:"order",     label:"Order Entry",       icon:"⚡", minW:2, minH:4, defaultW:3,  defaultH:7,  category:"Trading",   component: OrderEntryWidget },
  { id:"orderbook", label:"Order Book",        icon:"📋", minW:3, minH:3, defaultW:6,  defaultH:5,  category:"Trading",   component: OrderBookWidget },
  { id:"executions",label:"Executions",        icon:"✅", minW:3, minH:3, defaultW:6,  defaultH:4,  category:"Trading",   component: ExecutionListWidget },
  { id:"movers",    label:"Top Movers",        icon:"🚀", minW:2, minH:3, defaultW:3,  defaultH:6,  category:"Market",    component: TopMoversWidget },
  { id:"heatmap",   label:"Market Map",        icon:"🗺", minW:3, minH:3, defaultW:6,  defaultH:5,  category:"Market",    component: MarketMapWidget },
  { id:"depth",     label:"Market Depth",      icon:"📊", minW:2, minH:4, defaultW:3,  defaultH:6,  category:"Market",    component: MarketDepthWidget },
  { id:"pressure",  label:"Buy/Sell Pressure", icon:"⚖️", minW:2, minH:3, defaultW:3,  defaultH:5,  category:"Market",    component: BuySellPressureWidget },
  { id:"portfolio", label:"Portfolio",         icon:"💼", minW:2, minH:3, defaultW:3,  defaultH:6,  category:"Portfolio", component: PortfolioWidget },
  { id:"chart",     label:"Price Chart",       icon:"📉", minW:3, minH:3, defaultW:6,  defaultH:5,  category:"Market",    component: PriceChartWidget },
  { id:"notif",     label:"Notifications",     icon:"🔔", minW:2, minH:3, defaultW:3,  defaultH:5,  category:"System",    component: NotificationsWidget },
  { id:"ai",        label:"AI Prediction",     icon:"🤖", minW:2, minH:4, defaultW:3,  defaultH:6,  category:"AI",        component: AIPredictionWidget },
  { id:"index",     label:"Index Summary",     icon:"🏦", minW:2, minH:3, defaultW:3,  defaultH:5,  category:"Market",    component: IndexSummaryWidget },
  { id:"news",      label:"News Feed",         icon:"📰", minW:2, minH:3, defaultW:3,  defaultH:5,  category:"News",      component: NewsFeedWidget },
  { id:"rms",       label:"RMS Limits",        icon:"🛡️", minW:2, minH:3, defaultW:3,  defaultH:5,  category:"Risk",      component: RMSLimitsWidget },
]

export const CATEGORIES = [...new Set(WIDGET_REGISTRY.map(w => w.category))]
