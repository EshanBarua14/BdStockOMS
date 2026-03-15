import React from 'react';
interface Props extends React.ButtonHTMLAttributes<HTMLButtonElement> {
  icon: React.ReactNode;
  count?: number;
  title?: string;
}
export const TopbarIconBtn: React.FC<Props> = ({ icon, count, title, ...props }) => (
  <button title={title} style={{ position:'relative', background:'none', border:'none', cursor:'pointer', padding:'4px 6px', borderRadius:6, color:'var(--t-text2)', fontSize:16, display:'flex', alignItems:'center', justifyContent:'center' }} {...props}>
    {icon}
    {count != null && count > 0 && (
      <span style={{ position:'absolute', top:0, right:0, background:'var(--t-sell)', color:'#fff', fontSize:8, fontWeight:700, borderRadius:999, minWidth:14, height:14, display:'flex', alignItems:'center', justifyContent:'center', padding:'0 3px' }}>
        {count > 99 ? '99+' : count}
      </span>
    )}
  </button>
);
