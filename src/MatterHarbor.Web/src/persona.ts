import { createContext, useContext } from 'react'

export const personaOptions = [
  { key: 'alex', name: 'Alex Morgan', organization: 'Northwind Municipality', role: 'Administrator', userId: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa' },
  { key: 'casey', name: 'Casey Lee', organization: 'Contoso Housing', role: 'Administrator', userId: 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb' },
  { key: 'taylor', name: 'Taylor Park', organization: 'Northwind Municipality', role: 'CaseWorker', userId: 'cccccccc-cccc-cccc-cccc-cccccccccccc' },
  { key: 'jordan', name: 'Jordan Reed', organization: 'Northwind Municipality', role: 'Viewer', userId: 'dddddddd-dddd-dddd-dddd-dddddddddddd' },
]

export interface PersonaContextValue {
  persona: string
  setPersona: (persona: string) => void
}

export const PersonaContext = createContext<PersonaContextValue | null>(null)

export function usePersona(): PersonaContextValue {
  const context = useContext(PersonaContext)
  if (!context) {
    throw new Error('PersonaContext is unavailable.')
  }
  return context
}
