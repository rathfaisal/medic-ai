import { useEffect, useId, useRef } from 'react'

export function PageHeader({ title, description, actions }) {
  return <div className="page-header"><div><h1>{title}</h1>{description && <p>{description}</p>}</div>{actions && <div className="page-actions">{actions}</div>}</div>
}

export function Card({ title, children, className = '' }) {
  return <section className={`card ${className}`}><h2 className="card-title">{title}</h2>{children}</section>
}

export function StatCard({ label, value, hint }) {
  return <div className="stat-card"><span>{label}</span><strong>{value}</strong>{hint && <small>{hint}</small>}</div>
}

export function StatusBadge({ value }) {
  const key = String(value || '').toLowerCase().replaceAll('_', '-').replaceAll(' ', '-')
  return <span className={`status-badge status-${key}`}>{String(value || 'Unknown').replaceAll('_', ' ')}</span>
}

export function Alert({ type = 'info', children }) {
  return <div className={`alert alert-${type}`} role={type === 'danger' ? 'alert' : 'status'}>{children}</div>
}

export function Loading() {
  return <div className="loading" role="status"><div className="spinner" aria-hidden="true" /> Loading…</div>
}

export function EmptyState({ title = 'Nothing to show', description }) {
  return <div className="empty-state"><strong>{title}</strong>{description && <p>{description}</p>}</div>
}

export function Field({ label, required, hint, children }) {
  return <label className="field"><span>{label}{required && <b> *</b>}</span>{children}{hint && <small>{hint}</small>}</label>
}

export function Modal({ title, children, onClose }) {
  const dialog = useRef(null)
  const titleId = useId()

  useEffect(() => {
    const element = dialog.current
    const trigger = document.activeElement
    element.showModal()
    return () => {
      element.close()
      trigger?.focus()
    }
  }, [])

  return (
    <dialog
      className="modal"
      ref={dialog}
      aria-labelledby={titleId}
      onCancel={(event) => { event.preventDefault(); onClose() }}
      onClick={(event) => { if (event.target === event.currentTarget) onClose() }}
    >
      <div className="modal-content">
        <div className="modal-header">
          <h2 id={titleId}>{title}</h2>
          <button className="icon-button" type="button" aria-label="Close dialog" onClick={onClose}>×</button>
        </div>
        {children}
      </div>
    </dialog>
  )
}
