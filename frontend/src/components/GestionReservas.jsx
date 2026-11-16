import { useEffect, useId, useMemo, useRef, useState } from "react";
import { apiFetch } from "../services/api";
import { hoyComoTexto } from "../utils/disponibilidad";
import { filtrarLaboratoriosHabilitados } from "../utils/laboratorios";
import {
    formatearFechaReserva,
    formatearHoraReserva,
    ordenarReservas,
    tipoEstado,
} from "../utils/misReservas";
import { cancelarConConfirmacion, puedeCancelarReserva, resultadoCancelacion } from "../utils/cancelarReserva";
import {
    FILTROS_INICIALES,
    RUTA_CREAR_RESERVA_GESTION,
    RUTA_RESERVAS_GESTION,
    contarReservas,
    crearCuerpoReservaGestion,
    filtrarReservasGestion,
    hayFiltrosActivos,
    laboratoriosDeReservas,
    mensajeConfirmacionGestion,
    mensajeErrorCrearGestion,
    mensajeErrorGestion,
    validarReservaGestion,
} from "../utils/gestionReservas";
import "./Formularios.css";
import "./Reserva.css";
import "./MisReservas.css";
import "./GestionReservas.css";

const DATOS_INICIALES = { username: "", laboratorioId: "", fecha: "", horaInicio: "", horaFin: "" };

