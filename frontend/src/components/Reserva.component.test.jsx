import { act, render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { expect, test } from 'vitest';
import Reserva from './Reserva';
import { apiFetch } from '../services/api';
import { fechaFutura, laboratorios, respuesta } from '../test/fixtures';

const usuario = { usuarioId: 3, nombre: 'Ana Responsable' };

async function completar(user) {
    await user.selectOptions(screen.getByRole('combobox', { name: 'Laboratorio' }), '7');
    await user.type(screen.getByLabelText('Fecha'), fechaFutura());
    await user.type(screen.getByLabelText('Hora inicial'), '09:00');
    await user.type(screen.getByLabelText('Hora final'), '10:00');
}

test('responsable es solo lectura y envío vacío anuncia errores sin peticiones', async () => {
    const user = userEvent.setup();
    render(<Reserva usuario={usuario} laboratoriosIniciales={laboratorios} />);
    const responsable = screen.getByRole('textbox', { name: 'Responsable' });
    expect(responsable).toHaveValue('Ana Responsable');
    expect(responsable).toHaveAttribute('readonly');
    await user.click(screen.getByRole('button', { name: 'Registrar reserva' }));
    expect(screen.getAllByRole('alert')).toHaveLength(4);
    expect(screen.getByLabelText('Fecha')).toHaveAttribute('aria-invalid', 'true');
    expect(screen.getByLabelText('Fecha')).toHaveAccessibleDescription('Ingresa la fecha de la reserva.');
    expect(apiFetch).not.toHaveBeenCalled();
});

test('envía exclusivamente los cuatro campos del contrato y muestra confirmación', async () => {
    const user = userEvent.setup();
    apiFetch.mockResolvedValue(respuesta({ reserva: {
        fecha: fechaFutura(), horaInicio: '09:00:00', horaFin: '10:00:00',
    } }, 201));
    render(<Reserva usuario={usuario} laboratoriosIniciales={laboratorios} />);
    await completar(user);
    await user.click(screen.getByRole('button', { name: 'Registrar reserva' }));
    expect(await screen.findByRole('status')).toHaveTextContent('Reserva registrada correctamente.');
    expect(screen.getByRole('status')).toHaveTextContent('Laboratorio: Software');
    expect(apiFetch).toHaveBeenCalledTimes(1);
    const [ruta, opciones] = apiFetch.mock.calls[0];
    expect(ruta).toBe('/api/reservas');
    expect(opciones.method).toBe('POST');
    expect(JSON.parse(opciones.body)).toEqual({ laboratorioId: 7, fecha: fechaFutura(), horaInicio: '09:00:00', horaFin: '10:00:00' });
});

test('bloquea envíos simultáneos mientras la petición está pendiente', async () => {
    const user = userEvent.setup();
    let resolver;
    apiFetch.mockReturnValue(new Promise(resolve => { resolver = resolve; }));
    render(<Reserva usuario={usuario} laboratoriosIniciales={laboratorios} />);
    await completar(user);
    await user.dblClick(screen.getByRole('button', { name: 'Registrar reserva' }));
    expect(screen.getByRole('button', { name: 'Registrando...' })).toBeDisabled();
    expect(apiFetch).toHaveBeenCalledTimes(1);
    await act(async () => { resolver(respuesta({ reserva: null }, 201)); });
    await waitFor(() => expect(screen.getByRole('status')).toHaveTextContent('Reserva registrada correctamente.'));
    expect(apiFetch).toHaveBeenCalledTimes(1);
});
