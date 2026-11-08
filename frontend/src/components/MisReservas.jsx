import { useEffect, useId, useRef, useState } from "react";
import { apiFetch } from "../services/api";
import {
    formatearFechaReserva,
    formatearHoraReserva,
    mensajeErrorMisReservas,
    ordenarReservas,
    separarReservas,
    tipoEstado,
} from "../utils/misReservas";
import { puedeCancelarReserva, resultadoCancelacion, cancelarConConfirmacion } from "../utils/cancelarReserva";
import "./MisReservas.css";

// HU7 - AB#95: tabla de un grupo de reservas (activas o canceladas).
// HU8 - AB#103: si recibe `onCancelar`, agrega la columna con el botón para cancelar.
function TablaReservas({ titulo, reservas, vacio, onCancelar, cancelandoId }) {
    const tituloId = useId();
    return (
        <div className="mis-reservas-grupo">
            <h2 id={tituloId}>{titulo} ({reservas.length})</h2>
            {reservas.length === 0 ? (
                <p role="status">{vacio}</p>
            ) : (
                <div className="mis-reservas-tabla-contenedor" tabIndex={0} role="region" aria-labelledby={tituloId}>
                    <table className="mis-reservas-tabla" aria-labelledby={tituloId}>
                        <thead>
                            <tr>
                                <th scope="col">Laboratorio</th>
                                <th scope="col">Fecha</th>
                                <th scope="col">Horario</th>
                                <th scope="col">Estado</th>
                                {onCancelar && <th scope="col">Acciones</th>}
                            </tr>
                        </thead>
                        <tbody>
                            {reservas.map((reserva) => {
                                const tipo = tipoEstado(reserva.estado);
                                const fecha = formatearFechaReserva(reserva.fecha);
                                return (
                                    <tr key={reserva.id} className={`mis-reservas-fila--${tipo}`}>
                                        <td>{reserva.nombreLaboratorio}</td>
                                        <td>{fecha}</td>
                                        <td>
                                            {formatearHoraReserva(reserva.horaInicio)} a {formatearHoraReserva(reserva.horaFin)}
                                        </td>
                                        <td>
                                            <span className={`mis-reservas-estado mis-reservas-estado--${tipo}`}>
                                                {reserva.estado}
                                            </span>
                                        </td>
                                        {onCancelar && (
                                            <td>
                                                {puedeCancelarReserva(reserva) && (
                                                    <button
                                                        type="button"
                                                        className="mis-reservas-cancelar"
                                                        onClick={() => onCancelar(reserva.id)}
                                                        disabled={cancelandoId !== null}
                                                        aria-label={`Cancelar la reserva de ${reserva.nombreLaboratorio} del ${fecha}`}>
                                                        {cancelandoId === reserva.id ? "Cancelando..." : "Cancelar"}
                                                    </button>
                                                )}
                                            </td>
                                        )}
                                    </tr>
                                );
                            })}
                        </tbody>
                    </table>
                </div>
            )}
        </div>
    );
}

// HU7 - AB#94 y AB#95: reservas del usuario autenticado, separadas en activas y canceladas.
// HU8 - AB#103: cancelar una reserva y mostrar un aviso accesible con el resultado.
function MisReservas({ onNuevaReserva, onVolver }) {
    const [reservas, setReservas] = useState([]);
    const [cargando, setCargando] = useState(true);
    const [error, setError] = useState("");
    const [cancelandoId, setCancelandoId] = useState(null);
    const [aviso, setAviso] = useState(null); // { exito: boolean, mensaje: string }
    const avisoRef = useRef(null);

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

    async function cancelar(id) {
        if (cancelandoId !== null) return;
        setCancelandoId(id);
        setAviso(null);

        let resultado;
        try {
            const respuesta = await cancelarConConfirmacion(id, apiFetch, (mensaje) => window.confirm(mensaje));
            if (!respuesta) {
                setCancelandoId(null);
                return;
            }
            const cuerpo = await respuesta.json().catch(() => null);
            resultado = resultadoCancelacion(respuesta.status, cuerpo);
        } catch {
            resultado = resultadoCancelacion();
        }

        if (resultado.exito) {
            setReservas((actuales) =>
                actuales.map((reserva) => (reserva.id === id ? { ...reserva, estado: "Cancelada" } : reserva)));
        }
        setAviso({ exito: resultado.exito, mensaje: resultado.mensaje });
        setCancelandoId(null);

        // 404 o 409: la pantalla estaba desactualizada, se vuelve a pedir la lista al servidor.
        if (resultado.recargar) cargar();
    }

    useEffect(() => {
        const controller = new AbortController();
        const inicio = window.setTimeout(() => cargar(controller.signal), 0);
        return () => {
            window.clearTimeout(inicio);
            controller.abort();
        };
    }, []);

    // El botón desaparece de la lista al cancelar, así que el foco se mueve al aviso
    // para que quien usa teclado o lector de pantalla no lo pierda.
    useEffect(() => {
        if (aviso) avisoRef.current?.focus();
    }, [aviso]);

    const { activas, canceladas } = separarReservas(reservas);

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
            {/* El contenedor de éxito siempre está en pantalla para que los lectores lo anuncien. */}
            <div role="status" aria-live="polite" aria-atomic="true">
                {aviso?.exito && (
                    <p
                        ref={avisoRef}
                        tabIndex={-1}
                        className="mis-reservas-aviso mis-reservas-aviso--exito">
                        {aviso.mensaje}
                    </p>
                )}
            </div>
            {aviso && !aviso.exito && (
                <p
                    ref={avisoRef}
                    tabIndex={-1}
                    role="alert"
                    className="mis-reservas-aviso mis-reservas-aviso--error">
                    {aviso.mensaje}
                </p>
            )}
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
                <>
                    <TablaReservas
                        titulo="Reservas activas"
                        reservas={activas}
                        vacio="No tienes reservas activas."
                        onCancelar={cancelar}
                        cancelandoId={cancelandoId} />
                    <TablaReservas
                        titulo="Reservas canceladas"
                        reservas={canceladas}
                        vacio="No tienes reservas canceladas." />
                </>
            )}
        </section>
    );
}

export default MisReservas;
