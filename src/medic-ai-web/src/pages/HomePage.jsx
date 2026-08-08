import { Link } from 'react-router-dom'
import { useAuth } from '../auth/AuthContext'

const features = [
  {
    icon: '01',
    title: 'Online appointments',
    text: 'Patients can book, reschedule and manage clinic appointments through one secure interface.'
  },
  {
    icon: '02',
    title: 'AI-assisted pre-triage',
    text: 'Structured symptom information supports preliminary urgency classification before consultation.'
  },
  {
    icon: '03',
    title: 'Smart digital queue',
    text: 'Queue ordering considers confirmed urgency and arrival information while keeping clinic staff in control.'
  },
  {
    icon: '04',
    title: 'Doctor review & override',
    text: 'Doctors can review AI-supported results, change urgency when required and record the reason for every override.'
  },
  {
    icon: '05',
    title: 'Emergency safety layer',
    text: 'Doctor-approved red-flag rules take priority over machine learning and cannot be downgraded by the model.'
  },
  {
    icon: '06',
    title: 'Role-based clinic access',
    text: 'Patients, reception staff, doctors and administrators receive access appropriate to their responsibilities.'
  }
]

export default function HomePage() {
  const { user } = useAuth()

  return (
    <div className="public-home">
      <header className="public-nav">
        <Link className="public-brand" to="/">
          <span className="brand-mark">M+</span>
          <span>
            <strong>MEDIC-AI</strong>
            <small>Klinik Medic 24-7</small>
          </span>
        </Link>

        <nav className="public-nav-links" aria-label="Public navigation">
          <a href="#features">Features</a>
          <a href="#workflow">How it works</a>
          <a href="#safety">Safety</a>
        </nav>

        <div className="public-nav-actions">
          {user ? (
            <Link className="button button-primary" to="/dashboard">Open dashboard</Link>
          ) : (
            <>
              <Link className="button button-ghost public-ghost" to="/login">Sign in</Link>
              <Link className="button button-primary" to="/register">Patient registration</Link>
            </>
          )}
        </div>
      </header>

      <main>
        <section className="public-hero">
          <div className="public-hero-copy">
            <div className="public-kicker">AI-POWERED PATIENT PRE-TRIAGE & CLINIC MANAGEMENT</div>
            <h1>Smarter clinic flow with responsible AI assistance.</h1>
            <p className="public-lead">
              MEDIC-AI helps Klinik Medic 24-7 organise appointments, collect structured
              symptoms before consultation, prioritise the clinic queue and support staff
              with transparent preliminary urgency guidance.
            </p>

            <div className="public-hero-actions">
              {user ? (
                <Link className="button button-primary public-large-button" to="/dashboard">Go to dashboard</Link>
              ) : (
                <>
                  <Link className="button button-primary public-large-button" to="/register">Get started as a patient</Link>
                  <Link className="button button-ghost public-large-button public-ghost" to="/login">Clinic staff sign in</Link>
                </>
              )}
            </div>

            <div className="public-trust-row">
              <span>✓ Doctor remains in control</span>
              <span>✓ Emergency rules override ML</span>
              <span>✓ No AI diagnosis</span>
            </div>
          </div>

          <div className="public-hero-visual" aria-label="MEDIC-AI workflow preview">
            <div className="public-flow-card main-flow">
              <div className="public-flow-head">
                <span className="flow-status-dot"></span>
                <strong>Patient journey</strong>
                <span className="flow-live">SYSTEM FLOW</span>
              </div>

              <div className="flow-step">
                <span>1</span>
                <div><strong>Book appointment</strong><small>Choose an available clinic time</small></div>
              </div>
              <div className="flow-line"></div>
              <div className="flow-step">
                <span>2</span>
                <div><strong>Submit pre-triage</strong><small>Symptoms, duration, severity & red flags</small></div>
              </div>
              <div className="flow-line"></div>
              <div className="flow-step highlight">
                <span>3</span>
                <div><strong>Preliminary urgency</strong><small>Rules + ML with staff review safeguards</small></div>
              </div>
              <div className="flow-line"></div>
              <div className="flow-step">
                <span>4</span>
                <div><strong>Clinic review</strong><small>Queue, doctor review and consultation</small></div>
              </div>
            </div>

            <div className="public-mini-card safety-mini">
              <span className="mini-icon">!</span>
              <div><strong>Clinical safety first</strong><small>Red flags are never downgraded by ML.</small></div>
            </div>

            <div className="public-mini-card role-mini">
              <span className="mini-icon role">4</span>
              <div><strong>Four user roles</strong><small>Patient · Staff · Doctor · Admin</small></div>
            </div>
          </div>
        </section>

        <section className="public-section" id="features">
          <div className="public-section-heading">
            <div className="public-kicker">CORE SYSTEM</div>
            <h2>One platform for the clinic workflow</h2>
            <p>
              MEDIC-AI connects the patient-facing process with the clinic's operational
              workflow instead of treating pre-triage as an isolated AI feature.
            </p>
          </div>

          <div className="public-feature-grid">
            {features.map((feature) => (
              <article className="public-feature-card" key={feature.title}>
                <span className="feature-number">{feature.icon}</span>
                <h3>{feature.title}</h3>
                <p>{feature.text}</p>
              </article>
            ))}
          </div>
        </section>

        <section className="public-workflow-section" id="workflow">
          <div className="public-section-heading left">
            <div className="public-kicker">HOW IT WORKS</div>
            <h2>From patient submission to clinical review</h2>
          </div>

          <div className="public-workflow-grid">
            <article>
              <span>01</span>
              <h3>Patient submits information</h3>
              <p>Appointment and structured symptom information are submitted before consultation.</p>
            </article>
            <article>
              <span>02</span>
              <h3>Safety rules run first</h3>
              <p>Doctor-approved emergency red flags are checked before any machine-learning result is used.</p>
            </article>
            <article>
              <span>03</span>
              <h3>AI supports prioritisation</h3>
              <p>Non-emergency cases receive preliminary urgency guidance with confidence and review safeguards.</p>
            </article>
            <article>
              <span>04</span>
              <h3>Clinic staff stay in control</h3>
              <p>Staff manage the queue and doctors can review or override the preliminary urgency classification.</p>
            </article>
          </div>
        </section>

        <section className="public-safety-section" id="safety">
          <div className="public-safety-copy">
            <div className="public-kicker light">RESPONSIBLE AI</div>
            <h2>Designed to support healthcare staff — not replace them.</h2>
            <p>
              MEDIC-AI provides preliminary urgency guidance only. It does not diagnose
              patients, prescribe treatment or replace professional clinical assessment.
            </p>
          </div>

          <div className="public-safety-points">
            <div><strong>Rule-first emergency protection</strong><span>Known red flags take priority over ML.</span></div>
            <div><strong>Doctor override</strong><span>Clinical judgement remains the final authority.</span></div>
            <div><strong>Auditability</strong><span>Important staff actions and overrides can be recorded.</span></div>
            <div><strong>FYP prototype data</strong><span>Development and testing use fictional or synthetic records.</span></div>
          </div>
        </section>

        <section className="public-cta">
          <div>
            <div className="public-kicker">READY TO CONTINUE?</div>
            <h2>Access the MEDIC-AI prototype</h2>
            <p>Patients can create an account, while authorised clinic users can sign in with their assigned role.</p>
          </div>
          <div className="public-cta-actions">
            {user ? (
              <Link className="button button-primary public-large-button" to="/dashboard">Open dashboard</Link>
            ) : (
              <>
                <Link className="button button-primary public-large-button" to="/register">Create patient account</Link>
                <Link className="button button-ghost public-large-button public-ghost" to="/login">Sign in</Link>
              </>
            )}
          </div>
        </section>
      </main>

      <footer className="public-footer">
        <div className="public-brand">
          <span className="brand-mark">M+</span>
          <span><strong>MEDIC-AI</strong><small>Klinik Medic 24-7</small></span>
        </div>
        <p>FYP prototype — AI guidance is preliminary and is not a medical diagnosis.</p>
      </footer>
    </div>
  )
}
