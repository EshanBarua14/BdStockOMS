// @ts-nocheck
import { useEffect, useState } from "react";
import { getManagedBOAccounts, getBrokerages, updateBOAccount } from "@/api/client";

const mono = "\"JetBrains Mono\",monospace";
const col  = (v: string) => `var(${v})`;

const Badge = ({ active, label }: any) => (
  <span style={{ padding: "2px 8px", borderRadius: 99, fontSize: 10, fontWeight: 700, fontFamily: mono,
    background: active ? "rgba(0,212,170,0.15)" : "rgba(255,107,107,0.15)",
    color: active ? "#00D4AA" : "#FF6B6B", border: `1px solid ${active ? "#00D4AA40" : "#FF6B6B40"}` }}>
    {label ?? (active ? "ACTIVE" : "INACTIVE")}
  </span>
);

const Input = ({ label, value, onChange, type = "text" }: any) => (
  <div style={{ display: "flex", flexDirection: "column", gap: 4 }}>
    <label style={{ fontSize: 10, color: col("--t-text3"), fontFamily: mono, textTransform: "uppercase", letterSpacing: "0.05em" }}>{label}</label>
    <input type={type} value={value ?? ""} onChange={e => onChange(e.target.value)}
      style={{ background: col("--t-hover"), border: `1px solid ${col("--t-border")}`, borderRadius: 6,
        padding: "7px 10px", color: col("--t-text1"), fontSize: 12, fontFamily: mono, outline: "none", width: "100%" }}
      onFocus={e => e.currentTarget.style.borderColor = col("--t-accent")}
      onBlur={e => e.currentTarget.style.borderColor = col("--t-border")} />
  </div>
);

