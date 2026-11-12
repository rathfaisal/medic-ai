import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import api, { errorMessage } from '../api/client'
import { useAuth } from '../auth/AuthContext'
import { Alert, Card, EmptyState, Field, Loading, Modal, PageHeader, StatusBadge } from '../components/UI'

export default function QueuePage() {
  const { user } = useAuth()
  return user.role === 'Patient' ? <PatientQueue /> : <ClinicQueue />
}

function PatientQueue() {
  const [entry, setEntry] = useState(undefined)
  const [error, setError] = useState('')
  useEffect(() => { api.get('/queue/mine').then(({ data }) => setEntry(data)).catch((err) => { setError(errorMessage(err)); setEntry(null) }) }, [])
  if (entry === undefined) return <Loading />
  return <>
    <PageHeader title="My queue status" description="Your position is based on confirmed urgency and arrival time. Clinic staff and doctors retain final control." />
    {error && <Alert type="danger">{error}</Alert>}
    {!error && (entry ? <Card title="Current visit"><div className="queue-patient-card"><div className="queue-big-number">{entry.queueStatus === 'Waiting' ? '…' : '✓'}</div><div><div className="eyebrow">Current status</div><h2>{entry.queueStatus.replaceAll('_', ' ')}</h2><p>Checked in at {formatTime(entry.checkInTime)}</p><div className="tag-row"><StatusBadge value={entry.confirmedUrgency} /><StatusBadge value={entry.queueStatus} /></div></div></div>{entry.reviewRequired && <Alert type="warning">Your preliminary result requires staff review.</Alert>}</Card> : <EmptyState title="Not currently checked in" description="Clinic staff will add you to the digital queue when you arrive." />)}
  </>
}

