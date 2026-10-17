import test from "node:test";
import assert from "node:assert/strict";
import { crearCuerpoReserva, mensajeErrorReserva, validarReserva } from "./reserva.js";

const datos = { laboratorioId: "7", fecha: "2026-10-20", horaInicio: "08:15", horaFin: "09:30" };

test("validarReserva exige cada uno de los cuatro campos", () => {
    assert.deepEqual(validarReserva({}), {
        laboratorioId: "Selecciona un laboratorio.",
        fecha: "Ingresa la fecha de la reserva.",
        horaInicio: "Ingresa la hora inicial.",
        horaFin: "Ingresa la hora final.",
    });
    for (const campo of Object.keys(datos)) {
        assert.deepEqual(Object.keys(validarReserva({ ...datos, [campo]: "" })), [campo]);
    }
    assert.deepEqual(validarReserva(datos), {});
});

test("validarReserva rechaza una hora final anterior a la inicial", () => {
    assert.deepEqual(validarReserva({ ...datos, horaInicio: "10:00", horaFin: "09:00" }), {
        horaFin: "La hora final debe ser mayor que la hora inicial.",
    });
});

test("validarReserva rechaza horas iguales", () => {
    assert.deepEqual(validarReserva({ ...datos, horaInicio: "09:00", horaFin: "09:00" }), {
        horaFin: "La hora final debe ser mayor que la hora inicial.",
    });
});

test("validarReserva acepta un rango creciente", () => {
    assert.deepEqual(validarReserva(datos), {});
});

test("validarReserva todavía acepta fechas pasadas, pendiente de AB#68", () => {
    assert.deepEqual(validarReserva({ ...datos, fecha: "2000-01-01" }), {});
});

test("el cuerpo convierte laboratorioId a número y HH:mm a HH:mm:ss", () => {
    assert.deepEqual(crearCuerpoReserva(datos), {
        laboratorioId: 7, fecha: "2026-10-20", horaInicio: "08:15:00", horaFin: "09:30:00",
    });
    assert.equal(crearCuerpoReserva({ ...datos, horaInicio: "08:15:00" }).horaInicio, "08:15:00");
});

test("el cuerpo excluye usuarioId y cualquier dato de identidad aunque estén en el origen", () => {
    const cuerpo = JSON.parse(JSON.stringify(crearCuerpoReserva({ ...datos,
        usuarioId: 99, responsable: "Otra persona", nombre: "Nombre", rol: "Administrador", token: "secreto",
    })));
    assert.deepEqual(cuerpo, crearCuerpoReserva(datos));
    assert.deepEqual(Object.keys(cuerpo).sort(), ["fecha", "horaFin", "horaInicio", "laboratorioId"]);
});

test("interpreta los errores 400 del backend y ValidationProblemDetails", () => {
    assert.equal(mensajeErrorReserva(400, { mensaje: "Revisa los datos.", errores: ["Error de fecha", "Error de hora"] }),
        "Error de fecha Error de hora");
    assert.equal(mensajeErrorReserva(400, { errors: { Fecha: ["Fecha requerida"], HoraInicio: ["Hora requerida"] } }),
        "Fecha requerida Hora requerida");
    assert.equal(mensajeErrorReserva(400, { mensaje: "Datos inválidos" }), "Datos inválidos");
    assert.equal(mensajeErrorReserva(400, null), "Revisa los datos de la reserva.");
});

test("interpreta 401, 403, otros errores HTTP y errores de conexión", () => {
    assert.equal(mensajeErrorReserva(401), "Tu sesión venció. Inicia sesión de nuevo.");
    assert.equal(mensajeErrorReserva(403), "Tu usuario no tiene permiso para registrar reservas.");
    assert.equal(mensajeErrorReserva(500), "No se pudo registrar la reserva. Intenta de nuevo.");
    assert.equal(mensajeErrorReserva(), "No se pudo conectar con el servidor. Verifica tu conexión e intenta de nuevo.");
});
