interface LogoProps {
  size?: number
  animated?: boolean
  showText?: boolean
  textSize?: 'sm' | 'md' | 'lg'
}

export function Logo({ size = 36, animated = false, showText = false, textSize = 'md' }: LogoProps) {
  const textSizes = { sm: { name: 13, sub: 9 }, md: { name: 16, sub: 10 }, lg: { name: 22, sub: 12 } }
  const ts = textSizes[textSize]

  return (
    <div style={{ display: 'flex', alignItems: 'center', gap: size * 0.33 }}>
      <svg
        width={size} height={size}
        viewBox="0 0 48 48"
        className={animated ? 'animate-logo' : undefined}
        style={{ flexShrink: 0 }}
      >
        <defs>
          <linearGradient id="logo-g1" x1="0%" y1="0%" x2="100%" y2="100%">
            <stop offset="0%" stopColor="var(--accent-400)"/>
            <stop offset="100%" stopColor="var(--accent-600)"/>
          </linearGradient>
          <linearGradient id="logo-g2" x1="0%" y1="100%" x2="100%" y2="0%">
            <stop offset="0%" stopColor="var(--accent-600)" stopOpacity="0.3"/>
            <stop offset="100%" stopColor="var(--accent-300)" stopOpacity="0.7"/>
          </linearGradient>
          <filter id="logo-glow">
            <feGaussianBlur stdDeviation="1.5" result="coloredBlur"/>
            <feMerge><feMergeNode in="coloredBlur"/><feMergeNode in="SourceGraphic"/></feMerge>
          </filter>
        </defs>
        {/* Outer hexagon */}
        <polygon
          points="24,2 43,13 43,35 24,46 5,35 5,13"
          fill="var(--accent-glow)"
          stroke="url(#logo-g1)"
          strokeWidth="1.2"
        />
        {/* Inner hexagon ring */}
        <polygon
          points="24,8 38,16.5 38,31.5 24,40 10,31.5 10,16.5"
          fill="none"
          stroke="url(#logo-g2)"
          strokeWidth="0.7"
          opacity="0.5"
        />
        {/* Trend line */}
        <polyline
          points="13,31 19,23 25,26.5 35,16"
          fill="none"
          stroke="url(#logo-g1)"
          strokeWidth="2.4"
          strokeLinecap="round"
          strokeLinejoin="round"
          filter="url(#logo-glow)"
        />
        {/* Start dot */}
        <circle cx="13" cy="31" r="2" fill="var(--accent-500)" opacity="0.7"/>
        {/* Peak dot with glow */}
        <circle cx="35" cy="16" r="3" fill="var(--accent-400)" filter="url(#logo-glow)"/>
        <circle cx="35" cy="16" r="1.5" fill="#fff" opacity="0.9"/>
        {/* Up arrow at peak */}
        <polyline
          points="31.5,13 35,16 38.5,13"
          fill="none"
          stroke="var(--accent-300)"
          strokeWidth="1.8"
          strokeLinecap="round"
          strokeLinejoin="round"
        />
      </svg>

      {showText && (
        <div style={{ lineHeight: 1.1 }}>
          <div style={{
            fontFamily: 'var(--font-display)',
            fontWeight: 800,
            fontSize: ts.name,
            color: 'var(--text-primary)',
            letterSpacing: '-0.03em',
            lineHeight: 1,
          }}>
            BD<span style={{ color: 'var(--accent-400)' }}>OMS</span>
          </div>
          <div style={{
            fontSize: ts.sub,
            color: 'var(--text-tertiary)',
            letterSpacing: '.06em',
            textTransform: 'uppercase',
            marginTop: 2,
            fontWeight: 500,
          }}>
            Order Management
          </div>
        </div>
      )}
    </div>
  )
}
