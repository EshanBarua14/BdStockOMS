// @ts-nocheck
import { useEffect, useState } from 'react';
import { getFundRequests, createFundRequest, approveFundTrader, approveFundCCD, rejectFundRequest, completeFundRequest, getManagedBOAccounts } from '@/api/client';
const mono = '"JetBrains Mono",monospace';
const col = (v) => `var(${v})`;
const fmt = (n) => '৳' + Number(n).toLocaleString('en-BD',{minimumFractionDigits:2});
const STATUS_COLORS = {Pending:'#FFB800',ApprovedByTrader:'#64B4FF',ApprovedByCCD:'#9B59B6',Rejected:'#FF6B6B',Completed:'#00D4AA'};
const PAYMENT_METHODS = ['Cash','Cheque','BEFTN','RTGS','bKash','Nagad','Rocket','SellShares'];
const SBadge = ({s}) => { const c=STATUS_COLORS[s]||'var(--t-text3)'; return <span style={{padding:'2px 8px',borderRadius:99,fontSize:10,fontWeight:700,fontFamily:mono,background:c+'20',color:c}}>{s}</span>; };
export default function AccountsPage() {
  const [tab,setTab] = useState('requests');
  const [requests,setRequests] = useState([]);
  const [boAccounts,setBo] = useState([]);
  const [loading,setLoading] = useState(true);
  const [filterStatus,setFS] = useState('');
  const [showNew,setShowNew] = useState(false);
  const [form,setForm] = useState({amount:'',paymentMethod:'Cash',referenceNumber:'',notes:''});
  const [saving,setSaving] = useState(false);
  const [rejectId,setRejectId] = useState(null);
  const [rejectReason,setRR] = useState('');
  const load = () => { setLoading(true); Promise.all([getFundRequests(1,50,filterStatus),getManagedBOAccounts()]).then(([r,bo])=>{ setRequests(Array.isArray(r?.items)?r.items:Array.isArray(r)?r:[]); setBo(Array.isArray(bo)?bo:[]); setLoading(false); }).catch(()=>setLoading(false)); };
  useEffect(()=>{load();},[filterStatus]);
  const setF=(k)=>(v)=>setForm(f=>({...f,[k]:v}));
  const submitNew = async () => { setSaving(true); try { await createFundRequest({amount:Number(form.amount),paymentMethod:form.paymentMethod,referenceNumber:form.referenceNumber||null,notes:form.notes||null}); setShowNew(false); load(); } finally { setSaving(false); } };
  const approve = async (id,role) => { if(role==='trader') await approveFundTrader(id,{}); else if(role==='ccd') await approveFundCCD(id); else await completeFundRequest(id); load(); };
  const doReject = async () => { if(!rejectId||!rejectReason.trim()) return; await rejectFundRequest(rejectId,{reason:rejectReason}); setRejectId(null); setRR(''); load(); };
  const pending=requests.filter(r=>r.status==='Pending'||r.status===0).length;
  const completed=requests.filter(r=>r.status==='Completed'||r.status===4).length;
  const totalAmt=requests.reduce((s,r)=>s+Number(r.amount||0),0);
  const inputStyle={background:col('--t-hover'),border:`1px solid ${col('--t-border')}`,borderRadius:6,padding:'7px 10px',color:col('--t-text1'),fontSize:12,fontFamily:mono,outline:'none',width:'100%',boxSizing:'border-box'};
  return (
    <div style={{padding:24,color:col('--t-text1'),minHeight:'100vh'}}>
      <div style={{display:'flex',alignItems:'center',justifyContent:'space-between',marginBottom:20}}>
        <div><div style={{fontSize:20,fontWeight:800,fontFamily:mono}}>Accounts & Fund Management</div><div style={{fontSize:12,color:col('--t-text3'),marginTop:2}}>Deposit · Withdrawal · Fund Request Workflow</div></div>
        <button onClick={()=>setShowNew(true)} style={{padding:'8px 16px',background:col('--t-accent'),color:'#000',border:'none',borderRadius:7,fontWeight:700,fontSize:12,fontFamily:mono,cursor:'pointer'}}>+ New Fund Request</button>
      </div>
      <div style={{display:'grid',gridTemplateColumns:'repeat(4,1fr)',gap:12,marginBottom:20}}>
        {[{l:'Total Requests',v:requests.length,c:col('--t-accent')},{l:'Pending',v:pending,c:'#FFB800'},{l:'Completed',v:completed,c:'#00D4AA'},{l:'Total Amount',v:fmt(totalAmt),c:col('--t-text1')}].map(x=>(
          <div key={x.l} style={{background:col('--t-panel'),border:`1px solid ${col('--t-border')}`,borderRadius:10,padding:'14px 18px'}}>
            <div style={{fontSize:10,color:col('--t-text3'),fontFamily:mono,textTransform:'uppercase',marginBottom:6}}>{x.l}</div>
            <div style={{fontSize:22,fontWeight:800,fontFamily:mono,color:x.c}}>{x.v}</div>
          </div>
        ))}
      </div>
      <div style={{display:'flex',gap:4,marginBottom:16,borderBottom:`1px solid ${col('--t-border')}`}}>
        {[['requests','Fund Requests'],['bo','BO Accounts']].map(([id,label])=>(
          <button key={id} onClick={()=>setTab(id)} style={{padding:'8px 16px',border:'none',cursor:'pointer',fontSize:12,fontFamily:mono,fontWeight:600,background:'transparent',borderBottom:tab===id?`2px solid ${col('--t-accent')}`:'2px solid transparent',color:tab===id?col('--t-accent'):col('--t-text3'),marginBottom:-1}}>{label}</button>
        ))}
      </div>
      {tab==='requests'&&<div style={{marginBottom:16}}><select value={filterStatus} onChange={e=>setFS(e.target.value)} style={{background:col('--t-hover'),border:`1px solid ${col('--t-border')}`,borderRadius:7,padding:'8px 12px',color:col('--t-text1'),fontSize:12,fontFamily:mono,outline:'none'}}><option value=''>All Statuses</option>{['Pending','ApprovedByTrader','ApprovedByCCD','Rejected','Completed'].map(s=><option key={s} value={s}>{s}</option>)}</select></div>}
      {tab==='requests'&&(loading?<div style={{color:col('--t-text3'),fontFamily:mono}}>Loading...</div>:(
        <div style={{border:`1px solid ${col('--t-border')}`,borderRadius:10,overflow:'hidden'}}>
          <table style={{width:'100%',borderCollapse:'collapse',fontSize:12,fontFamily:mono}}>
            <thead><tr style={{background:col('--t-panel'),borderBottom:`1px solid ${col('--t-border')}`}}>{['ID','Investor','Amount','Method','Ref','Status','Date','Actions'].map(h=><th key={h} style={{padding:'10px 14px',textAlign:'left',fontSize:10,color:col('--t-text3'),textTransform:'uppercase',fontWeight:600}}>{h}</th>)}</tr></thead>
            <tbody>
              {requests.map((r,i)=>(
                <tr key={r.id||i} style={{borderBottom:`1px solid ${col('--t-border')}`,background:i%2===0?'transparent':'rgba(255,255,255,0.01)'}}>
                  <td style={{padding:'10px 14px',color:col('--t-text3')}}>{r.id}</td>
                  <td style={{padding:'10px 14px',fontWeight:600}}>{r.investorName||r.investorId||'—'}</td>
                  <td style={{padding:'10px 14px',color:'#00D4AA',fontWeight:700}}>{fmt(r.amount||0)}</td>
                  <td style={{padding:'10px 14px',color:col('--t-text2')}}>{r.paymentMethod}</td>
                  <td style={{padding:'10px 14px',color:col('--t-text3')}}>{r.referenceNumber||'—'}</td>
                  <td style={{padding:'10px 14px'}}><SBadge s={r.status}/></td>
                  <td style={{padding:'10px 14px',color:col('--t-text3'),fontSize:10}}>{r.createdAt?new Date(r.createdAt).toLocaleDateString('en-BD'):'—'}</td>
                  <td style={{padding:'10px 14px'}}><div style={{display:'flex',gap:4}}>
                    {(r.status==='Pending'||r.status===0)&&<button onClick={()=>approve(r.id,'trader')} style={{padding:'3px 8px',fontSize:10,fontFamily:mono,background:'rgba(100,180,255,0.15)',border:'1px solid rgba(100,180,255,0.3)',borderRadius:4,color:'#64B4FF',cursor:'pointer'}}>✓ Trader</button>}
                    {(r.status==='ApprovedByTrader'||r.status===1)&&<button onClick={()=>approve(r.id,'ccd')} style={{padding:'3px 8px',fontSize:10,fontFamily:mono,background:'rgba(155,89,182,0.15)',border:'1px solid rgba(155,89,182,0.3)',borderRadius:4,color:'#9B59B6',cursor:'pointer'}}>✓ CCD</button>}
                    {(r.status==='ApprovedByCCD'||r.status===2)&&<button onClick={()=>approve(r.id,'complete')} style={{padding:'3px 8px',fontSize:10,fontFamily:mono,background:'rgba(0,212,170,0.15)',border:'1px solid rgba(0,212,170,0.3)',borderRadius:4,color:'#00D4AA',cursor:'pointer'}}>Complete</button>}
                    {(r.status==='Pending'||r.status==='ApprovedByTrader'||r.status===0||r.status===1)&&<button onClick={()=>{setRejectId(r.id);setRR('');}} style={{padding:'3px 8px',fontSize:10,fontFamily:mono,background:'rgba(255,107,107,0.15)',border:'1px solid rgba(255,107,107,0.3)',borderRadius:4,color:'#FF6B6B',cursor:'pointer'}}>Reject</button>}
                  </div></td>
                </tr>
              ))}
              {requests.length===0&&<tr><td colSpan={8} style={{padding:24,textAlign:'center',color:col('--t-text3')}}>No fund requests found.</td></tr>}
            </tbody>
          </table>
        </div>
      ))}
      {tab==='bo'&&(loading?<div style={{color:col('--t-text3'),fontFamily:mono}}>Loading...</div>:(
        <div style={{border:`1px solid ${col('--t-border')}`,borderRadius:10,overflow:'hidden'}}>
          <table style={{width:'100%',borderCollapse:'collapse',fontSize:12,fontFamily:mono}}>
            <thead><tr style={{background:col('--t-panel'),borderBottom:`1px solid ${col('--t-border')}`}}>{['BO Number','Name','Type','Cash Balance','Margin Limit','Available','Status'].map(h=><th key={h} style={{padding:'10px 14px',textAlign:'left',fontSize:10,color:col('--t-text3'),textTransform:'uppercase',fontWeight:600}}>{h}</th>)}</tr></thead>
            <tbody>
              {boAccounts.map((a,i)=>(
                <tr key={a.userId||i} style={{borderBottom:`1px solid ${col('--t-border')}`,background:i%2===0?'transparent':'rgba(255,255,255,0.01)'}}>
                  <td style={{padding:'10px 14px',color:col('--t-accent'),fontWeight:700}}>{a.boNumber}</td>
                  <td style={{padding:'10px 14px',fontWeight:600}}>{a.fullName}</td>
                  <td style={{padding:'10px 14px'}}><span style={{padding:'2px 8px',borderRadius:99,fontSize:10,fontWeight:700,background:'rgba(100,180,255,0.15)',color:'#64B4FF'}}>{a.accountType==='0'||a.accountType==='Cash'?'CASH':'MARGIN'}</span></td>
                  <td style={{padding:'10px 14px',color:'#00D4AA',fontWeight:600}}>{fmt(a.cashBalance)}</td>
                  <td style={{padding:'10px 14px'}}>{fmt(a.marginLimit)}</td>
                  <td style={{padding:'10px 14px',color:a.availableMargin>0?'#00D4AA':'#FF6B6B'}}>{fmt(a.availableMargin)}</td>
                  <td style={{padding:'10px 14px'}}><span style={{padding:'2px 8px',borderRadius:99,fontSize:10,fontWeight:700,background:a.isBOAccountActive?'rgba(0,212,170,0.15)':'rgba(255,107,107,0.15)',color:a.isBOAccountActive?'#00D4AA':'#FF6B6B'}}>{a.isBOAccountActive?'ACTIVE':'INACTIVE'}</span></td>
                </tr>
              ))}
              {boAccounts.length===0&&<tr><td colSpan={7} style={{padding:24,textAlign:'center',color:col('--t-text3')}}>No BO accounts found.</td></tr>}
            </tbody>
          </table>
        </div>
      ))}
      {showNew&&<><div onClick={()=>setShowNew(false)} style={{position:'fixed',inset:0,background:'rgba(0,0,0,0.6)',zIndex:999}}/><div style={{position:'fixed',top:'50%',left:'50%',transform:'translate(-50%,-50%)',width:460,background:col('--t-surface'),border:`1px solid ${col('--t-border')}`,borderRadius:12,zIndex:1000,padding:24,boxShadow:'0 24px 48px rgba(0,0,0,0.6)'}}>
        <div style={{fontSize:14,fontWeight:800,fontFamily:mono,marginBottom:20}}>New Fund Request</div>
        <div style={{display:'grid',gridTemplateColumns:'1fr 1fr',gap:14,marginBottom:20}}>
          <div style={{gridColumn:'1/-1'}}><label style={{fontSize:10,color:col('--t-text3'),fontFamily:mono,textTransform:'uppercase',display:'block',marginBottom:4}}>Amount (৳)</label><input type='number' value={form.amount} onChange={e=>setF('amount')(e.target.value)} placeholder='e.g. 50000' style={inputStyle}/></div>
          <div><label style={{fontSize:10,color:col('--t-text3'),fontFamily:mono,textTransform:'uppercase',display:'block',marginBottom:4}}>Payment Method</label><select value={form.paymentMethod} onChange={e=>setF('paymentMethod')(e.target.value)} style={inputStyle}>{PAYMENT_METHODS.map(m=><option key={m} value={m}>{m}</option>)}</select></div>
          <div><label style={{fontSize:10,color:col('--t-text3'),fontFamily:mono,textTransform:'uppercase',display:'block',marginBottom:4}}>Reference No.</label><input value={form.referenceNumber} onChange={e=>setF('referenceNumber')(e.target.value)} placeholder='Cheque/Txn no.' style={inputStyle}/></div>
          <div style={{gridColumn:'1/-1'}}><label style={{fontSize:10,color:col('--t-text3'),fontFamily:mono,textTransform:'uppercase',display:'block',marginBottom:4}}>Notes</label><input value={form.notes} onChange={e=>setF('notes')(e.target.value)} placeholder='Optional...' style={inputStyle}/></div>
        </div>
        <div style={{display:'flex',gap:10,justifyContent:'flex-end'}}>
          <button onClick={()=>setShowNew(false)} style={{padding:'8px 16px',background:col('--t-hover'),border:`1px solid ${col('--t-border')}`,borderRadius:7,color:col('--t-text2'),fontSize:12,fontFamily:mono,cursor:'pointer'}}>Cancel</button>
          <button onClick={submitNew} disabled={saving} style={{padding:'8px 20px',background:col('--t-accent'),color:'#000',border:'none',borderRadius:7,fontWeight:700,fontSize:12,fontFamily:mono,cursor:'pointer',opacity:saving?0.7:1}}>{saving?'Submitting...':'Submit'}</button>
        </div>
      </div></>}
      {rejectId&&<><div onClick={()=>setRejectId(null)} style={{position:'fixed',inset:0,background:'rgba(0,0,0,0.6)',zIndex:999}}/><div style={{position:'fixed',top:'50%',left:'50%',transform:'translate(-50%,-50%)',width:380,background:col('--t-surface'),border:`1px solid ${col('--t-border')}`,borderRadius:12,zIndex:1000,padding:24,boxShadow:'0 24px 48px rgba(0,0,0,0.6)'}}>
        <div style={{fontSize:14,fontWeight:800,fontFamily:mono,marginBottom:16}}>Reject Request #{rejectId}</div>
        <div style={{marginBottom:20}}><label style={{fontSize:10,color:col('--t-text3'),fontFamily:mono,textTransform:'uppercase',display:'block',marginBottom:4}}>Reason *</label><input value={rejectReason} onChange={e=>setRR(e.target.value)} placeholder='Enter reason...' style={inputStyle}/></div>
        <div style={{display:'flex',gap:10,justifyContent:'flex-end'}}>
          <button onClick={()=>setRejectId(null)} style={{padding:'8px 16px',background:col('--t-hover'),border:`1px solid ${col('--t-border')}`,borderRadius:7,color:col('--t-text2'),fontSize:12,fontFamily:mono,cursor:'pointer'}}>Cancel</button>
          <button onClick={doReject} style={{padding:'8px 20px',background:'rgba(255,107,107,0.2)',color:'#FF6B6B',border:'1px solid rgba(255,107,107,0.3)',borderRadius:7,fontWeight:700,fontSize:12,fontFamily:mono,cursor:'pointer'}}>Reject</button>
        </div>
      </div></>}
    </div>
  );
}