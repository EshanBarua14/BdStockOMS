// @ts-nocheck
import { useEffect, useState } from "react";
import { getBranches, getBrokerages, createBranch, updateBranch, toggleBranch } from "@/api/client";

const mono = "\"JetBrains Mono\",monospace";
const col  = (v: string) => `var(${v})`;

const Badge = ({ active }: any) => (
  <span style={{ padding: "2px 8px", borderRadius: 99, fontSize: 10, fontWeight: 700, fontFamily: mono,
    background: active ? "rgba(0,212,170,0.15)" : "rgba(255,107,107,0.15)",
    color: active ? "#00D4AA" : "#FF6B6B", border: `1px solid ${active ? "#00D4AA40" : "#FF6B6B40"}` }}>
    {active ? "ACTIVE" : "INACTIVE"}
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

const EMPTY = { brokerageHouseId: 0, name: "", branchCode: "", address: "", phone: "", email: "", managerName: "" };

export default function BranchManagementPage() {
  const [branches,    setBranches]    = useState<any[]>([]);
  const [brokerages,  setBrokerages]  = useState<any[]>([]);
  const [loading,     setLoading]     = useState(true);
  const [showForm,    setShowForm]    = useState(false);
  const [editing,     setEditing]     = useState<any>(null);
  const [form,        setForm]        = useState(EMPTY);
  const [saving,      setSaving]      = useState(false);
  const [filterBrok,  setFilterBrok]  = useState(0);
  const [search,      setSearch]      = useState("");

  const load = () => {
    setLoading(true);
    Promise.all([getBranches(), getBrokerages()]).then(([b, br]: any) => {
      setBranches(Array.isArray(b) ? b : []);
      setBrokerages(Array.isArray(br) ? br : []);
      setLoading(false);
    }).catch(() => setLoading(false));
  };

  useEffect(() => { load(); }, []);

  const set = (k: string) => (v: any) => setForm(f => ({ ...f, [k]: v }));

  const openCreate = () => { setEditing(null); setForm(EMPTY); setShowForm(true); };
  const openEdit   = (b: any) => {
    setEditing(b);
    setForm({ brokerageHouseId: b.brokerageHouseId, name: b.name, branchCode: b.branchCode,
      address: b.address, phone: b.phone ?? "", email: b.email ?? "", managerName: b.managerName ?? "" });
    setShowForm(true);
  };

  const save = async () => {
    setSaving(true);
    try {
      if (editing) await updateBranch(editing.id, form);
      else         await createBranch(form);
      setShowForm(false); load();
    } finally { setSaving(false); }
  };

  const toggle = async (b: any) => { await toggleBranch(b.id, !b.isActive); load(); };

  const filtered = branches.filter(b =>
    (!filterBrok || b.brokerageHouseId === filterBrok) &&
    (b.name?.toLowerCase().includes(search.toLowerCase()) ||
     b.branchCode?.toLowerCase().includes(search.toLowerCase()))
  );

  return (
    <div style={{ padding: 24, color: col("--t-text1"), minHeight: "100vh" }}>
      <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between", marginBottom: 20 }}>
        <div>
          <div style={{ fontSize: 20, fontWeight: 800, fontFamily: mono }}>Branch Offices</div>
          <div style={{ fontSize: 12, color: col("--t-text3"), marginTop: 2 }}>{branches.length} branches across all brokerages</div>
        </div>
        <button onClick={openCreate} style={{ padding: "8px 16px", background: col("--t-accent"), color: "#000",
          border: "none", borderRadius: 7, fontWeight: 700, fontSize: 12, fontFamily: mono, cursor: "pointer" }}>
          + New Branch
        </button>
      </div>

      {/* Filters */}
      <div style={{ display: "flex", gap: 12, marginBottom: 16 }}>
        <input placeholder="Search branches..." value={search} onChange={e => setSearch(e.target.value)}
          style={{ width: 280, background: col("--t-hover"), border: `1px solid ${col("--t-border")}`,
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
                {["Code","Name","Brokerage","Manager","Phone","Status","Actions"].map(h => (
                  <th key={h} style={{ padding: "10px 14px", textAlign: "left", fontSize: 10,
                    color: col("--t-text3"), textTransform: "uppercase", letterSpacing: "0.05em", fontWeight: 600 }}>{h}</th>
                ))}
              </tr>
            </thead>
            <tbody>
              {filtered.map((b, i) => (
                <tr key={b.id} style={{ borderBottom: `1px solid ${col("--t-border")}`,
                  background: i % 2 === 0 ? "transparent" : "rgba(255,255,255,0.01)" }}>
                  <td style={{ padding: "10px 14px", color: col("--t-accent"), fontWeight: 700 }}>{b.branchCode}</td>
                  <td style={{ padding: "10px 14px", fontWeight: 600 }}>{b.name}</td>
                  <td style={{ padding: "10px 14px", color: col("--t-text2") }}>{b.brokerageHouseName}</td>
                  <td style={{ padding: "10px 14px", color: col("--t-text2") }}>{b.managerName ?? "—"}</td>
                  <td style={{ padding: "10px 14px", color: col("--t-text2") }}>{b.phone ?? "—"}</td>
                  <td style={{ padding: "10px 14px" }}><Badge active={b.isActive} /></td>
                  <td style={{ padding: "10px 14px" }}>
                    <div style={{ display: "flex", gap: 6 }}>
                      <button onClick={() => openEdit(b)} style={{ padding: "4px 10px", background: col("--t-hover"),
                        border: `1px solid ${col("--t-border")}`, borderRadius: 5, color: col("--t-text1"),
                        fontSize: 11, fontFamily: mono, cursor: "pointer" }}>Edit</button>
                      <button onClick={() => toggle(b)} style={{ padding: "4px 10px",
                        background: b.isActive ? "rgba(255,107,107,0.1)" : "rgba(0,212,170,0.1)",
                        border: `1px solid ${b.isActive ? "#FF6B6B40" : "#00D4AA40"}`, borderRadius: 5,
                        color: b.isActive ? "#FF6B6B" : "#00D4AA", fontSize: 11, fontFamily: mono, cursor: "pointer" }}>
                        {b.isActive ? "Deactivate" : "Activate"}
                      </button>
                    </div>
                  </td>
                </tr>
              ))}
              {filtered.length === 0 && (
                <tr><td colSpan={7} style={{ padding: 24, textAlign: "center", color: col("--t-text3") }}>No branches found</td></tr>
              )}
            </tbody>
          </table>
        </div>
      )}

      {/* Modal */}
      {showForm && (
        <>
          <div onClick={() => setShowForm(false)} style={{ position: "fixed", inset: 0, background: "rgba(0,0,0,0.6)", zIndex: 999 }} />
          <div style={{ position: "fixed", top: "50%", left: "50%", transform: "translate(-50%,-50%)",
            width: 520, background: col("--t-surface"), border: `1px solid ${col("--t-border")}`,
            borderRadius: 12, zIndex: 1000, padding: 24, boxShadow: "0 24px 48px rgba(0,0,0,0.6)" }}>
            <div style={{ fontSize: 14, fontWeight: 800, fontFamily: mono, marginBottom: 20 }}>
              {editing ? "Edit Branch" : "New Branch Office"}
            </div>
            <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: 14, marginBottom: 20 }}>
              <div style={{ gridColumn: "1/-1", display: "flex", flexDirection: "column", gap: 4 }}>
                <label style={{ fontSize: 10, color: col("--t-text3"), fontFamily: mono, textTransform: "uppercase" }}>Brokerage House</label>
                <select value={form.brokerageHouseId} onChange={e => set("brokerageHouseId")(Number(e.target.value))}
                  style={{ background: col("--t-hover"), border: `1px solid ${col("--t-border")}`, borderRadius: 6,
                    padding: "7px 10px", color: col("--t-text1"), fontSize: 12, fontFamily: mono, outline: "none" }}>
                  <option value={0}>Select brokerage...</option>
                  {brokerages.map((b: any) => <option key={b.id} value={b.id}>{b.name}</option>)}
                </select>
              </div>
              <Input label="Branch Name" value={form.name}       onChange={set("name")} />
              <Input label="Branch Code" value={form.branchCode} onChange={set("branchCode")} />
              <div style={{ gridColumn: "1/-1" }}>
                <Input label="Address"    value={form.address}    onChange={set("address")} />
              </div>
              <Input label="Phone"       value={form.phone}       onChange={set("phone")} />
              <Input label="Email"       value={form.email}       onChange={set("email")} type="email" />
              <div style={{ gridColumn: "1/-1" }}>
                <Input label="Manager Name" value={form.managerName} onChange={set("managerName")} />
              </div>
            </div>
            <div style={{ display: "flex", gap: 10, justifyContent: "flex-end" }}>
              <button onClick={() => setShowForm(false)} style={{ padding: "8px 16px", background: col("--t-hover"),
                border: `1px solid ${col("--t-border")}`, borderRadius: 7, color: col("--t-text2"),
                fontSize: 12, fontFamily: mono, cursor: "pointer" }}>Cancel</button>
              <button onClick={save} disabled={saving} style={{ padding: "8px 20px", background: col("--t-accent"),
                color: "#000", border: "none", borderRadius: 7, fontWeight: 700,
                fontSize: 12, fontFamily: mono, cursor: "pointer", opacity: saving ? 0.7 : 1 }}>
                {saving ? "Saving..." : "Save"}
              </button>
            </div>
          </div>
        </>
      )}
    </div>
  );
}
