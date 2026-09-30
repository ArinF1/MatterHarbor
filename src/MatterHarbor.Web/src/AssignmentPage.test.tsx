import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { beforeEach, expect, test, vi } from 'vitest'
import { Router } from 'wouter'
import { memoryLocation } from 'wouter/memory-location'
import { AppRoutes } from './App'

const item = {
  id: '33333333-3333-3333-3333-333333333333',
  caseNumber: 'OC-20260930-33333333',
  title: 'Fictional case',
  description: 'Fictional description.',
  priority: 'Normal',
  status: 'New',
  assignedUserId: null,
  createdAt: '2026-09-30T12:00:00Z',
  updatedAt: '2026-09-30T12:00:00Z',
  version: 1,
}

beforeEach(() => localStorage.setItem('matterharbor-persona', 'alex'))

test('administrator assigns an eligible member with a version and retry key', async () => {
  const assignee = { id: 'cccccccc-cccc-cccc-cccc-cccccccccccc', displayName: 'Taylor Park' }
  const fetchMock = vi.fn()
    .mockResolvedValueOnce(new Response(JSON.stringify(item), { status: 200 }))
    .mockResolvedValueOnce(new Response(JSON.stringify([assignee]), { status: 200 }))
    .mockResolvedValueOnce(new Response(JSON.stringify({ ...item, assignedUserId: assignee.id, version: 2 }), { status: 200 }))
  vi.stubGlobal('fetch', fetchMock)
  const user = userEvent.setup()
  const { hook } = memoryLocation({ path: `/cases/${item.id}` })
  render(<Router hook={hook}><AppRoutes /></Router>)

  await screen.findByRole('heading', { name: item.title })
  await user.selectOptions(await screen.findByLabelText('Assign to'), assignee.id)
  await user.click(screen.getByRole('button', { name: 'Update assignment' }))

  expect(await screen.findByText('Taylor Park', { selector: 'dd' })).toBeVisible()
  expect(fetchMock.mock.calls[2][1]).toMatchObject({
    method: 'PUT',
    headers: expect.objectContaining({
      'If-Match': '"v1"',
      'Idempotency-Key': expect.any(String),
    }),
  })
})

test('viewer sees case details without mutation controls', async () => {
  localStorage.setItem('matterharbor-persona', 'jordan')
  vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(JSON.stringify(item), { status: 200 })))
  const { hook } = memoryLocation({ path: `/cases/${item.id}` })
  render(<Router hook={hook}><AppRoutes /></Router>)

  await screen.findByRole('heading', { name: item.title })
  expect(screen.queryByRole('button', { name: 'Update status' })).not.toBeInTheDocument()
  expect(screen.queryByRole('button', { name: 'Update assignment' })).not.toBeInTheDocument()
})
