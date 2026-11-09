import test from "node:test";
import assert from "node:assert/strict";
import {
    esReservaActiva,
    esReservaCancelada,
    separarReservas,
    tipoEstado,
} from "./misReservas.js";

test("esReservaCancelada reconoce Cancelada sin importar mayúsculas ni espacios (AB#95)", () => {
    assert.equal(esReservaCancelada("Cancelada"), true);
    assert.equal(esReservaCancelada("  CANCELADA "), true);
    assert.equal(esReservaCancelada("Activa"), false);
    assert.equal(esReservaCancelada(null), false);
    assert.equal(esReservaCancelada(undefined), false);
});

test("esReservaActiva reconoce solo el estado Activa", () => {
    assert.equal(esReservaActiva("Activa"), true);
    assert.equal(esReservaActiva(" activa "), true);
    assert.equal(esReservaActiva("Cancelada"), false);
    assert.equal(esReservaActiva(null), false);
});

test("tipoEstado clasifica en activa, cancelada u otro", () => {
    assert.equal(tipoEstado("Activa"), "activa");
    assert.equal(tipoEstado("Cancelada"), "cancelada");
    assert.equal(tipoEstado("Finalizada"), "otro");
    assert.equal(tipoEstado(undefined), "otro");
});

test("separarReservas divide en activas y canceladas conservando el orden", () => {
    const { activas, canceladas } = separarReservas([
        { id: 1, estado: "Activa" },
        { id: 2, estado: "Cancelada" },
        { id: 3, estado: "Activa" },
        { id: 4, estado: "Cancelada" },
    ]);
    assert.deepEqual(activas.map((reserva) => reserva.id), [1, 3]);
    assert.deepEqual(canceladas.map((reserva) => reserva.id), [2, 4]);
});

test("separarReservas deja con las activas cualquier estado que no sea cancelado", () => {
    const { activas, canceladas } = separarReservas([
        { id: 1, estado: "Finalizada" },
        { id: 2, estado: "Cancelada" },
    ]);
    assert.deepEqual(activas.map((reserva) => reserva.id), [1]);
    assert.deepEqual(canceladas.map((reserva) => reserva.id), [2]);
});

test("separarReservas con una lista inválida devuelve grupos vacíos", () => {
    assert.deepEqual(separarReservas(null), { activas: [], canceladas: [] });
    assert.deepEqual(separarReservas({ mensaje: "error" }), { activas: [], canceladas: [] });
});