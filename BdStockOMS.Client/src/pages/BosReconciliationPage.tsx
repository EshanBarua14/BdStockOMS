// @ts-nocheck
// BosReconciliationPage.tsx - Day 74
import { useState, useEffect, useCallback } from 'react'
import { bosGetSessions, bosUploadClients, bosUploadPositions, bosExportPositions, bosGetCompliance, bosRefreshCompliance } from '@/api/client'
import { useAuthStore } from '@/store/authStore'

const mono = "'JetBrains Mono', monospace"

function Pill({ v }: { v: string }) {
  const cfg: Record<string,string> = {
    Reconciled:'bg-emerald-500/15 text-emerald-400 border-emerald-500/30',
    Verified:'bg-sky-500/15 text-sky-400 border-sky-500/30',
    Pending:'bg-amber-500/15 text-amber-400 border-amber-500/30',
    Failed:'bg-red-500/15 text-red-400 border-red-500/40',
    Passed:'bg-emerald-500/15 text-emerald-400 border-emerald-500/30',
    Critical:'bg-red-500/15 text-red-400 border-red-500/40',
    Warning:'bg-amber-500/15 text-amber-400 border-amber-500/30',
  }
  const cls = cfg[v] ?? 'bg-zinc-500/15 text-zinc-400 border-zinc-500/30'
  return <span className={'inline-flex items-center px-2 py-0.5 rounded-full text-[10px] font-semibold border ' + cls}>{v}</span>
}

function Md2Badge({ ok }: { ok: boolean }) {
  return ok
    ? <span className="text-[10px] font-semibold text-emerald-400">MD5 OK</span>
    : <span className="text-[10px] font-semibold text-red-400">MD5 FAIL</span>
}

function Tabs({ active, onChange }: { active: string; onChange: (t:string)=>void }) {
  return (
    <div style={{display:'flex',gap:2,padding:'6px 16px',borderBottom:'1px solid var(--t-border)',background:'var(--t-panel)',flexShrink:0}}>
      {['Upload','Sessions','Compliance','Export'].map(t => (
        <button key={t} onClick={()=>onChange(t)} style={{
          padding:'6px 16px',borderRadius:6,fontSize:12,fontFamily:mono,cursor:'pointer',
          background:active===t?'var(--t-hover)':'none',
          border:active===t?'1px solid var(--t-border)':'1px solid transparent',
          color:active===t?'var(--t-text1)':'var(--t-text3)',
          fontWeight:active===t?700:400,
        }}>{t}</button>
      ))}
    </div>
  )
}

