import { clsx } from 'clsx'

interface AlertProps {
  variant?: 'error' | 'warning' | 'success' | 'info'
  title?: string
  children: React.ReactNode
  className?: string
  onDismiss?: () => void
}

const variants = {
  error:   'bg-danger/10 border-danger/30 text-danger',
  warning: 'bg-warning/10 border-warning/30 text-warning',
  success: 'bg-success/10 border-success/30 text-success',
  info:    'bg-brand-500/10 border-brand-500/30 text-brand-300',
}

const icons = {
  error:   '✕',
  warning: '⚠',
  success: '✓',
  info:    'ℹ',
}

export function Alert({
  variant = 'error',
  title,
  children,
  className,
  onDismiss,
}: AlertProps) {
  return (
    <div
      role="alert"
      className={clsx(
        'flex gap-3 px-4 py-3 rounded-lg border text-sm animate-fade-in',
        variants[variant],
        className,
      )}
    >
      <span className="mt-0.5 shrink-0 font-bold">{icons[variant]}</span>
      <div className="flex-1 min-w-0">
        {title && <p className="font-semibold mb-0.5">{title}</p>}
        <p className="opacity-90">{children}</p>
      </div>
      {onDismiss && (
        <button
          onClick={onDismiss}
          className="shrink-0 opacity-60 hover:opacity-100 transition-opacity"
          aria-label="Dismiss"
        >
          ✕
        </button>
      )}
    </div>
  )
}
