import { useEffect, useState } from 'react'
import api, { errorMessage } from '../api/client'
import { useAuth } from '../auth/AuthContext'
import { Alert, Card, EmptyState, Loading, PageHeader, StatusBadge } from '../components/UI'

export default function EmergencyAlertsPage() {
  const { user } = useAuth()
  const [alerts, setAlerts] = useState(null)
  const [status, setStatus] = useState('')
  const [error, setError] = useState('')
  const [message, setMessage] = useState('')

  const load = async () => {
    try { const { data } = await api.get('/emergency-alerts', { params: { status: status || undefined } }); setAlerts(data) }
    catch (err) { setError(errorMessage(err)); setAlerts([]) }
  }
  useEffect(() => { load(); const timer = setInterval(load, 20000); return () => clearInterval(timer) }, [status])
  if (!alerts) return <Loading />

  const action = async (alert, type) => {
    let notes = ''
    if (type === 'acknowledge') notes = window.prompt('Optional acknowledgement note:', '') ?? ''
    else notes = window.prompt(type === 'false-alarm' ? 'Enter the clinical/administrative reason:' : 'Enter resolution notes:', '') ?? ''
    if (type !== 'acknowledge' && !notes.trim()) return
    try { await api.post(`/emergency-alerts/${alert.id}/${type}`, { notes: notes || null }); setMessage(`Alert ${type.replace('-', ' ')} action completed.`); setError(''); await load() }
    catch (err) { setError(errorMessage(err)) }
  }

  return <>
    <PageHeader title="Emergency alerts" description="Red-flag alerts are separate from the normal queue and require acknowledgement, documented action and resolution." actions={<select value={status} onChange={(e) => setStatus(e.target.value)}><option value="">All statuses</option><option>Created</option><option>Acknowledged</option><option>Escalated</option><option>Resolved</option><option value="CancelledAsFalseAlarm">Cancelled as false alarm</option></select>} />
    <Alert type="danger"><strong>This dashboard does not contact an ambulance automatically.</strong> Follow the clinic's approved emergency procedure and seek appropriate professional assistance.</Alert>
    {message && <Alert type="success">{message}</Alert>}{error && <Alert type="danger">{error}</Alert>}
    <Card title={`Alerts (${alerts.length})`}>
      {alerts.length ? <div className="alert-list">{alerts.map((item) => <article className={`emergency-alert status-panel-${item.status.toLowerCase()}`} key={item.id}>
        <div className="emergency-symbol">!</div><div className="emergency-content"><div className="assessment-head"><div><h3>{item.patientName}</h3><span>Created {formatDate(item.createdAt)} · Alert #{item.id}</span></div><StatusBadge value={item.status} /></div><p><strong>Warning reason:</strong> {item.warningReason}</p><div className="detail-grid compact"><div><span>Assigned role</span><strong>{item.assignedRole}</strong></div><div><span>Acknowledged by</span><strong>{item.acknowledgedBy || 'Not acknowledged'}</strong></div><div><span>Acknowledged at</span><strong>{item.acknowledgedAt ? formatDate(item.acknowledgedAt) : '—'}</strong></div><div><span>Acknowledgement note</span><strong>{item.acknowledgementNotes || '—'}</strong></div><div><span>Resolution</span><strong>{item.resolutionNotes || 'Open'}</strong></div></div>
          <div className="action-row"><a className="button button-small button-secondary" href={`/patient-review?patientId=${item.patientId}`}>Review patient</a>{!['Resolved', 'CancelledAsFalseAlarm'].includes(item.status) && item.status !== 'Acknowledged' && <button className="button button-small button-primary" onClick={() => action(item, 'acknowledge')}>Acknowledge</button>}{!['Resolved', 'CancelledAsFalseAlarm'].includes(item.status) && <button className="button button-small button-success" onClick={() => action(item, 'resolve')}>Resolve</button>}{['Doctor', 'Admin'].includes(user.role) && !['Resolved', 'CancelledAsFalseAlarm'].includes(item.status) && <button className="button button-small button-danger-soft" onClick={() => action(item, 'false-alarm')}>Mark false alarm</button>}</div>
        </div>
      </article>)}</div> : <EmptyState title="No matching emergency alerts" description="Open red-flag submissions will appear here immediately." />}
    </Card>
  </>
}
const formatDate = (value) => new Date(value).toLocaleString([], { dateStyle: 'medium', timeStyle: 'short' })
