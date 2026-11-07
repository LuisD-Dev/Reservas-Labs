import test from "node:test";
import assert from "node:assert/strict";
import { MENSAJE_CANCELACION_EXITOSA, resultadoCancelacion } from "./cancelarReserva.js";

test("resultadoCancelacion muestra la confirmación cuando la reserva se cancela (AB#103)", () => {
    const resultado = resultadoCancelacion(200, { mensaje: "Reserva cancelada correctamente." });
    assert.equal(resultado.exito, true);
    assert.equal(resultado.mensaje, "Reserva cancelada correctamente.");
    assert.equal(resultado.recargar, false);
});

test("resultadoCancelacion usa el mensaje por defecto si el servidor no envía texto", () => {
    assert.equal(resultadoCancelacion(200, {}).mensaje, MENSAJE_CANCELACION_EXITOSA);
    assert.equal(resultadoCancelacion(200, null).mensaje, MENSAJE_CANCELACION_EXITOSA);
    assert.equal(resultadoCancelacion(200, { mensaje: "   " }).mensaje, MENSAJE_CANCELACION_EXITOSA);
});

test("resultadoCancelacion avisa cuando la reserva no existe y pide recargar la lista", () => {
    const resultado = resultadoCancelacion(404, { mensaje: "La reserva no existe." });
    assert.equal(resultado.exito, false);
    assert.match(resultado.mensaje, /no existe/);
    assert.equal(resultado.recargar, true);
});

test("resultadoCancelacion avisa cuando la reserva ya estaba cancelada y pide recargar", () => {
    const resultado = resultadoCancelacion(409, { mensaje: "La reserva ya está cancelada." });
    assert.equal(resultado.exito, false);
    assert.equal(resultado.mensaje, "La reserva ya está cancelada.");
    assert.equal(resultado.recargar, true);
});

test("resultadoCancelacion en 409 tiene mensaje propio si el servidor no envía texto", () => {
    const resultado = resultadoCancelacion(409, undefined);
    assert.match(resultado.mensaje, /ya no se puede cancelar/);
});

test("resultadoCancelacion avisa cuando no tiene permiso porque la reserva es de otro usuario", () => {
    const resultado = resultadoCancelacion(403, { mensaje: "No tienes permiso para cancelar esta reserva." });
    assert.equal(resultado.exito, false);
    assert.match(resultado.mensaje, /pertenece a otro usuario/);
    assert.equal(resultado.recargar, false);
});

test("resultadoCancelacion avisa cuando no hay comunicación con el servidor", () => {
    for (const status of [undefined, 0, null]) {
        const resultado = resultadoCancelacion(status);
        assert.equal(resultado.exito, false);
        assert.match(resultado.mensaje, /conectar con el servidor/);
        assert.equal(resultado.recargar, false);
    }
});

test("resultadoCancelacion avisa cuando la sesión venció", () => {
    const resultado = resultadoCancelacion(401);
    assert.equal(resultado.exito, false);
    assert.match(resultado.mensaje, /sesión venció/);
});

test("resultadoCancelacion da un mensaje general para otros errores", () => {
    const resultado = resultadoCancelacion(500, { mensaje: "detalle interno" });
    assert.equal(resultado.exito, false);
    assert.match(resultado.mensaje, /No se pudo cancelar/);
    assert.doesNotMatch(resultado.mensaje, /detalle interno/);
});
