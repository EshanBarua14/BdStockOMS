import { clsx } from 'clsx'

interface SpinnerProps {
  size?: 'sm' | 'md' | 'lg'
  className?: string
}

const sizes = {
  sm: 'w-4 h-4 border-2',
  md: 'w-6 h-6 border-2',
  lg: 'w-8 h-8 border-[3px]',
}

export function Spinner({ size = 'md', className }: SpinnerProps) {
  return (
    <div
      role="status"
      aria-label="Loading"
      className={clsx(
        'rounded-full border-surface-border border-t-brand-500 animate-spin',
        sizes[size],
        className,
      )}
    />
  )
}

export function PageSpinner() {
  return (
    <div className="fixed inset-0 flex items-center justify-center bg-surface">
      <div className="flex flex-col items-center gap-4">
        <Spinner size="lg" />
        <p className="text-muted text-sm">Loading…</p>
      </div>
    </div>
  )
}
