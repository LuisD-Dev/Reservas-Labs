import test from "node:test";
import assert from "node:assert/strict";
import {
    FILTROS_INICIALES,
    contarReservas,
    crearCuerpoReservaGestion,
    filtrarReservasGestion,
    hayFiltrosActivos,
    laboratoriosDeReservas,
    mensajeConfirmacionGestion,
    mensajeErrorCrearGestion,
    mensajeErrorGestion,
    validarReservaGestion,
} from "./gestionReservas.js";

const reservas = [
    { id: 1, laboratorioId: 7, nombreLaboratorio: "Programación", nombreUsuario: "Ana Responsable", username: "ana", fecha: "2026-11-20", estado: "Activa" },
    { id: 2, laboratorioId: 9, nombreLaboratorio: "Redes", nombreUsuario: "Luis Docente", username: "luis", fecha: "2026-11-20", estado: "Cancelada" },
    { id: 3, laboratorioId: 7, nombreLaboratorio: "Programación", nombreUsuario: "Luis Docente", username: "luis", fecha: "2026-11-21", estado: "Activa" },
];
const ids = (lista) => lista.map((reserva) => reserva.id);

test("sin filtros devuelve todas las reservas en el mismo orden", () => {
    assert.deepEqual(ids(filtrarReservasGestion(reservas)), [1, 2, 3]);
    assert.deepEqual(ids(filtrarReservasGestion(reservas, FILTROS_INICIALES)), [1, 2, 3]);
});

test("filtra por estado activa o cancelada", () => {
    assert.deepEqual(ids(filtrarReservasGestion(reservas, { estado: "activa" })), [1, 3]);
    assert.deepEqual(ids(filtrarReservasGestion(reservas, { estado: "cancelada" })), [2]);
});

test("filtra por laboratorio y por fecha", () => {
    assert.deepEqual(ids(filtrarReservasGestion(reservas, { laboratorioId: "7" })), [1, 3]);
    assert.deepEqual(ids(filtrarReservasGestion(reservas, { fecha: "2026-11-21" })), [3]);
});

test("la búsqueda ignora mayúsculas y tildes, y mira responsable, usuario y laboratorio", () => {
    assert.deepEqual(ids(filtrarReservasGestion(reservas, { busqueda: "programacion" })), [1, 3]);
    assert.deepEqual(ids(filtrarReservasGestion(reservas, { busqueda: "  LUIS " })), [2, 3]);
    assert.deepEqual(ids(filtrarReservasGestion(reservas, { busqueda: "responsable" })), [1]);
});

test("combina los filtros y no modifica la lista original", () => {
    const copia = structuredClone(reservas);
    assert.deepEqual(ids(filtrarReservasGestion(reservas, { estado: "activa", busqueda: "luis" })), [3]);
    assert.deepEqual(filtrarReservasGestion(reservas, { estado: "cancelada", fecha: "2026-11-21" }), []);
    assert.deepEqual(reservas, copia);
});

test("una respuesta que no es lista se trata como vacía", () => {
    assert.deepEqual(filtrarReservasGestion(null), []);
    assert.deepEqual(laboratoriosDeReservas({ mensaje: "error" }), []);
    assert.deepEqual(contarReservas(undefined), { total: 0, activas: 0, canceladas: 0 });
});

test("hayFiltrosActivos distingue filtros vacíos de filtros aplicados", () => {
    assert.equal(hayFiltrosActivos(FILTROS_INICIALES), false);
    assert.equal(hayFiltrosActivos({ busqueda: "   " }), false);
    assert.equal(hayFiltrosActivos({ estado: "activa" }), true);
    assert.equal(hayFiltrosActivos({ fecha: "2026-11-20" }), true);
});

test("laboratoriosDeReservas no repite laboratorios y los ordena por nombre", () => {
    assert.deepEqual(laboratoriosDeReservas(reservas), [
        { id: 7, nombre: "Programación" },
        { id: 9, nombre: "Redes" },
    ]);
});

test("contarReservas separa activas y canceladas", () => {
    assert.deepEqual(contarReservas(reservas), { total: 3, activas: 2, canceladas: 1 });
});

test("validarReservaGestion exige el usuario además de los datos de la reserva", () => {
    const datos = { username: "ana", laboratorioId: "7", fecha: "2026-11-20", horaInicio: "09:00", horaFin: "11:00" };
    assert.deepEqual(validarReservaGestion(datos, "2026-11-12"), {});
    assert.equal(
        validarReservaGestion({ ...datos, username: "   " }, "2026-11-12").username,
        "Indica el usuario responsable de la reserva.");
    assert.equal(
        validarReservaGestion({ ...datos, fecha: "2026-11-11" }, "2026-11-12").fecha,
        "La fecha no puede ser anterior a hoy.");
    assert.equal(
        validarReservaGestion({ ...datos, horaFin: "09:00" }, "2026-11-12").horaFin,
        "La hora final debe ser mayor que la hora inicial.");
});

test("crearCuerpoReservaGestion recorta el usuario y agrega segundos a las horas", () => {
    assert.deepEqual(
        crearCuerpoReservaGestion({ username: "  ana ", laboratorioId: "7", fecha: "2026-11-20", horaInicio: "09:00", horaFin: "11:00" }),
        { username: "ana", laboratorioId: 7, fecha: "2026-11-20", horaInicio: "09:00:00", horaFin: "11:00:00" });
});

test("mensajes de error al cargar las reservas", () => {
    assert.match(mensajeErrorGestion(), /No se pudo conectar/);
    assert.match(mensajeErrorGestion(401), /sesión venció/);
    assert.match(mensajeErrorGestion(403), /administrador/);
    assert.match(mensajeErrorGestion(500), /No se pudieron cargar/);
});

test("mensajes de error al registrar a nombre de otro usuario", () => {
    assert.match(mensajeErrorCrearGestion(403), /administrador/);
    assert.equal(
        mensajeErrorCrearGestion(400, { errores: { username: "No existe un usuario con ese nombre de usuario." } }),
        "No existe un usuario con ese nombre de usuario.");
    assert.match(mensajeErrorCrearGestion(), /No se pudo conectar/);
});

test("la confirmación incluye al responsable cuando el servidor lo informa", () => {
    const reserva = { fecha: "2026-11-20", horaInicio: "09:00:00", horaFin: "11:00:00", responsable: "ana" };
    assert.equal(
        mensajeConfirmacionGestion(reserva, "Redes"),
        "Reserva registrada correctamente. Laboratorio: Redes, Fecha: 2026-11-20, Horario: 09:00 a 11:00. Responsable: ana.");
    assert.equal(mensajeConfirmacionGestion(null), "Reserva registrada correctamente.");
});