function UploadTab({ brokerageHouseId, userId }: { brokerageHouseId:number; userId:number }) {
  const [mode,setMode]        = useState<'clients'|'positions'>('clients')
  const [xmlContent,setXml]   = useState('')
  const [xmlName,setXmlName]  = useState('')
  const [ctrlContent,setCtrl] = useState('')
  const [ctrlName,setCtrlName]= useState('')
  const [busy,setBusy]        = useState(false)
  const [result,setResult]    = useState<any>(null)
  const [error,setError]      = useState('')

  const readFile = (f:File):Promise<string> => new Promise((res,rej)=>{ const r=new FileReader(); r.onload=e=>res(e.target?.result as string); r.onerror=rej; r.readAsText(f) })
  const onXml  = async (e:any) => { const f=e.target.files?.[0]; if(f){setXmlName(f.name);setXml(await readFile(f))} }
  const onCtrl = async (e:any) => { const f=e.target.files?.[0]; if(f){setCtrlName(f.name);setCtrl(await readFile(f))} }

  const submit = async () => {
    if(!xmlContent){setError('XML file required');return}
    setBusy(true);setError('');setResult(null)
    try {
      const dto={brokerageHouseId,uploadedByUserId:userId,xmlFileName:xmlName,xmlContent,ctrlFileName:ctrlName,ctrlContent}
      setResult(mode==='clients' ? await bosUploadClients(dto) : await bosUploadPositions(dto))
    } catch(e:any){setError(e.message)}
    finally{setBusy(false)}
  }

  const inp={width:'100%',background:'var(--t-hover)',border:'1px solid var(--t-border)',borderRadius:6,padding:'8px 12px',color:'var(--t-text1)',fontSize:12,fontFamily:mono,outline:'none'}

  return (
    <div style={{padding:20,maxWidth:680,display:'flex',flexDirection:'column',gap:16}}>
      <div style={{display:'flex',gap:8}}>
        {(['clients','positions'] as const).map(m=>(
          <button key={m} onClick={()=>{setMode(m);setResult(null);setError('')}} style={{
            padding:'6px 18px',borderRadius:6,fontSize:12,fontFamily:mono,cursor:'pointer',
            background:mode===m?'rgba(0,212,170,0.12)':'var(--t-hover)',
            border:'1px solid '+(mode===m?'var(--t-accent)':'var(--t-border)'),
            color:mode===m?'var(--t-accent)':'var(--t-text2)',fontWeight:mode===m?700:400,
          }}>{m==='clients'?'Clients (UBR)':'Positions (UBR)'}</button>
        ))}
      </div>
      <div style={{display:'flex',flexDirection:'column',gap:6}}>
        <label style={{fontSize:11,color:'var(--t-text3)',fontFamily:mono}}>XML FILE *</label>
        <input type='file' accept='.xml' onChange={onXml} style={inp as any} />
        {xmlName && <span style={{fontSize:10,color:'var(--t-text3)',fontFamily:mono}}>{xmlName} - {xmlContent.length.toLocaleString()} chars</span>}
      </div>
      <div style={{display:'flex',flexDirection:'column',gap:6}}>
        <label style={{fontSize:11,color:'var(--t-text3)',fontFamily:mono}}>CTRL FILE (optional - MD5 verification)</label>
        <input type='file' accept='.ctrl,.xml,.txt' onChange={onCtrl} style={inp as any} />
        {ctrlName && <span style={{fontSize:10,color:'var(--t-text3)',fontFamily:mono}}>{ctrlName}</span>}
      </div>
      <button onClick={submit} disabled={busy} style={{
        padding:'10px 24px',borderRadius:6,fontSize:13,fontFamily:mono,cursor:busy?'wait':'pointer',
        background:busy?'var(--t-hover)':'rgba(0,212,170,0.15)',
        border:'1px solid '+(busy?'var(--t-border)':'var(--t-accent)'),
        color:busy?'var(--t-text3)':'var(--t-accent)',fontWeight:700,
      }}>{busy?'Processing...':'Run Reconciliation'}</button>
      {error && <div style={{background:'rgba(255,107,107,0.08)',border:'1px solid rgba(255,107,107,0.25)',borderRadius:6,padding:'10px 14px',fontSize:12,color:'var(--t-sell)',fontFamily:mono}}>{error}</div>}
      {result && (
        <div style={{background:'var(--t-panel)',border:'1px solid var(--t-border)',borderRadius:8,padding:16,display:'flex',flexDirection:'column',gap:12}}>
          <div style={{display:'flex',alignItems:'center',gap:12}}><Pill v={result.status}/><Md2Badge ok={result.md5Verified}/></div>
          <div style={{display:'grid',gridTemplateColumns:'1fr 1fr 1fr',gap:12}}>
            {[['Total',result.totalRecords],['Reconciled',result.reconciledRecords],['Unmatched',result.unmatchedRecords]].map(([l,v])=>(
              <div key={l as string} style={{background:'var(--t-hover)',borderRadius:6,padding:'10px 14px'}}>
                <div style={{fontSize:10,color:'var(--t-text3)',fontFamily:mono,marginBottom:4}}>{l}</div>
                <div style={{fontSize:20,fontWeight:700,color:'var(--t-text1)',fontFamily:mono}}>{v}</div>
              </div>
            ))}
          </div>
          {result.unmatchedItems?.length > 0 && (
            <div>
              <div style={{fontSize:11,color:'var(--t-text3)',fontFamily:mono,marginBottom:8}}>UNMATCHED BO NUMBERS</div>
              <div style={{display:'flex',flexWrap:'wrap',gap:6}}>
                {result.unmatchedItems.map((bo:string)=>(
                  <span key={bo} style={{background:'rgba(255,107,107,0.08)',border:'1px solid rgba(255,107,107,0.2)',borderRadius:4,padding:'2px 8px',fontSize:10,color:'var(--t-sell)',fontFamily:mono}}>{bo}</span>
                ))}
              </div>
            </div>
          )}
          {result.errorMessage && <div style={{fontSize:11,color:'var(--t-sell)',fontFamily:mono}}>{result.errorMessage}</div>}
        </div>
      )}
    </div>
  )
}

