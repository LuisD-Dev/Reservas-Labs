import { validarSolicitud } from "./validacionHorario.js";
export { hoyComoTexto } from "./validacionHorario.js";

export function validarConsulta(datos, hoy) {
    return validarSolicitud(datos, hoy, "Ingresa la fecha a consultar.");
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

// Presenta fechas ISO sin convertirlas a Date, evitando cambios de día por zona horaria.
export function formatearFecha(fecha) {
    const partes = /^(\d{4})-(\d{2})-(\d{2})/.exec(String(fecha ?? ""));
    return partes ? `${partes[3]}/${partes[2]}/${partes[1]}` : String(fecha ?? "");
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
                ? "No hay horarios disponibles para esa fecha."
                : "Ningún horario disponible cubre todo el rango solicitado.",
        horariosDelDia,
    };
}

// El veredicto procede del servidor; nunca se recalcula con horarios nominales.
export function interpretarDisponibilidad(datos) {
    if (typeof datos?.disponible !== "boolean" || typeof datos?.motivo !== "string"
        || !Array.isArray(datos?.horariosDelDia)) {
        throw new Error("Respuesta de disponibilidad inválida.");
    }
    return {
        disponible: datos.disponible,
        motivo: datos.motivo,
        horariosDelDia: datos.horariosDelDia.map(normalizarHorario),
    };
}
