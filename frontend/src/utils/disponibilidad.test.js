import test from "node:test";
import assert from "node:assert/strict";
import {
    evaluarDisponibilidad,
    formatearFecha,
    normalizarHorario,
    validarConsulta,
} from "./disponibilidad.js";

test("validarConsulta exige todos los campos", () => {
    assert.deepEqual(
        validarConsulta({ laboratorioId: "", fecha: "", horaInicio: "", horaFin: "" }),
        {
            laboratorioId: "Selecciona un laboratorio.",
            fecha: "Ingresa la fecha a consultar.",
            horaInicio: "Ingresa la hora inicial.",
            horaFin: "Ingresa la hora final.",
        }
    );
});

test("validarConsulta rechaza fechas pasadas y rangos de hora inválidos", () => {
    const errores = validarConsulta({
        laboratorioId: "1",
        fecha: "2000-01-01",
        horaInicio: "10:00",
        horaFin: "09:00",
    });

    assert.equal(errores.fecha, "La fecha no puede ser anterior a hoy.");
    assert.equal(errores.horaFin, "La hora final debe ser mayor que la hora inicial.");
});

test("normaliza el formato de fecha y hora enviado por .NET", () => {
    assert.deepEqual(
        normalizarHorario({
            id: 7,
            fecha: "2026-10-10T00:00:00",
            horaInicio: "08:00:00",
            horaFin: "10:30:00",
            estado: "Disponible",
        }),
        {
            id: 7,
            fecha: "2026-10-10",
            horaInicio: "08:00",
            horaFin: "10:30",
            estado: "Disponible",
        }
    );
});

test("formatea fechas ISO sin desplazarlas por zona horaria", () => {
    assert.equal(formatearFecha("2026-10-10"), "10/10/2026");
    assert.equal(formatearFecha("2026-10-10T00:00:00"), "10/10/2026");
});

test("marca disponible un rango cubierto por un horario libre", () => {
    const resultado = evaluarDisponibilidad(
        [
            {
                id: 1,
                fecha: "2026-10-10T00:00:00",
                horaInicio: "08:00:00",
                horaFin: "12:00:00",
                estado: "Disponible",
            },
        ],
        { fecha: "2026-10-10", horaInicio: "09:00", horaFin: "11:00" },
        { estado: "Habilitado" }
    );

    assert.equal(resultado.disponible, true);
    assert.equal(resultado.horariosDelDia.length, 1);
});

test("un laboratorio fuera de servicio nunca queda disponible", () => {
    const resultado = evaluarDisponibilidad(
        [
            {
                id: 1,
                fecha: "2026-10-10",
                horaInicio: "08:00",
                horaFin: "12:00",
                estado: "Disponible",
            },
        ],
        { fecha: "2026-10-10", horaInicio: "09:00", horaFin: "11:00" },
        { estado: "Fuera de servicio" }
    );

    assert.equal(resultado.disponible, false);
    assert.equal(resultado.motivo, "El laboratorio está fuera de servicio.");
});
