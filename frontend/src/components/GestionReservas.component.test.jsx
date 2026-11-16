import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { expect, test, vi } from 'vitest';
import GestionReservas from './GestionReservas';
import { apiFetch } from '../services/api';
import { fechaFutura, laboratorios, reservasGestion, respuesta } from '../test/fixtures';

// Responde según la ruta, así cada prueba no depende del orden de las peticiones.
function servidor({ lista = reservasGestion(), crear } = {}) {
    apiFetch.mockImplementation(async (ruta, opciones = {}) => {
        if (ruta === '/api/reservas' && !opciones.method) return respuesta(lista);
        if (ruta === '/api/laboratorios?soloHabilitados=true') return respuesta(laboratorios);
        if (ruta === '/api/reservas/gestion') return crear ? crear(opciones) : respuesta({}, 201);
        if (/^\/api\/reservas\/\d+\/cancelar$/.test(ruta)) return respuesta({ mensaje: 'Reserva cancelada correctamente.' });
        throw new Error(`Petición inesperada: ${ruta}`);
    });
}

const cargasDeLista = () => apiFetch.mock.calls.filter(([ruta, opciones]) => ruta === '/api/reservas' && !opciones?.method).length;
const tablaReservas = () => screen.findByRole('table', { name: 'Reservas' });

async function completarFormulario(user, { username = 'ana' } = {}) {
    await screen.findByRole('option', { name: /Software — Edificio A/ });
    await user.type(screen.getByRole('textbox', { name: 'Usuario responsable' }), username);
    await user.selectOptions(screen.getByRole('combobox', { name: 'Laboratorio' }), '7');
    await user.type(screen.getByLabelText('Fecha'), fechaFutura());
    await user.type(screen.getByLabelText('Hora inicial'), '09:00');
    await user.type(screen.getByLabelText('Hora final'), '10:00');
}

test('carga las reservas de todos los usuarios con responsable, laboratorio y estado', async () => {
    servidor();
    render(<GestionReservas />);
    const tabla = await tablaReservas();
    expect(apiFetch).toHaveBeenCalledWith('/api/reservas', expect.objectContaining({ signal: expect.any(AbortSignal) }));
    const filas = within(tabla).getAllByRole('row');
    expect(filas).toHaveLength(3);
    expect(within(filas[1]).getByText('Ana Responsable')).toBeInTheDocument();
    expect(within(filas[1]).getByText('ana')).toBeInTheDocument();
    expect(within(filas[1]).getByText('Software')).toBeInTheDocument();
    expect(within(filas[1]).getByText('Activa')).toBeInTheDocument();
    expect(within(filas[2]).getByText('Luis Docente')).toBeInTheDocument();
    expect(within(filas[2]).getByText('Cancelada')).toBeInTheDocument();
    expect(screen.getByText('Mostrando 2 de 2 reservas (1 activas, 1 canceladas).')).toBeInTheDocument();
});

test('solo las reservas activas tienen botón para cancelar', async () => {
    servidor();
    render(<GestionReservas />);
    await tablaReservas();
    const botones = screen.getAllByRole('button', { name: /^Cancelar la reserva/ });
    expect(botones).toHaveLength(1);
    expect(botones[0]).toHaveAccessibleName(/Software de Ana Responsable/);
});

test('sin reservas informa que todavía no hay registros', async () => {
    servidor({ lista: [] });
    render(<GestionReservas />);
    expect(await screen.findByText('Todavía no hay reservas registradas.')).toBeInTheDocument();
    expect(screen.queryByRole('table')).not.toBeInTheDocument();
});

test('si falla la carga muestra el error y permite reintentar', async () => {
    const user = userEvent.setup();
    apiFetch.mockResolvedValueOnce(respuesta({}, 500)).mockResolvedValueOnce(respuesta(reservasGestion()));
    render(<GestionReservas />);
    expect(await screen.findByRole('alert')).toHaveTextContent('No se pudieron cargar las reservas. Intenta de nuevo.');
    await user.click(screen.getByRole('button', { name: 'Reintentar' }));
    expect(await tablaReservas()).toBeInTheDocument();
    expect(screen.queryByRole('alert')).not.toBeInTheDocument();
});

test('403 al cargar indica que solo un administrador puede gestionar las reservas', async () => {
    apiFetch.mockResolvedValue(respuesta({}, 403));
    render(<GestionReservas />);
    expect(await screen.findByRole('alert')).toHaveTextContent('Solo un administrador puede gestionar las reservas.');
});

