import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { expect, test, vi } from 'vitest';
import Disponibilidad from './Disponibilidad';
import { apiFetch } from '../services/api';
import { fechaFutura, laboratorios, respuesta } from '../test/fixtures';

async function consultar(user) {
    await user.selectOptions(screen.getByRole('combobox', { name: 'Laboratorio' }), '7');
    await user.type(screen.getByLabelText('Fecha'), fechaFutura());
    await user.type(screen.getByLabelText('Hora inicial'), '09:00');
    await user.type(screen.getByLabelText('Hora final'), '10:00');
    await user.click(screen.getByRole('button', { name: 'Consultar', exact: true }));
}

test('excluye fuera de servicio y rechaza su seleccion inicial sin consultar la API', async () => {
    const user = userEvent.setup();
    render(<Disponibilidad laboratorios={laboratorios} laboratorioInicialId={9} />);
    expect(screen.getByRole('combobox', { name: 'Laboratorio' })).toHaveValue('');
    expect(screen.queryByRole('option', { name: /Redes/ })).not.toBeInTheDocument();
    expect(screen.getByRole('option', { name: /Software/ })).toBeInTheDocument();
    await user.type(screen.getByLabelText('Fecha'), fechaFutura());
    await user.type(screen.getByLabelText('Hora inicial'), '09:00');
    await user.type(screen.getByLabelText('Hora final'), '10:00');
    await user.click(screen.getByRole('button', { name: 'Consultar', exact: true }));
    expect(screen.getByLabelText('Laboratorio')).toHaveAttribute('aria-invalid', 'true');
    expect(apiFetch).not.toHaveBeenCalled();
});

test('una actualizacion de props deja de mostrar el laboratorio deshabilitado seleccionado', () => {
    const { rerender } = render(<Disponibilidad laboratorios={laboratorios} laboratorioInicialId={7} />);
    expect(screen.getByLabelText('Laboratorio')).toHaveValue('7');
    rerender(<Disponibilidad laboratorios={[{ ...laboratorios[0], estado: 'Fuera de servicio' }]} laboratorioInicialId={7} />);
    expect(screen.getByLabelText('Laboratorio')).toHaveValue('');
    expect(screen.queryByRole('option', { name: /Software/ })).not.toBeInTheDocument();
    expect(screen.queryByText('Estado: Fuera de servicio')).not.toBeInTheDocument();
});

test('deshabilitar el laboratorio invalida tambien la continuacion de una consulta previa', async () => {
    const user = userEvent.setup();
    apiFetch.mockResolvedValue(respuesta({ disponible: true, motivo: 'Rango libre.', horariosDelDia: [] }));
    const { rerender } = render(<Disponibilidad laboratorios={laboratorios} onContinuarReserva={vi.fn()} />);
    await consultar(user);
    expect(await screen.findByRole('button', { name: 'Continuar con la reserva' })).toBeInTheDocument();
    rerender(<Disponibilidad laboratorios={[{ ...laboratorios[0], estado: 'Fuera de servicio' }]} onContinuarReserva={vi.fn()} />);
    expect(screen.queryByRole('button', { name: 'Continuar con la reserva' })).not.toBeInTheDocument();
    expect(screen.getByLabelText('Laboratorio')).toHaveValue('');
    expect(apiFetch).toHaveBeenCalledTimes(1);
});

test('envío vacío anuncia cuatro errores y los relaciona con sus campos', async () => {
    const user = userEvent.setup();
    render(<Disponibilidad laboratorios={laboratorios} />);
    await user.click(screen.getByRole('button', { name: 'Consultar', exact: true }));
    expect(screen.getAllByRole('alert')).toHaveLength(4);
    for (const [nombre, mensaje] of [
        ['Laboratorio', 'Selecciona un laboratorio.'],
        ['Fecha', 'Ingresa la fecha a consultar.'],
        ['Hora inicial', 'Ingresa la hora inicial.'],
        ['Hora final', 'Ingresa la hora final.'],
    ]) {
        const campo = screen.getByLabelText(nombre);
        expect(campo).toHaveAttribute('aria-invalid', 'true');
        expect(campo).toHaveAttribute('aria-describedby');
        expect(campo).toHaveAccessibleDescription(mensaje);
    }
    expect(apiFetch).not.toHaveBeenCalled();
});

test('consulta autoritativa envía contrato correcto y permite continuar a Reserva', async () => {
    const user = userEvent.setup();
    const continuar = vi.fn();
    apiFetch.mockResolvedValue(respuesta({ disponible: true, motivo: 'Rango libre.', horariosDelDia: [] }));
    render(<Disponibilidad laboratorios={laboratorios} onContinuarReserva={continuar} />);
    await consultar(user);
    const boton = await screen.findByRole('button', { name: 'Continuar con la reserva' });
    expect(screen.getByText('Disponible', { exact: true })).toBeInTheDocument();
    expect(apiFetch).toHaveBeenCalledTimes(1);
    const [ruta, opciones] = apiFetch.mock.calls[0];
    expect(ruta).toBe('/api/laboratorios/7/disponibilidad/verificar');
    expect(opciones.method).toBe('POST');
    expect(JSON.parse(opciones.body)).toEqual({ fecha: fechaFutura(), horaInicio: '09:00:00', horaFin: '10:00:00' });
    await user.click(boton);
    expect(continuar).toHaveBeenCalledExactlyOnceWith({ laboratorioId: 7, fecha: fechaFutura(), horaInicio: '09:00', horaFin: '10:00' });
});

test('un veredicto no disponible impide continuar aunque existan horarios nominales', async () => {
    const user = userEvent.setup();
    apiFetch.mockResolvedValue(respuesta({ disponible: false, motivo: 'Existe una reserva activa.', horariosDelDia: [
        { id: 1, fecha: fechaFutura(), horaInicio: '08:00:00', horaFin: '12:00:00', estado: 'Disponible' },
    ] }));
    render(<Disponibilidad laboratorios={laboratorios} onContinuarReserva={vi.fn()} />);
    await consultar(user);
    expect(await screen.findByText('No disponible', { exact: true })).toBeInTheDocument();
    expect(screen.getByText(/Existe una reserva activa/)).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Continuar con la reserva' })).not.toBeInTheDocument();
});