function ClinicQueue() {
  const [entries, setEntries] = useState(null)
  const [error, setError] = useState('')
  const [message, setMessage] = useState('')
  const [queueError, setQueueError] = useState('')
  const [pending, setPending] = useState(false)
  const [patients, setPatients] = useState([])
  const [walkInOpen, setWalkInOpen] = useState(false)
  const [walkIn, setWalkIn] = useState({ patientId: '', manualUrgency: 'Routine' })
  const load = async () => {
    try {
      const { data } = await api.get('/queue')
      setEntries(data)
      setQueueError('')
    } catch (err) {
      setQueueError(errorMessage(err))
    }
  }
  useEffect(() => {
    load()
    api.get('/patients?take=100').then(({ data }) => setPatients(data)).catch(() => setPatients([]))
    const timer = setInterval(load, 30000)
    return () => clearInterval(timer)
  }, [])
  if (!entries && !queueError) return <Loading />
  const activeEntries = entries || []
  const waiting = activeEntries.filter((entry) => entry.queueStatus === 'Waiting').length

  const changeStatus = async (id, status) => {
    setError(''); setMessage(''); setPending(true)
    try { await api.patch(`/queue/${id}/status`, { status }); setMessage(`Queue status changed to ${status}.`); await load() }
    catch (err) { setError(errorMessage(err)) }
    finally { setPending(false) }
  }
  const callNext = async () => {
    setError(''); setMessage(''); setPending(true)
    try { const { data } = await api.post('/queue/call-next'); setMessage(data ? `${data.patientName} has been called.` : 'No patient is waiting.'); await load() }
    catch (err) { setError(errorMessage(err)) }
    finally { setPending(false) }
  }

  const checkInWalkIn = async (event) => {
    event.preventDefault(); setError(''); setMessage(''); setPending(true)
    try {
      await api.post('/queue/check-in-walk-in', { patientId: Number(walkIn.patientId), assessmentId: null, manualUrgency: walkIn.manualUrgency })
      setWalkInOpen(false); setWalkIn({ patientId: '', manualUrgency: 'Routine' }); setMessage('Walk-in patient checked in. The latest assessment is used when available.'); await load()
    } catch (err) { setError(errorMessage(err)) }
    finally { setPending(false) }
  }

  return <>
    <PageHeader title="Clinic queue" description="Patients are ordered by staff-confirmed urgency and arrival time. Emergency-warning cases are handled separately." actions={<div className="action-row"><button className="button button-secondary" disabled={pending} onClick={() => { setError(''); setWalkInOpen(true) }}>Check in walk-in</button><button className="button button-primary" disabled={pending || !!queueError || waiting === 0} onClick={callNext}>Call next patient</button></div>} />
    {message && <Alert type="success">{message}</Alert>}{error && !walkInOpen && <Alert type="danger">{error}</Alert>}
    {queueError && <Alert type="danger">The queue could not refresh. {queueError} <button className="button button-small button-secondary" onClick={load}>Retry</button></Alert>}
    <Card title="Active queue">
      {entries && <dl className="queue-summary">
        <div><dt>Waiting</dt><dd>{waiting}</dd></div>
        <div><dt>Called</dt><dd>{entries.filter((entry) => entry.queueStatus === 'Called').length}</dd></div>
        <div><dt>In consultation</dt><dd>{entries.filter((entry) => entry.queueStatus === 'InConsultation').length}</dd></div>
      </dl>}
      {activeEntries.length ? <ol className="queue-list">{activeEntries.map((entry, index) => <li className={`queue-entry urgency-${entry.confirmedUrgency.toLowerCase()}`} key={entry.id}>
        <div className="queue-position">{index + 1}</div>
        <div className="queue-person"><strong>{entry.patientName}</strong><span>Checked in {formatTime(entry.checkInTime)}</span>{entry.appointmentDateTime && <small>Appointment: {formatTime(entry.appointmentDateTime)}</small>}</div>
        <div><StatusBadge value={entry.confirmedUrgency} />{entry.reviewRequired && <span className="review-flag">Review required</span>}</div>
        <div><StatusBadge value={entry.queueStatus} /><small>Score {entry.priorityScore}</small></div>
        <div className="action-row">
          <Link className="button button-small button-secondary" to={`/patient-review?patientId=${entry.patientId}`}>Review</Link>
          {entry.queueStatus === 'Waiting' && <button className="button button-small button-primary" disabled={pending || !!queueError} onClick={() => changeStatus(entry.id, 'Called')}>Call</button>}
          {['Waiting', 'Called'].includes(entry.queueStatus) && <button className="button button-small button-secondary" disabled={pending || !!queueError} onClick={() => changeStatus(entry.id, 'InConsultation')}>Start</button>}
          {entry.queueStatus === 'InConsultation' && <button className="button button-small button-success" disabled={pending || !!queueError} onClick={() => changeStatus(entry.id, 'Completed')}>Complete</button>}
          <button className="button button-small button-danger-soft" disabled={pending || !!queueError} onClick={() => { if (window.confirm('Remove this patient from the active queue?')) changeStatus(entry.id, 'Removed') }}>Remove</button>
        </div>
      </li>)}</ol> : !queueError && <EmptyState title="Queue is clear" description="No patients are currently waiting." />}
    </Card>
    {walkInOpen && <Modal title="Check in walk-in patient" onClose={() => setWalkInOpen(false)}><form className="form-stack" onSubmit={checkInWalkIn}>
      {error && <Alert type="danger">{error}</Alert>}
      <Field label="Patient" required><select value={walkIn.patientId} onChange={(e) => setWalkIn({ ...walkIn, patientId: e.target.value })} required><option value="">Select patient</option>{patients.map((patient) => <option key={patient.id} value={patient.id}>{patient.fullName} — {patient.phone}</option>)}</select></Field>
      <Field label="Manual urgency fallback" hint="The latest completed pre-triage assessment takes priority. This value is used only when no assessment exists."><select value={walkIn.manualUrgency} onChange={(e) => setWalkIn({ ...walkIn, manualUrgency: e.target.value })}><option>Routine</option><option>Priority</option><option>Urgent</option></select></Field>
      <Alert type="warning">Do not place an emergency-warning patient in the normal queue. Use the emergency-alert workflow.</Alert>
      <div className="modal-actions"><button type="button" className="button button-ghost" onClick={() => setWalkInOpen(false)}>Cancel</button><button className="button button-primary" disabled={pending}>Check in</button></div>
    </form></Modal>}
  </>
}

const formatTime = (value) => new Date(value).toLocaleString([], { dateStyle: 'medium', timeStyle: 'short' })