test('filtra por estado, por texto y vuelve a mostrar todo al limpiar', async () => {
    const user = userEvent.setup();
    servidor();
    render(<GestionReservas />);
    await tablaReservas();
    expect(screen.queryByRole('button', { name: 'Limpiar filtros' })).not.toBeInTheDocument();

    await user.selectOptions(screen.getByRole('combobox', { name: 'Filtrar por estado' }), 'activa');
    expect(within(screen.getByRole('table', { name: 'Reservas' })).getAllByRole('row')).toHaveLength(2);
    expect(screen.getByText('Mostrando 1 de 2 reservas (1 activas, 1 canceladas).')).toBeInTheDocument();

    await user.selectOptions(screen.getByRole('combobox', { name: 'Filtrar por estado' }), '');
    await user.type(screen.getByRole('searchbox', { name: 'Buscar' }), 'LUIS');
    const filas = within(screen.getByRole('table', { name: 'Reservas' })).getAllByRole('row');
    expect(filas).toHaveLength(2);
    expect(within(filas[1]).getByText('Luis Docente')).toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: 'Limpiar filtros' }));
    expect(within(screen.getByRole('table', { name: 'Reservas' })).getAllByRole('row')).toHaveLength(3);
    expect(screen.getByRole('searchbox', { name: 'Buscar' })).toHaveValue('');
});

test('filtra por laboratorio y avisa cuando ninguna reserva coincide', async () => {
    const user = userEvent.setup();
    servidor();
    render(<GestionReservas />);
    await tablaReservas();
    const filtroLaboratorio = screen.getByRole('combobox', { name: 'Filtrar por laboratorio' });
    expect(within(filtroLaboratorio).getAllByRole('option').map((o) => o.textContent)).toEqual(['Todos', 'Redes', 'Software']);

    await user.selectOptions(filtroLaboratorio, '9');
    expect(within(screen.getByRole('table', { name: 'Reservas' })).getByText('Luis Docente')).toBeInTheDocument();

    await user.selectOptions(screen.getByRole('combobox', { name: 'Filtrar por estado' }), 'activa');
    expect(screen.getByText('No hay reservas que coincidan con los filtros.')).toBeInTheDocument();
    expect(screen.queryByRole('table')).not.toBeInTheDocument();
});

test('rechazar la confirmación conserva la reserva sin enviar la cancelación', async () => {
    const user = userEvent.setup();
    const confirmar = vi.spyOn(window, 'confirm').mockReturnValue(false);
    servidor();
    render(<GestionReservas />);
    await tablaReservas();
    await user.click(screen.getByRole('button', { name: /Cancelar la reserva de Software/ }));
    expect(confirmar).toHaveBeenCalledTimes(1);
    expect(apiFetch).toHaveBeenCalledTimes(1);
    expect(screen.getByRole('button', { name: /Cancelar la reserva de Software/ })).toBeEnabled();
});

test('confirmar cancela cualquier reserva con POST, actualiza el estado y mueve el foco al aviso', async () => {
    const user = userEvent.setup();
    vi.spyOn(window, 'confirm').mockReturnValue(true);
    servidor();
    render(<GestionReservas />);
    await tablaReservas();
    await user.click(screen.getByRole('button', { name: /Cancelar la reserva de Software/ }));
    await waitFor(() => expect(screen.getByText('Reserva cancelada correctamente.')).toBeInTheDocument());
    expect(apiFetch).toHaveBeenCalledWith('/api/reservas/11/cancelar', { method: 'POST' });
    expect(screen.getByText('Reserva cancelada correctamente.')).toHaveFocus();
    expect(screen.queryByRole('button', { name: /Cancelar la reserva/ })).not.toBeInTheDocument();
    expect(screen.getByText('Mostrando 2 de 2 reservas (0 activas, 2 canceladas).')).toBeInTheDocument();
});

test('un 409 al cancelar muestra el motivo y vuelve a pedir la lista', async () => {
    const user = userEvent.setup();
    vi.spyOn(window, 'confirm').mockReturnValue(true);
    apiFetch.mockImplementation(async (ruta, opciones = {}) => {
        if (opciones.method === 'POST') return respuesta({ mensaje: 'La reserva ya está cancelada.' }, 409);
        return respuesta(reservasGestion());
    });
    render(<GestionReservas />);
    await tablaReservas();
    await user.click(screen.getByRole('button', { name: /Cancelar la reserva de Software/ }));
    expect(await screen.findByRole('alert')).toHaveTextContent('La reserva ya está cancelada.');
    await waitFor(() => expect(cargasDeLista()).toBe(2));
});

