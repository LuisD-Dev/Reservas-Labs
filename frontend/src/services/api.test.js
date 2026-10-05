import test from "node:test";
import assert from "node:assert/strict";
import {
    EVENTO_SESION_EXPIRADA,
    apiFetch,
    guardarSesion,
    obtenerSesion,
} from "./api.js";

const almacenamiento = new Map();

Object.defineProperty(globalThis, "localStorage", {
    configurable: true,
    value: {
        getItem: (clave) => almacenamiento.get(clave) ?? null,
        setItem: (clave, valor) => almacenamiento.set(clave, valor),
        removeItem: (clave) => almacenamiento.delete(clave),
    },
});

function crearToken(datos) {
    const encabezado = Buffer.from(JSON.stringify({ alg: "HS256", typ: "JWT" })).toString("base64url");
    const contenido = Buffer.from(JSON.stringify(datos)).toString("base64url");
    return `${encabezado}.${contenido}.firma`;
}

function prepararSesion() {
    almacenamiento.clear();
    Object.defineProperty(globalThis, "window", {
        configurable: true,
        value: new EventTarget(),
    });

    guardarSesion({
        token: crearToken({ role: "Administrador", name: "Ada" }),
        expiraUtc: new Date(Date.now() + 60_000).toISOString(),
    });
}

test("obtenerSesion usa el nombre y el rol del token", () => {
    prepararSesion();

    const sesion = obtenerSesion();

    assert.equal(sesion.nombre, "Ada");
    assert.equal(sesion.rol, "Administrador");
});

test("apiFetch envía el token y cierra la sesión ante cualquier 401", async () => {
    prepararSesion();
    let autorizacion;
    let eventos = 0;
    window.addEventListener(EVENTO_SESION_EXPIRADA, () => { eventos += 1; });
    Object.defineProperty(globalThis, "fetch", {
        configurable: true,
        value: async (_url, opciones) => {
            autorizacion = opciones.headers.Authorization;
            return new Response(null, { status: 401 });
        },
    });

    const respuesta = await apiFetch("/api/laboratorios");

    assert.equal(respuesta.status, 401);
    assert.match(autorizacion, /^Bearer /);
    assert.equal(obtenerSesion(), null);
    assert.equal(eventos, 1);
});

test("apiFetch avisa de inmediato si la sesión ya venció", async () => {
    almacenamiento.clear();
    Object.defineProperty(globalThis, "window", {
        configurable: true,
        value: new EventTarget(),
    });
    let eventos = 0;
    let llamadas = 0;
    window.addEventListener(EVENTO_SESION_EXPIRADA, () => { eventos += 1; });
    Object.defineProperty(globalThis, "fetch", {
        configurable: true,
        value: async () => {
            llamadas += 1;
            return new Response(null, { status: 200 });
        },
    });

    const respuesta = await apiFetch("/api/laboratorios");

    assert.equal(respuesta.status, 401);
    assert.equal(eventos, 1);
    assert.equal(llamadas, 0);
});
