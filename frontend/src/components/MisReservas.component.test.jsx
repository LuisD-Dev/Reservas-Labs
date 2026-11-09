import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { expect, test, vi } from 'vitest';
import MisReservas from './MisReservas';
import { apiFetch } from '../services/api';
import { reservas, respuesta } from '../test/fixtures';

test('carga y separa reservas en tablas y regiones con nombres accesibles', async () => {
    apiFetch.mockResolvedValue(respuesta(reservas()));
    render(<MisReservas />);
    const activas = await screen.findByRole('table', { name: /Reservas activas/ });
    const canceladas = screen.getByRole('table', { name: /Reservas canceladas/ });
    expect(within(activas).getByRole('cell', { name: 'Software' })).toBeInTheDocument();
    expect(within(activas).queryByRole('cell', { name: 'Redes' })).not.toBeInTheDocument();
    expect(within(canceladas).getByRole('cell', { name: 'Redes' })).toBeInTheDocument();
    expect(within(canceladas).queryByRole('button', { name: /Cancelar/ })).not.toBeInTheDocument();
    expect(screen.getByRole('region', { name: /Reservas activas/ })).toHaveAttribute('tabindex', '0');
    expect(screen.getByRole('region', { name: /Reservas canceladas/ })).toBeInTheDocument();
});

test('rechazar confirmación conserva la reserva sin enviar cancelación', async () => {
    const user = userEvent.setup();
    const confirmar = vi.spyOn(window, 'confirm').mockReturnValue(false);
    apiFetch.mockResolvedValue(respuesta(reservas()));
    render(<MisReservas />);
    await user.click(await screen.findByRole('button', { name: /Cancelar la reserva de Software/ }));
    expect(confirmar).toHaveBeenCalledTimes(1);
    expect(apiFetch).toHaveBeenCalledTimes(1);
    expect(apiFetch.mock.calls[0][0]).toBe('/api/reservas/mias');
    expect(screen.getByRole('table', { name: /Reservas activas/ })).toBeInTheDocument();
});

test('confirmar cancela mediante POST y actualiza las tablas y el aviso', async () => {
    const user = userEvent.setup();
    vi.spyOn(window, 'confirm').mockReturnValue(true);
    apiFetch.mockResolvedValueOnce(respuesta(reservas()))
        .mockResolvedValueOnce(respuesta({ mensaje: 'Reserva cancelada correctamente.' }));
    render(<MisReservas />);
    await user.click(await screen.findByRole('button', { name: /Cancelar la reserva de Software/ }));
    await waitFor(() => expect(screen.getAllByRole('status').some(region =>
        within(region).queryByText('Reserva cancelada correctamente.'))).toBe(true));
    expect(screen.getByText('Reserva cancelada correctamente.')).toHaveFocus();
    expect(apiFetch).toHaveBeenNthCalledWith(2, '/api/reservas/11/cancelar', { method: 'POST' });
    expect(screen.queryByRole('table', { name: /Reservas activas/ })).not.toBeInTheDocument();
    expect(within(screen.getByRole('table', { name: /Reservas canceladas/ }))
        .getByRole('cell', { name: 'Software' })).toBeInTheDocument();
});
