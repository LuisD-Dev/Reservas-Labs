// HU8 - AB#102: funciones de apoyo para cancelar una reserva desde Mis Reservas.
import { esReservaActiva } from "./misReservas.js";

// Solo las reservas activas se pueden cancelar.
export function puedeCancelarReserva(reserva) {
    return esReservaActiva(reserva?.estado);
}

export function rutaCancelarReserva(id) {
    return `/api/reservas/${encodeURIComponent(id)}/cancelar`;
}

// Mensaje según la respuesta del backend (401, 403, 404 y 409 vienen con { mensaje }).
export function mensajeErrorCancelar(status, datos) {
    if (!status) return "No se pudo conectar con el servidor. Verifica tu conexión e intenta de nuevo.";
    if (status === 401) return "Tu sesión venció. Inicia sesión de nuevo.";
    if (status === 403) return "No puedes cancelar esta reserva porque pertenece a otro usuario.";
    if (status === 404) return "La reserva ya no existe.";
    if (status === 409) return datos?.mensaje || "La reserva ya no se puede cancelar.";
    return "No se pudo cancelar la reserva. Intenta de nuevo.";
}

// HU8 - AB#103: aviso que se muestra al terminar de cancelar una reserva.
export const MENSAJE_CANCELACION_EXITOSA = "Reserva cancelada correctamente.";

function textoDelServidor(datos) {
    const mensaje = datos?.mensaje;
    return typeof mensaje === "string" && mensaje.trim() ? mensaje.trim() : "";
}

// Traduce la respuesta de POST /api/reservas/{id}/cancelar a un aviso para la persona.
// `status` es el código HTTP; si es undefined, no hubo comunicación con el servidor.
// Los mensajes de error vienen de mensajeErrorCancelar.
// Devuelve { exito, mensaje, recargar }: `recargar` indica que conviene volver a pedir la
// lista porque lo que se ve en pantalla ya no coincide con el servidor (404 o 409).
export function resultadoCancelacion(status, datos) {
    if (status >= 200 && status < 300) {
        return {
            exito: true,
            recargar: false,
            mensaje: textoDelServidor(datos) || MENSAJE_CANCELACION_EXITOSA,
        };
    }
    return {
        exito: false,
        recargar: status === 404 || status === 409,
        mensaje: mensajeErrorCancelar(status, datos),
    };
}

export async function cancelarConConfirmacion(id, enviar, confirmar) {
    if (!confirmar("¿Deseas cancelar esta reserva?")) return null;
    return enviar(rutaCancelarReserva(id), { method: "POST" });
}
