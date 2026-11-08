import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { expect, test, vi } from 'vitest';
import Laboratorios from './Laboratorios';
import { apiFetch } from '../services/api';
import { laboratorios, respuesta } from '../test/fixtures';

test('carga laboratorios y filtra por ubicación', async () => {
    const user = userEvent.setup();
    apiFetch.mockResolvedValue(respuesta(laboratorios));
    render(<Laboratorios />);
    expect(await screen.findByRole('heading', { name: 'Software' })).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Redes' })).toBeInTheDocument();
    expect(apiFetch).toHaveBeenCalledWith('/api/laboratorios', expect.objectContaining({ signal: expect.any(AbortSignal) }));
    await user.type(screen.getByRole('searchbox', { name: 'Buscar laboratorio' }), 'Edificio B');
    expect(screen.queryByRole('heading', { name: 'Software' })).not.toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Redes' })).toBeInTheDocument();
});

test('consultar disponibilidad entrega el laboratorio elegido y la lista', async () => {
    const user = userEvent.setup();
    const navegar = vi.fn();
    apiFetch.mockResolvedValue(respuesta(laboratorios));
    render(<Laboratorios onConsultarDisponibilidad={navegar} />);
    const botones = await screen.findAllByRole('button', { name: 'Consultar disponibilidad' });
    expect(botones).toHaveLength(1);
    expect(screen.getByRole('heading', { name: 'Redes' })).toBeInTheDocument();
    await user.click(botones[0]);
    expect(navegar).toHaveBeenCalledExactlyOnceWith(7, [laboratorios[0]]);
});

test('un fallo de red se muestra como alerta y permite reintentar', async () => {
    const user = userEvent.setup();
    apiFetch.mockRejectedValueOnce(new Error('sin conexión')).mockResolvedValue(respuesta(laboratorios));
    render(<Laboratorios />);
    expect(await screen.findByRole('alert')).toHaveTextContent('No se pudo conectar con el servidor.');
    await user.click(screen.getByRole('button', { name: 'Reintentar' }));
    expect(await screen.findByRole('heading', { name: 'Software' })).toBeInTheDocument();
    expect(screen.queryByRole('alert')).not.toBeInTheDocument();
});
