import "./Dashboard.css";
import { useState } from "react";
import Laboratorios from "./Laboratorios";

// Cada módulo indica qué roles lo pueden ver. El backend sigue
// siendo quien autoriza; aquí solo se evita mostrar opciones que el
// usuario no puede usar.
const MODULOS = [
    {
        id: "laboratorios",
        icono: "🖥️",
        titulo: "Laboratorios OBLD",
        descripcion: "Ver estado actual, equipos disponibles y horarios de los laboratorios.",
        roles: ["Administrador", "Usuario"],
    },
    {
        id: "mis-reservas",
        icono: "🗓️",
        titulo: "Mis Reservas",
        descripcion: "Registrar, consultar o cancelar mis reservas de laboratorio.",
        roles: ["Usuario"],
    },
    {
        id: "gestion-reservas",
        icono: "📅",
        titulo: "Gestión de Reservas",
        descripcion: "Crear, aprobar o cancelar solicitudes de espacio para lecciones y prácticas.",
        roles: ["Administrador"],
    },
    {
        id: "reportes",
        icono: "📊",
        titulo: "Reportes y Estadísticas",
        descripcion: "Consultar el historial de uso de los laboratorios por grupos y carreras.",
        roles: ["Administrador"],
    },
];

function modulosPorRol(rol) {
    return MODULOS.filter((modulo) => modulo.roles.includes(rol));
}

function Dashboard({ usuario, onCerrarSesion }) {
    const [vista, setVista] = useState(null);
    const modulos = modulosPorRol(usuario?.rol);
    const esAdministrador = usuario?.rol === "Administrador";

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
                    <div className="user-badge">
                        <span className="user-name">{usuario?.nombre}</span>
                        <span className="user-role">{usuario?.rol}</span>
                    </div>
                    <button onClick={onCerrarSesion} className="btn-logout">
                        Cerrar sesión
                    </button>
                </div>
            </header>

            {/* Contenido principal */}
            <main className="dashboard-main">
                {vista === "laboratorios" ? (
                    <Laboratorios onVolver={() => setVista(null)} />
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
                                    <div
                                        key={modulo.id}
                                        className="dashboard-card"
                                        onClick={() =>
                                            modulo.id === "laboratorios"
                                                ? setVista("laboratorios")
                                                : alert(`Módulo: ${modulo.titulo}`)
                                        }
                                    >
                                        <div className="card-icon">{modulo.icono}</div>
                                        <h3>{modulo.titulo}</h3>
                                        <p>{modulo.descripcion}</p>
                                    </div>
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
