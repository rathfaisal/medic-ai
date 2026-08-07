import { useEffect, useState } from 'react'
import api, { errorMessage } from '../api/client'
import { Alert, Card, EmptyState, Field, Loading, Modal, PageHeader, StatusBadge } from '../components/UI'

const empty = { email: '', password: '', role: 'Staff', fullName: '', phone: '', position: 'Clinic Staff', registrationNo: '', speciality: '' }

export default function AdminUsersPage() {
  const [users, setUsers] = useState(null)
  const [form, setForm] = useState(empty)
  const [open, setOpen] = useState(false)
  const [error, setError] = useState('')
  const [message, setMessage] = useState('')
  const load = async () => { try { const { data } = await api.get('/admin/users'); setUsers(data) } catch (err) { setError(errorMessage(err)); setUsers([]) } }
  useEffect(() => { load() }, [])
  if (!users) return <Loading />

  const create = async (event) => {
    event.preventDefault(); setError(''); setMessage('')
    try { await api.post('/admin/users', { ...form, phone: form.phone || null, position: form.position || null, registrationNo: form.registrationNo || null, speciality: form.speciality || null }); setOpen(false); setForm(empty); setMessage('User account created.'); await load() }
    catch (err) { setError(errorMessage(err)) }
  }

  const update = async (item, changes) => {
    try {
      await api.patch(`/admin/users/${item.id}`, { status: changes.status ?? item.status, role: changes.role ?? item.role, isDoctorAvailable: changes.isDoctorAvailable ?? null })
      setMessage('User account updated.'); setError(''); await load()
    } catch (err) { setError(errorMessage(err)) }
  }

  return <>
    <PageHeader title="Users and roles" description="Create authorised clinic accounts and control account status, roles and doctor availability." actions={<button className="button button-primary" onClick={() => setOpen(true)}>Create staff account</button>} />
    {message && <Alert type="success">{message}</Alert>}{error && <Alert type="danger">{error}</Alert>}
    <Card title={`Registered users (${users.length})`}>
      {users.length ? <div className="table-wrap"><table><thead><tr><th>User</th><th>Role</th><th>Status</th><th>Last login</th><th>Created</th><th>Actions</th></tr></thead><tbody>{users.map((item) => <tr key={item.id}><td><strong>{item.displayName}</strong><small>{item.email}</small></td><td><StatusBadge value={item.role} /></td><td><StatusBadge value={item.status} /></td><td>{item.lastLoginAt ? formatDate(item.lastLoginAt) : 'Never'}</td><td>{formatDate(item.createdAt)}</td><td><div className="action-row"><button className="button button-small button-secondary" onClick={() => update(item, { status: item.status === 'Active' ? 'Inactive' : 'Active' })}>{item.status === 'Active' ? 'Disable' : 'Activate'}</button>{['Staff', 'Admin'].includes(item.role) && <button className="button button-small button-secondary" onClick={() => update(item, { role: item.role === 'Admin' ? 'Staff' : 'Admin' })}>{item.role === 'Admin' ? 'Make staff' : 'Make admin'}</button>}{item.role === 'Doctor' && <button className="button button-small button-secondary" onClick={() => update(item, { isDoctorAvailable: !item.isDoctorAvailable })}>{item.isDoctorAvailable ? 'Set unavailable' : 'Set available'}</button>}</div></td></tr>)}</tbody></table></div> : <EmptyState description="No users are registered." />}
    </Card>
    {open && <Modal title="Create clinic user" onClose={() => setOpen(false)}><form className="form-stack" onSubmit={create}>
      <div className="form-grid two"><Field label="Full name" required><input value={form.fullName} onChange={(e) => setForm({ ...form, fullName: e.target.value })} required /></Field><Field label="Role" required><select value={form.role} onChange={(e) => setForm({ ...form, role: e.target.value })}><option>Staff</option><option>Doctor</option><option>Admin</option></select></Field><Field label="Email" required><input type="email" value={form.email} onChange={(e) => setForm({ ...form, email: e.target.value })} required /></Field><Field label="Temporary password" required><input type="password" minLength="8" value={form.password} onChange={(e) => setForm({ ...form, password: e.target.value })} required /></Field><Field label="Phone"><input value={form.phone} onChange={(e) => setForm({ ...form, phone: e.target.value })} /></Field>{form.role !== 'Doctor' && <Field label="Position"><input value={form.position} onChange={(e) => setForm({ ...form, position: e.target.value })} /></Field>}{form.role === 'Doctor' && <><Field label="Registration number" required><input value={form.registrationNo} onChange={(e) => setForm({ ...form, registrationNo: e.target.value })} required /></Field><Field label="Speciality"><input value={form.speciality} onChange={(e) => setForm({ ...form, speciality: e.target.value })} /></Field></>}</div>
      <div className="modal-actions"><button type="button" className="button button-ghost" onClick={() => setOpen(false)}>Cancel</button><button className="button button-primary">Create account</button></div>
    </form></Modal>}
  </>
}
const formatDate = (value) => new Date(value).toLocaleString([], { dateStyle: 'medium', timeStyle: 'short' })
