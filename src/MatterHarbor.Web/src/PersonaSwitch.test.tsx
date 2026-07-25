import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { beforeEach, expect, test, vi } from 'vitest'
import { Router } from 'wouter'
import { memoryLocation } from 'wouter/memory-location'
import { AppRoutes } from './App'

const alexCase = {
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
})

test('returns to the organization case list when the persona changes', async () => {
  const fetchMock = vi.fn()
    .mockResolvedValueOnce(new Response(JSON.stringify(alexCase), { status: 200 }))
    .mockResolvedValueOnce(new Response(JSON.stringify([]), { status: 200 }))
    .mockResolvedValueOnce(new Response(JSON.stringify([alexCase]), { status: 200 }))
  vi.stubGlobal('fetch', fetchMock)
  const user = userEvent.setup()
  const { hook } = memoryLocation({ path: `/cases/${alexCase.id}` })
  render(<Router hook={hook}><AppRoutes /></Router>)

  expect(await screen.findByRole('heading', { name: alexCase.title })).toBeVisible()

  await user.selectOptions(screen.getByLabelText('Development persona'), 'casey')
  expect(await screen.findByRole('heading', { name: 'Cases' })).toBeVisible()
  expect(await screen.findByText('No cases yet.')).toBeVisible()
  expect(fetchMock.mock.calls[1][1]).toMatchObject({
    headers: expect.objectContaining({ 'X-MatterHarbor-User': 'casey' }),
  })

  await user.selectOptions(screen.getByLabelText('Development persona'), 'alex')
  expect(await screen.findByText(alexCase.title)).toBeVisible()
  expect(fetchMock.mock.calls[2][1]).toMatchObject({
    headers: expect.objectContaining({ 'X-MatterHarbor-User': 'alex' }),
  })
})
