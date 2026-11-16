// Gestión de Reservas (Administrador): funciones de apoyo de la pantalla.
import { esReservaActiva, esReservaCancelada } from "./misReservas.js";
import {
    crearCuerpoReserva,
    mensajeConfirmacionReserva,
    mensajeErrorReserva,
    validarReserva,
} from "./reserva.js";

export const RUTA_RESERVAS_GESTION = "/api/reservas";
export const RUTA_CREAR_RESERVA_GESTION = "/api/reservas/gestion";

export const FILTROS_INICIALES = { busqueda: "", estado: "", laboratorioId: "", fecha: "" };

// Sin mayúsculas ni tildes, para que "Programación" se encuentre al escribir "programacion".
function normalizarTexto(texto) {
    return String(texto ?? "")
        .trim()
        .toLowerCase()
        .normalize("NFD")
        .replace(/[̀-ͯ]/g, "");
}

function comoLista(reservas) {
    return Array.isArray(reservas) ? reservas : [];
}

export function hayFiltrosActivos(filtros) {
    const actuales = { ...FILTROS_INICIALES, ...filtros };
    return Object.keys(FILTROS_INICIALES).some((clave) => String(actuales[clave] ?? "").trim() !== "");
}

// Filtra sin modificar la lista y conserva el orden recibido.
// estado: "" (todas), "activa" o "cancelada". fecha: "AAAA-MM-DD".
// busqueda: coincide con el nombre o usuario del responsable y con el laboratorio.
export function filtrarReservasGestion(reservas, filtros = FILTROS_INICIALES) {
    const { busqueda, estado, laboratorioId, fecha } = { ...FILTROS_INICIALES, ...filtros };
    const texto = normalizarTexto(busqueda);

    return comoLista(reservas).filter((reserva) => {
        if (estado === "activa" && !esReservaActiva(reserva.estado)) return false;
        if (estado === "cancelada" && !esReservaCancelada(reserva.estado)) return false;
        if (laboratorioId && String(reserva.laboratorioId) !== String(laboratorioId)) return false;
        if (fecha && !String(reserva.fecha ?? "").startsWith(fecha)) return false;
        if (texto) {
            const campos = [reserva.nombreUsuario, reserva.username, reserva.nombreLaboratorio];
            if (!campos.some((campo) => normalizarTexto(campo).includes(texto))) return false;
        }
        return true;
    });
}

// Laboratorios que aparecen en las reservas, sin repetir y por nombre, para el filtro.
export function laboratoriosDeReservas(reservas) {
    const porId = new Map();
    for (const reserva of comoLista(reservas)) {
        if (!porId.has(reserva.laboratorioId)) {
            porId.set(reserva.laboratorioId, { id: reserva.laboratorioId, nombre: reserva.nombreLaboratorio });
        }
    }
    return [...porId.values()].sort((a, b) => String(a.nombre).localeCompare(String(b.nombre), "es"));
}

export function contarReservas(reservas) {
    const lista = comoLista(reservas);
    return {
        total: lista.length,
        activas: lista.filter((reserva) => esReservaActiva(reserva.estado)).length,
        canceladas: lista.filter((reserva) => esReservaCancelada(reserva.estado)).length,
    };
}

// Mismas reglas que Registrar reserva, más el usuario al que se le registra.
export function validarReservaGestion(datos, hoy) {
    const errores = validarReserva(datos, hoy);
    if (!String(datos?.username ?? "").trim()) {
        errores.username = "Indica el usuario responsable de la reserva.";
    }
    return errores;
}

export function crearCuerpoReservaGestion(datos) {
    return { username: String(datos?.username ?? "").trim(), ...crearCuerpoReserva(datos) };
}

export function mensajeErrorGestion(status) {
    if (!status) return "No se pudo conectar con el servidor. Verifica tu conexión e intenta de nuevo.";
    if (status === 401) return "Tu sesión venció. Inicia sesión de nuevo.";
    if (status === 403) return "Solo un administrador puede gestionar las reservas.";
    return "No se pudieron cargar las reservas. Intenta de nuevo.";
}

export function mensajeErrorCrearGestion(status, datos) {
    if (status === 403) return "Solo un administrador puede registrar reservas a nombre de otro usuario.";
    return mensajeErrorReserva(status, datos);
}

// "Reserva registrada correctamente. Laboratorio: ..., Fecha: ..., Horario: ... Responsable: ana."
export function mensajeConfirmacionGestion(reserva, nombreLaboratorio) {
    const base = mensajeConfirmacionReserva(reserva, nombreLaboratorio);
    return reserva?.responsable ? `${base} Responsable: ${reserva.responsable}.` : base;
}
