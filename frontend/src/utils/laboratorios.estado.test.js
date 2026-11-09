import test from "node:test";
import assert from "node:assert/strict";
import {
    cuerpoCambioEstado,
    estadoOpuesto,
    etiquetaCambioEstado,
    mensajeErrorCambioEstado,
    reemplazarLaboratorio,
    rutaEstadoLaboratorio,
} from "./laboratorios.js";

test("estadoOpuesto alterna entre Habilitado y Fuera de servicio (AB#111)", () => {
    assert.equal(estadoOpuesto("Habilitado"), "Fuera de servicio");
    assert.equal(estadoOpuesto("Fuera de servicio"), "Habilitado");
    assert.equal(estadoOpuesto(undefined), "Habilitado");
});

test("etiquetaCambioEstado describe la acción disponible", () => {
    assert.equal(etiquetaCambioEstado("Habilitado"), "Marcar fuera de servicio");
    assert.equal(etiquetaCambioEstado("Fuera de servicio"), "Habilitar");
});

test("rutaEstadoLaboratorio y cuerpoCambioEstado arman la petición PATCH", () => {
    assert.equal(rutaEstadoLaboratorio(7), "/api/laboratorios/7/estado");
    assert.deepEqual(cuerpoCambioEstado("Habilitado"), { estado: "Habilitado" });
});

test("reemplazarLaboratorio cambia solo el laboratorio indicado y no modifica la lista original (AB#112)", () => {
    const original = [
        { id: 7, nombre: "Software", estado: "Habilitado" },
        { id: 9, nombre: "Redes", estado: "Fuera de servicio" },
    ];
    const resultado = reemplazarLaboratorio(original, { id: 7, nombre: "Software", estado: "Fuera de servicio" });
    assert.equal(resultado[0].estado, "Fuera de servicio");
    assert.equal(resultado[1].estado, "Fuera de servicio");
    assert.equal(original[0].estado, "Habilitado");
});

test("reemplazarLaboratorio ignora un laboratorio vacío o una lista inválida", () => {
    const lista = [{ id: 7, estado: "Habilitado" }];
    assert.deepEqual(reemplazarLaboratorio(lista, null), lista);
    assert.deepEqual(reemplazarLaboratorio(null, { id: 7 }), []);
});

test("mensajeErrorCambioEstado distingue conexión, 401, 403, 404, 400 y otros errores (AB#113)", () => {
    assert.match(mensajeErrorCambioEstado(), /conectar con el servidor/);
    assert.match(mensajeErrorCambioEstado(401), /sesión venció/);
    assert.match(mensajeErrorCambioEstado(403), /Solo el administrador/);
    assert.equal(mensajeErrorCambioEstado(404, { mensaje: "No existe." }), "No existe.");
    assert.match(mensajeErrorCambioEstado(404), /ya no existe/);
    assert.match(mensajeErrorCambioEstado(400), /no es válido/);
    assert.match(mensajeErrorCambioEstado(500), /No se pudo actualizar/);
});