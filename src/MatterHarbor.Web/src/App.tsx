import { FormEvent, useEffect, useState } from 'react'
import {
  Link,
  Navigate,
  Outlet,
  Route,
  Routes,
  useNavigate,
  useParams,
} from 'react-router-dom'
import { api, CaseItem, CasePriority } from './api'
import { PersonaContext, personaOptions, usePersona } from './persona'

function Layout() {
  const [persona, setPersonaState] = useState(() => localStorage.getItem('matterharbor-persona') ?? 'alex')
  const setPersona = (next: string) => {
    localStorage.setItem('matterharbor-persona', next)
    setPersonaState(next)
  }

  return (
    <PersonaContext.Provider value={{ persona, setPersona }}>
      <header className="app-header">
        <Link className="brand" to="/cases">MatterHarbor</Link>
        <label>
          Development persona
          <select value={persona} onChange={(event) => setPersona(event.target.value)}>
            {personaOptions.map((option) => (
              <option key={option.key} value={option.key}>
                {option.name} — {option.organization}
              </option>
            ))}
          </select>
        </label>
      </header>
      <main className="container"><Outlet /></main>
    </PersonaContext.Provider>
  )
}

function CaseListPage() {
  const { persona } = usePersona()
  const [cases, setCases] = useState<CaseItem[]>([])
  const [error, setError] = useState('')

  useEffect(() => {
    let active = true
    setError('')
    api.listCases(persona)
      .then((items) => active && setCases(items))
      .catch((reason: unknown) => active && setError(toMessage(reason)))
    return () => { active = false }
  }, [persona])

  return (
    <>
      <div className="page-heading">
        <div><h1>Cases</h1><p>Cases for the selected development organization.</p></div>
        <Link className="button" to="/cases/new">Create case</Link>
      </div>
      {error && <p className="error" role="alert">{error}</p>}
      {cases.length === 0 && !error ? <p>No cases yet.</p> : (
        <div className="case-list">
          {cases.map((item) => (
            <Link className="case-card" key={item.id} to={`/cases/${item.id}`}>
              <span className="case-number">{item.caseNumber}</span>
              <strong>{item.title}</strong>
              <span>{item.priority} · {item.status}</span>
            </Link>
          ))}
        </div>
      )}
    </>
  )
}

export function CreateCasePage() {
  const { persona } = usePersona()
  const navigate = useNavigate()
  const [title, setTitle] = useState('')
  const [description, setDescription] = useState('')
  const [priority, setPriority] = useState<CasePriority>('Normal')
  const [idempotencyKey] = useState(() => crypto.randomUUID())
  const [error, setError] = useState('')
  const [submitting, setSubmitting] = useState(false)

  const submit = async (event: FormEvent) => {
    event.preventDefault()
    setSubmitting(true)
    setError('')
    try {
      const created = await api.createCase(
        persona,
        { title, description, priority, assignedUserId: null },
        idempotencyKey,
      )
      navigate(`/cases/${created.id}`)
    } catch (reason) {
      setError(toMessage(reason))
      setSubmitting(false)
    }
  }

  return (
    <section className="form-card">
      <h1>Create case</h1>
      {error && <p className="error" role="alert">{error}</p>}
      <form onSubmit={submit}>
        <label>Title<input value={title} onChange={(event) => setTitle(event.target.value)} required maxLength={200} /></label>
        <label>Description<textarea value={description} onChange={(event) => setDescription(event.target.value)} required maxLength={4000} rows={7} /></label>
        <label>Priority<select value={priority} onChange={(event) => setPriority(event.target.value as CasePriority)}>
          <option>Low</option><option>Normal</option><option>High</option><option>Critical</option>
        </select></label>
        <div className="actions"><Link to="/cases">Cancel</Link><button disabled={submitting} type="submit">{submitting ? 'Creating…' : 'Create case'}</button></div>
      </form>
    </section>
  )
}

function CaseDetailsPage() {
  const { persona } = usePersona()
  const { id = '' } = useParams()
  const [item, setItem] = useState<CaseItem | null>(null)
  const [error, setError] = useState('')

  useEffect(() => {
    let active = true
    api.getCase(persona, id)
      .then((value) => active && setItem(value))
      .catch((reason: unknown) => active && setError(toMessage(reason)))
    return () => { active = false }
  }, [id, persona])

  if (error) return <p className="error" role="alert">{error}</p>
  if (!item) return <p>Loading case…</p>

  return (
    <article className="details-card">
      <Link to="/cases">← All cases</Link>
      <span className="case-number">{item.caseNumber}</span>
      <h1>{item.title}</h1>
      <dl><div><dt>Status</dt><dd>{item.status}</dd></div><div><dt>Priority</dt><dd>{item.priority}</dd></div><div><dt>Version</dt><dd>{item.version}</dd></div></dl>
      <h2>Description</h2><p className="description">{item.description}</p>
    </article>
  )
}

function toMessage(reason: unknown): string {
  return reason instanceof Error ? reason.message : 'Something went wrong.'
}

export function AppRoutes() {
  return (
    <Routes>
      <Route element={<Layout />}>
        <Route index element={<Navigate to="/cases" replace />} />
        <Route path="cases" element={<CaseListPage />} />
        <Route path="cases/new" element={<CreateCasePage />} />
        <Route path="cases/:id" element={<CaseDetailsPage />} />
      </Route>
    </Routes>
  )
}
