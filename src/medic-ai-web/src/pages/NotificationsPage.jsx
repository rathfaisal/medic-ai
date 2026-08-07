import { useEffect, useState } from 'react'
import api, { errorMessage } from '../api/client'
import { Alert, Card, EmptyState, Loading, PageHeader, StatusBadge } from '../components/UI'

export default function NotificationsPage() {
  const [items, setItems] = useState(null)
  const [error, setError] = useState('')
  const load = async () => { try { const { data } = await api.get('/notifications'); setItems(data) } catch (err) { setError(errorMessage(err)); setItems([]) } }
  useEffect(() => { load() }, [])
  if (!items) return <Loading />
  const markRead = async (id) => { try { await api.post(`/notifications/${id}/read`); await load() } catch (err) { setError(errorMessage(err)) } }
  return <>
    <PageHeader title="Notifications" description="In-app appointment, queue and safety messages for your authorised account." />
    {error && <Alert type="danger">{error}</Alert>}
    <Card title={`Messages (${items.length})`}>{items.length ? <div className="notification-list">{items.map((item) => <article className={item.isRead ? 'notification read' : 'notification unread'} key={item.id}><div className="notification-dot" /><div><p>{item.message}</p><small>{formatDate(item.createdAt)} · {item.channel}</small></div><StatusBadge value={item.deliveryStatus} />{!item.isRead && <button className="button button-small button-secondary" onClick={() => markRead(item.id)}>Mark read</button>}</article>)}</div> : <EmptyState description="You have no notifications." />}</Card>
  </>
}
const formatDate = (value) => new Date(value).toLocaleString([], { dateStyle: 'medium', timeStyle: 'short' })
