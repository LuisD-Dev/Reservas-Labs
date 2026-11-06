import { useEffect, useState } from "react";
import { apiFetch } from "../services/api";
import {
    formatearFechaReserva,
    formatearHoraReserva,
    mensajeErrorMisReservas,
    ordenarReservas,
} from "../utils/misReservas";
import "./MisReservas.css";

// HU7 - AB#94: lista de las reservas del usuario autenticado.
function MisReservas({ onNuevaReserva, onVolver }) {
    const [reservas, setReservas] = useState([]);
    const [cargando, setCargando] = useState(true);
    const [error, setError] = useState("");

    async function cargar(signal) {
        setCargando(true);
        setError("");
        try {
            const respuesta = await apiFetch("/api/reservas/mias", { signal });
            if (signal?.aborted) return;
            if (respuesta.ok) {
                const lista = await respuesta.json();
                if (!signal?.aborted) setReservas(ordenarReservas(lista));
            } else {
                setError(mensajeErrorMisReservas(respuesta.status));
            }
        } catch (falla) {
            if (!signal?.aborted && falla.name !== "AbortError") setError(mensajeErrorMisReservas());
        } finally {
            if (!signal?.aborted) setCargando(false);
        }
    }

    useEffect(() => {
        const controller = new AbortController();
        const inicio = window.setTimeout(() => cargar(controller.signal), 0);
        return () => {
            window.clearTimeout(inicio);
            controller.abort();
        };
    }, []);

    return (
        <section className="mis-reservas-section">
            <button className="labs-volver" onClick={onVolver}>Volver</button>
            <div className="labs-encabezado">
                <h1>Mis reservas</h1>
                <p>Consulta las reservas de laboratorio que has registrado.</p>
            </div>
            <div className="mis-reservas-acciones">
                <button type="button" className="mis-reservas-nueva" onClick={onNuevaReserva}>
                    Nueva reserva
                </button>
            </div>
            {cargando && <p role="status">Cargando tus reservas...</p>}
            {error && (
                <div className="labs-error" role="alert">
                    <p>{error}</p>
                    <button onClick={() => cargar()} disabled={cargando}>Reintentar</button>
                </div>
            )}
            {!cargando && !error && reservas.length === 0 && (
                <p role="status">Todavía no tienes reservas registradas.</p>
            )}
            {!cargando && !error && reservas.length > 0 && (
                <div className="mis-reservas-tabla-contenedor">
                    <table className="mis-reservas-tabla">
                        <thead>
                            <tr>
                                <th scope="col">Laboratorio</th>
                                <th scope="col">Fecha</th>
                                <th scope="col">Horario</th>
                                <th scope="col">Estado</th>
                            </tr>
                        </thead>
                        <tbody>
                            {reservas.map((reserva) => (
                                <tr key={reserva.id}>
                                    <td>{reserva.nombreLaboratorio}</td>
                                    <td>{formatearFechaReserva(reserva.fecha)}</td>
                                    <td>
                                        {formatearHoraReserva(reserva.horaInicio)} a {formatearHoraReserva(reserva.horaFin)}
                                    </td>
                                    <td>{reserva.estado}</td>
                                </tr>
                            ))}
                        </tbody>
                    </table>
                </div>
            )}
        </section>
    );
}

export default MisReservas;