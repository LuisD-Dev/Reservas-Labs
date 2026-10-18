// AB#62: solo campos obligatorios, sin nuevas reglas de fecha u horario.
export function validarReserva({ laboratorioId, fecha, horaInicio, horaFin }) {
    const errores = {};
    if (!laboratorioId) errores.laboratorioId = "Selecciona un laboratorio.";
    if (!fecha) errores.fecha = "Ingresa la fecha de la reserva.";
    if (!horaInicio) errores.horaInicio = "Ingresa la hora inicial.";
    if (!horaFin) errores.horaFin = "Ingresa la hora final.";
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