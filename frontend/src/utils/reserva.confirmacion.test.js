import test from "node:test";
import assert from "node:assert/strict";
import { mensajeConfirmacionReserva } from "./reserva.js";

test("la confirmación incluye laboratorio, fecha y horario (AB#66)", () => {
    const mensaje = mensajeConfirmacionReserva(
        { fecha: "2026-10-20", horaInicio: "09:00:00", horaFin: "11:00:00" },
        "Laboratorio 3");
    assert.equal(mensaje,
        "Reserva registrada correctamente. Laboratorio: Laboratorio 3, Fecha: 2026-10-20, Horario: 09:00 a 11:00.");
});

test("la confirmación omite el laboratorio si no se conoce su nombre", () => {
    const mensaje = mensajeConfirmacionReserva(
        { fecha: "2026-10-20", horaInicio: "09:00:00", horaFin: "11:00:00" });
    assert.equal(mensaje,
        "Reserva registrada correctamente. Fecha: 2026-10-20, Horario: 09:00 a 11:00.");
});

test("sin datos de la reserva muestra solo el mensaje base", () => {
    assert.equal(mensajeConfirmacionReserva(null, "Laboratorio 3"),
        "Reserva registrada correctamente.");
});