function SessionsTab({ brokerageHouseId }: { brokerageHouseId:number }) {
  const [sessions,setSessions] = useState<any[]>([])
  const [loading,setLoading]   = useState(true)
  const [error,setError]       = useState('')
  const load = useCallback(async()=>{
    setLoading(true);setError('')
    try{setSessions(await bosGetSessions(brokerageHouseId))}
    catch(e:any){setError(e.message)}
    finally{setLoading(false)}
  },[brokerageHouseId])
  useEffect(()=>{load()},[load])
  return (
    <div style={{display:'flex',flexDirection:'column',height:'100%'}}>
      <div style={{padding:'8px 16px',borderBottom:'1px solid var(--t-border)',display:'flex',justifyContent:'space-between',alignItems:'center'}}>
        <span style={{fontSize:12,color:'var(--t-text3)',fontFamily:mono}}>{sessions.length} sessions</span>
        <button onClick={load} style={{fontSize:11,color:'var(--t-accent)',background:'none',border:'none',cursor:'pointer',fontFamily:mono}}>Refresh</button>
      </div>
      {loading ? <div style={{padding:24,textAlign:'center',color:'var(--t-text3)',fontSize:12,fontFamily:mono}}>Loading...</div>
      : error   ? <div style={{padding:24,color:'var(--t-sell)',fontSize:12,fontFamily:mono}}>{error}</div>
      : (
        <div style={{overflowX:'auto',flex:1}}>
          <table style={{width:'100%',borderCollapse:'collapse',fontSize:11,fontFamily:mono}}>
            <thead><tr style={{background:'var(--t-panel)'}}>
              {['#','Type','File','MD5','Total','Matched','Unmatched','Status','At'].map(c=>(
                <th key={c} style={{padding:'6px 10px',textAlign:'left',color:'var(--t-text3)',fontSize:10,letterSpacing:'0.06em',borderBottom:'1px solid var(--t-border)',whiteSpace:'nowrap'}}>{c}</th>
              ))}
            </tr></thead>
            <tbody>
              {sessions.length === 0
                ? <tr><td colSpan={9} style={{padding:24,textAlign:'center',color:'var(--t-text3)'}}>No sessions yet</td></tr>
                : sessions.map((s,i)=>(
                  <tr key={s.id} style={{borderBottom:'1px solid var(--t-border)'}}
                    onMouseEnter={e=>(e.currentTarget.style.background='var(--t-hover)')}
                    onMouseLeave={e=>(e.currentTarget.style.background='transparent')}>
                    <td style={{padding:'5px 10px',color:'var(--t-text3)'}}>{i+1}</td>
                    <td style={{padding:'5px 10px',color:'var(--t-text2)'}}>{s.fileType}</td>
                    <td style={{padding:'5px 10px',color:'var(--t-text1)',maxWidth:160,overflow:'hidden',textOverflow:'ellipsis',whiteSpace:'nowrap'}}>{s.xmlFileName}</td>
                    <td style={{padding:'5px 10px'}}><Md2Badge ok={s.md5Verified}/></td>
                    <td style={{padding:'5px 10px',color:'var(--t-text2)'}}>{s.totalRecords}</td>
                    <td style={{padding:'5px 10px',color:'#00D4AA'}}>{s.reconciledRecords}</td>
                    <td style={{padding:'5px 10px',color:s.unmatchedRecords>0?'var(--t-sell)':'var(--t-text3)'}}>{s.unmatchedRecords}</td>
                    <td style={{padding:'5px 10px'}}><Pill v={s.status}/></td>
                    <td style={{padding:'5px 10px',color:'var(--t-text3)',whiteSpace:'nowrap'}}>{new Date(s.importedAt).toLocaleString('en-GB',{day:'2-digit',month:'short',hour:'2-digit',minute:'2-digit'})}</td>
                  </tr>
                ))
              }
            </tbody>
          </table>
        </div>
      )}
    </div>
  )
}