// Formulario para registrar una reserva a nombre de otro usuario.
// Las reglas de horario son las mismas de "Registrar reserva"; el backend vuelve a validarlas.
function FormularioNuevaReserva({ onRegistrada, onCerrar }) {
    const [laboratorios, setLaboratorios] = useState([]);
    const [cargando, setCargando] = useState(true);
    const [errorCarga, setErrorCarga] = useState("");
    const [datos, setDatos] = useState(DATOS_INICIALES);
    const [errores, setErrores] = useState({});
    const [error, setError] = useState("");
    const [enviando, setEnviando] = useState(false);
    const solicitudEnCurso = useRef(false);
    const campoUsuario = useRef(null);

    async function cargarLaboratorios(signal) {
        setCargando(true);
        setErrorCarga("");
        try {
            const respuesta = await apiFetch("/api/laboratorios?soloHabilitados=true", { signal });
            if (signal?.aborted) return;
            if (respuesta.ok) {
                const lista = await respuesta.json();
                if (!signal?.aborted) setLaboratorios(filtrarLaboratoriosHabilitados(lista));
            } else {
                setErrorCarga(respuesta.status === 401 ? mensajeErrorGestion(401)
                    : "No se pudo cargar la lista de laboratorios.");
            }
        } catch (falla) {
            if (!signal?.aborted && falla.name !== "AbortError") setErrorCarga(mensajeErrorGestion());
        } finally {
            if (!signal?.aborted) setCargando(false);
        }
    }

    useEffect(() => {
        const controller = new AbortController();
        const inicio = window.setTimeout(() => cargarLaboratorios(controller.signal), 0);
        return () => {
            window.clearTimeout(inicio);
            controller.abort();
        };
    }, []);

    // Quien abre el formulario con teclado o lector de pantalla empieza en el primer campo.
    // Los campos están deshabilitados mientras cargan los laboratorios y un campo
    // deshabilitado no recibe el foco, por eso se espera a que termine la carga.
    useEffect(() => {
        if (!cargando) campoUsuario.current?.focus();
    }, [cargando]);

    function cambiar(campo, valor) {
        setDatos((anteriores) => ({ ...anteriores, [campo]: valor }));
        setErrores((anteriores) => ({ ...anteriores, [campo]: undefined }));
        setError("");
    }

    async function registrar(evento) {
        evento.preventDefault();
        if (solicitudEnCurso.current || cargando || errorCarga) return;

        const nuevosErrores = validarReservaGestion(datos);
        setErrores(nuevosErrores);
        setError("");
        if (Object.keys(nuevosErrores).length > 0) return;

        solicitudEnCurso.current = true;
        setEnviando(true);
        try {
            const respuesta = await apiFetch(RUTA_CREAR_RESERVA_GESTION, {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify(crearCuerpoReservaGestion(datos)),
            });
            if (respuesta.status === 201) {
                const cuerpo = await respuesta.json().catch(() => null);
                const laboratorio = laboratorios.find((lab) => String(lab.id) === String(datos.laboratorioId));
                onRegistrada(mensajeConfirmacionGestion(cuerpo?.reserva, laboratorio?.nombre));
                return;
            }
            const detalle = respuesta.status === 400 ? await respuesta.json().catch(() => null) : null;
            setError(mensajeErrorCrearGestion(respuesta.status, detalle));
        } catch {
            setError(mensajeErrorCrearGestion());
        } finally {
            solicitudEnCurso.current = false;
            setEnviando(false);
        }
    }

    const bloqueado = cargando || enviando || Boolean(errorCarga);

    return (
        <section className="gestion-nueva" aria-labelledby="gestion-nueva-titulo">
            <h2 id="gestion-nueva-titulo">Nueva reserva</h2>

            {cargando && <p role="status">Cargando laboratorios...</p>}
            {errorCarga && (
                <div className="labs-error" role="alert">
                    <p>{errorCarga}</p>
                    <button type="button" onClick={() => cargarLaboratorios()} disabled={cargando}>Reintentar</button>
                </div>
            )}

            <form className="reserva-form" onSubmit={registrar} noValidate aria-busy={enviando}>
                <div className="reserva-campo reserva-ancho">
                    <label htmlFor="gestion-username">Usuario responsable</label>
                    <input id="gestion-username" ref={campoUsuario} value={datos.username}
                        autoComplete="off" disabled={bloqueado} required
                        aria-invalid={Boolean(errores.username)}
                        aria-describedby={errores.username ? "error-gestion-username" : undefined}
                        onChange={(e) => cambiar("username", e.target.value)} />
                    {errores.username && <span className="reserva-error" id="error-gestion-username" role="alert">{errores.username}</span>}
                </div>

                <div className="reserva-campo reserva-ancho">
                    <label htmlFor="gestion-laboratorioId">Laboratorio</label>
                    <select id="gestion-laboratorioId" value={datos.laboratorioId}
                        disabled={bloqueado} required
                        aria-invalid={Boolean(errores.laboratorioId)}
                        aria-describedby={errores.laboratorioId ? "error-gestion-laboratorioId" : undefined}
                        onChange={(e) => cambiar("laboratorioId", e.target.value)}>
                        <option value="">Selecciona un laboratorio</option>
                        {laboratorios.map((lab) => (
                            <option key={lab.id} value={lab.id}>{lab.nombre} — {lab.ubicacion}</option>
                        ))}
                    </select>
                    {errores.laboratorioId && <span className="reserva-error" id="error-gestion-laboratorioId" role="alert">{errores.laboratorioId}</span>}
                </div>

                <div className="reserva-campo">
                    <label htmlFor="gestion-fecha">Fecha</label>
                    <input id="gestion-fecha" type="date" value={datos.fecha}
                        min={hoyComoTexto()} disabled={bloqueado} required
                        aria-invalid={Boolean(errores.fecha)}
                        aria-describedby={errores.fecha ? "error-gestion-fecha" : undefined}
                        onChange={(e) => cambiar("fecha", e.target.value)} />
                    {errores.fecha && <span className="reserva-error" id="error-gestion-fecha" role="alert">{errores.fecha}</span>}
                </div>

                <div className="reserva-campo">
                    <label htmlFor="gestion-horaInicio">Hora inicial</label>
                    <input id="gestion-horaInicio" type="time" value={datos.horaInicio}
                        disabled={bloqueado} required
                        aria-invalid={Boolean(errores.horaInicio)}
                        aria-describedby={errores.horaInicio ? "error-gestion-horaInicio" : undefined}
                        onChange={(e) => cambiar("horaInicio", e.target.value)} />
                    {errores.horaInicio && <span className="reserva-error" id="error-gestion-horaInicio" role="alert">{errores.horaInicio}</span>}
                </div>

                <div className="reserva-campo">
                    <label htmlFor="gestion-horaFin">Hora final</label>
                    <input id="gestion-horaFin" type="time" value={datos.horaFin}
                        disabled={bloqueado} required
                        aria-invalid={Boolean(errores.horaFin)}
                        aria-describedby={errores.horaFin ? "error-gestion-horaFin" : undefined}
                        onChange={(e) => cambiar("horaFin", e.target.value)} />
                    {errores.horaFin && <span className="reserva-error" id="error-gestion-horaFin" role="alert">{errores.horaFin}</span>}
                </div>

                <div className="gestion-nueva-botones reserva-ancho">
                    <button type="submit" className="reserva-registrar" disabled={bloqueado}>
                        {enviando ? "Registrando..." : "Registrar reserva"}
                    </button>
                    <button type="button" className="gestion-secundario" onClick={onCerrar} disabled={enviando}>
                        Cerrar formulario
                    </button>
                </div>
            </form>

            {error && <p className="labs-error" role="alert">{error}</p>}
        </section>
    );
}

