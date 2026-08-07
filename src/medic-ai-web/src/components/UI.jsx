export function PageHeader({ title, description, actions }) {
  return <div className="page-header"><div><h2>{title}</h2>{description && <p>{description}</p>}</div>{actions && <div className="page-actions">{actions}</div>}</div>
}

export function Card({ title, children, className = '' }) {
  return <section className={`card ${className}`}><div className="card-title">{title}</div>{children}</section>
}

export function StatCard({ label, value, hint }) {
  return <div className="stat-card"><span>{label}</span><strong>{value}</strong>{hint && <small>{hint}</small>}</div>
}

export function StatusBadge({ value }) {
  const key = String(value || '').toLowerCase().replaceAll('_', '-').replaceAll(' ', '-')
  return <span className={`status-badge status-${key}`}>{String(value || 'Unknown').replaceAll('_', ' ')}</span>
}

export function Alert({ type = 'info', children }) {
  return <div className={`alert alert-${type}`}>{children}</div>
}

export function Loading() {
  return <div className="loading"><div className="spinner" /> Loading…</div>
}

export function EmptyState({ title = 'Nothing to show', description }) {
  return <div className="empty-state"><strong>{title}</strong>{description && <p>{description}</p>}</div>
}

export function Field({ label, required, hint, children }) {
  return <label className="field"><span>{label}{required && <b> *</b>}</span>{children}{hint && <small>{hint}</small>}</label>
}

export function Modal({ title, children, onClose }) {
  return <div className="modal-backdrop" onMouseDown={onClose}><div className="modal" onMouseDown={(event) => event.stopPropagation()}><div className="modal-header"><h3>{title}</h3><button className="icon-button" onClick={onClose}>×</button></div>{children}</div></div>
}
