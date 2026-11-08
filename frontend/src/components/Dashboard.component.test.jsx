import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { expect, test, vi } from 'vitest';
import Dashboard from './Dashboard';
import { apiFetch } from '../services/api';
import { laboratorios, respuesta } from '../test/fixtures';

test('Usuario ve sus módulos y puede cerrar sesión', async () => {
    const user = userEvent.setup();
    const onCerrarSesion = vi.fn();
    render(<Dashboard usuario={{ nombre: 'Ana', rol: 'Usuario' }} onCerrarSesion={onCerrarSesion} />);
    expect(screen.getByRole('button', { name: /^Laboratorios/ })).toBeEnabled();
    expect(screen.getByRole('button', { name: /^Mis Reservas/ })).toBeEnabled();
    expect(screen.queryByRole('button', { name: /^Gestión de Reservas/ })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /^Reportes/ })).not.toBeInTheDocument();
    await user.click(screen.getByRole('button', { name: 'Cerrar sesión' }));
    expect(onCerrarSesion).toHaveBeenCalledTimes(1);
});

test('Administrador ve módulos pendientes deshabilitados con Próximamente', () => {
    render(<Dashboard usuario={{ nombre: 'Admin', rol: 'Administrador' }} />);
    expect(screen.getByRole('button', { name: /^Laboratorios/ })).toBeEnabled();
    expect(screen.queryByRole('button', { name: /^Mis Reservas/ })).not.toBeInTheDocument();
    for (const nombre of [/^Gestión de Reservas/, /^Reportes y Estadísticas/]) {
        const modulo = screen.getByRole('button', { name: nombre });
        expect(modulo).toBeDisabled();
        expect(within(modulo).getByText('Próximamente')).toBeInTheDocument();
    }
});

test('navega a laboratorios y restaura el inicio mediante popstate', async () => {
    const user = userEvent.setup();
    apiFetch.mockResolvedValue(respuesta(laboratorios));
    render(<Dashboard usuario={{ nombre: 'Ana', rol: 'Usuario' }} />);
    await user.click(screen.getByRole('button', { name: /^Laboratorios/ }));
    expect(await screen.findByRole('heading', { name: 'Laboratorios OBLD' })).toBeInTheDocument();
    expect(window.history.state.vista).toBe('laboratorios');
    await user.click(screen.getByRole('button', { name: 'Volver al inicio' }));
    await waitFor(() => expect(screen.getByRole('heading', { name: 'Bienvenido, Ana' })).toBeInTheDocument());
    expect(window.history.state.vista).toBeNull();
});
