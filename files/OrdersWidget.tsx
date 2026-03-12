// src/components/widgets/OrdersWidget.tsx
// Full-featured orders table: sortable, searchable, filterable columns, drag-reorder

import React from "react"
import { DataTable } from "../ui/DataTable"
import { useTableState, ColumnDef } from "../../hooks/useTableState"
import { ORDER_STATUS, ORDER_TYPE_LABEL, ORDER_CAT_LABEL } from "../../hooks/useOrders"
import type { Order } from "../../hooks/useOrders"

function StatusBadge({ status }: { status: number }) {
  const s = ORDER_STATUS[status] ?? { label: "Unknown", color: "text-zinc-500" }
  return <span className={`text-[10px] font-semibold ${s.color}`}>{s.label}</span>
}

function TypeBadge({ type }: { type: number }) {
  return (
    <span className={`text-[10px] font-bold px-1.5 py-0.5 rounded ${type === 0 ? "bg-emerald-500/15 text-emerald-400" : "bg-red-500/15 text-red-400"}`}>
      {ORDER_TYPE_LABEL[type] ?? "—"}
    </span>
  )
}

const ORDER_COLUMNS: ColumnDef<Order>[] = [
  {
    key: "createdAt", label: "Time", visible: true, width: 72, sortable: true,
    getValue: r => new Date(r.createdAt).getTime(),
    render: (r) => <span className="text-zinc-500 font-mono text-[10px]">{new Date(r.createdAt).toLocaleTimeString("en-GB", { hour: "2-digit", minute: "2-digit", second: "2-digit" })}</span>,
  },
  {
    key: "tradingCode", label: "Symbol", visible: true, width: 70, sortable: true,
    render: (r) => <span className="font-bold text-zinc-100 font-mono text-[11px]">{r.tradingCode || `#${r.stockId}`}</span>,
  },
  {
    key: "orderType", label: "Side", visible: true, width: 52, sortable: true,
    render: (r) => <TypeBadge type={r.orderType} />,
    getValue: r => r.orderType,
  },
  {
    key: "orderCategory", label: "Type", visible: true, width: 60, sortable: true,
    render: (r) => <span className="text-zinc-400 text-[10px]">{ORDER_CAT_LABEL[r.orderCategory]}</span>,
  },
  {
    key: "quantity", label: "Qty", visible: true, width: 60, sortable: true,
    render: (r) => <span className="font-mono text-[11px] text-zinc-300">{r.quantity.toLocaleString()}</span>,
  },
  {
    key: "filledQuantity", label: "Filled", visible: true, width: 60, sortable: true,
    render: (r) => <span className="font-mono text-[11px] text-zinc-400">{(r.filledQuantity ?? 0).toLocaleString()}</span>,
  },
  {
    key: "limitPrice", label: "Limit", visible: true, width: 70, sortable: true,
    render: (r) => r.limitPrice != null
      ? <span className="font-mono text-[11px] text-zinc-300">{r.limitPrice.toFixed(2)}</span>
      : <span className="text-zinc-600 text-[10px]">MKT</span>,
  },
  {
    key: "averagePrice", label: "Avg Price", visible: false, width: 75, sortable: true,
    render: (r) => r.averagePrice != null
      ? <span className="font-mono text-[11px] text-zinc-300">{r.averagePrice.toFixed(2)}</span>
      : <span className="text-zinc-700">—</span>,
  },
  {
    key: "status", label: "Status", visible: true, width: 72, sortable: true,
    render: (r) => <StatusBadge status={r.status} />,
  },
  {
    key: "id", label: "Order ID", visible: false, width: 65, sortable: true,
    render: (r) => <span className="font-mono text-[10px] text-zinc-600">#{r.id > 0 ? r.id : "…"}</span>,
  },
]

interface OrdersWidgetProps {
  ordersData: {
    orders: Order[]
    loading: boolean
    cancel: (id: number) => void
  }
}

export function OrdersWidget({ ordersData }: OrdersWidgetProps) {
  const { orders, loading, cancel } = ordersData

  const tableState = useTableState<Order>(
    "orders-widget",
    orders,
    ORDER_COLUMNS,
    50
  )

  if (loading && orders.length === 0) {
    return <div className="flex items-center justify-center h-full text-xs text-zinc-600">Loading orders…</div>
  }

  return (
    <DataTable
      tableState={tableState}
      rowKey={r => r.id}
      showSearch
      showColumnPicker
      showPagination
      stickyHeader
      compact
      emptyMessage="No orders yet"
      rowClassName={r => r.id < 0 ? "opacity-60" : ""}
      onRowClick={r => {
        if (r.status === 0 || r.status === 1) {
          if (confirm(`Cancel order #${r.id} for ${r.tradingCode}?`)) cancel(r.id)
        }
      }}
    />
  )
}

// ─── Executions Widget ────────────────────────────────────────────────────────

const EXEC_COLUMNS: ColumnDef<Order>[] = [
  {
    key: "createdAt", label: "Time", visible: true, width: 72, sortable: true,
    getValue: r => new Date(r.createdAt).getTime(),
    render: (r) => <span className="text-zinc-500 font-mono text-[10px]">{new Date(r.createdAt).toLocaleTimeString("en-GB", { hour: "2-digit", minute: "2-digit", second: "2-digit" })}</span>,
  },
  {
    key: "tradingCode", label: "Symbol", visible: true, width: 70, sortable: true,
    render: (r) => <span className="font-bold text-zinc-100 font-mono text-[11px]">{r.tradingCode || `#${r.stockId}`}</span>,
  },
  {
    key: "orderType", label: "Side", visible: true, width: 52, sortable: true,
    render: (r) => <TypeBadge type={r.orderType} />,
  },
  {
    key: "filledQuantity", label: "Filled", visible: true, width: 65, sortable: true,
    render: (r) => <span className="font-mono text-[11px] text-emerald-400">{(r.filledQuantity ?? r.quantity).toLocaleString()}</span>,
  },
  {
    key: "averagePrice", label: "Avg Price", visible: true, width: 80, sortable: true,
    render: (r) => <span className="font-mono text-[11px] text-zinc-200">{r.averagePrice?.toFixed(2) ?? "—"}</span>,
  },
  {
    key: "status", label: "Status", visible: true, width: 80, sortable: true,
    render: (r) => <StatusBadge status={r.status} />,
  },
  {
    key: "quantity", label: "Orig Qty", visible: false, width: 65, sortable: true,
    render: (r) => <span className="font-mono text-[11px] text-zinc-500">{r.quantity.toLocaleString()}</span>,
  },
]

interface ExecutionsWidgetProps {
  ordersData: { executions: Order[] }
}

export function ExecutionsWidget({ ordersData }: ExecutionsWidgetProps) {
  const tableState = useTableState<Order>("executions-widget", ordersData.executions, EXEC_COLUMNS, 50)
  return (
    <DataTable
      tableState={tableState}
      rowKey={r => r.id}
      showSearch
      showColumnPicker
      compact
      stickyHeader
      emptyMessage="No executions yet"
    />
  )
}