function ComplianceTab({ brokerageHouseId }: { brokerageHouseId:number }) {
  const [report,setReport]   = useState<any>(null)
  const [loading,setLoading] = useState(true)
  const [refreshing,setRef]  = useState(false)
  const [error,setError]     = useState('')
  const load = useCallback(async()=>{
    setLoading(true);setError('')
    try{setReport(await bosGetCompliance(brokerageHouseId))}
    catch(e:any){setError(e.message)}
    finally{setLoading(false)}
  },[brokerageHouseId])
  useEffect(()=>{load()},[load])
  const refresh = async()=>{
    setRef(true);setError('')
    try{setReport(await bosRefreshCompliance(brokerageHouseId))}
    catch(e:any){setError(e.message)}
    finally{setRef(false)}
  }
  if(loading) return <div style={{padding:24,textAlign:'center',color:'var(--t-text3)',fontSize:12,fontFamily:mono}}>Loading compliance...</div>
  if(error)   return <div style={{padding:24,color:'var(--t-sell)',fontSize:12,fontFamily:mono}}>{error}</div>
  if(!report) return null
  const passed = report.checks?.filter((c:any)=>c.passed).length ?? 0
  const total  = report.checks?.length ?? 0
  return (
    <div style={{padding:20,display:'flex',flexDirection:'column',gap:16}}>
      <div style={{display:'flex',alignItems:'center',justifyContent:'space-between'}}>
        <div style={{display:'flex',alignItems:'center',gap:12}}>
          <div style={{width:10,height:10,borderRadius:'50%',background:passed===total?'#00D4AA':'#FF6B6B'}}/>
          <span style={{fontSize:14,fontWeight:700,color:'var(--t-text1)',fontFamily:mono}}>{report.brokerageName}</span>
          <span style={{fontSize:12,color:'var(--t-text3)',fontFamily:mono}}>{passed}/{total} passed</span>
          {report.fromCache && <span style={{fontSize:10,color:'var(--t-text3)',fontFamily:mono}}>(cached)</span>}
        </div>
        <button onClick={refresh} disabled={refreshing} style={{padding:'6px 14px',borderRadius:6,fontSize:11,fontFamily:mono,cursor:refreshing?'wait':'pointer',background:'var(--t-hover)',border:'1px solid var(--t-border)',color:'var(--t-text2)'}}>{refreshing?'Refreshing...':'Force Refresh'}</button>
      </div>
      <div style={{height:4,background:'var(--t-hover)',borderRadius:2,overflow:'hidden'}}>
        <div style={{height:'100%',width:(passed/Math.max(total,1)*100)+'%',background:passed===total?'#00D4AA':'#F59E0B',borderRadius:2,transition:'width 0.4s'}}/>
      </div>
      <div style={{display:'flex',flexDirection:'column',gap:8}}>
        {report.checks?.map((c:any)=>(
          <div key={c.checkName} style={{
            background:'var(--t-panel)',border:'1px solid var(--t-border)',
            borderLeft:'3px solid '+(c.passed?'#00D4AA':c.severity==='Critical'?'#FF6B6B':'#F59E0B'),
            borderRadius:6,padding:'10px 14px',display:'flex',alignItems:'flex-start',gap:12,
          }}>
            <span style={{fontSize:14,marginTop:1,color:c.passed?'#00D4AA':'#FF6B6B'}}>{c.passed?'v':'x'}</span>
            <div style={{flex:1}}>
              <div style={{display:'flex',alignItems:'center',gap:8,marginBottom:c.failureReason?4:0}}>
                <span style={{fontSize:11,fontWeight:700,color:'var(--t-text1)',fontFamily:mono}}>{c.checkName}</span>
                {c.passed ? null : <Pill v={c.severity}/>}
              </div>
              <div style={{fontSize:11,color:'var(--t-text3)',fontFamily:mono}}>{c.description}</div>
              {c.failureReason && <div style={{fontSize:11,color:'var(--t-sell)',fontFamily:mono,marginTop:4}}>{c.failureReason}</div>}
            </div>
            <Pill v={c.passed?'Passed':'Failed'}/>
          </div>
        ))}
      </div>
    </div>
  )
}

