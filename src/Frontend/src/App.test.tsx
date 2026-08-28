import { fireEvent, render, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { App } from './App'
import { ApiError } from './services/api/movementsApi'
import * as movementsApi from './services/api/movementsApi'

vi.mock('./services/api/movementsApi', async (importOriginal) => {
  const original = await importOriginal<typeof movementsApi>()
  return {
    ...original,
    getBalance: vi.fn(),
    getMovements: vi.fn(),
    createMovement: vi.fn(),
  }
})

const getBalanceMock = vi.mocked(movementsApi.getBalance)
const getMovementsMock = vi.mocked(movementsApi.getMovements)
const createMovementMock = vi.mocked(movementsApi.createMovement)

beforeEach(() => {
  vi.clearAllMocks()
  getBalanceMock.mockResolvedValue({ balance: 1250 })
  getMovementsMock.mockResolvedValue([])
})

describe('App', () => {
  it('shows the balance and empty history returned by the API', async () => {
    render(<App />)

    expect(await screen.findByText(/R\$\s*1\.250,00/)).toBeInTheDocument()
    expect(screen.getByText('Nenhuma movimentação registrada.')).toBeInTheDocument()
  })

  it('renders movements in the order received', async () => {
    getMovementsMock.mockResolvedValue([
      movement('newer', 'debit', 25, '2026-08-26T15:30:00Z'),
      movement('older', 'credit', 100, '2026-08-26T14:30:00Z'),
    ])
    render(<App />)

    const history = screen.getByRole('heading', { name: 'Histórico' }).closest('section')
    expect(history).not.toBeNull()
    const labels = await within(history!).findAllByText(/^(Entrada|Saída)$/)

    expect(labels[0]).toHaveTextContent('Saída')
    expect(labels[1]).toHaveTextContent('Entrada')
  })

  it('submits a credit and refreshes balance and history', async () => {
    const user = userEvent.setup()
    createMovementMock.mockResolvedValue(movement('created', 'credit', 50))
    getBalanceMock.mockResolvedValueOnce({ balance: 100 }).mockResolvedValueOnce({ balance: 150 })
    getMovementsMock
      .mockResolvedValueOnce([])
      .mockResolvedValueOnce([movement('created', 'credit', 50)])
    render(<App />)
    await screen.findByText(/R\$\s*100,00/)

    await user.type(screen.getByLabelText('Valor'), '5000')
    await user.click(screen.getByRole('button', { name: 'Registrar movimentação' }))

    await screen.findByText('Movimentação registrada com sucesso.')
    expect(createMovementMock).toHaveBeenCalledWith({ type: 'credit', amount: 50 })
    expect(await screen.findByText(/R\$\s*150,00/)).toBeInTheDocument()
    expect(screen.getByLabelText('Valor')).toHaveValue('0,00')
  })

  it('submits a debit selected with Radix Toggle Group', async () => {
    const user = userEvent.setup()
    createMovementMock.mockResolvedValue(movement('created', 'debit', 25))
    render(<App />)
    await screen.findByText(/R\$\s*1\.250,00/)

    await user.click(screen.getByRole('radio', { name: 'Saída' }))
    await user.type(screen.getByLabelText('Valor'), '2500')
    await user.click(screen.getByRole('button', { name: 'Registrar movimentação' }))

    await waitFor(() => expect(createMovementMock).toHaveBeenCalledWith({ type: 'debit', amount: 25 }))
  })

  it('shows a friendly message when the debit is rejected', async () => {
    const user = userEvent.setup()
    createMovementMock.mockRejectedValue(new ApiError(409, { status: 409 }))
    render(<App />)
    await screen.findByText(/R\$\s*1\.250,00/)

    await user.click(screen.getByRole('radio', { name: 'Saída' }))
    await user.type(screen.getByLabelText('Valor'), '200000')
    await user.click(screen.getByRole('button', { name: 'Registrar movimentação' }))

    expect(await screen.findByRole('alert')).toHaveTextContent(
      'Saldo insuficiente para realizar esta saída.',
    )
  })

  it.each([
    [new ApiError(400, { status: 400 }), 'Informe um tipo e um valor maior que zero.'],
    [new ApiError(500, { status: 500 }), 'Não foi possível concluir a operação. Tente novamente.'],
    [new TypeError('network'), 'Não foi possível conectar ao servidor.'],
  ])('maps operation failures to accessible feedback', async (error, expectedMessage) => {
    const user = userEvent.setup()
    createMovementMock.mockRejectedValue(error)
    render(<App />)
    await screen.findByText(/R\$\s*1\.250,00/)

    await user.type(screen.getByLabelText('Valor'), '1000')
    await user.click(screen.getByRole('button', { name: 'Registrar movimentação' }))

    expect(await screen.findByRole('alert')).toHaveTextContent(expectedMessage)
  })

  it('keeps success context and offers retry when refresh fails after creation', async () => {
    const user = userEvent.setup()
    createMovementMock.mockResolvedValue(movement('created', 'credit', 10))
    getBalanceMock.mockResolvedValueOnce({ balance: 100 }).mockRejectedValueOnce(new TypeError('network'))
    render(<App />)
    await screen.findByText(/R\$\s*100,00/)

    await user.type(screen.getByLabelText('Valor'), '1000')
    await user.click(screen.getByRole('button', { name: 'Registrar movimentação' }))

    expect(
      await screen.findByText('Movimentação registrada, mas não foi possível atualizar os dados.'),
    ).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Atualizar novamente' })).toBeInTheDocument()
    expect(screen.getByText(/R\$\s*100,00/)).toBeInTheDocument()
  })

  it('disables the submit button while the request is in progress', async () => {
    const user = userEvent.setup()
    let resolveCreation!: (value: ReturnType<typeof movement>) => void
    createMovementMock.mockReturnValue(
      new Promise((resolve) => {
        resolveCreation = resolve
      }),
    )
    render(<App />)
    await screen.findByText(/R\$\s*1\.250,00/)

    await user.type(screen.getByLabelText('Valor'), '1000')
    await user.click(screen.getByRole('button', { name: 'Registrar movimentação' }))

    const submittingButton = screen.getByRole('button', { name: 'Registrando…' })
    expect(submittingButton).toBeDisabled()
    await user.click(submittingButton)
    expect(createMovementMock).toHaveBeenCalledTimes(1)
    resolveCreation(movement('created', 'credit', 10))
  })

  it('allows retry after an initial loading error', async () => {
    const user = userEvent.setup()
    getBalanceMock.mockRejectedValueOnce(new TypeError('network')).mockResolvedValueOnce({ balance: 0 })
    render(<App />)

    expect(await screen.findByText('Não foi possível carregar os dados')).toBeInTheDocument()
    await user.click(screen.getByRole('button', { name: 'Tentar novamente' }))

    expect(await screen.findByText(/R\$\s*0,00/)).toBeInTheDocument()
  })

  it('formats typed digits as BRL cents with thousands separators', async () => {
    const user = userEvent.setup()
    render(<App />)

    const input = screen.getByLabelText('Valor')
    expect(input).toHaveValue('0,00')

    await user.type(input, '1')
    expect(input).toHaveValue('0,01')

    await user.type(input, '2345')
    expect(input).toHaveValue('123,45')

    await user.type(input, '6')
    expect(input).toHaveValue('1.234,56')
  })

  it('sanitizes invalid typed and pasted content', async () => {
    const user = userEvent.setup()
    render(<App />)
    const input = screen.getByLabelText('Valor')

    fireEvent.change(input, { target: { value: 'a1eE2+-3!@#' } })
    expect(input).toHaveValue('1,23')

    await user.clear(input)
    await user.click(input)
    await user.paste('R$ 1.234,56 inválido')
    expect(input).toHaveValue('1.234,56')
  })

  it('limits the amount to thirteen integer digits and two decimal digits', () => {
    render(<App />)
    const input = screen.getByLabelText('Valor')

    fireEvent.change(input, { target: { value: '99999999999999999' } })

    expect(input).toHaveValue('9.999.999.999.999,99')
  })

  it('keeps submission disabled for zero and sends the unmasked numeric amount', async () => {
    const user = userEvent.setup()
    createMovementMock.mockResolvedValue(movement('created', 'credit', 1234.56))
    render(<App />)

    const submitButton = screen.getByRole('button', { name: 'Registrar movimentação' })
    expect(submitButton).toBeDisabled()

    await user.type(screen.getByLabelText('Valor'), '123456')
    await user.click(submitButton)

    await waitFor(() => {
      expect(createMovementMock).toHaveBeenCalledWith({ type: 'credit', amount: 1234.56 })
    })
  })
})

function movement(
  id: string,
  type: 'credit' | 'debit',
  amount = 10,
  occurredAt = '2026-08-26T15:30:00Z',
) {
  return { id, type, amount, occurredAt }
}
