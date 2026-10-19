import "./Dashboard.css";
import { useEffect, useState } from "react";
import Laboratorios from "./Laboratorios";
import Disponibilidad from "./Disponibilidad";
import Reserva from "./Reserva";
import Icono from "./Iconos";

// Cada módulo indica qué roles lo pueden ver. El backend sigue
// siendo quien autoriza; aquí solo se evita mostrar opciones que el
// usuario no puede usar. 
const MODULOS = [
    {
        id: "laboratorios",
        titulo: "Laboratorios",
        descripcion: "Ver estado actual, equipos disponibles y horarios de los laboratorios.",
        roles: ["Administrador", "Usuario"],
    },
    {
        id: "mis-reservas",
        titulo: "Mis Reservas",
        descripcion: "Registrar, consultar o cancelar mis reservas de laboratorio.",
        roles: ["Usuario"],
    },
    {
        id: "gestion-reservas",
        titulo: "Gestión de Reservas",
        descripcion: "Crear, aprobar o cancelar solicitudes de espacio para lecciones y prácticas.",
        roles: ["Administrador"],
    },
    {
        id: "reportes",
        titulo: "Reportes y Estadísticas",
        descripcion: "Consultar el historial de uso de los laboratorios por grupos y carreras.",
        roles: ["Administrador"],
    },
];

function modulosPorRol(rol) {
    return MODULOS.filter((modulo) => modulo.roles.includes(rol));
}

// Iniciales para el avatar: primera y última palabra del nombre.
function iniciales(nombre) {
    const partes = (nombre ?? "").trim().split(/\s+/).filter(Boolean);
    if (partes.length === 0) return "?";
    if (partes.length === 1) return partes[0].slice(0, 2).toUpperCase();
    return (partes[0][0] + partes[partes.length - 1][0]).toUpperCase();
}

function Dashboard({ usuario, onCerrarSesion }) {
    const [vista, setVista] = useState(null);
    // HU3 - #38 Datos para abrir la consulta de disponibilidad desde Laboratorios.
    const [disponibilidad, setDisponibilidad] = useState(null);
    const [reserva, setReserva] = useState(null);
    const modulos = modulosPorRol(usuario?.rol);
    const esAdministrador = usuario?.rol === "Administrador";

    useEffect(() => {
        window.history.replaceState({ vista: null, disponibilidad: null }, "");

        const alCambiarHistorial = (evento) => {
            setVista(evento.state?.vista ?? null);
            setDisponibilidad(evento.state?.disponibilidad ?? null);
            setReserva(evento.state?.reserva ?? null);
        };

        window.addEventListener("popstate", alCambiarHistorial);
        return () => window.removeEventListener("popstate", alCambiarHistorial);
    }, []);

    function irA(nuevaVista, datosDisponibilidad = null, datosReserva = null) {
        window.history.pushState(
            { vista: nuevaVista, disponibilidad: datosDisponibilidad, reserva: datosReserva },
            ""
        );
        setVista(nuevaVista);
        setDisponibilidad(datosDisponibilidad);
        setReserva(datosReserva);
    }

    return (
        <div className="dashboard-container">
            {/* Barra de navegación superior institucional */}
            <header className="dashboard-header">
                <div className="dashboard-brand">
                    <img src="/utn-logo.png" alt="UTN" className="dashboard-logo" />
                    <div className="dashboard-titles">
                        <h2>Laboratorio OBLD</h2>
                        <span>Universidad Técnica Nacional</span>
                    </div>
                </div>

                <div className="dashboard-user-info">
                    <div className="user-profile">
                        <div className="user-avatar" aria-hidden="true">
                            {iniciales(usuario?.nombre)}
                        </div>
                        <div className="user-badge">
                            <span className="user-name">{usuario?.nombre}</span>
                            <span className="user-role">{usuario?.rol}</span>
                        </div>
                    </div>
                    <button onClick={onCerrarSesion} className="btn-logout">
                        Cerrar sesión
                    </button>
                </div>
            </header>

            {/* Contenido principal */}
            <main className="dashboard-main">
                {vista === "reserva" ? (
                    <Reserva usuario={usuario} datosIniciales={reserva ?? undefined}
                        laboratoriosIniciales={disponibilidad?.laboratorios}
                        onVolver={() => window.history.back()} />
                ) : vista === "disponibilidad" && disponibilidad ? (
                    <Disponibilidad
                        laboratorios={disponibilidad.laboratorios}
                        laboratorioInicialId={disponibilidad.laboratorioId}
                        onContinuarReserva={(datos) => irA("reserva", disponibilidad, datos)}
                        onVolver={() => window.history.back()}
                    />
                ) : vista === "laboratorios" ? (
                    <Laboratorios
                        onVolver={() => window.history.back()}
                        onConsultarDisponibilidad={(laboratorioId, laboratorios) =>
                            irA("disponibilidad", { laboratorioId, laboratorios })
                        }
                    />
                ) : (
                    <>
                        <div className="welcome-banner">
                            <h1>Bienvenido, {usuario?.nombre}</h1>
                            <p>
                                {esAdministrador
                                    ? "Panel de Control para la Gestión y Reserva de Laboratorios de la UTN."
                                    : "Consulta los laboratorios y administra tus reservas."}
                            </p>
                        </div>

                        {/* Tarjetas de acceso rápido según el rol */}
                        {modulos.length > 0 ? (
                            <div className="dashboard-grid">
                                {modulos.map((modulo) => (
                                    <button
                                        key={modulo.id}
                                        type="button"
                                        className="dashboard-card"
                                        onClick={() =>
                                            ["laboratorios", "mis-reservas"].includes(modulo.id)
                                                ? irA(modulo.id === "mis-reservas" ? "reserva" : "laboratorios")
                                                : alert(`Módulo: ${modulo.titulo}`)
                                        }
                                    >
                                        <div className="card-icon">
                                            <Icono nombre={modulo.id} />
                                        </div>
                                        <h3>{modulo.titulo}</h3>
                                        <p>{modulo.descripcion}</p>
                                    </button>
                                ))}
                            </div>
                        ) : (
                            <p>Tu usuario no tiene módulos asignados. Contacta al administrador.</p>
                        )}
                    </>
                )}
            </main>
        </div>
    );
}

export default Dashboard;
