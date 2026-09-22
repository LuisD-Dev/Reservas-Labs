import { useState } from "react";
import "./Dashboard.css";

function Dashboard({ usuario, onCerrarSesion }) {
    const [seccionActiva, setSeccionActiva] = useState("inicio");

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
                        <span className="user-name">{usuario?.nombre || "Administrador"}</span>
                        <span className="user-role">{usuario?.rol || "Administrador"}</span>
                    </div>
                    <button onClick={onCerrarSesion} className="btn-logout">
                        Cerrar sesión
                    </button>
                </div>
            </header>

            {/* Contenido principal */}
            <main className="dashboard-main">
                <div className="welcome-banner">
                    <h1>Bienvenido, {usuario?.nombre || "Administrador"}</h1>
                    <p>Panel de Control para la Gestión y Reserva de Laboratorios de la UTN.</p>
                </div>

                {/* Tarjetas de acceso rápido / Módulos */}
                <div className="dashboard-grid">
                    <div className="dashboard-card" onClick={() => alert("Módulo de Laboratorios")}>
                        <div className="card-icon">🖥️</div>
                        <h3>Laboratorios OBLD</h3>
                        <p>Ver estado actual, equipos disponibles y horarios de los laboratorios.</p>
                    </div>

                    <div className="dashboard-card" onClick={() => alert("Módulo de Reservas")}>
                        <div className="card-icon">📅</div>
                        <h3>Gestión de Reservas</h3>
                        <p>Crear, aprobar o cancelar solicitudes de espacio para lecciones y prácticas.</p>
                    </div>

                    <div className="dashboard-card" onClick={() => alert("Módulo de Reportes")}>
                        <div className="card-icon">📊</div>
                        <h3>Reportes y Estadísticas</h3>
                        <p>Consultar el historial de uso de los laboratorios por grupos y carreras.</p>
                    </div>
                </div>
            </main>
        </div>
    );
}

export default Dashboard;