function ExportTab({ brokerageHouseId }: { brokerageHouseId:number }) {
  const [busy,setBusy]     = useState(false)
  const [result,setResult] = useState<any>(null)
  const [error,setError]   = useState('')
  const run = async()=>{
    setBusy(true);setError('');setResult(null)
    try{setResult(await bosExportPositions(brokerageHouseId))}
    catch(e:any){setError(e.message)}
    finally{setBusy(false)}
  }
  const download = ()=>{
    if(!result)return
    const a=document.createElement('a')
    a.href=URL.createObjectURL(new Blob([result.xmlContent],{type:'text/xml'}))
    a.download=result.fileName;a.click()
  }
  return (
    <div style={{padding:20,maxWidth:600,display:'flex',flexDirection:'column',gap:16}}>
      <div style={{background:'var(--t-panel)',border:'1px solid var(--t-border)',borderRadius:8,padding:16}}>
        <div style={{fontSize:13,fontWeight:700,color:'var(--t-text1)',fontFamily:mono,marginBottom:8}}>EOD Positions Export</div>
        <div style={{fontSize:12,color:'var(--t-text3)',fontFamily:mono,marginBottom:16}}>Generates BOS-compatible XML with MD5 checksum for all current portfolio positions.</div>
        <button onClick={run} disabled={busy} style={{padding:'10px 24px',borderRadius:6,fontSize:13,fontFamily:mono,cursor:busy?'wait':'pointer',background:'rgba(0,212,170,0.12)',border:'1px solid var(--t-accent)',color:'var(--t-accent)',fontWeight:700}}>{busy?'Generating...':'Generate XML'}</button>
      </div>
      {error && <div style={{background:'rgba(255,107,107,0.08)',border:'1px solid rgba(255,107,107,0.25)',borderRadius:6,padding:'10px 14px',fontSize:12,color:'var(--t-sell)',fontFamily:mono}}>{error}</div>}
      {result && (
        <div style={{background:'var(--t-panel)',border:'1px solid var(--t-border)',borderRadius:8,padding:16,display:'flex',flexDirection:'column',gap:12}}>
          <div style={{display:'flex',justifyContent:'space-between',alignItems:'center'}}>
            <span style={{fontSize:12,fontWeight:700,color:'var(--t-text1)',fontFamily:mono}}>{result.fileName}</span>
            <button onClick={download} style={{padding:'6px 16px',borderRadius:6,fontSize:11,fontFamily:mono,cursor:'pointer',background:'rgba(0,212,170,0.12)',border:'1px solid var(--t-accent)',color:'var(--t-accent)'}}>Download XML</button>
          </div>
          <div style={{display:'flex',gap:8}}>
            <span style={{fontSize:10,color:'var(--t-text3)',fontFamily:mono}}>MD5:</span>
            <span style={{fontSize:10,color:'var(--t-accent)',fontFamily:mono}}>{result.md5Hash}</span>
          </div>
          <pre style={{background:'var(--t-hover)',borderRadius:6,padding:12,fontSize:10,color:'var(--t-text2)',fontFamily:mono,overflow:'auto',maxHeight:300,margin:0}}>{result.xmlContent?.slice(0,2000)}{result.xmlContent?.length>2000?'\n...':''}</pre>
        </div>
      )}
    </div>
  )
}

export default function BosReconciliationPage() {
  const [tab,setTab] = useState('Upload')
  const user = useAuthStore(s=>s.user)
  const brokerageHouseId = user?.brokerageHouseId ?? 1
  const userId = user?.id ?? 1
  return (
    <div style={{height:'100%',display:'flex',flexDirection:'column',background:'var(--t-surface)',overflow:'hidden'}}>
      <div style={{padding:'12px 20px',borderBottom:'1px solid var(--t-border)',background:'var(--t-panel)',flexShrink:0,display:'flex',alignItems:'center',gap:12}}>
        <span style={{fontSize:16,fontWeight:700,color:'var(--t-text1)',fontFamily:mono}}>BOS XML Reconciliation</span>
        <span style={{fontSize:11,color:'var(--t-text3)',fontFamily:mono}}>Brokerage #{brokerageHouseId}</span>
      </div>
      <Tabs active={tab} onChange={setTab}/>
      <div style={{flex:1,overflow:'auto'}}>
        {tab==='Upload'     && <UploadTab     brokerageHouseId={brokerageHouseId} userId={userId}/>}
        {tab==='Sessions'   && <SessionsTab   brokerageHouseId={brokerageHouseId}/>}
        {tab==='Compliance' && <ComplianceTab brokerageHouseId={brokerageHouseId}/>}
        {tab==='Export'     && <ExportTab     brokerageHouseId={brokerageHouseId}/>}
      </div>
    </div>
  )
}
