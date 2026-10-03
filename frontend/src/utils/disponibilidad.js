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
