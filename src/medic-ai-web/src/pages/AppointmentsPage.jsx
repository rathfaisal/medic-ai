import { useEffect, useMemo, useState } from 'react'
import api, { errorMessage } from '../api/client'
import { useAuth } from '../auth/AuthContext'
import { Alert, Card, EmptyState, Field, Loading, Modal, PageHeader, StatusBadge } from '../components/UI'

const emptyForm = { patientId: '', assignedDoctorId: '', appointmentDateTime: '', serviceType: 'General Consultation', reason: '' }

export default function AppointmentsPage() {
  const { user } = useAuth()
  const [appointments, setAppointments] = useState(null)
  const [doctors, setDoctors] = useState([])
  const [patients, setPatients] = useState([])
  const [form, setForm] = useState(emptyForm)
  const [editing, setEditing] = useState(null)
  const [open, setOpen] = useState(false)
  const [error, setError] = useState('')
  const [message, setMessage] = useState('')
  const canCreate = ['Patient', 'Staff', 'Admin'].includes(user.role)
  const isStaff = ['Staff', 'Admin'].includes(user.role)

  const load = async () => {
    setError('')
    try {
      const requests = [api.get('/appointments'), api.get('/doctors')]
      if (isStaff) requests.push(api.get('/patients?take=100'))
      const [appointmentResult, doctorResult, patientResult] = await Promise.all(requests)
      setAppointments(appointmentResult.data); setDoctors(doctorResult.data); setPatients(patientResult?.data || [])
    } catch (err) { setError(errorMessage(err)); setAppointments([]) }
  }
  useEffect(() => { load() }, [user.role])

  const sorted = useMemo(() => [...(appointments || [])].sort((a, b) => new Date(a.appointmentDateTime) - new Date(b.appointmentDateTime)), [appointments])
  if (!appointments) return <Loading />

  const openCreate = () => { setEditing(null); setForm(emptyForm); setOpen(true) }
  const openEdit = (appointment) => {
    setEditing(appointment)
    setForm({ patientId: appointment.patientId, assignedDoctorId: appointment.assignedDoctorId || '', appointmentDateTime: toLocalInput(appointment.appointmentDateTime), serviceType: appointment.serviceType, reason: appointment.reason || '' })
    setOpen(true)
  }
  const save = async (event) => {
    event.preventDefault(); setError(''); setMessage('')
    const payload = {
      patientId: isStaff ? Number(form.patientId) : null,
      assignedDoctorId: form.assignedDoctorId ? Number(form.assignedDoctorId) : null,
      appointmentDateTime: new Date(form.appointmentDateTime).toISOString(),
      serviceType: form.serviceType,
      reason: form.reason || null
    }
    try {
      if (editing) await api.put(`/appointments/${editing.id}`, payload)
      else await api.post('/appointments', payload)
      setOpen(false); setMessage(editing ? 'Appointment updated.' : 'Appointment booked.'); await load()
    } catch (err) { setError(errorMessage(err)) }
  }
  const cancel = async (id) => {
    if (!window.confirm('Cancel this appointment?')) return
    try { await api.delete(`/appointments/${id}`); setMessage('Appointment cancelled.'); await load() }
    catch (err) { setError(errorMessage(err)) }
  }
  const checkIn = async (appointment) => {
    let manualUrgency = null
    if (!appointment.hasPreTriage) {
      manualUrgency = window.prompt('No pre-triage assessment exists. Enter Routine, Priority or Urgent after staff assessment:', 'Routine')
      if (!manualUrgency) return
    }
    try {
      await api.post('/queue/check-in', { appointmentId: appointment.id, manualUrgency })
      setMessage(`${appointment.patientName} checked in.`); await load()
    } catch (err) { setError(errorMessage(err)) }
  }

  return <>
    <PageHeader title="Appointments" description="Book, reschedule, cancel and manage clinic appointments without slot conflicts." actions={canCreate && <button className="button button-primary" onClick={openCreate}>New appointment</button>} />
    {message && <Alert type="success">{message}</Alert>}{error && <Alert type="danger">{error}</Alert>}
    <Card title="Appointment schedule">
      {sorted.length ? <div className="table-wrap"><table><thead><tr><th>Date & time</th><th>Patient</th><th>Doctor</th><th>Service</th><th>Status</th><th>Pre-triage</th><th>Actions</th></tr></thead><tbody>
        {sorted.map((item) => <tr key={item.id}><td>{formatDate(item.appointmentDateTime)}</td><td>{item.patientName}</td><td>{item.doctorName || 'Not assigned'}</td><td>{item.serviceType}</td><td><StatusBadge value={item.status} /></td><td>{item.hasPreTriage ? <StatusBadge value="Completed" /> : <StatusBadge value="Pending" />}</td><td><div className="action-row">
          {canCreate && ['Booked', 'CheckedIn'].includes(item.status) && <button className="button button-small button-secondary" onClick={() => openEdit(item)}>Edit</button>}
          {canCreate && !['Completed', 'Cancelled'].includes(item.status) && <button className="button button-small button-danger-soft" onClick={() => cancel(item.id)}>Cancel</button>}
          {isStaff && item.status === 'Booked' && <button className="button button-small button-primary" onClick={() => checkIn(item)}>Check in</button>}
        </div></td></tr>)}
      </tbody></table></div> : <EmptyState title="No appointments" description="Create an appointment to start the clinic workflow." />}
    </Card>

    {open && <Modal title={editing ? 'Reschedule appointment' : 'New appointment'} onClose={() => setOpen(false)}><form className="form-stack" onSubmit={save}>
      {isStaff && <Field label="Patient" required><select value={form.patientId} onChange={(e) => setForm({ ...form, patientId: e.target.value })} required><option value="">Select patient</option>{patients.map((patient) => <option key={patient.id} value={patient.id}>{patient.fullName} — {patient.phone}</option>)}</select></Field>}
      <div className="form-grid two">
        <Field label="Date and time" required><input type="datetime-local" value={form.appointmentDateTime} onChange={(e) => setForm({ ...form, appointmentDateTime: e.target.value })} required /></Field>
        <Field label="Doctor"><select value={form.assignedDoctorId} onChange={(e) => setForm({ ...form, assignedDoctorId: e.target.value })}><option value="">Assign later</option>{doctors.filter((d) => d.isAvailable).map((doctor) => <option key={doctor.id} value={doctor.id}>{doctor.fullName} — {doctor.speciality || 'General Practice'}</option>)}</select></Field>
      </div>
      <Field label="Service" required><select value={form.serviceType} onChange={(e) => setForm({ ...form, serviceType: e.target.value })}><option>General Consultation</option><option>Follow-up Consultation</option><option>Minor Injury Review</option><option>Vaccination Consultation</option><option>Other Clinic Service</option></select></Field>
      <Field label="Reason for visit"><textarea rows="3" value={form.reason} onChange={(e) => setForm({ ...form, reason: e.target.value })} /></Field>
      <div className="modal-actions"><button type="button" className="button button-ghost" onClick={() => setOpen(false)}>Cancel</button><button className="button button-primary">Save appointment</button></div>
    </form></Modal>}
  </>
}

function toLocalInput(value) {
  const date = new Date(value); const offset = date.getTimezoneOffset() * 60000
  return new Date(date.getTime() - offset).toISOString().slice(0, 16)
}
const formatDate = (value) => new Date(value).toLocaleString([], { dateStyle: 'medium', timeStyle: 'short' })
