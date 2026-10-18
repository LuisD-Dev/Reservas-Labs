import { hoyComoTexto } from "./disponibilidad.js";

// AB#69 y AB#70: mismos valores y mensajes que ReglasReserva del backend.
export const HORA_APERTURA = "07:00";
export const HORA_CIERRE = "21:00";
export const DURACION_MAXIMA_MINUTOS = 240;
export const MENSAJE_FUERA_DE_HORARIO =
    `La reserva debe estar dentro del horario de funcionamiento (${HORA_APERTURA} a ${HORA_CIERRE}).`;
export const MENSAJE_DURACION_EXCEDIDA =
    `La reserva no puede durar más de ${DURACION_MAXIMA_MINUTOS / 60} horas.`;

const aMinutos = (hora) => {
    const [horas, minutos] = hora.split(":").map(Number);
    return horas * 60 + minutos;
};

// AB#62, AB#67, AB#68, AB#69 y AB#70: campos obligatorios, orden de horas, fecha,
// horario de funcionamiento y duración máxima.
export function validarReserva({ laboratorioId, fecha, horaInicio, horaFin }, hoy = hoyComoTexto()) {
    const errores = {};
    if (!laboratorioId) errores.laboratorioId = "Selecciona un laboratorio.";
    if (!fecha) errores.fecha = "Ingresa la fecha de la reserva.";
    else if (fecha < hoy) errores.fecha = "La fecha no puede ser anterior a hoy.";
    if (!horaInicio) errores.horaInicio = "Ingresa la hora inicial.";
    if (!horaFin) errores.horaFin = "Ingresa la hora final.";
    if (horaInicio && horaFin && horaFin <= horaInicio) {
        errores.horaFin = "La hora final debe ser mayor que la hora inicial.";
    }
    if (horaInicio && horaFin && !errores.horaFin) {
        const inicio = aMinutos(horaInicio);
        const fin = aMinutos(horaFin);
        if (inicio < aMinutos(HORA_APERTURA) || fin > aMinutos(HORA_CIERRE)) {
            errores.horaFin = MENSAJE_FUERA_DE_HORARIO;
        } else if (fin - inicio > DURACION_MAXIMA_MINUTOS) {
            errores.horaFin = MENSAJE_DURACION_EXCEDIDA;
        }
    }
    return errores;
}

export function crearCuerpoReserva({ laboratorioId, fecha, horaInicio, horaFin }) {
    const conSegundos = (hora) => hora.length === 5 ? `${hora}:00` : hora;
    return {
        laboratorioId: Number(laboratorioId), fecha,
        horaInicio: conSegundos(horaInicio), horaFin: conSegundos(horaFin)
    };
}

export function mensajeErrorReserva(status, datos) {
    if (!status) return "No se pudo conectar con el servidor. Verifica tu conexión e intenta de nuevo.";
    if (status === 401) return "Tu sesión venció. Inicia sesión de nuevo.";
    if (status === 403) return "Tu usuario no tiene permiso para registrar reservas.";
    if (status === 400) {
        const detalles = datos?.errores ?? datos?.errors;
        const mensajes = (Array.isArray(detalles) ? detalles : Object.values(detalles ?? {}))
            .flat().filter((mensaje) => typeof mensaje === "string");
        return mensajes.length ? mensajes.join(" ")
            : datos?.mensaje || datos?.detail || datos?.title || "Revisa los datos de la reserva.";
    }
    return "No se pudo registrar la reserva. Intenta de nuevo.";
}


// AB#66: texto de confirmación tras registrar la reserva.
export function mensajeConfirmacionReserva(reserva, nombreLaboratorio) {
    const base = "Reserva registrada correctamente.";
    if (!reserva) return base;
    const hora = (valor) => String(valor ?? "").slice(0, 5);
    const detalle = [
        nombreLaboratorio ? `Laboratorio: ${nombreLaboratorio}` : null,
        reserva.fecha ? `Fecha: ${reserva.fecha}` : null,
        reserva.horaInicio && reserva.horaFin
            ? `Horario: ${hora(reserva.horaInicio)} a ${hora(reserva.horaFin)}` : null,
    ].filter(Boolean);
    return detalle.length ? `${base} ${detalle.join(", ")}.` : base;
}