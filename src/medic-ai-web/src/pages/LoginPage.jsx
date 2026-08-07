import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useAuth } from '../auth/AuthContext'
import { errorMessage } from '../api/client'
import { Alert, Field } from '../components/UI'

export default function LoginPage() {
  const [form, setForm] = useState({ email: 'patient@medic-ai.local', password: 'Patient123!' })
  const [error, setError] = useState('')
  const [busy, setBusy] = useState(false)
  const { login } = useAuth()
  const navigate = useNavigate()

  const submit = async (event) => {
    event.preventDefault()
    setBusy(true); setError('')
    try { await login(form.email, form.password); navigate('/') }
    catch (err) { setError(errorMessage(err)) }
    finally { setBusy(false) }
  }

  return <div className="auth-page">
    <div className="auth-panel auth-intro">
      <div className="brand large"><div className="brand-mark">M+</div><div><strong>MEDIC-AI</strong><span>Klinik Medic 24-7</span></div></div>
      <h1>Smarter clinic flow with responsible AI assistance.</h1>
      <p>Manage appointments, collect structured symptoms, identify red flags, organise the digital queue and keep doctors in control.</p>
      <div className="safety-box"><strong>Clinical safety first</strong><span>The system provides preliminary urgency guidance only and never provides an AI diagnosis.</span></div>
    </div>
    <div className="auth-panel auth-form-panel">
      <form className="auth-form" onSubmit={submit}>
        <div><div className="eyebrow">Secure access</div><h2>Sign in to MEDIC-AI</h2><p>Use your authorised patient or clinic account.</p></div>
        {error && <Alert type="danger">{error}</Alert>}
        <Field label="Email address" required><input type="email" value={form.email} onChange={(e) => setForm({ ...form, email: e.target.value })} required /></Field>
        <Field label="Password" required><input type="password" value={form.password} onChange={(e) => setForm({ ...form, password: e.target.value })} required /></Field>
        <button className="button button-primary full" disabled={busy}>{busy ? 'Signing in…' : 'Sign in'}</button>
        <p className="auth-link">New patient? <Link to="/register">Create a patient account</Link></p>
        <details className="demo-details"><summary>Demo accounts</summary><p>Admin: admin@medic-ai.local / Admin123!</p><p>Doctor: doctor@medic-ai.local / Doctor123!</p><p>Staff: staff@medic-ai.local / Staff123!</p><p>Patient: patient@medic-ai.local / Patient123!</p></details>
      </form>
    </div>
  </div>
}
