import { useEffect, useState } from 'react'
import api, { errorMessage } from '../api/client'
import { Alert, Card, EmptyState, Field, Loading, PageHeader, StatusBadge } from '../components/UI'

const initial = {
  appointmentId: '', mainSymptom: '', durationValue: 1, durationUnit: 'Days', painSeverity: 0,
  additionalSymptoms: '', consentConfirmed: false, fever: false, vomiting: false, dizziness: false,
  worseningSymptoms: false, severeBreathingDifficulty: false, chestPain: false,
  lossOfConsciousness: false, majorBleeding: false, seizure: false,
  severeAllergicReaction: false, suddenSevereWeakness: false
}

export default function PreTriagePage() {
  const [appointments, setAppointments] = useState(null)
  const [form, setForm] = useState(initial)
  const [result, setResult] = useState(null)
  const [error, setError] = useState('')
  const [busy, setBusy] = useState(false)

  useEffect(() => { api.get('/appointments').then(({ data }) => setAppointments(data.filter((x) => ['Booked', 'CheckedIn'].includes(x.status) && !x.hasPreTriage))).catch((err) => { setError(errorMessage(err)); setAppointments([]) }) }, [])
  if (!appointments) return <Loading />

  const toggle = (key) => (event) => setForm({ ...form, [key]: event.target.checked })
  const submit = async (event) => {
    event.preventDefault(); setBusy(true); setError(''); setResult(null)
    try {
      const payload = { ...form, appointmentId: form.appointmentId ? Number(form.appointmentId) : null, durationValue: Number(form.durationValue), painSeverity: Number(form.painSeverity) }
      const { data } = await api.post('/triage', payload)
      setResult(data)
    } catch (err) { setError(errorMessage(err)) }
    finally { setBusy(false) }
  }

  return <>
    <PageHeader title="Patient pre-triage" description="Provide structured symptom information before consultation. The result is preliminary and non-diagnostic." />
    <Alert type="warning"><strong>Emergency warning:</strong> If you are currently experiencing severe breathing difficulty, loss of consciousness, major bleeding, a seizure, severe allergic reaction or another life-threatening condition, seek immediate professional medical or emergency assistance.</Alert>
    {error && <Alert type="danger">{error}</Alert>}
    {result ? <Result result={result} onNew={() => { setResult(null); setForm(initial) }} /> : <Card title="Symptom information">
      {appointments.length === 0 && <Alert type="info">No eligible appointment is available. You may submit without selecting an appointment, or book a new appointment first.</Alert>}
      <form className="form-stack" onSubmit={submit}>
        <Field label="Related appointment"><select value={form.appointmentId} onChange={(e) => setForm({ ...form, appointmentId: e.target.value })}><option value="">Walk-in / no appointment selected</option>{appointments.map((item) => <option key={item.id} value={item.id}>{formatDate(item.appointmentDateTime)} — {item.serviceType}</option>)}</select></Field>
        <Field label="Main symptom" required hint="Use a brief description such as fever, cough, abdominal pain or dizziness."><input value={form.mainSymptom} onChange={(e) => setForm({ ...form, mainSymptom: e.target.value })} required maxLength="255" /></Field>
        <div className="form-grid three">
          <Field label="Duration" required><input type="number" min="1" max="3650" value={form.durationValue} onChange={(e) => setForm({ ...form, durationValue: e.target.value })} required /></Field>
          <Field label="Duration unit"><select value={form.durationUnit} onChange={(e) => setForm({ ...form, durationUnit: e.target.value })}><option>Hours</option><option>Days</option><option>Weeks</option><option>Months</option></select></Field>
          <Field label={`Pain severity: ${form.painSeverity}/10`}><input type="range" min="0" max="10" value={form.painSeverity} onChange={(e) => setForm({ ...form, painSeverity: e.target.value })} /></Field>
        </div>
        <Field label="Additional symptoms or context"><textarea rows="4" value={form.additionalSymptoms} onChange={(e) => setForm({ ...form, additionalSymptoms: e.target.value })} /></Field>

        <fieldset><legend>Common symptom indicators</legend><div className="checkbox-grid">
          {['fever', 'vomiting', 'dizziness', 'worseningSymptoms'].map((key) => <label className="check" key={key}><input type="checkbox" checked={form[key]} onChange={toggle(key)} /><span>{label(key)}</span></label>)}
        </div></fieldset>

        <fieldset className="danger-fieldset"><legend>Red-flag safety questions</legend><p>Select every item that applies. These rules run before the machine-learning model.</p><div className="checkbox-grid two">
          {['severeBreathingDifficulty', 'chestPain', 'lossOfConsciousness', 'majorBleeding', 'seizure', 'severeAllergicReaction', 'suddenSevereWeakness'].map((key) => <label className="check" key={key}><input type="checkbox" checked={form[key]} onChange={toggle(key)} /><span>{label(key)}</span></label>)}
        </div></fieldset>

        <label className="consent-box"><input type="checkbox" checked={form.consentConfirmed} onChange={toggle('consentConfirmed')} required /><span>I consent to submit this information for preliminary urgency assessment and clinic-management purposes. I understand that the AI result is not a diagnosis and may be changed by clinic staff or a doctor.</span></label>
        <div><button className="button button-primary" disabled={busy}>{busy ? 'Assessing…' : 'Submit pre-triage assessment'}</button></div>
      </form>
    </Card>}
  </>
}

