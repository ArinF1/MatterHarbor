import { createContext, useContext } from 'react'

export const personaOptions = [
  { key: 'alex', name: 'Alex Morgan', organization: 'Northwind Municipality' },
  { key: 'casey', name: 'Casey Lee', organization: 'Contoso Housing' },
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
