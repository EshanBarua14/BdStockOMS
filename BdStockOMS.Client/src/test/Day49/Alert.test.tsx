import { describe, it, expect, vi } from 'vitest'
import { render, screen, fireEvent } from '@testing-library/react'
import { Alert } from '@/components/ui/Alert'

describe('Alert', () => {
  it('renders children text', () => {
    render(<Alert>Something went wrong</Alert>)
    expect(screen.getByText('Something went wrong')).toBeInTheDocument()
  })

  it('renders title when provided', () => {
    render(<Alert title="Error Title">Detail message</Alert>)
    expect(screen.getByText('Error Title')).toBeInTheDocument()
  })

  it('renders with alert role', () => {
    render(<Alert>Error message</Alert>)
    expect(screen.getByRole('alert')).toBeInTheDocument()
  })

  it('renders success variant', () => {
    render(<Alert variant="success">Order placed successfully</Alert>)
    expect(screen.getByText('Order placed successfully')).toBeInTheDocument()
  })

  it('renders warning variant', () => {
    render(<Alert variant="warning">Low balance warning</Alert>)
    expect(screen.getByText('Low balance warning')).toBeInTheDocument()
  })

  it('calls onDismiss when dismiss button clicked', () => {
    const onDismiss = vi.fn()
    render(<Alert onDismiss={onDismiss}>Dismissible alert</Alert>)
    fireEvent.click(screen.getByRole('button', { name: /dismiss/i }))
    expect(onDismiss).toHaveBeenCalledOnce()
  })

  it('does not render dismiss button when onDismiss not provided', () => {
    render(<Alert>No dismiss</Alert>)
    expect(screen.queryByRole('button')).toBeNull()
  })
})
