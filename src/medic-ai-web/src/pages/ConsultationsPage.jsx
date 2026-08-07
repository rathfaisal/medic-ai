import { useEffect, useState } from 'react'
import api, { errorMessage } from '../api/client'
import { Alert, Card, EmptyState, Loading, PageHeader } from '../components/UI'

export default function ConsultationsPage() {
  const [items, setItems] = useState(null)
  const [error, setError] = useState('')
  useEffect(() => { api.get('/consultations/mine').then(({ data }) => setItems(data)).catch((err) => { setError(errorMessage(err)); setItems([]) }) }, [])
  if (!items) return <Loading />
  return <>
    <PageHeader title="Consultation history" description="View consultation and follow-up information recorded by your clinic doctor." />
    {error && <Alert type="danger">{error}</Alert>}
    <Card title={`Consultations (${items.length})`}>{items.length ? <div className="timeline">{items.map((item) => <article key={item.id}><div className="timeline-dot" /><div><strong>{formatDate(item.createdAt)} — {item.doctorName}</strong><p>{item.clinicalNotes}</p>{item.diagnosis && <p><b>Doctor diagnosis:</b> {item.diagnosis}</p>}{item.treatmentPlan && <p><b>Treatment plan:</b> {item.treatmentPlan}</p>}{item.followUpDate && <div className="follow-up">Follow-up date: {item.followUpDate}</div>}</div></article>)}</div> : <EmptyState description="No consultation records have been created." />}</Card>
  </>
}
const formatDate = (value) => new Date(value).toLocaleString([], { dateStyle: 'medium', timeStyle: 'short' })
