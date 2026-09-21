import { useState } from "react";

const API_URL = "http://localhost:5282/api/auth/login";

function Login({ onLoginExitoso }) {
    const [usuario, setUsuario] = useState("");
    const [contrasena, setContrasena] = useState("");
    const [error, setError] = useState("");
    const [bloqueado, setBloqueado] = useState(false);
    const [cargando, setCargando] = useState(false);

    async function handleSubmit(e) {
        e.preventDefault();
        setError("");

        // Criterio: campos vacíos
        if (!usuario.trim() || !contrasena.trim()) {
            setError("Usuario y contraseña son obligatorios.");
            return;
        }

        setCargando(true);

        try {
            const respuesta = await fetch(API_URL, {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ Username: usuario, Password: contrasena }),
            });

            const datos = await respuesta.json();

            if (respuesta.ok) {
                // Login exitoso: la sesión se guarda en App.jsx (localStorage)
                const usuarioSesion = {
                    usuarioId: datos.usuarioId,
                    nombre: datos.nombre,
                    rol: datos.rol,
                };
                onLoginExitoso?.(usuarioSesion);
            } else if (respuesta.status === 423) {
                // Bloqueado por intentos fallidos
                setBloqueado(true);
                setError(datos.mensaje || "Cuenta bloqueada temporalmente.");
            } else {
                // 401 u otro error de credenciales
                setError(datos.mensaje || "Usuario o contraseña incorrectos.");
            }
        } catch (err) {
            setError("No se pudo conectar con el servidor. Verifica que el backend esté corriendo.");
        } finally {
            setCargando(false);
        }
    }

    return (
        <form onSubmit={handleSubmit} style={{ maxWidth: 320, margin: "40px auto" }}>
            <h2>Iniciar sesión</h2>

            <div style={{ marginBottom: 12 }}>
                <label htmlFor="usuario">Usuario</label>
                <input
                    id="usuario"
                    type="text"
                    value={usuario}
                    onChange={(e) => setUsuario(e.target.value)}
                    disabled={bloqueado || cargando}
                    style={{ width: "100%", padding: 8 }}
                />
            </div>

            <div style={{ marginBottom: 12 }}>
                <label htmlFor="contrasena">Contraseña</label>
                <input
                    id="contrasena"
                    type="password"
                    value={contrasena}
                    onChange={(e) => setContrasena(e.target.value)}
                    disabled={bloqueado || cargando}
                    style={{ width: "100%", padding: 8 }}
                />
            </div>

            {error && (
                <p role="alert" style={{ color: "red", marginBottom: 12 }}>
                    {error}
                </p>
            )}

            <button type="submit" disabled={bloqueado || cargando} style={{ width: "100%", padding: 10 }}>
                {cargando ? "Ingresando..." : "Iniciar sesión"}
            </button>
        </form>
    );
}

export default Login;
