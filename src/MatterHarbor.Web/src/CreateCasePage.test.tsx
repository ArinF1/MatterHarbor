import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { beforeEach, expect, test, vi } from 'vitest'
import { Router } from 'wouter'
import { memoryLocation } from 'wouter/memory-location'
import { AppRoutes } from './App'

const createdCase = {
  id: '33333333-3333-3333-3333-333333333333',
  caseNumber: 'OC-20260722-33333333',
  title: 'Broken streetlight',
  description: 'Lamp outside the library is dark.',
  priority: 'High',
  status: 'New',
  assignedUserId: null,
  createdAt: '2026-07-22T12:00:00Z',
  updatedAt: '2026-07-22T12:00:00Z',
  version: 1,
}

beforeEach(() => {
  localStorage.setItem('matterharbor-persona', 'alex')
  vi.stubGlobal('crypto', { randomUUID: () => '44444444-4444-4444-4444-444444444444' })
})

test('creates a case with persona and idempotency headers, then opens it', async () => {
  const fetchMock = vi.fn()
    .mockResolvedValueOnce(new Response(JSON.stringify(createdCase), { status: 201 }))
    .mockResolvedValueOnce(new Response(JSON.stringify(createdCase), { status: 200 }))
  vi.stubGlobal('fetch', fetchMock)
  const user = userEvent.setup()
  const { hook } = memoryLocation({ path: '/cases/new' })
  render(<Router hook={hook}><AppRoutes /></Router>)

  await user.type(screen.getByLabelText('Title'), createdCase.title)
  await user.type(screen.getByLabelText('Description'), createdCase.description)
  await user.selectOptions(screen.getByLabelText('Priority'), 'High')
  await user.click(screen.getByRole('button', { name: 'Create case' }))

  expect(await screen.findByRole('heading', { name: createdCase.title })).toBeVisible()
  const request = fetchMock.mock.calls[0][1] as RequestInit
  expect(request.headers).toMatchObject({
    'X-MatterHarbor-User': 'alex',
    'Idempotency-Key': '44444444-4444-4444-4444-444444444444',
  })
})
