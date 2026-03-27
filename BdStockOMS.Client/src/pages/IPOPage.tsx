// @ts-nocheck
import { useState } from 'react';
const mono = '"JetBrains Mono",monospace';
const col = (v) => `var(${v})`;
const fmt = (n) => '৳' + Number(n).toLocaleString('en-BD',{minimumFractionDigits:2});
const IPO_DATA = [
  {id:1,company:'Dhaka Power Distribution Co.',symbol:'DPDC',sector:'Energy',lotSize:500,lotPrice:5000,deadline:'2026-04-15',open:true,minLots:1,maxLots:5,premium:20},
  {id:2,company:'Bangladesh Telecom Ltd.',symbol:'BTEL',sector:'Telecom',lotSize:500,lotPrice:5000,deadline:'2026-04-22',open:true,minLots:1,maxLots:10,premium:0},
  {id:3,company:'National Insurance Ltd.',symbol:'NINS',sector:'Insurance',lotSize:500,lotPrice:5000,deadline:'2026-05-01',open:true,minLots:1,maxLots:20,premium:0},
];
const MY_APPS = [
  {id:1,company:'Green Energy Corp.',symbol:'GENCO',lots:2,amount:4000,status:'Allotted',date:'2026-03-28',shares:200},
  {id:2,company:'Bangladesh Export Corp.',symbol:'BECL',lots:1,amount:5000,status:'Not Allotted',date:'2026-03-10',shares:0},
];
export default function IPOPage() {
  const [tab,setTab] = useState('open');
  const [applyId,setApplyId] = useState(null);
  const [lots,setLots] = useState(1);
  const [boNumber,setBo] = useState('');
  const ipo = IPO_DATA.find(i=>i.id===applyId);
  const inputStyle = {background:'var(--t-hover)',border:'1px solid var(--t-border)',borderRadius:6,padding:'7px 10px',color:'var(--t-text1)',fontSize:12,fontFamily:mono,outline:'none',width:'100%',boxSizing:'border-box'};
  return (
    <div style={{padding:24,color:'var(--t-text1)',minHeight:'100vh'}}>
      <div style={{marginBottom:20}}><div style={{fontSize:20,fontWeight:800,fontFamily:mono}}>IPO Applications</div><div style={{fontSize:12,color:'var(--t-text3)',marginTop:2}}>Apply for Initial Public Offerings — DSE/CSE</div></div>
      <div style={{display:'flex',gap:4,marginBottom:16,borderBottom:'1px solid var(--t-border)'}}>
        {[['open','Open IPOs'],['my','My Applications']].map(([id,label])=>(
          <button key={id} onClick={()=>setTab(id)} style={{padding:'8px 16px',border:'none',cursor:'pointer',fontSize:12,fontFamily:mono,fontWeight:600,background:'transparent',borderBottom:tab===id?'2px solid var(--t-accent)':'2px solid transparent',color:tab===id?'var(--t-accent)':'var(--t-text3)',marginBottom:-1}}>{label}</button>
        ))}
      </div>
      {tab==='open'&&<div style={{display:'grid',gridTemplateColumns:'repeat(auto-fill,minmax(300px,1fr))',gap:16}}>
        {IPO_DATA.filter(i=>i.open).map(i=>(
          <div key={i.id} style={{background:'var(--t-panel)',border:'1px solid var(--t-border)',borderRadius:12,padding:20}}>
            <div style={{display:'flex',justifyContent:'space-between',alignItems:'flex-start',marginBottom:12}}>
              <div><div style={{fontSize:14,fontWeight:800,fontFamily:mono}}>{i.symbol}</div><div style={{fontSize:11,color:'var(--t-text2)',marginTop:2}}>{i.company}</div><div style={{fontSize:10,color:'var(--t-text3)',marginTop:2}}>{i.sector}</div></div>
              {i.premium>0&&<span style={{padding:'3px 8px',borderRadius:99,fontSize:10,fontWeight:700,background:'rgba(255,184,0,0.15)',color:'#FFB800'}}>+{i.premium}%</span>}
            </div>
            <div style={{display:'grid',gridTemplateColumns:'1fr 1fr',gap:8,marginBottom:16}}>
              {[['Lot Size',i.lotSize+' shares'],['Lot Price',fmt(i.lotPrice)],['Min Lots',i.minLots],['Max Lots',i.maxLots]].map(([l,v])=>(
                <div key={l} style={{background:'var(--t-bg)',borderRadius:6,padding:'8px 10px'}}><div style={{fontSize:9,color:'var(--t-text3)',fontFamily:mono,textTransform:'uppercase',marginBottom:3}}>{l}</div><div style={{fontSize:12,fontWeight:700,fontFamily:mono}}>{v}</div></div>
              ))}
            </div>
            <div style={{fontSize:10,color:'var(--t-text3)',fontFamily:mono,marginBottom:12}}>Deadline: <span style={{color:'#FFB800',fontWeight:700}}>{i.deadline}</span></div>
            <button onClick={()=>{setApplyId(i.id);setLots(i.minLots);}} style={{width:'100%',padding:'9px',background:'var(--t-accent)',color:'#000',border:'none',borderRadius:7,fontWeight:700,fontSize:12,fontFamily:mono,cursor:'pointer'}}>Apply Now</button>
          </div>
        ))}
      </div>}
      {tab==='my'&&<div style={{border:'1px solid var(--t-border)',borderRadius:10,overflow:'hidden'}}>
        <table style={{width:'100%',borderCollapse:'collapse',fontSize:12,fontFamily:mono}}>
          <thead><tr style={{background:'var(--t-panel)',borderBottom:'1px solid var(--t-border)'}}>{['Symbol','Company','Lots','Amount','Status','Applied','Shares'].map(h=><th key={h} style={{padding:'10px 14px',textAlign:'left',fontSize:10,color:'var(--t-text3)',textTransform:'uppercase',fontWeight:600}}>{h}</th>)}</tr></thead>
          <tbody>{MY_APPS.map((a,i)=>(
            <tr key={a.id} style={{borderBottom:'1px solid var(--t-border)',background:i%2===0?'transparent':'rgba(255,255,255,0.01)'}}>
              <td style={{padding:'10px 14px',color:'var(--t-accent)',fontWeight:700}}>{a.symbol}</td>
              <td style={{padding:'10px 14px'}}>{a.company}</td>
              <td style={{padding:'10px 14px'}}>{a.lots}</td>
              <td style={{padding:'10px 14px',color:'#00D4AA',fontWeight:600}}>{fmt(a.amount)}</td>
              <td style={{padding:'10px 14px'}}><span style={{padding:'2px 8px',borderRadius:99,fontSize:10,fontWeight:700,background:a.status==='Allotted'?'rgba(0,212,170,0.15)':'rgba(255,107,107,0.15)',color:a.status==='Allotted'?'#00D4AA':'#FF6B6B'}}>{a.status}</span></td>
              <td style={{padding:'10px 14px',color:'var(--t-text3)',fontSize:10}}>{a.date}</td>
              <td style={{padding:'10px 14px',color:a.shares>0?'#00D4AA':'var(--t-text3)'}}>{a.shares>0?a.shares+' shares':'—'}</td>
            </tr>
          ))}</tbody>
        </table>
      </div>}
      {applyId&&ipo&&<><div onClick={()=>setApplyId(null)} style={{position:'fixed',inset:0,background:'rgba(0,0,0,0.6)',zIndex:999}}/><div style={{position:'fixed',top:'50%',left:'50%',transform:'translate(-50%,-50%)',width:420,background:'var(--t-surface)',border:'1px solid var(--t-border)',borderRadius:12,zIndex:1000,padding:24,boxShadow:'0 24px 48px rgba(0,0,0,0.6)'}}>
        <div style={{fontSize:14,fontWeight:800,fontFamily:mono,marginBottom:4}}>Apply for {ipo.symbol}</div>
        <div style={{fontSize:11,color:'var(--t-text3)',marginBottom:20}}>{ipo.company}</div>
        <div style={{display:'flex',flexDirection:'column',gap:14,marginBottom:20}}>
          <div><label style={{fontSize:10,color:'var(--t-text3)',fontFamily:mono,textTransform:'uppercase',display:'block',marginBottom:4}}>Lots (min:{ipo.minLots} max:{ipo.maxLots})</label><input type='number' min={ipo.minLots} max={ipo.maxLots} value={lots} onChange={e=>setLots(Math.max(ipo.minLots,Math.min(ipo.maxLots,Number(e.target.value))))} style={inputStyle}/></div>
          <div><label style={{fontSize:10,color:'var(--t-text3)',fontFamily:mono,textTransform:'uppercase',display:'block',marginBottom:4}}>BO Account</label><input value={boNumber} onChange={e=>setBo(e.target.value)} placeholder='e.g. BO1001000001' style={inputStyle}/></div>
          <div style={{background:'var(--t-panel)',borderRadius:8,padding:'12px 16px'}}>
            <div style={{display:'flex',justifyContent:'space-between',marginBottom:4}}><span style={{fontSize:11,color:'var(--t-text3)',fontFamily:mono}}>Total</span><span style={{fontSize:16,fontWeight:800,color:'var(--t-accent)',fontFamily:mono}}>{fmt(lots*ipo.lotPrice)}</span></div>
            <div style={{fontSize:10,color:'var(--t-text3)',fontFamily:mono}}>{lots} lots × {fmt(ipo.lotPrice)} = {lots*ipo.lotSize} shares</div>
          </div>
        </div>
        <div style={{display:'flex',gap:10,justifyContent:'flex-end'}}>
          <button onClick={()=>setApplyId(null)} style={{padding:'8px 16px',background:'var(--t-hover)',border:'1px solid var(--t-border)',borderRadius:7,color:'var(--t-text2)',fontSize:12,fontFamily:mono,cursor:'pointer'}}>Cancel</button>
          <button onClick={()=>setApplyId(null)} style={{padding:'8px 20px',background:'var(--t-accent)',color:'#000',border:'none',borderRadius:7,fontWeight:700,fontSize:12,fontFamily:mono,cursor:'pointer'}}>Submit</button>
        </div>
      </div></>}
    </div>
  );
}