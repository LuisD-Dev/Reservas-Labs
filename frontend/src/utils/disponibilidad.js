// HU3 - Reglas de la consulta de disponibilidad, separadas de la interfaz
// para poder probarlas sin levantar la pantalla.

export function hoyComoTexto() {
    const hoy = new Date();
    const mes = String(hoy.getMonth() + 1).padStart(2, "0");
    const dia = String(hoy.getDate()).padStart(2, "0");
    return `${hoy.getFullYear()}-${mes}-${dia}`;
}

export function validarConsulta({ laboratorioId, fecha, horaInicio, horaFin }) {
    const errores = {};

    if (!laboratorioId) {
        errores.laboratorioId = "Selecciona un laboratorio.";
    }
    if (!fecha) {
        errores.fecha = "Ingresa la fecha a consultar.";
    } else if (fecha < hoyComoTexto()) {
        errores.fecha = "La fecha no puede ser anterior a hoy.";
    }
    if (!horaInicio) {
        errores.horaInicio = "Ingresa la hora inicial.";
    }
    if (!horaFin) {
        errores.horaFin = "Ingresa la hora final.";
    }
    if (horaInicio && horaFin && horaFin <= horaInicio) {
        errores.horaFin = "La hora final debe ser mayor que la hora inicial.";
    }

    return errores;
}

// HU3 - #39 Normaliza un horario tal como lo devuelve la API
// (fecha "2026-10-01T00:00:00", horas "08:00:00").
export function normalizarHorario(horario) {
    return {
        ...horario,
        fecha: String(horario.fecha).slice(0, 10),
        horaInicio: String(horario.horaInicio).slice(0, 5),
        horaFin: String(horario.horaFin).slice(0, 5),
    };
}

// HU3 - #39 Decide si el laboratorio está disponible para la consulta.
// Está disponible si ese día existe un horario "Disponible" que cubre todo
// el rango pedido. Un laboratorio "Fuera de servicio" nunca está disponible.
export function evaluarDisponibilidad(horarios, { fecha, horaInicio, horaFin }, laboratorio) {
    const horariosDelDia = horarios
        .map(normalizarHorario)
        .filter((h) => h.fecha === fecha)
        .sort((a, b) => a.horaInicio.localeCompare(b.horaInicio));

    if (laboratorio && laboratorio.estado !== "Habilitado") {
        return {
            disponible: false,
            motivo: "El laboratorio está fuera de servicio.",
            horariosDelDia,
        };
    }

    const cubre = horariosDelDia.find(
        (h) => h.estado === "Disponible" && h.horaInicio <= horaInicio && h.horaFin >= horaFin
    );

    if (cubre) {
        return {
            disponible: true,
            motivo: `El horario de ${cubre.horaInicio} a ${cubre.horaFin} está libre.`,
            horariosDelDia,
        };
    }

    return {
        disponible: false,
        motivo:
            horariosDelDia.length === 0
                ? "No hay horarios registrados para esa fecha."
                : "Ningún horario disponible cubre todo el rango solicitado.",
        horariosDelDia,
    };
}
