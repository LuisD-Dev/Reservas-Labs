export const laboratorios = [
    { id: 7, nombre: 'Software', ubicacion: 'Edificio A', capacidad: 30, estado: 'Habilitado' },
    { id: 9, nombre: 'Redes', ubicacion: 'Edificio B', capacidad: 20, estado: 'Fuera de servicio' },
];

export function fechaFutura() {
    const fecha = new Date();
    fecha.setDate(fecha.getDate() + 2);
    return `${fecha.getFullYear()}-${String(fecha.getMonth() + 1).padStart(2, '0')}-${String(fecha.getDate()).padStart(2, '0')}`;
}

export function respuesta(datos, status = 200) {
    return { ok: status >= 200 && status < 300, status, json: async () => datos };
}

export function reservas() {
    return [
        { id: 11, laboratorioId: 7, nombreLaboratorio: 'Software', fecha: fechaFutura(), horaInicio: '09:00:00', horaFin: '10:00:00', estado: 'Activa' },
        { id: 12, laboratorioId: 9, nombreLaboratorio: 'Redes', fecha: fechaFutura(), horaInicio: '11:00:00', horaFin: '12:00:00', estado: 'Cancelada' },
    ];
}
