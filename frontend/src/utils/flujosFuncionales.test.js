import test from "node:test";
import assert from "node:assert/strict";
import { validarConsulta, interpretarDisponibilidad } from "./disponibilidad.js";
import { validarReserva, MENSAJE_FUERA_DE_HORARIO, MENSAJE_DURACION_EXCEDIDA } from "./reserva.js";
import { cancelarConConfirmacion } from "./cancelarReserva.js";
import { crearCredenciales } from "./login.js";

const base = { laboratorioId: "1", fecha: "2030-01-02" };
const hoy = "2030-01-01";

for (const [inicio, fin, mensaje] of [
    ["06:00", "08:00", MENSAJE_FUERA_DE_HORARIO],
    ["20:00", "22:00", MENSAJE_FUERA_DE_HORARIO],
    ["08:00", "12:01", MENSAJE_DURACION_EXCEDIDA],
    ["10:00", "09:00", "La hora final debe ser mayor que la hora inicial."],
]) {
    test(`consulta y registro comparten validación para ${inicio}–${fin}`, () => {
        const datos = { ...base, horaInicio: inicio, horaFin: fin };
        assert.deepEqual(validarConsulta(datos, hoy), validarReserva(datos, hoy));
        assert.equal(validarConsulta(datos, hoy).horaFin, mensaje);
    });
}

test("ambos flujos aceptan límites válidos y rechazan fechas pasadas", () => {
    for (const validar of [validarConsulta, validarReserva]) {
        assert.deepEqual(validar({ ...base, horaInicio: "07:00", horaFin: "11:00" }, hoy), {});
        assert.deepEqual(validar({ ...base, horaInicio: "17:00", horaFin: "21:00" }, hoy), {});
        assert.equal(validar({ ...base, fecha: "2000-01-01" }, hoy).fecha,
            "La fecha no puede ser anterior a hoy.");
        assert.equal(validar({}, hoy).laboratorioId, "Selecciona un laboratorio.");
    }
});

test("rechazar confirmación no envía peticiones", async () => {
    let llamadas = 0;
    const resultado = await cancelarConConfirmacion(7, () => { llamadas++; }, () => false);
    assert.equal(resultado, null);
    assert.equal(llamadas, 0);
});

test("aceptar confirmación envía una petición conservando POST", async () => {
    const respuesta = { status: 200 };
    const llamadas = [];
    const resultado = await cancelarConConfirmacion(7, (...args) => {
        llamadas.push(args);
        return respuesta;
    }, () => true);
    assert.equal(resultado, respuesta);
    assert.deepEqual(llamadas, [["/api/reservas/7/cancelar", { method: "POST" }]]);
});

test("el veredicto negativo del backend prevalece sobre horarios nominales", () => {
    const resultado = interpretarDisponibilidad({
        disponible: false, motivo: "Conflicto", horariosDelDia: [
            { id: 1, fecha: "2030-01-02", horaInicio: "08:00:00", horaFin: "12:00:00", estado: "Disponible" },
        ],
    });
    assert.equal(resultado.disponible, false);
    assert.equal(resultado.motivo, "Conflicto");
    assert.equal(resultado.horariosDelDia[0].horaInicio, "08:00");
});

test("interpreta disponibilidad positiva y rechaza respuestas incompletas", () => {
    assert.equal(interpretarDisponibilidad({ disponible: true, motivo: "Libre", horariosDelDia: [] }).disponible, true);
    assert.throws(() => interpretarDisponibilidad({ horariosDelDia: [] }));
});

test("login conserva caracteres Unicode y espacios de contraseña", () => {
    assert.deepEqual(crearCredenciales("  José + 工 @dominio  ", "  clave con espacios  "), {
        Username: "José + 工 @dominio", Password: "  clave con espacios  ",
    });
});
