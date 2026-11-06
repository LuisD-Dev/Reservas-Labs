import test from "node:test";
import assert from "node:assert/strict";
import {
    formatearFechaReserva,
    formatearHoraReserva,
    mensajeErrorMisReservas,
    ordenarReservas,
} from "./misReservas.js";

test("ordenarReservas deja primero la fecha y la hora más próximas (AB#94)", () => {
    const orden = ordenarReservas([
        { id: 1, fecha: "2026-10-21", horaInicio: "09:00:00" },
        { id: 2, fecha: "2026-10-20", horaInicio: "10:00:00" },
        { id: 3, fecha: "2026-10-20", horaInicio: "08:00:00" },
    ]);
    assert.deepEqual(orden.map((reserva) => reserva.id), [3, 2, 1]);
});

test("ordenarReservas no modifica la lista original", () => {
    const original = [
        { id: 1, fecha: "2026-10-21", horaInicio: "09:00:00" },
        { id: 2, fecha: "2026-10-20", horaInicio: "09:00:00" },
    ];
    ordenarReservas(original);
    assert.deepEqual(original.map((reserva) => reserva.id), [1, 2]);
});

test("ordenarReservas devuelve una lista vacía si la respuesta no es una lista", () => {
    assert.deepEqual(ordenarReservas(null), []);
    assert.deepEqual(ordenarReservas({ mensaje: "error" }), []);
});

test("formatearHoraReserva recorta los segundos", () => {
    assert.equal(formatearHoraReserva("09:00:00"), "09:00");
    assert.equal(formatearHoraReserva(undefined), "");
});

test("formatearFechaReserva muestra día/mes/año sin desplazar la fecha", () => {
    assert.equal(formatearFechaReserva("2026-10-20"), "20/10/2026");
    assert.equal(formatearFechaReserva("2026-01-01T00:00:00"), "01/01/2026");
});

test("formatearFechaReserva devuelve el texto original si no tiene formato ISO", () => {
    assert.equal(formatearFechaReserva("pronto"), "pronto");
    assert.equal(formatearFechaReserva(null), "");
});

test("mensajeErrorMisReservas distingue conexión, 401, 403 y otros errores", () => {
    assert.match(mensajeErrorMisReservas(), /conectar con el servidor/);
    assert.match(mensajeErrorMisReservas(401), /sesión venció/);
    assert.match(mensajeErrorMisReservas(403), /no tiene permiso/);
    assert.match(mensajeErrorMisReservas(500), /No se pudieron cargar/);
});