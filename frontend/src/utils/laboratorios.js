export function esLaboratorioHabilitado(laboratorio) {
    return laboratorio?.estado === "Habilitado";
}

export function filtrarLaboratoriosHabilitados(laboratorios) {
    return Array.isArray(laboratorios) ? laboratorios.filter(esLaboratorioHabilitado) : [];
}
