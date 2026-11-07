import test from "node:test";
import assert from "node:assert/strict";
import {
    mensajeErrorCancelar,
    puedeCancelarReserva,
    rutaCancelarReserva,
} from "./cancelarReserva.js";

test("puedeCancelarReserva solo permite cancelar reservas activas (AB#102)", () => {
    assert.equal(puedeCancelarReserva({ estado: "Activa" }), true);
    assert.equal(puedeCancelarReserva({ estado: "Cancelada" }), false);
    assert.equal(puedeCancelarReserva({}), false);
    assert.equal(puedeCancelarReserva(null), false);
});

test("rutaCancelarReserva arma la ruta del endpoint de cancelar", () => {
    assert.equal(rutaCancelarReserva(7), "/api/reservas/7/cancelar");
});

test("mensajeErrorCancelar distingue conexión, 401, 403 y 404", () => {
    assert.match(mensajeErrorCancelar(), /conectar con el servidor/);
    assert.match(mensajeErrorCancelar(401), /sesión venció/);
    assert.match(mensajeErrorCancelar(403), /pertenece a otro usuario/);
    assert.match(mensajeErrorCancelar(404), /ya no existe/);
});

test("mensajeErrorCancelar usa el mensaje del servidor en un 409 y un texto genérico en otros errores", () => {
    assert.equal(mensajeErrorCancelar(409, { mensaje: "La reserva ya está cancelada." }), "La reserva ya está cancelada.");
    assert.match(mensajeErrorCancelar(409), /ya no se puede cancelar/);
    assert.match(mensajeErrorCancelar(500), /No se pudo cancelar/);
});