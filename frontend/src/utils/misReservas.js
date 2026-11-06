// AB#94: funciones de apoyo de la pantalla Mis Reservas.

// Próximas primero: ordena por fecha y hora de inicio. No modifica la lista original.
export function ordenarReservas(reservas) {
    const lista = Array.isArray(reservas) ? [...reservas] : [];
    return lista.sort((a, b) =>
        `${a.fecha} ${a.horaInicio}`.localeCompare(`${b.fecha} ${b.horaInicio}`));
}

// "09:00:00" -> "09:00"
export function formatearHoraReserva(hora) {
    return String(hora ?? "").slice(0, 5);
}

// "2026-10-20" -> "20/10/2026", sin pasar por Date para no desplazar el día por zona horaria.
export function formatearFechaReserva(fecha) {
    const texto = String(fecha ?? "");
    const coincidencia = /^(\d{4})-(\d{2})-(\d{2})/.exec(texto);
    if (!coincidencia) return texto;
    const [, anio, mes, dia] = coincidencia;
    return `${dia}/${mes}/${anio}`;
}

export function mensajeErrorMisReservas(status) {
    if (!status) return "No se pudo conectar con el servidor. Verifica tu conexión e intenta de nuevo.";
    if (status === 401) return "Tu sesión venció. Inicia sesión de nuevo.";
    if (status === 403) return "Tu usuario no tiene permiso para consultar reservas.";
    return "No se pudieron cargar tus reservas. Intenta de nuevo.";
}