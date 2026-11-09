export function hoyComoTexto() {
    const hoy = new Date();
    const mes = String(hoy.getMonth() + 1).padStart(2, "0");
    const dia = String(hoy.getDate()).padStart(2, "0");
    return `${hoy.getFullYear()}-${mes}-${dia}`;
}

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
export function validarSolicitud({ laboratorioId, fecha, horaInicio, horaFin }, hoy = hoyComoTexto(), mensajeFecha = "Ingresa la fecha de la reserva.") {
    const errores = {};
    if (!laboratorioId) errores.laboratorioId = "Selecciona un laboratorio.";
    if (!fecha) errores.fecha = mensajeFecha;
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
