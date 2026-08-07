import { useEffect, useMemo, useState } from 'react'
import { useSearchParams } from 'react-router-dom'
import api, { errorMessage } from '../api/client'
import { useAuth } from '../auth/AuthContext'
import { Alert, Card, EmptyState, Field, Loading, PageHeader, StatusBadge } from '../components/UI'

const consultationInitial = { appointmentId: '', clinicalNotes: '', diagnosis: '', treatmentPlan: '', followUpDate: '' }

export default function PatientReviewPage() {
  const { user } = useAuth()
  const [params, setParams] = useSearchParams()
  const [patients, setPatients] = useState([])
  const [search, setSearch] = useState('')
  const [selectedId, setSelectedId] = useState(params.get('patientId') || '')
  const [review, setReview] = useState(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')
  const [message, setMessage] = useState('')
  const [override, setOverride] = useState({ assessmentId: '', urgency: 'Priority', reason: '' })
  const [consultation, setConsultation] = useState(consultationInitial)

  const loadPatients = async (term = '') => {
    try {
      const { data } = await api.get('/patients', { params: { search: term || undefined, take: 100 } })
      setPatients(data)
    } catch (err) { setError(errorMessage(err)) }
  }

  const loadReview = async (patientId) => {
    if (!patientId) { setReview(null); return }
    setLoading(true); setError(''); setMessage('')
    try {
      const { data } = await api.get(`/doctor/patients/${patientId}/review`)
      setReview(data)
      const openAppointment = data.appointments.find((x) => !['Completed', 'Cancelled', 'NoShow'].includes(x.status))
      setConsultation({ ...consultationInitial, appointmentId: openAppointment?.id || '' })
      const latestAssessment = data.assessments.find((x) => x.reviewRequired) || data.assessments[0]
      setOverride({ assessmentId: latestAssessment?.assessmentId || '', urgency: latestAssessment?.effectiveUrgency || 'Priority', reason: '' })
      setParams({ patientId: String(patientId) })
    } catch (err) { setError(errorMessage(err)); setReview(null) }
    finally { setLoading(false) }
  }

  useEffect(() => { loadPatients(); if (selectedId) loadReview(selectedId) }, [])

  const submitSearch = async (event) => { event.preventDefault(); await loadPatients(search) }
  const choosePatient = (id) => { setSelectedId(String(id)); loadReview(id) }

  const submitOverride = async (event) => {
    event.preventDefault(); setError(''); setMessage('')
    try {
      await api.post(`/doctor/assessments/${override.assessmentId}/override`, { urgency: override.urgency, reason: override.reason })
      setMessage('Urgency override saved, queue priority recalculated and audit log created.')
      await loadReview(selectedId)
    } catch (err) { setError(errorMessage(err)) }
  }

  const submitConsultation = async (event) => {
    event.preventDefault(); setError(''); setMessage('')
    try {
      await api.post('/doctor/consultations', {
        appointmentId: consultation.appointmentId ? Number(consultation.appointmentId) : null,
        patientId: Number(selectedId),
        clinicalNotes: consultation.clinicalNotes,
        diagnosis: consultation.diagnosis || null,
        treatmentPlan: consultation.treatmentPlan || null,
        followUpDate: consultation.followUpDate || null
      })
      setMessage('Consultation record saved successfully.')
      setConsultation(consultationInitial)
      await loadReview(selectedId)
    } catch (err) { setError(errorMessage(err)) }
  }

  const latestAssessment = useMemo(() => review?.assessments?.[0], [review])

  return <>
    <PageHeader title="Patient review" description="Review patient-reported symptoms, preliminary urgency, appointment history and doctor-controlled clinical records." />
    {message && <Alert type="success">{message}</Alert>}{error && <Alert type="danger">{error}</Alert>}
    <div className="content-grid review-grid">
      <Card title="Find a patient" className="patient-search-card">
        <form className="search-row" onSubmit={submitSearch}><input value={search} onChange={(e) => setSearch(e.target.value)} placeholder="Name, email or phone" /><button className="button button-secondary">Search</button></form>
        <div className="patient-list">{patients.map((patient) => <button key={patient.id} className={`patient-list-item ${String(patient.id) === String(selectedId) ? 'selected' : ''}`} onClick={() => choosePatient(patient.id)}><strong>{patient.fullName}</strong><span>{patient.phone}</span><small>{patient.email}</small></button>)}</div>
      </Card>
      <div className="review-main">
        {loading ? <Loading /> : !review ? <EmptyState title="Select a patient" description="Choose a patient to view their appointment, triage and consultation history." /> : <>
          <Card title="Patient overview">
            <div className="patient-banner"><div className="avatar">{review.patient.fullName.slice(0, 1)}</div><div><h2>{review.patient.fullName}</h2><p>{review.patient.email} · {review.patient.phone}</p></div>{latestAssessment && <StatusBadge value={latestAssessment.effectiveUrgency} />}</div>
            <div className="detail-grid"><div><span>Date of birth</span><strong>{review.patient.dateOfBirth}</strong></div><div><span>Gender</span><strong>{review.patient.gender || 'Not provided'}</strong></div><div><span>Allergies</span><strong>{review.patient.allergies || 'None recorded'}</strong></div><div><span>Existing conditions</span><strong>{review.patient.existingConditions || 'None recorded'}</strong></div></div>
          </Card>

          <Card title="Pre-triage assessments">
            {review.assessments.length ? <div className="assessment-list">{review.assessments.map((item) => <article className="assessment-item" key={item.assessmentId}><div className="assessment-head"><div><strong>{formatDate(item.assessedAt)}</strong><span>Assessment #{item.assessmentId} · {sourceLabel(item.source)}</span></div><div className="tag-row"><StatusBadge value={item.effectiveUrgency} />{item.reviewRequired && <span className="review-flag">Review required</span>}</div></div><p>{item.explanation}</p><small>{item.guidance}</small>{item.overriddenUrgency && <Alert type="info">Doctor override: {item.overriddenUrgency}. Reason: {item.overrideReason}</Alert>}</article>)}</div> : <EmptyState description="No pre-triage assessments are recorded." />}
          </Card>

          {user.role === 'Doctor' && review.assessments.length > 0 && <Card title="Doctor urgency override">
            <form className="form-stack" onSubmit={submitOverride}>
              <div className="form-grid three"><Field label="Assessment"><select value={override.assessmentId} onChange={(e) => setOverride({ ...override, assessmentId: e.target.value })}>{review.assessments.map((x) => <option key={x.assessmentId} value={x.assessmentId}>#{x.assessmentId} — {x.effectiveUrgency}</option>)}</select></Field><Field label="Final urgency"><select value={override.urgency} onChange={(e) => setOverride({ ...override, urgency: e.target.value })}><option>Routine</option><option>Priority</option><option>Urgent</option><option>EmergencyWarning</option></select></Field><div className="field-action"><button className="button button-primary">Save override</button></div></div>
              <Field label="Clinical reason" required><textarea rows="3" minLength="5" value={override.reason} onChange={(e) => setOverride({ ...override, reason: e.target.value })} required /></Field>
              <Alert type="warning">Changing urgency updates any linked queue entry. Emergency-warning cases are removed from the normal queue.</Alert>
            </form>
          </Card>}

          {user.role === 'Doctor' && <Card title="Record consultation">
            <form className="form-stack" onSubmit={submitConsultation}>
              <Field label="Related appointment"><select value={consultation.appointmentId} onChange={(e) => setConsultation({ ...consultation, appointmentId: e.target.value })}><option value="">No appointment / walk-in</option>{review.appointments.map((x) => <option key={x.id} value={x.id}>#{x.id} — {formatDate(x.appointmentDateTime)} — {x.status}</option>)}</select></Field>
              <Field label="Clinical notes" required><textarea rows="5" minLength="5" value={consultation.clinicalNotes} onChange={(e) => setConsultation({ ...consultation, clinicalNotes: e.target.value })} required /></Field>
              <div className="form-grid two"><Field label="Doctor diagnosis (not AI-generated)"><textarea rows="3" value={consultation.diagnosis} onChange={(e) => setConsultation({ ...consultation, diagnosis: e.target.value })} /></Field><Field label="Treatment plan"><textarea rows="3" value={consultation.treatmentPlan} onChange={(e) => setConsultation({ ...consultation, treatmentPlan: e.target.value })} /></Field></div>
              <Field label="Follow-up date"><input type="date" value={consultation.followUpDate} onChange={(e) => setConsultation({ ...consultation, followUpDate: e.target.value })} /></Field>
              <div><button className="button button-success">Save consultation</button></div>
            </form>
          </Card>}

          <Card title="Consultation history">
            {review.consultations.length ? <div className="timeline">{review.consultations.map((item) => <article key={item.id}><div className="timeline-dot" /><div><strong>{formatDate(item.createdAt)} — {item.doctorName}</strong><p>{item.clinicalNotes}</p>{item.diagnosis && <p><b>Doctor diagnosis:</b> {item.diagnosis}</p>}{item.treatmentPlan && <p><b>Treatment:</b> {item.treatmentPlan}</p>}{item.followUpDate && <small>Follow-up: {item.followUpDate}</small>}</div></article>)}</div> : <EmptyState description="No consultation records are available." />}
          </Card>
        </>}
      </div>
    </div>
  </>
}

const formatDate = (value) => new Date(value).toLocaleString([], { dateStyle: 'medium', timeStyle: 'short' })

const sourceLabel = (value) => ({ RuleEngine: 'Rule engine', MachineLearning: 'ML.NET', ManualFallback: 'Manual fallback' }[value] || value || 'Unknown')
