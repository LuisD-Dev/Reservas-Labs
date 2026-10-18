import test from "node:test";
import assert from "node:assert/strict";
import {
    validarReserva,
    MENSAJE_FUERA_DE_HORARIO,
    MENSAJE_DURACION_EXCEDIDA,
} from "./reserva.js";

const base = { laboratorioId: "1", fecha: "2026-10-20" };
const hoy = "2026-10-17";

test("validarReserva acepta un rango dentro del horario y de la duración máxima (AB#69, AB#70)", () => {
    const errores = validarReserva({ ...base, horaInicio: "09:00", horaFin: "11:00" }, hoy);
    assert.deepEqual(errores, {});
});

test("validarReserva acepta una reserva que termina justo al cierre", () => {
    const errores = validarReserva({ ...base, horaInicio: "19:00", horaFin: "21:00" }, hoy);
    assert.deepEqual(errores, {});
});

test("validarReserva rechaza una reserva que empieza antes de la apertura", () => {
    const errores = validarReserva({ ...base, horaInicio: "06:00", horaFin: "08:00" }, hoy);
    assert.equal(errores.horaFin, MENSAJE_FUERA_DE_HORARIO);
});

test("validarReserva rechaza una reserva que termina después del cierre", () => {
    const errores = validarReserva({ ...base, horaInicio: "20:00", horaFin: "22:00" }, hoy);
    assert.equal(errores.horaFin, MENSAJE_FUERA_DE_HORARIO);
});

test("validarReserva rechaza una reserva más larga que el máximo", () => {
    const errores = validarReserva({ ...base, horaInicio: "09:00", horaFin: "12:00" }, hoy);
    assert.equal(errores.horaFin, MENSAJE_DURACION_EXCEDIDA);
});

test("validarReserva mantiene el mensaje de hora final inválida sin mezclarlo con los nuevos", () => {
    const errores = validarReserva({ ...base, horaInicio: "10:00", horaFin: "09:00" }, hoy);
    assert.equal(errores.horaFin, "La hora final debe ser mayor que la hora inicial.");
});
