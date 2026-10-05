// NFR1 - #46 Cliente de la API: guarda la sesión y envía el token JWT
// en cada petición. Si la API responde 401 o el token venció, la sesión
// se cierra y se avisa a la aplicación con el evento "sesion-expirada".

export const API_BASE_URL = import.meta.env?.VITE_API_URL ?? "http://localhost:5282";

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

const CLAIM_ROL = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role";
const CLAIM_NOMBRE = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name";

// NFR1 - #47 Lee los datos del token. El rol y el nombre se toman de aquí y
// no del texto guardado, para que editar localStorage no cambie el rol.
// Si alguien altera el token, la firma deja de ser válida y la API responde 401.
function leerToken(token) {
    const base64 = token.split(".")[1].replace(/-/g, "+").replace(/_/g, "/");
    const relleno = "=".repeat((4 - (base64.length % 4)) % 4);
    const bytes = Uint8Array.from(atob(base64 + relleno), (c) => c.charCodeAt(0));
    return JSON.parse(new TextDecoder().decode(bytes));
}

// Devuelve la sesión guardada solo si tiene token y no ha vencido.
export function obtenerSesion() {
    try {
        const sesion = JSON.parse(localStorage.getItem(CLAVE_SESION));
        if (!sesion?.token || tokenVencido(sesion)) {
            cerrarSesion();
            return null;
        }
        const datos = leerToken(sesion.token);
        return {
            ...sesion,
            rol: datos.role ?? datos[CLAIM_ROL] ?? null,
            nombre: datos.unique_name ?? datos.name ?? datos[CLAIM_NOMBRE] ?? sesion.nombre,
        };
    } catch {
        cerrarSesion();
        return null;
    }
}

// Igual que fetch, pero con la URL base de la API y el token en el encabezado.
export async function apiFetch(ruta, opciones = {}) {
    const sesion = obtenerSesion();
    const headers = { ...(opciones.headers ?? {}) };

    if (!sesion) {
        window.dispatchEvent(new Event(EVENTO_SESION_EXPIRADA));
        return new Response(null, { status: 401, statusText: "Unauthorized" });
    }

    headers.Authorization = `Bearer ${sesion.token}`;

    const respuesta = await fetch(`${API_BASE_URL}${ruta}`, { ...opciones, headers });

    if (respuesta.status === 401) {
        cerrarSesion();
        window.dispatchEvent(new Event(EVENTO_SESION_EXPIRADA));
    }

    return respuesta;
}