export default function BOManagementPage() {
  const [accounts,   setAccounts]   = useState<any[]>([]);
  const [brokerages, setBrokerages] = useState<any[]>([]);
  const [loading,    setLoading]    = useState(true);
  const [showForm,   setShowForm]   = useState(false);
  const [editing,    setEditing]    = useState<any>(null);
  const [form,       setForm]       = useState({ fullName: "", phone: "", isActive: true, isBOAccountActive: true, marginLimit: 0 });
  const [saving,     setSaving]     = useState(false);
  const [filterBrok, setFilterBrok] = useState(0);
  const [search,     setSearch]     = useState("");

  const load = () => {
    setLoading(true);
    Promise.all([getManagedBOAccounts(), getBrokerages()]).then(([a, b]: any) => {
      setAccounts(Array.isArray(a) ? a : []);
      setBrokerages(Array.isArray(b) ? b : []);
      setLoading(false);
    }).catch(() => setLoading(false));
  };

  useEffect(() => { load(); }, []);

  const set = (k: string) => (v: any) => setForm(f => ({ ...f, [k]: v }));

  const openEdit = (a: any) => {
    setEditing(a);
    setForm({ fullName: a.fullName, phone: "", isActive: a.isActive,
      isBOAccountActive: a.isBOAccountActive, marginLimit: a.marginLimit });
    setShowForm(true);
  };

  const save = async () => {
    setSaving(true);
    try {
      await updateBOAccount(editing.userId, form);
      setShowForm(false); load();
    } finally { setSaving(false); }
  };

  const filtered = accounts.filter(a =>
    (!filterBrok || a.brokerageHouseId === filterBrok) &&
    (a.fullName?.toLowerCase().includes(search.toLowerCase()) ||
     a.boNumber?.toLowerCase().includes(search.toLowerCase()))
  );

  const fmt = (n: number) => "৳" + n.toLocaleString("en-BD", { minimumFractionDigits: 2 });

  return (
    <div style={{ padding: 24, color: col("--t-text1"), minHeight: "100vh" }}>
      <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between", marginBottom: 20 }}>
        <div>
          <div style={{ fontSize: 20, fontWeight: 800, fontFamily: mono }}>BO Account Management</div>
          <div style={{ fontSize: 12, color: col("--t-text3"), marginTop: 2 }}>{accounts.length} BO accounts</div>
        </div>
      </div>

      {/* Summary cards */}
      <div style={{ display: "grid", gridTemplateColumns: "repeat(4,1fr)", gap: 12, marginBottom: 20 }}>
        {[
          { label: "Total Accounts",    value: accounts.length,                                          color: col("--t-accent") },
          { label: "Active Accounts",   value: accounts.filter(a => a.isBOAccountActive).length,        color: "#00D4AA" },
          { label: "Cash Accounts",     value: accounts.filter(a => a.accountType === "Cash").length,   color: col("--t-text1") },
          { label: "Margin Accounts",   value: accounts.filter(a => a.accountType === "Margin").length, color: "#FFB800" },
        ].map(c => (
          <div key={c.label} style={{ background: col("--t-panel"), border: `1px solid ${col("--t-border")}`,
            borderRadius: 10, padding: "14px 18px" }}>
            <div style={{ fontSize: 10, color: col("--t-text3"), fontFamily: mono, textTransform: "uppercase", marginBottom: 6 }}>{c.label}</div>
            <div style={{ fontSize: 24, fontWeight: 800, fontFamily: mono, color: c.color }}>{c.value}</div>
          </div>
        ))}
      </div>

      {/* Filters */}
      <div style={{ display: "flex", gap: 12, marginBottom: 16 }}>
        <input placeholder="Search by name or BO number..." value={search} onChange={e => setSearch(e.target.value)}
          style={{ width: 300, background: col("--t-hover"), border: `1px solid ${col("--t-border")}`,
            borderRadius: 7, padding: "8px 12px", color: col("--t-text1"), fontSize: 12, fontFamily: mono, outline: "none" }} />
        <select value={filterBrok} onChange={e => setFilterBrok(Number(e.target.value))}
          style={{ background: col("--t-hover"), border: `1px solid ${col("--t-border")}`, borderRadius: 7,
            padding: "8px 12px", color: col("--t-text1"), fontSize: 12, fontFamily: mono, outline: "none" }}>
          <option value={0}>All Brokerages</option>
          {brokerages.map((b: any) => <option key={b.id} value={b.id}>{b.name}</option>)}
        </select>
      </div>

      {/* Table */}
      {loading ? <div style={{ color: col("--t-text3"), fontFamily: mono, fontSize: 12 }}>Loading...</div> : (
        <div style={{ border: `1px solid ${col("--t-border")}`, borderRadius: 10, overflow: "hidden" }}>
          <table style={{ width: "100%", borderCollapse: "collapse", fontSize: 12, fontFamily: mono }}>
            <thead>
              <tr style={{ background: col("--t-panel"), borderBottom: `1px solid ${col("--t-border")}` }}>
                {["BO Number","Name","Brokerage","Type","Cash Balance","Margin","Available","BO Status","Actions"].map(h => (
                  <th key={h} style={{ padding: "10px 14px", textAlign: "left", fontSize: 10,
                    color: col("--t-text3"), textTransform: "uppercase", letterSpacing: "0.05em", fontWeight: 600 }}>{h}</th>
                ))}
              </tr>
            </thead>
            <tbody>
              {filtered.map((a, i) => (
                <tr key={a.userId} style={{ borderBottom: `1px solid ${col("--t-border")}`,
                  background: i % 2 === 0 ? "transparent" : "rgba(255,255,255,0.01)" }}>
                  <td style={{ padding: "10px 14px", color: col("--t-accent"), fontWeight: 700 }}>{a.boNumber}</td>
                  <td style={{ padding: "10px 14px", fontWeight: 600 }}>{a.fullName}</td>
                  <td style={{ padding: "10px 14px", color: col("--t-text2") }}>{a.brokerageHouseName}</td>
                  <td style={{ padding: "10px 14px" }}>
                    <span style={{ padding: "2px 8px", borderRadius: 99, fontSize: 10, fontWeight: 700,
                      background: a.accountType === "1" || a.accountType === "Margin" ? "rgba(255,184,0,0.15)" : "rgba(100,180,255,0.15)",
                      color: a.accountType === "1" || a.accountType === "Margin" ? "#FFB800" : "#64B4FF",
                      border: "1px solid currentColor" }}>
                      {a.accountType === "0" || a.accountType === "Cash" ? "CASH" : "MARGIN"}
                    </span>
                  </td>
                  <td style={{ padding: "10px 14px", color: "#00D4AA", fontWeight: 600 }}>{fmt(a.cashBalance)}</td>
                  <td style={{ padding: "10px 14px", color: col("--t-text2") }}>{fmt(a.marginLimit)}</td>
                  <td style={{ padding: "10px 14px", color: a.availableMargin > 0 ? "#00D4AA" : "#FF6B6B" }}>{fmt(a.availableMargin)}</td>
                  <td style={{ padding: "10px 14px" }}><Badge active={a.isBOAccountActive} /></td>
                  <td style={{ padding: "10px 14px" }}>
                    <button onClick={() => openEdit(a)} style={{ padding: "4px 10px", background: col("--t-hover"),
                      border: `1px solid ${col("--t-border")}`, borderRadius: 5, color: col("--t-text1"),
                      fontSize: 11, fontFamily: mono, cursor: "pointer" }}>Edit</button>
                  </td>
                </tr>
              ))}
              {filtered.length === 0 && (
                <tr><td colSpan={9} style={{ padding: 24, textAlign: "center", color: col("--t-text3") }}>No BO accounts found</td></tr>
              )}
            </tbody>
          </table>
        </div>
      )}

      {/* Edit Modal */}
      {showForm && editing && (
        <>
          <div onClick={() => setShowForm(false)} style={{ position: "fixed", inset: 0, background: "rgba(0,0,0,0.6)", zIndex: 999 }} />
          <div style={{ position: "fixed", top: "50%", left: "50%", transform: "translate(-50%,-50%)",
            width: 420, background: col("--t-surface"), border: `1px solid ${col("--t-border")}`,
            borderRadius: 12, zIndex: 1000, padding: 24, boxShadow: "0 24px 48px rgba(0,0,0,0.6)" }}>
            <div style={{ fontSize: 14, fontWeight: 800, fontFamily: mono, marginBottom: 4 }}>Edit BO Account</div>
            <div style={{ fontSize: 11, color: col("--t-text3"), fontFamily: mono, marginBottom: 20 }}>{editing.boNumber} — {editing.fullName}</div>
            <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: 14, marginBottom: 16 }}>
              <div style={{ gridColumn: "1/-1" }}>
                <Input label="Full Name" value={form.fullName} onChange={set("fullName")} />
              </div>
              <Input label="Margin Limit (৳)" value={form.marginLimit} onChange={(v: any) => set("marginLimit")(Number(v))} type="number" />
              <div style={{ display: "flex", flexDirection: "column", gap: 8 }}>
                <label style={{ fontSize: 10, color: col("--t-text3"), fontFamily: mono, textTransform: "uppercase" }}>Account Status</label>
                <div style={{ display: "flex", gap: 8 }}>
                  {[{ label: "Active", v: true }, { label: "Inactive", v: false }].map(opt => (
                    <button key={opt.label} onClick={() => set("isBOAccountActive")(opt.v)}
                      style={{ flex: 1, padding: "6px", borderRadius: 6, cursor: "pointer", fontSize: 11, fontFamily: mono, fontWeight: 600,
                        background: form.isBOAccountActive === opt.v ? (opt.v ? "rgba(0,212,170,0.2)" : "rgba(255,107,107,0.2)") : col("--t-hover"),
                        border: `1px solid ${form.isBOAccountActive === opt.v ? (opt.v ? "#00D4AA" : "#FF6B6B") : col("--t-border")}`,
                        color: form.isBOAccountActive === opt.v ? (opt.v ? "#00D4AA" : "#FF6B6B") : col("--t-text2") }}>
                      {opt.label}
                    </button>
                  ))}
                </div>
              </div>
            </div>
            <div style={{ display: "flex", gap: 10, justifyContent: "flex-end" }}>
              <button onClick={() => setShowForm(false)} style={{ padding: "8px 16px", background: col("--t-hover"),
                border: `1px solid ${col("--t-border")}`, borderRadius: 7, color: col("--t-text2"),
                fontSize: 12, fontFamily: mono, cursor: "pointer" }}>Cancel</button>
              <button onClick={save} disabled={saving} style={{ padding: "8px 20px", background: col("--t-accent"),
                color: "#000", border: "none", borderRadius: 7, fontWeight: 700,
                fontSize: 12, fontFamily: mono, cursor: "pointer", opacity: saving ? 0.7 : 1 }}>
                {saving ? "Saving..." : "Save Changes"}
              </button>
            </div>
          </div>
        </>
      )}
    </div>
  );
}
