// @ts-nocheck
import { useState } from 'react';
const mono = '"JetBrains Mono",monospace';
const col  = (v) => `var(${v})`;
const fmt  = (n) => '৳' + Number(n).toLocaleString('en-BD',{minimumFractionDigits:2});
const BONDS = [
  {id:1,name:'Bangladesh Govt 5Y Bond',code:'BGT5Y',tenor:'5 Year',coupon:8.5,faceValue:100000,maturityDate:'2031-03-01',yieldToMaturity:8.45,available:true,type:'Government'},
  {id:2,name:'Bangladesh Govt 10Y Bond',code:'BGT10Y',tenor:'10 Year',coupon:9.0,faceValue:100000,maturityDate:'2036-03-01',yieldToMaturity:8.95,available:true,type:'Government'},
  {id:3,name:'Treasury Bill 91D',code:'TB91D',tenor:'91 Days',coupon:7.5,faceValue:100000,maturityDate:'2026-06-15',yieldToMaturity:7.48,available:true,type:'T-Bill'},
  {id:4,name:'Treasury Bill 182D',code:'TB182D',tenor:'182 Days',coupon:7.8,faceValue:100000,maturityDate:'2026-09-15',yieldToMaturity:7.75,available:false,type:'T-Bill'},
];
const MY_HOLDINGS = [
  {id:1,code:'BGT5Y',name:'Bangladesh Govt 5Y Bond',units:5,faceValue:100000,coupon:8.5,maturity:'2031-03-01',currentValue:520000,pnl:20000},
  {id:2,code:'TB91D',name:'Treasury Bill 91D',units:2,faceValue:100000,coupon:7.5,maturity:'2026-06-15',currentValue:201500,pnl:1500},
];
export default function TBondPage() {
  const [tab,setTab]       = useState('market');
  const [buyId,setBuyId]   = useState(null);
  const [units,setUnits]   = useState(1);
  const [boNumber,setBoNumber] = useState('');
  const bond = BONDS.find(b=>b.id===buyId);
  const totalValue = MY_HOLDINGS.reduce((s,h)=>s+h.currentValue,0);
  const totalPnl   = MY_HOLDINGS.reduce((s,h)=>s+h.pnl,0);
  return (
    <div style={{padding:24,color:col('--t-text1'),minHeight:'100vh'}}>
      <div style={{marginBottom:20}}>
        <div style={{fontSize:20,fontWeight:800,fontFamily:mono}}>T-Bond / T-Bill Market</div>
        <div style={{fontSize:12,color:col('--t-text3'),marginTop:2}}>Government Securities — Bangladesh Bank Approved</div>
      </div>
      <div style={{display:'grid',gridTemplateColumns:'repeat(3,1fr)',gap:12,marginBottom:20}}>
        {[{label:'Portfolio Value',value:fmt(totalValue),color:col('--t-accent')},{label:'Total P&L',value:fmt(totalPnl),color:totalPnl>=0?'#00D4AA':'#FF6B6B'},{label:'Holdings',value:MY_HOLDINGS.length,color:col('--t-text1')}].map(c=>(
          <div key={c.label} style={{background:col('--t-panel'),border:`1px solid ${col('--t-border')}`,borderRadius:10,padding:'14px 18px'}}>
            <div style={{fontSize:10,color:col('--t-text3'),fontFamily:mono,textTransform:'uppercase',marginBottom:6}}>{c.label}</div>
            <div style={{fontSize:22,fontWeight:800,fontFamily:mono,color:c.color}}>{c.value}</div>
          </div>
        ))}
      </div>
      <div style={{display:'flex',gap:4,marginBottom:16,borderBottom:`1px solid ${col('--t-border')}`}}>
        {[['market','Market'],['holdings','My Holdings']].map(([id,label])=>(
          <button key={id} onClick={()=>setTab(id)} style={{padding:'8px 16px',border:'none',cursor:'pointer',fontSize:12,fontFamily:mono,fontWeight:600,background:'transparent',borderBottom:tab===id?`2px solid ${col('--t-accent')}`:'2px solid transparent',color:tab===id?col('--t-accent'):col('--t-text3'),marginBottom:-1}}>{label}</button>
        ))}
      </div>
      {tab==='market'&&(
        <div style={{border:`1px solid ${col('--t-border')}`,borderRadius:10,overflow:'hidden'}}>
          <table style={{width:'100%',borderCollapse:'collapse',fontSize:12,fontFamily:mono}}>
            <thead><tr style={{background:col('--t-panel'),borderBottom:`1px solid ${col('--t-border')}`}}>{['Code','Name','Type','Tenor','Coupon','YTM','Face Value','Maturity','Status',''].map(h=><th key={h} style={{padding:'10px 14px',textAlign:'left',fontSize:10,color:col('--t-text3'),textTransform:'uppercase',fontWeight:600}}>{h}</th>)}</tr></thead>
            <tbody>{BONDS.map((b,i)=>(
              <tr key={b.id} style={{borderBottom:`1px solid ${col('--t-border')}`,background:i%2===0?'transparent':'rgba(255,255,255,0.01)'}}>
                <td style={{padding:'10px 14px',color:col('--t-accent'),fontWeight:700}}>{b.code}</td>
                <td style={{padding:'10px 14px',fontWeight:600}}>{b.name}</td>
                <td style={{padding:'10px 14px'}}><span style={{padding:'2px 8px',borderRadius:99,fontSize:10,fontWeight:700,background:'rgba(100,180,255,0.15)',color:'#64B4FF'}}>{b.type}</span></td>
                <td style={{padding:'10px 14px',color:col('--t-text2')}}>{b.tenor}</td>
                <td style={{padding:'10px 14px',color:'#00D4AA',fontWeight:700}}>{b.coupon}%</td>
                <td style={{padding:'10px 14px',color:'#FFB800',fontWeight:700}}>{b.yieldToMaturity}%</td>
                <td style={{padding:'10px 14px'}}>{fmt(b.faceValue)}</td>
                <td style={{padding:'10px 14px',color:col('--t-text3'),fontSize:10}}>{b.maturityDate}</td>
                <td style={{padding:'10px 14px'}}><span style={{padding:'2px 8px',borderRadius:99,fontSize:10,fontWeight:700,background:b.available?'rgba(0,212,170,0.15)':'rgba(255,107,107,0.15)',color:b.available?'#00D4AA':'#FF6B6B'}}>{b.available?'OPEN':'CLOSED'}</span></td>
                <td style={{padding:'10px 14px'}}>{b.available&&<button onClick={()=>{setBuyId(b.id);setUnits(1);}} style={{padding:'4px 12px',background:col('--t-accent'),color:'#000',border:'none',borderRadius:5,fontSize:11,fontFamily:mono,fontWeight:700,cursor:'pointer'}}>Buy</button>}</td>
              </tr>
            ))}</tbody>
          </table>
        </div>
      )}
      {tab==='holdings'&&(
        <div style={{border:`1px solid ${col('--t-border')}`,borderRadius:10,overflow:'hidden'}}>
          <table style={{width:'100%',borderCollapse:'collapse',fontSize:12,fontFamily:mono}}>
            <thead><tr style={{background:col('--t-panel'),borderBottom:`1px solid ${col('--t-border')}`}}>{['Code','Name','Units','Face Value','Coupon','Maturity','Current Value','P&L'].map(h=><th key={h} style={{padding:'10px 14px',textAlign:'left',fontSize:10,color:col('--t-text3'),textTransform:'uppercase',fontWeight:600}}>{h}</th>)}</tr></thead>
            <tbody>{MY_HOLDINGS.map((h,i)=>(
              <tr key={h.id} style={{borderBottom:`1px solid ${col('--t-border')}`,background:i%2===0?'transparent':'rgba(255,255,255,0.01)'}}>
                <td style={{padding:'10px 14px',color:col('--t-accent'),fontWeight:700}}>{h.code}</td>
                <td style={{padding:'10px 14px',fontWeight:600}}>{h.name}</td>
                <td style={{padding:'10px 14px'}}>{h.units}</td>
                <td style={{padding:'10px 14px'}}>{fmt(h.faceValue)}</td>
                <td style={{padding:'10px 14px',color:'#00D4AA',fontWeight:700}}>{h.coupon}%</td>
                <td style={{padding:'10px 14px',color:col('--t-text3'),fontSize:10}}>{h.maturity}</td>
                <td style={{padding:'10px 14px',color:col('--t-accent'),fontWeight:600}}>{fmt(h.currentValue)}</td>
                <td style={{padding:'10px 14px',color:h.pnl>=0?'#00D4AA':'#FF6B6B',fontWeight:700}}>{h.pnl>=0?'+':''}{fmt(h.pnl)}</td>
              </tr>
            ))}</tbody>
          </table>
        </div>
      )}
      {buyId&&bond&&<><div onClick={()=>setBuyId(null)} style={{position:'fixed',inset:0,background:'rgba(0,0,0,0.6)',zIndex:999}}/><div style={{position:'fixed',top:'50%',left:'50%',transform:'translate(-50%,-50%)',width:420,background:col('--t-surface'),border:`1px solid ${col('--t-border')}`,borderRadius:12,zIndex:1000,padding:24,boxShadow:'0 24px 48px rgba(0,0,0,0.6)'}}>
        <div style={{fontSize:14,fontWeight:800,fontFamily:mono,marginBottom:4}}>Buy {bond.code}</div>
        <div style={{fontSize:11,color:col('--t-text3'),marginBottom:20}}>{bond.name} — {bond.coupon}% coupon</div>
        <div style={{display:'flex',flexDirection:'column',gap:14,marginBottom:20}}>
          <div><label style={{fontSize:10,color:col('--t-text3'),fontFamily:mono,textTransform:'uppercase',display:'block',marginBottom:4}}>Units</label><input type='number' min={1} value={units} onChange={e=>setUnits(Math.max(1,Number(e.target.value)))} style={{background:col('--t-hover'),border:`1px solid ${col('--t-border')}`,borderRadius:6,padding:'7px 10px',color:col('--t-text1'),fontSize:12,fontFamily:mono,outline:'none',width:'100%'}}/></div>
          <div><label style={{fontSize:10,color:col('--t-text3'),fontFamily:mono,textTransform:'uppercase',display:'block',marginBottom:4}}>BO Account</label><input value={boNumber} onChange={e=>setBoNumber(e.target.value)} placeholder='e.g. BO1001000001' style={{background:col('--t-hover'),border:`1px solid ${col('--t-border')}`,borderRadius:6,padding:'7px 10px',color:col('--t-text1'),fontSize:12,fontFamily:mono,outline:'none',width:'100%'}}/></div>
          <div style={{background:col('--t-panel'),borderRadius:8,padding:'12px 16px'}}>
            <div style={{display:'flex',justifyContent:'space-between',marginBottom:4}}><span style={{fontSize:11,color:col('--t-text3'),fontFamily:mono}}>Total Investment</span><span style={{fontSize:16,fontWeight:800,color:col('--t-accent'),fontFamily:mono}}>{fmt(units*bond.faceValue)}</span></div>
            <div style={{display:'flex',justifyContent:'space-between'}}><span style={{fontSize:10,color:col('--t-text3'),fontFamily:mono}}>Annual Interest</span><span style={{fontSize:12,color:'#00D4AA',fontWeight:700,fontFamily:mono}}>{fmt(units*bond.faceValue*bond.coupon/100)}</span></div>
          </div>
        </div>
        <div style={{display:'flex',gap:10,justifyContent:'flex-end'}}>
          <button onClick={()=>setBuyId(null)} style={{padding:'8px 16px',background:col('--t-hover'),border:`1px solid ${col('--t-border')}`,borderRadius:7,color:col('--t-text2'),fontSize:12,fontFamily:mono,cursor:'pointer'}}>Cancel</button>
          <button onClick={()=>setBuyId(null)} style={{padding:'8px 20px',background:col('--t-accent'),color:'#000',border:'none',borderRadius:7,fontWeight:700,fontSize:12,fontFamily:mono,cursor:'pointer'}}>Confirm</button>
        </div>
      </div></>}
    </div>
  );
}