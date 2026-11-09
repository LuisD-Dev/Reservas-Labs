import { render, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { expect, test, vi } from 'vitest';
import Laboratorios from './Laboratorios';
import { apiFetch } from '../services/api';
import { laboratorios, respuesta } from '../test/fixtures';

// La lista se carga siempre igual; solo cambia lo que responde el PATCH de estado.
function simular(respuestaPatch) {
    apiFetch.mockImplementation(async (ruta, opciones) => {
        if (opciones?.method === 'PATCH') {
            if (respuestaPatch instanceof Error) throw respuestaPatch;
            return respuestaPatch;
        }
        return respuesta(laboratorios);
    });
}

function tarjeta(nombre) {
    return within(screen.getByRole('heading', { name: nombre }).closest('article'));
}

test('un usuario que no es administrador no ve los controles de estado (AB#113)', async () => {
    simular(respuesta({}));
    render(<Laboratorios />);
    await screen.findByRole('heading', { name: 'Software' });
    expect(screen.queryByRole('button', { name: /^(Marcar fuera de servicio|Habilitar):/ })).not.toBeInTheDocument();
});

test('el administrador ve el control que corresponde al estado de cada laboratorio', async () => {
    simular(respuesta({}));
    render(<Laboratorios esAdministrador />);
    expect(await screen.findByRole('button', { name: 'Marcar fuera de servicio: Software' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Habilitar: Redes' })).toBeInTheDocument();
});

test('al marcar un laboratorio fuera de servicio se actualiza la tarjeta y se confirma el cambio (AB#112)', async () => {
    const user = userEvent.setup();
    simular(respuesta({ mensaje: 'Estado actualizado.', laboratorio: { ...laboratorios[0], estado: 'Fuera de servicio' } }));
    render(<Laboratorios esAdministrador onConsultarDisponibilidad={vi.fn()} />);
    await user.click(await screen.findByRole('button', { name: 'Marcar fuera de servicio: Software' }));
    expect(apiFetch).toHaveBeenCalledWith(
        '/api/laboratorios/7/estado',
        expect.objectContaining({ method: 'PATCH', body: JSON.stringify({ estado: 'Fuera de servicio' }) })
    );
    expect(await screen.findByRole('status')).toHaveTextContent('Software');
    expect(tarjeta('Software').getByText('Fuera de servicio')).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Consultar disponibilidad' })).not.toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Habilitar: Software' })).toBeInTheDocument();
});

test('al habilitar un laboratorio fuera de servicio se envía el estado Habilitado', async () => {
    const user = userEvent.setup();
    simular(respuesta({ mensaje: 'Estado actualizado.', laboratorio: { ...laboratorios[1], estado: 'Habilitado' } }));
    render(<Laboratorios esAdministrador />);
    await user.click(await screen.findByRole('button', { name: 'Habilitar: Redes' }));
    expect(apiFetch).toHaveBeenCalledWith(
        '/api/laboratorios/9/estado',
        expect.objectContaining({ method: 'PATCH', body: JSON.stringify({ estado: 'Habilitado' }) })
    );
    expect(await screen.findByRole('status')).toHaveTextContent('Redes');
    expect(tarjeta('Redes').getByText('Habilitado')).toBeInTheDocument();
});

test('si el servidor responde 403 se avisa y el estado no cambia (AB#113)', async () => {
    const user = userEvent.setup();
    simular(respuesta(null, 403));
    render(<Laboratorios esAdministrador />);
    await user.click(await screen.findByRole('button', { name: 'Marcar fuera de servicio: Software' }));
    expect(await screen.findByRole('alert')).toHaveTextContent('Solo el administrador');
    expect(tarjeta('Software').getByText('Habilitado')).toBeInTheDocument();
});

test('un fallo de red al cambiar el estado se muestra como alerta (AB#113)', async () => {
    const user = userEvent.setup();
    simular(new Error('sin conexión'));
    render(<Laboratorios esAdministrador />);
    await user.click(await screen.findByRole('button', { name: 'Marcar fuera de servicio: Software' }));
    expect(await screen.findByRole('alert')).toHaveTextContent('No se pudo conectar con el servidor.');
    expect(tarjeta('Software').getByText('Habilitado')).toBeInTheDocument();
});