// Administrador: consulta las reservas de todos los usuarios, las filtra,
// registra una a nombre de otro usuario y cancela cualquiera.
function GestionReservas({ onVolver }) {
    const [reservas, setReservas] = useState([]);
    const [cargando, setCargando] = useState(true);
    const [error, setError] = useState("");
    const [filtros, setFiltros] = useState(FILTROS_INICIALES);
    const [cancelandoId, setCancelandoId] = useState(null);
    const [aviso, setAviso] = useState(null); // { exito: boolean, mensaje: string }
    const [mostrarFormulario, setMostrarFormulario] = useState(false);
    const avisoRef = useRef(null);
    const tituloTablaId = useId();

    async function cargar(signal) {
        setCargando(true);
        setError("");
        try {
            const respuesta = await apiFetch(RUTA_RESERVAS_GESTION, { signal });
            if (signal?.aborted) return;
            if (respuesta.ok) {
                const lista = await respuesta.json();
                if (!signal?.aborted) setReservas(ordenarReservas(lista));
            } else {
                setError(mensajeErrorGestion(respuesta.status));
            }
        } catch (falla) {
            if (!signal?.aborted && falla.name !== "AbortError") setError(mensajeErrorGestion());
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

    function alRegistrar(mensaje) {
        setMostrarFormulario(false);
        setAviso({ exito: true, mensaje });
        cargar();
    }

    useEffect(() => {
        const controller = new AbortController();
        const inicio = window.setTimeout(() => cargar(controller.signal), 0);
        return () => {
            window.clearTimeout(inicio);
            controller.abort();
        };
    }, []);

    // El resultado se anuncia y recibe el foco: el botón de cancelar cambia o desaparece
    // y quien usa teclado o lector de pantalla no debe perder su lugar.
    useEffect(() => {
        if (aviso) avisoRef.current?.focus();
    }, [aviso]);

    function cambiarFiltro(campo, valor) {
        setFiltros((anteriores) => ({ ...anteriores, [campo]: valor }));
    }

    const laboratorios = useMemo(() => laboratoriosDeReservas(reservas), [reservas]);
    const visibles = useMemo(() => filtrarReservasGestion(reservas, filtros), [reservas, filtros]);
    const conteo = contarReservas(reservas);
    const filtrando = hayFiltrosActivos(filtros);

    return (
        <section className="mis-reservas-section gestion-section">
            <button className="labs-volver" onClick={onVolver}>Volver</button>
            <div className="labs-encabezado">
                <h1>Gestión de reservas</h1>
                <p>Consulta las reservas de todos los usuarios, registra una a nombre de otro usuario o cancela una existente.</p>
            </div>

            <div className="mis-reservas-acciones">
                <button type="button" className="mis-reservas-nueva"
                    aria-expanded={mostrarFormulario}
                    onClick={() => setMostrarFormulario((abierto) => !abierto)}>
                    {mostrarFormulario ? "Ocultar formulario" : "Nueva reserva"}
                </button>
            </div>

            {mostrarFormulario && (
                <FormularioNuevaReserva onRegistrada={alRegistrar} onCerrar={() => setMostrarFormulario(false)} />
            )}

            {/* El contenedor de éxito siempre está en pantalla para que los lectores lo anuncien. */}
            <div role="status" aria-live="polite" aria-atomic="true">
                {aviso?.exito && (
                    <p ref={avisoRef} tabIndex={-1} className="mis-reservas-aviso mis-reservas-aviso--exito">
                        {aviso.mensaje}
                    </p>
                )}
            </div>
            {aviso && !aviso.exito && (
                <p ref={avisoRef} tabIndex={-1} role="alert" className="mis-reservas-aviso mis-reservas-aviso--error">
                    {aviso.mensaje}
                </p>
            )}

            {cargando && <p role="status">Cargando reservas...</p>}
            {error && (
                <div className="labs-error" role="alert">
                    <p>{error}</p>
                    <button onClick={() => cargar()} disabled={cargando}>Reintentar</button>
                </div>
            )}
            {!cargando && !error && reservas.length === 0 && (
                <p role="status">Todavía no hay reservas registradas.</p>
            )}

            {!cargando && !error && reservas.length > 0 && (
                <>
                    <div className="gestion-filtros" role="group" aria-label="Filtros de reservas">
                        <div className="gestion-filtro gestion-filtro--ancho">
                            <label htmlFor="gestion-buscar">Buscar</label>
                            <input id="gestion-buscar" type="search" value={filtros.busqueda}
                                placeholder="Responsable, usuario o laboratorio"
                                onChange={(e) => cambiarFiltro("busqueda", e.target.value)} />
                        </div>
                        <div className="gestion-filtro">
                            <label htmlFor="gestion-filtro-estado">Filtrar por estado</label>
                            <select id="gestion-filtro-estado" value={filtros.estado}
                                onChange={(e) => cambiarFiltro("estado", e.target.value)}>
                                <option value="">Todas</option>
                                <option value="activa">Activas</option>
                                <option value="cancelada">Canceladas</option>
                            </select>
                        </div>
                        <div className="gestion-filtro">
                            <label htmlFor="gestion-filtro-laboratorio">Filtrar por laboratorio</label>
                            <select id="gestion-filtro-laboratorio" value={filtros.laboratorioId}
                                onChange={(e) => cambiarFiltro("laboratorioId", e.target.value)}>
                                <option value="">Todos</option>
                                {laboratorios.map((lab) => <option key={lab.id} value={lab.id}>{lab.nombre}</option>)}
                            </select>
                        </div>
                        <div className="gestion-filtro">
                            <label htmlFor="gestion-filtro-fecha">Filtrar por fecha</label>
                            <input id="gestion-filtro-fecha" type="date" value={filtros.fecha}
                                onChange={(e) => cambiarFiltro("fecha", e.target.value)} />
                        </div>
                        {filtrando && (
                            <button type="button" className="gestion-secundario"
                                onClick={() => setFiltros(FILTROS_INICIALES)}>
                                Limpiar filtros
                            </button>
                        )}
                    </div>

                    <div className="mis-reservas-grupo">
                        <h2 id={tituloTablaId}>Reservas</h2>
                        <p className="gestion-resumen" role="status">
                            Mostrando {visibles.length} de {conteo.total} reservas ({conteo.activas} activas, {conteo.canceladas} canceladas).
                        </p>

                        {visibles.length === 0 ? (
                            <p role="status">No hay reservas que coincidan con los filtros.</p>
                        ) : (
                            <div className="mis-reservas-tabla-contenedor" tabIndex={0} role="region" aria-labelledby={tituloTablaId}>
                                <table className="mis-reservas-tabla gestion-tabla" aria-labelledby={tituloTablaId}>
                                    <thead>
                                        <tr>
                                            <th scope="col">Responsable</th>
                                            <th scope="col">Laboratorio</th>
                                            <th scope="col">Fecha</th>
                                            <th scope="col">Horario</th>
                                            <th scope="col">Estado</th>
                                            <th scope="col">Acciones</th>
                                        </tr>
                                    </thead>
                                    <tbody>
                                        {visibles.map((reserva) => {
                                            const tipo = tipoEstado(reserva.estado);
                                            const fecha = formatearFechaReserva(reserva.fecha);
                                            return (
                                                <tr key={reserva.id} className={`mis-reservas-fila--${tipo}`}>
                                                    <td>
                                                        {reserva.nombreUsuario}
                                                        <span className="gestion-usuario">{reserva.username}</span>
                                                    </td>
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
                                                    <td>
                                                        {puedeCancelarReserva(reserva) && (
                                                            <button
                                                                type="button"
                                                                className="mis-reservas-cancelar"
                                                                onClick={() => cancelar(reserva.id)}
                                                                disabled={cancelandoId !== null}
                                                                aria-label={`Cancelar la reserva de ${reserva.nombreLaboratorio} de ${reserva.nombreUsuario} del ${fecha}`}>
                                                                {cancelandoId === reserva.id ? "Cancelando..." : "Cancelar"}
                                                            </button>
                                                        )}
                                                    </td>
                                                </tr>
                                            );
                                        })}
                                    </tbody>
                                </table>
                            </div>
                        )}
                    </div>
                </>
            )}
        </section>
    );
}

export default GestionReservas;
