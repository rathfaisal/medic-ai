import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import api, { errorMessage } from '../api/client'
import { useAuth } from '../auth/AuthContext'
import { Alert, Card, EmptyState, Loading, PageHeader, StatCard, StatusBadge } from '../components/UI'

export default function DashboardPage() {
  const { user } = useAuth()
  const [data, setData] = useState(null)
  const [error, setError] = useState('')

  useEffect(() => { load() }, [user.role])

  const load = async () => {
    setError('')
    try {
      if (user.role === 'Patient') {
        const [appointments, triage, queue, notifications] = await Promise.all([
          api.get('/appointments'), api.get('/triage/mine'), api.get('/queue/mine'), api.get('/notifications')
        ])
        setData({ appointments: appointments.data, triage: triage.data, queue: queue.data, notifications: notifications.data })
      } else {
        const requests = [api.get('/appointments'), api.get('/queue'), api.get('/emergency-alerts')]
        if (['Doctor', 'Admin'].includes(user.role)) requests.push(api.get('/reports/summary'))
        const responses = await Promise.all(requests)
        setData({ appointments: responses[0].data, queue: responses[1].data, alerts: responses[2].data, summary: responses[3]?.data })
      }
    } catch (err) { setError(errorMessage(err)); setData({}) }
  }

  if (!data) return <Loading />
  return <>
    <PageHeader title={`Welcome, ${user.displayName}`} description="Your role-based overview of today's clinic activity." />
    {error && <Alert type="danger">{error}</Alert>}
    {user.role === 'Patient' ? <PatientDashboard data={data} /> : <ClinicDashboard role={user.role} data={data} />}
  </>
}

function PatientDashboard({ data }) {
  const upcoming = data.appointments?.filter((x) => ['Booked', 'CheckedIn', 'InConsultation'].includes(x.status)) || []
  const latest = data.triage?.[0]
  const unread = data.notifications?.filter((x) => !x.isRead).length || 0
  return <>
    <div className="stats-grid">
      <StatCard label="Upcoming appointments" value={upcoming.length} />
      <StatCard label="Latest urgency" value={latest?.effectiveUrgency || 'Not assessed'} />
      <StatCard label="Queue status" value={data.queue?.queueStatus || 'Not checked in'} />
      <StatCard label="Unread notifications" value={unread} />
    </div>
    <div className="content-grid two-thirds">
      <Card title="Next appointment">
        {upcoming.length ? <div className="appointment-highlight"><div><strong>{formatDate(upcoming[0].appointmentDateTime)}</strong><span>{upcoming[0].serviceType}</span><span>{upcoming[0].doctorName || 'Doctor to be assigned'}</span></div><StatusBadge value={upcoming[0].status} /></div> : <EmptyState description="Book an appointment to begin the pre-triage workflow." />}
        <Link className="text-link" to="/appointments">Manage appointments →</Link>
      </Card>
      <Card title="Pre-triage guidance">
        {latest ? <><div className="result-line"><StatusBadge value={latest.effectiveUrgency} /><span>{Math.round((latest.confidence || 0) * 100)}% confidence</span></div><p>{latest.guidance}</p>{latest.reviewRequired && <Alert type="warning">Staff review is required.</Alert>}</> : <EmptyState description="Submit symptoms before your clinic visit." />}
        <Link className="text-link" to="/pre-triage">Open pre-triage form →</Link>
      </Card>
    </div>
    <Alert type="info"><strong>Important:</strong> MEDIC-AI provides preliminary urgency guidance only. Contact a healthcare professional if you are worried, and seek emergency help for severe symptoms.</Alert>
  </>
}

function ClinicDashboard({ role, data }) {
  const summary = data.summary || {}
  const today = (data.appointments || []).filter((x) => new Date(x.appointmentDateTime).toDateString() === new Date().toDateString())
  return <>
    <div className="stats-grid">
      <StatCard label="Today's appointments" value={summary.todayAppointments ?? today.length} />
      <StatCard label="Active queue" value={summary.waitingPatients ?? data.queue?.length ?? 0} />
      <StatCard label="Open emergency alerts" value={summary.emergencyAlerts ?? data.alerts?.length ?? 0} />
      <StatCard label={role === 'Admin' ? 'Registered patients' : 'Consultations (30 days)'} value={role === 'Admin' ? (summary.totalPatients ?? '—') : (summary.consultationsLast30Days ?? '—')} />
    </div>
    {(data.alerts?.length > 0) && <Alert type="danger"><strong>{data.alerts.length} emergency alert(s) require attention.</strong> Open the emergency-alert dashboard immediately.</Alert>}
    <div className="content-grid two-thirds">
      <Card title="Current digital queue">
        {data.queue?.length ? <div className="compact-list">{data.queue.slice(0, 6).map((entry, index) => <div className="list-row" key={entry.id}><span className="queue-number">{index + 1}</span><div><strong>{entry.patientName}</strong><small>Checked in {formatTime(entry.checkInTime)}</small></div><StatusBadge value={entry.confirmedUrgency} /><StatusBadge value={entry.queueStatus} /></div>)}</div> : <EmptyState description="No patients are waiting." />}
        <Link className="text-link" to="/queue">Manage digital queue →</Link>
      </Card>
      <Card title="Today's schedule">
        {today.length ? <div className="compact-list">{today.slice(0, 7).map((appointment) => <div className="list-row" key={appointment.id}><div className="time-block">{formatTime(appointment.appointmentDateTime)}</div><div><strong>{appointment.patientName}</strong><small>{appointment.serviceType}</small></div><StatusBadge value={appointment.status} /></div>)}</div> : <EmptyState description="No appointments are scheduled today." />}
        <Link className="text-link" to="/appointments">View appointments →</Link>
      </Card>
    </div>
  </>
}

const formatDate = (value) => new Date(value).toLocaleString([], { dateStyle: 'medium', timeStyle: 'short' })
const formatTime = (value) => new Date(value).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })
