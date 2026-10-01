// NFR1 - #46 Cliente de la API: guarda la sesión y envía el token JWT
// en cada petición. Si la API responde 401 o el token venció, la sesión
// se cierra y se avisa a la aplicación con el evento "sesion-expirada".

export const API_BASE_URL = import.meta.env.VITE_API_URL ?? "http://localhost:5282";

const CLAVE_SESION = "usuario";
export const EVENTO_SESION_EXPIRADA = "sesion-expirada";

export function guardarSesion(sesion) {
    localStorage.setItem(CLAVE_SESION, JSON.stringify(sesion));
}

export function cerrarSesion() {
    localStorage.removeItem(CLAVE_SESION);
}

function tokenVencido(sesion) {
    if (!sesion?.expiraUtc) {
        return true;
    }
    return new Date(sesion.expiraUtc).getTime() <= Date.now();
}

// Devuelve la sesión guardada solo si tiene token y no ha vencido.
export function obtenerSesion() {
    try {
        const sesion = JSON.parse(localStorage.getItem(CLAVE_SESION));
        if (!sesion?.token || tokenVencido(sesion)) {
            cerrarSesion();
            return null;
        }
        return sesion;
    } catch {
        cerrarSesion();
        return null;
    }
}

// Igual que fetch, pero con la URL base de la API y el token en el encabezado.
export async function apiFetch(ruta, opciones = {}) {
    const sesion = obtenerSesion();
    const headers = { ...(opciones.headers ?? {}) };

    if (sesion?.token) {
        headers.Authorization = `Bearer ${sesion.token}`;
    }

    const respuesta = await fetch(`${API_BASE_URL}${ruta}`, { ...opciones, headers });

    if (respuesta.status === 401 && sesion) {
        cerrarSesion();
        window.dispatchEvent(new Event(EVENTO_SESION_EXPIRADA));
    }

    return respuesta;
}
