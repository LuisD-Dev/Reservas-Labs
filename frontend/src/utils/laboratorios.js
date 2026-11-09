export const ESTADO_HABILITADO = "Habilitado";
export const ESTADO_FUERA_DE_SERVICIO = "Fuera de servicio";

export function esLaboratorioHabilitado(laboratorio) {
    return laboratorio?.estado === ESTADO_HABILITADO;
}

export function filtrarLaboratoriosHabilitados(laboratorios) {
    return Array.isArray(laboratorios) ? laboratorios.filter(esLaboratorioHabilitado) : [];
}

// HU9 - AB#111: estado al que pasa un laboratorio cuando el administrador lo cambia.
export function estadoOpuesto(estado) {
    return estado === ESTADO_HABILITADO ? ESTADO_FUERA_DE_SERVICIO : ESTADO_HABILITADO;
}

export function etiquetaCambioEstado(estado) {
    return estado === ESTADO_HABILITADO ? "Marcar fuera de servicio" : "Habilitar";
}

export function rutaEstadoLaboratorio(id) {
    return `/api/laboratorios/${encodeURIComponent(id)}/estado`;
}

export function cuerpoCambioEstado(estado) {
    return { estado };
}

// HU9 - AB#112: reemplaza el laboratorio actualizado sin modificar la lista original.
export function reemplazarLaboratorio(laboratorios, actualizado) {
    const lista = Array.isArray(laboratorios) ? laboratorios : [];
    if (!actualizado) return lista;
    return lista.map((lab) => (lab.id === actualizado.id ? { ...lab, ...actualizado } : lab));
}

export function mensajeErrorCambioEstado(status, datos) {
    if (!status) return "No se pudo conectar con el servidor. Verifica tu conexión e intenta de nuevo.";
    if (status === 401) return "Tu sesión venció. Inicia sesión de nuevo.";
    if (status === 403) return "Solo el administrador puede cambiar el estado de un laboratorio.";
    if (status === 404) return datos?.mensaje || "El laboratorio ya no existe.";
    if (status === 400) return datos?.mensaje || "El estado indicado no es válido.";
    return "No se pudo actualizar el estado del laboratorio. Intenta de nuevo.";
}