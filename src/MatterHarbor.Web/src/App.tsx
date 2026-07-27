import { FormEvent, ReactNode, useEffect, useState } from 'react'
import {
  Link,
  Redirect,
  Route,
  Switch,
  useLocation,
  useParams,
} from 'wouter'
import { api, ApiError, CaseItem, CasePriority, CaseStatus } from './api'
import { PersonaContext, personaOptions, usePersona } from './persona'

function Layout({ children }: { children: ReactNode }) {
  const [, navigate] = useLocation()
  const [persona, setPersonaState] = useState(() => localStorage.getItem('matterharbor-persona') ?? 'alex')
  const setPersona = (next: string) => {
    localStorage.setItem('matterharbor-persona', next)
    setPersonaState(next)
    navigate('/cases')
  }

  return (
    <PersonaContext.Provider value={{ persona, setPersona }}>
      <header className="app-header">
        <Link className="brand" href="/cases">MatterHarbor</Link>
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
      <main className="container">{children}</main>
    </PersonaContext.Provider>
  )
}

function CaseListPage() {
  const { persona } = usePersona()
  const [cases, setCases] = useState<CaseItem[]>([])
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(true)
  const [reloadToken, setReloadToken] = useState(0)

  useEffect(() => {
    let active = true
    setError('')
    setLoading(true)
    api.listCases(persona)
      .then((items) => {
        if (active) setCases(items)
      })
      .catch((reason: unknown) => {
        if (active) setError(toMessage(reason))
      })
      .finally(() => {
        if (active) setLoading(false)
      })
    return () => { active = false }
  }, [persona, reloadToken])

  return (
    <>
      <div className="page-heading">
        <div><h1>Cases</h1><p>Cases for the selected development organization.</p></div>
        <Link className="button" href="/cases/new">Create case</Link>
      </div>
      {loading && <p className="status" role="status">Loading cases…</p>}
      {error && <div className="error" role="alert"><p>{error}</p><button type="button" onClick={() => setReloadToken((value) => value + 1)}>Try again</button></div>}
      {!loading && cases.length === 0 && !error ? <p>No cases yet.</p> : (
        <div className="case-list">
          {cases.map((item) => (
            <Link className="case-card" key={item.id} href={`/cases/${item.id}`}>
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
  const [, navigate] = useLocation()
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
      <form onSubmit={submit} aria-busy={submitting}>
        <label>Title<input value={title} onChange={(event) => setTitle(event.target.value)} required maxLength={200} /></label>
        <label>Description<textarea value={description} onChange={(event) => setDescription(event.target.value)} required maxLength={4000} rows={7} /></label>
        <label>Priority<select value={priority} onChange={(event) => setPriority(event.target.value as CasePriority)}>
          <option>Low</option><option>Normal</option><option>High</option><option>Critical</option>
        </select></label>
        <div className="actions"><Link href="/cases">Cancel</Link><button disabled={submitting} type="submit">{submitting ? 'Creating…' : 'Create case'}</button></div>
      </form>
    </section>
  )
}

function CaseDetailsPage() {
  const { persona } = usePersona()
  const { id = '' } = useParams<{ id: string }>()
  const [item, setItem] = useState<CaseItem | null>(null)
  const [error, setError] = useState('')
  const [reloadToken, setReloadToken] = useState(0)
  const [nextStatus, setNextStatus] = useState<CaseStatus>('New')
  const [saving, setSaving] = useState(false)
  const [conflict, setConflict] = useState(false)

  useEffect(() => {
    let active = true
    setError('')
    setItem(null)
    setConflict(false)
    api.getCase(persona, id)
      .then((value) => {
        if (active) {
          setItem(value)
          setNextStatus(value.status)
        }
      })
      .catch((reason: unknown) => {
        if (active) setError(toMessage(reason))
      })
    return () => { active = false }
  }, [id, persona, reloadToken])

  const updateStatus = async (event: FormEvent) => {
    event.preventDefault()
    if (!item) return
    setSaving(true)
    setError('')
    setConflict(false)
    try {
      const updated = await api.changeCaseStatus(persona, item.id, nextStatus, item.version)
      setItem(updated)
      setNextStatus(updated.status)
    } catch (reason) {
      if (reason instanceof ApiError && reason.status === 409) {
        setConflict(true)
      } else {
        setError(toMessage(reason))
      }
    } finally {
      setSaving(false)
    }
  }

  if (error && !item) return <div className="error" role="alert"><p>{error}</p><button type="button" onClick={() => setReloadToken((value) => value + 1)}>Try again</button></div>
  if (!item) return <p className="status" role="status">Loading case…</p>

  return (
    <article className="details-card">
      <Link href="/cases">← All cases</Link>
      <span className="case-number">{item.caseNumber}</span>
      <h1>{item.title}</h1>
      <dl><div><dt>Status</dt><dd>{item.status}</dd></div><div><dt>Priority</dt><dd>{item.priority}</dd></div><div><dt>Version</dt><dd>{item.version}</dd></div></dl>
      {error && <p className="error" role="alert">{error}</p>}
      {conflict && <div className="conflict" role="alert"><p>This case changed while you were editing. Reload the latest version before trying again.</p><button type="button" onClick={() => setReloadToken((value) => value + 1)}>Reload case</button></div>}
      <form className="status-form" onSubmit={updateStatus} aria-busy={saving}>
        <label>Status<select value={nextStatus} onChange={(event) => setNextStatus(event.target.value as CaseStatus)}>
          <option>New</option><option>InProgress</option><option>Resolved</option><option>Closed</option>
        </select></label>
        <button disabled={saving || nextStatus === item.status} type="submit">{saving ? 'Updating…' : 'Update status'}</button>
      </form>
      <h2>Description</h2><p className="description">{item.description}</p>
    </article>
  )
}

function toMessage(reason: unknown): string {
  return reason instanceof Error ? reason.message : 'Something went wrong.'
}

export function AppRoutes() {
  return (
    <Layout>
      <Switch>
        <Route path="/cases/new" component={CreateCasePage} />
        <Route path="/cases/:id" component={CaseDetailsPage} />
        <Route path="/cases" component={CaseListPage} />
        <Route><Redirect to="/cases" replace /></Route>
      </Switch>
    </Layout>
  )
}
