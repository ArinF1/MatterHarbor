import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { beforeEach, expect, test, vi } from 'vitest'
import { Router } from 'wouter'
import { memoryLocation } from 'wouter/memory-location'
import { AppRoutes } from './App'

const caseItem = {
  id: '33333333-3333-3333-3333-333333333333',
  caseNumber: 'OC-20260722-33333333',
  title: 'Broken streetlight',
  description: 'Fictional description.',
  priority: 'High',
  status: 'New',
  assignedUserId: null,
  createdAt: '2026-07-22T12:00:00Z',
  updatedAt: '2026-07-22T12:00:00Z',
  version: 1,
}

beforeEach(() => {
  localStorage.setItem('matterharbor-persona', 'alex')
})

test('announces list loading and offers a retry after an error', async () => {
  const fetchMock = vi.fn()
    .mockRejectedValueOnce(new Error('Network unavailable'))
    .mockResolvedValueOnce(new Response(JSON.stringify([]), { status: 200 }))
  vi.stubGlobal('fetch', fetchMock)
  const user = userEvent.setup()
  const { hook } = memoryLocation({ path: '/cases' })
  render(<Router hook={hook}><AppRoutes /></Router>)

  expect(screen.getByRole('status')).toHaveTextContent('Loading cases')
  expect(await screen.findByRole('alert')).toHaveTextContent('Network unavailable')
  await user.click(screen.getByRole('button', { name: 'Try again' }))
  expect(await screen.findByText('No cases yet.')).toBeVisible()
})

test('explains a concurrency conflict and reloads the latest case', async () => {
  const latest = { ...caseItem, status: 'Resolved', version: 2 }
  const conflict = {
    type: 'https://matterharbor.dev/problems/concurrency-conflict',
    title: 'Concurrency conflict',
    status: 409,
  }
  const fetchMock = vi.fn()
    .mockResolvedValueOnce(new Response(JSON.stringify(caseItem), { status: 200 }))
    .mockResolvedValueOnce(new Response(JSON.stringify(conflict), { status: 409 }))
    .mockResolvedValueOnce(new Response(JSON.stringify(latest), { status: 200 }))
  vi.stubGlobal('fetch', fetchMock)
  const user = userEvent.setup()
  const { hook } = memoryLocation({ path: `/cases/${caseItem.id}` })
  render(<Router hook={hook}><AppRoutes /></Router>)

  expect(await screen.findByRole('heading', { name: caseItem.title })).toBeVisible()
  await user.selectOptions(screen.getByLabelText('Status'), 'Resolved')
  await user.click(screen.getByRole('button', { name: 'Update status' }))
  expect(await screen.findByRole('alert')).toHaveTextContent('changed while you were editing')
  await user.click(screen.getByRole('button', { name: 'Reload case' }))
  expect(await screen.findByText('2')).toBeVisible()
  expect(screen.getByText('Resolved', { selector: 'dd' })).toBeVisible()
})
