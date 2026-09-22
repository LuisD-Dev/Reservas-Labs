import { useState } from "react";
import "./Login.css";


const API_URL = "http://localhost:5282/api/auth/login";
const USUARIO_MAX_LENGTH = 50;
const CONTRASENA_MAX_LENGTH = 100;
const USUARIO_REGEX = /^[a-zA-Z0-9._]*$/;

function Login({ onLoginExitoso }) {
    const [usuario, setUsuario] = useState("");
    const [contrasena, setContrasena] = useState("");
    const [mostrarContrasena, setMostrarContrasena] = useState(false);
    const [error, setError] = useState("");
    const [bloqueado, setBloqueado] = useState(false);
    const [cargando, setCargando] = useState(false);

    function handleUsuarioChange(e) {
        const valor = e.target.value;
        if (USUARIO_REGEX.test(valor)) {
            setUsuario(valor);
        }
    }

    async function handleSubmit(e) {
        e.preventDefault();
        setError("");

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
                const usuarioSesion = {
                    usuarioId: datos.usuarioId,
                    nombre: datos.nombre,
                    rol: datos.rol,
                };
                onLoginExitoso?.(usuarioSesion);
            } else if (respuesta.status === 423) {
                setBloqueado(true);
                setError(datos.mensaje || "Cuenta bloqueada temporalmente.");
            } else {
                setError(datos.mensaje || "Usuario o contraseña incorrectos.");
            }
        } catch (err) {
            setError("No se pudo conectar con el servidor. Verifica que el backend esté corriendo.");
        } finally {
            setCargando(false);
        }
    }

    return (
        <div className="login-page">
            <div className="login-card">
                <div className="login-header">
                    <div className="login-logo-container">
                        <img
                            src="/utn-logo.png"
                            alt="Universidad Técnica Nacional"
                            className="login-utn-logo"
                        />
                    </div>
                    <h1>Laboratorio OBLD</h1>
                    <p className="login-subtitle">Sistema de Reserva de Laboratorios</p>
                </div>

                <form onSubmit={handleSubmit} className="login-form">
                    <div className="login-field">
                        <label htmlFor="usuario">Usuario</label>
                        <input
                            id="usuario"
                            type="text"
                            value={usuario}
                            onChange={handleUsuarioChange}
                            disabled={bloqueado || cargando}
                            autoComplete="username"
                            placeholder="Ingrese su usuario"
                            maxLength={USUARIO_MAX_LENGTH}
                        />
                    </div>

                    <div className="login-field">
                        <label htmlFor="contrasena">Contraseña</label>
                        <div className="login-password-wrapper">
                            <input
                                id="contrasena"
                                type={mostrarContrasena ? "text" : "password"}
                                value={contrasena}
                                onChange={(e) => setContrasena(e.target.value)}
                                disabled={bloqueado || cargando}
                                autoComplete="current-password"
                                placeholder="Ingrese su contraseña"
                                maxLength={CONTRASENA_MAX_LENGTH}
                            />
                            <button
                                type="button"
                                className="login-toggle-password"
                                onClick={() => setMostrarContrasena((v) => !v)}
                                disabled={bloqueado || cargando}
                                aria-label={mostrarContrasena ? "Ocultar contraseña" : "Mostrar contraseña"}
                                tabIndex={-1}
                            >
                                {mostrarContrasena ? (
                                    <svg viewBox="0 0 24 24" width="20" height="20" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                                        <path d="M17.94 17.94A10.94 10.94 0 0 1 12 20c-7 0-11-8-11-8a20.3 20.3 0 0 1 5.06-6.06M9.9 4.24A10.94 10.94 0 0 1 12 4c7 0 11 8 11 8a20.29 20.29 0 0 1-3.22 4.44M14.12 14.12a3 3 0 1 1-4.24-4.24" />
                                        <line x1="1" y1="1" x2="23" y2="23" />
                                    </svg>
                                ) : (
                                    <svg viewBox="0 0 24 24" width="20" height="20" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                                        <path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8Z" />
                                        <circle cx="12" cy="12" r="3" />
                                    </svg>
                                )}
                            </button>
                        </div>
                    </div>

                    {error && (
                        <p role="alert" className="login-error">
                            {error}
                        </p>
                    )}

                    <button type="submit" disabled={bloqueado || cargando} className="login-button">
                        {cargando ? "Ingresando..." : "Iniciar sesión"}
                    </button>
                </form>

                <p className="login-footer">Universidad Técnica Nacional · LABORATORIO OBLD</p>
            </div>
        </div>
    );
}

export default Login;