function Result({ result, onNew }) {
  return <Card title="Preliminary urgency guidance" className={result.isEmergency ? 'emergency-card' : ''}>
    <div className="triage-result"><div className="triage-icon">{result.isEmergency ? '!' : '✓'}</div><div><div className="eyebrow">Preliminary category</div><h2><StatusBadge value={result.effectiveUrgency} /></h2><p>{result.guidance}</p></div></div>
    {result.isEmergency && <Alert type="danger"><strong>Do not wait for the normal clinic queue.</strong> Seek immediate professional medical or emergency assistance. A clinic alert has also been created, but the system does not guarantee emergency-service contact.</Alert>}
    <div className="result-details"><div><span>Assessment source</span><strong>{sourceLabel(result.source)}</strong></div><div><span>Model confidence</span><strong>{result.source === 'MachineLearning' && result.confidence != null ? `${Math.round(result.confidence * 100)}%` : 'Not applicable'}</strong></div><div><span>Staff review</span><strong>{result.reviewRequired ? 'Required' : 'May review'}</strong></div><div><span>Assessment time</span><strong>{formatDate(result.assessedAt)}</strong></div></div>
    <h3>Explanation</h3><p>{result.explanation}</p>
    {result.ruleFlags?.length > 0 && <div className="tag-row">{result.ruleFlags.map((flag) => <span className="tag danger" key={flag}>{flag}</span>)}</div>}
    {result.source === 'MachineLearning' && result.lowConfidence && <Alert type="warning">Low model confidence triggered automatic escalation by one urgency level and a staff-review flag.</Alert>}
    {result.source === 'ManualFallback' && <Alert type="warning">The AI model was unavailable. No machine-learning prediction was produced; the temporary category exists only to route this submission for manual review.</Alert>}
    <div className="modal-actions"><button className="button button-secondary" onClick={onNew}>Submit another assessment</button></div>
  </Card>
}

const labels = {
  fever: 'Fever', vomiting: 'Vomiting', dizziness: 'Dizziness', worseningSymptoms: 'Symptoms are worsening',
  severeBreathingDifficulty: 'Severe breathing difficulty', chestPain: 'Chest pain or severe chest discomfort',
  lossOfConsciousness: 'Loss of consciousness', majorBleeding: 'Major or uncontrolled bleeding', seizure: 'Seizure',
  severeAllergicReaction: 'Severe allergic reaction', suddenSevereWeakness: 'Sudden severe weakness'
}
const label = (key) => labels[key] || key
const formatDate = (value) => new Date(value).toLocaleString([], { dateStyle: 'medium', timeStyle: 'short' })

const sourceLabel = (value) => ({ RuleEngine: 'Red-flag rule engine', MachineLearning: 'ML.NET model', ManualFallback: 'Manual-review fallback' }[value] || value || 'Unknown')