test('el formulario se abre en el primer campo y envía vacío anuncia los errores sin peticiones', async () => {
    const user = userEvent.setup();
    servidor();
    render(<GestionReservas />);
    await tablaReservas();
    const abrir = screen.getByRole('button', { name: 'Nueva reserva' });
    expect(abrir).toHaveAttribute('aria-expanded', 'false');
    await user.click(abrir);
    expect(screen.getByRole('button', { name: 'Ocultar formulario' })).toHaveAttribute('aria-expanded', 'true');

    // Solo se ofrecen laboratorios habilitados, y el foco llega al primer campo al terminar la carga.
    await screen.findByRole('option', { name: /Software — Edificio A/ });
    expect(screen.queryByRole('option', { name: /Redes — Edificio B/ })).not.toBeInTheDocument();
    await waitFor(() => expect(screen.getByRole('textbox', { name: 'Usuario responsable' })).toHaveFocus());
    await user.click(screen.getByRole('button', { name: 'Registrar reserva' }));
    expect(screen.getAllByRole('alert')).toHaveLength(5);
    expect(screen.getByRole('textbox', { name: 'Usuario responsable' }))
        .toHaveAccessibleDescription('Indica el usuario responsable de la reserva.');
    expect(apiFetch).not.toHaveBeenCalledWith('/api/reservas/gestion', expect.anything());
});

test('registra una reserva a nombre de otro usuario, cierra el formulario y recarga la lista', async () => {
    const user = userEvent.setup();
    servidor({
        crear: () => respuesta({
            reserva: { id: 30, fecha: fechaFutura(), horaInicio: '09:00:00', horaFin: '10:00:00', responsable: 'ana', estado: 'Activa' },
        }, 201),
    });
    render(<GestionReservas />);
    await tablaReservas();
    await user.click(screen.getByRole('button', { name: 'Nueva reserva' }));
    await completarFormulario(user, { username: '  ana ' });
    await user.click(screen.getByRole('button', { name: 'Registrar reserva' }));

    await waitFor(() => expect(screen.queryByRole('heading', { name: 'Nueva reserva' })).not.toBeInTheDocument());
    const [ruta, opciones] = apiFetch.mock.calls.find(([destino]) => destino === '/api/reservas/gestion');
    expect(ruta).toBe('/api/reservas/gestion');
    expect(opciones.method).toBe('POST');
    expect(JSON.parse(opciones.body)).toEqual({
        username: 'ana', laboratorioId: 7, fecha: fechaFutura(), horaInicio: '09:00:00', horaFin: '10:00:00',
    });
    expect(screen.getByText(`Reserva registrada correctamente. Laboratorio: Software, Fecha: ${fechaFutura()}, Horario: 09:00 a 10:00. Responsable: ana.`))
        .toHaveFocus();
    await waitFor(() => expect(cargasDeLista()).toBe(2));
});

test('si el usuario no existe muestra el error del servidor y conserva lo escrito', async () => {
    const user = userEvent.setup();
    servidor({
        crear: () => respuesta({
            mensaje: 'Revisa los datos de la reserva.',
            errores: { username: 'No existe un usuario con ese nombre de usuario.' },
        }, 400),
    });
    render(<GestionReservas />);
    await tablaReservas();
    await user.click(screen.getByRole('button', { name: 'Nueva reserva' }));
    await completarFormulario(user, { username: 'nadie' });
    await user.click(screen.getByRole('button', { name: 'Registrar reserva' }));

    expect(await screen.findByText('No existe un usuario con ese nombre de usuario.')).toBeInTheDocument();
    expect(screen.getByRole('textbox', { name: 'Usuario responsable' })).toHaveValue('nadie');
    expect(screen.getByRole('button', { name: 'Registrar reserva' })).toBeEnabled();
    expect(cargasDeLista()).toBe(1);
});

test('un conflicto de horario del servidor se muestra sin cerrar el formulario', async () => {
    const user = userEvent.setup();
    servidor({
        crear: () => respuesta({ errores: { conflicto: 'El laboratorio ya tiene una reserva en ese horario.' } }, 400),
    });
    render(<GestionReservas />);
    await tablaReservas();
    await user.click(screen.getByRole('button', { name: 'Nueva reserva' }));
    await completarFormulario(user);
    await user.click(screen.getByRole('button', { name: 'Registrar reserva' }));

    expect(await screen.findByText('El laboratorio ya tiene una reserva en ese horario.')).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Nueva reserva' })).toBeInTheDocument();
});

test('cerrar el formulario lo oculta sin enviar nada', async () => {
    const user = userEvent.setup();
    servidor();
    render(<GestionReservas />);
    await tablaReservas();
    await user.click(screen.getByRole('button', { name: 'Nueva reserva' }));
    await user.click(screen.getByRole('button', { name: 'Cerrar formulario' }));
    expect(screen.queryByRole('heading', { name: 'Nueva reserva' })).not.toBeInTheDocument();
    expect(apiFetch).not.toHaveBeenCalledWith('/api/reservas/gestion', expect.anything());
});
