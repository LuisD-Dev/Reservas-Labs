import { useEffect, useRef, useState } from "react";
import { apiFetch } from "../services/api";
import { crearCuerpoReserva, mensajeErrorReserva, validarReserva } from "../utils/reserva";
import { crearCuerpoReserva, mensajeConfirmacionReserva, mensajeErrorReserva, validarReserva } from "../utils/reserva";

function Reserva({ usuario, datosIniciales = {}, laboratoriosIniciales, onVolver }) {
    const reutilizar = laboratoriosIniciales?.length > 0;
    const [laboratorios, setLaboratorios] = useState(laboratoriosIniciales ?? []);
    const [cargando, setCargando] = useState(!reutilizar);
    const [errorCarga, setErrorCarga] = useState("");
    const [datos, setDatos] = useState({
        laboratorioId: String(datosIniciales.laboratorioId ?? ""),
        fecha: datosIniciales.fecha ?? "",
        horaInicio: datosIniciales.horaInicio ?? "",
        horaFin: datosIniciales.horaFin ?? "",
    });
    const [errores, setErrores] = useState({});
    const [error, setError] = useState("");
    const [enviando, setEnviando] = useState(false);
    const [exito, setExito] = useState(false);
    const [confirmacion, setConfirmacion] = useState("");
    const solicitudEnCurso = useRef(false);

    async function cargar(signal) {
        setCargando(true);
        setErrorCarga("");
        try {
            const respuesta = await apiFetch("/api/laboratorios", { signal });
            if (signal?.aborted) return;
            if (respuesta.ok) {
                const lista = await respuesta.json();
                if (!signal?.aborted) setLaboratorios(lista);
            } else {
                setErrorCarga(respuesta.status === 401 ? mensajeErrorReserva(401)
                    : respuesta.status === 403 ? "Tu usuario no tiene permiso para ver los laboratorios."
                        : "No se pudo cargar la lista de laboratorios.");
            }
        } catch (errorCarga) {
            if (!signal?.aborted && errorCarga.name !== "AbortError") setErrorCarga(mensajeErrorReserva());
        } finally {
            if (!signal?.aborted) setCargando(false);
        }
    }

    useEffect(() => {
        if (reutilizar) return;
        const controller = new AbortController();
        const inicio = window.setTimeout(() => cargar(controller.signal), 0);
        return () => {
            window.clearTimeout(inicio);
            controller.abort();
        };
    }, [reutilizar]);

    function cambiar(campo, valor) {
        setDatos((anteriores) => ({ ...anteriores, [campo]: valor }));
        setErrores((anteriores) => ({ ...anteriores, [campo]: undefined }));
        setError("");
        setExito(false);
    }

    async function registrar(evento) {
        evento.preventDefault();
        if (solicitudEnCurso.current || cargando || errorCarga || exito) return;
        const nuevosErrores = validarReserva(datos);
        setErrores(nuevosErrores);
        setError("");
        if (Object.keys(nuevosErrores).length > 0) return;
        solicitudEnCurso.current = true;
        setEnviando(true);
        try {
            const respuesta = await apiFetch("/api/reservas", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify(crearCuerpoReserva(datos)),
            });
            if (respuesta.status === 201) {
                const cuerpo = await respuesta.json().catch(() => null);
                const laboratorio = laboratorios.find(
                    (lab) => String(lab.id) === String(datos.laboratorioId));
                setConfirmacion(mensajeConfirmacionReserva(cuerpo?.reserva, laboratorio?.nombre));
                setExito(true);
            } else {
                const detalle = respuesta.status === 400
                    ? await respuesta.json().catch(() => null) : null;
                setError(mensajeErrorReserva(respuesta.status, detalle));
            }
        } catch {
            setError(mensajeErrorReserva());
        } finally {
            solicitudEnCurso.current = false;
            setEnviando(false);
        }
    }

    const bloqueado = cargando || enviando || Boolean(errorCarga);
    return (
        <section className="reserva-section">
            <button className="labs-volver" onClick={onVolver}>Volver</button>
            <div className="labs-encabezado">
                <h1>Registrar reserva</h1>
                <p>Selecciona el laboratorio, la fecha y el horario que necesitas.</p>
            </div>
            {cargando && <p role="status">Cargando laboratorios...</p>}
            {errorCarga && <div className="labs-error" role="alert">
                <p>{errorCarga}</p>
                <button onClick={() => cargar()} disabled={cargando}>Reintentar</button>
            </div>}
            {!cargando && !errorCarga && laboratorios.length === 0 && (
                <p role="status">Todavía no hay laboratorios registrados.</p>
            )}
            <form className="reserva-form" onSubmit={registrar} noValidate aria-busy={enviando}>
                <div className="reserva-campo reserva-ancho">
                    <label htmlFor="reserva-responsable">Responsable</label>
                    <input id="reserva-responsable" value={usuario?.nombre ?? ""} readOnly />
                </div>
                <div className="reserva-campo reserva-ancho">
                    <label htmlFor="reserva-laboratorioId">Laboratorio</label>
                    <select id="reserva-laboratorioId" value={datos.laboratorioId}
                        disabled={bloqueado} required
                        aria-invalid={Boolean(errores.laboratorioId)}
                        aria-describedby={errores.laboratorioId ? "error-laboratorioId" : undefined}
                        onChange={(e) => cambiar("laboratorioId", e.target.value)}>
                        <option value="">Selecciona un laboratorio</option>
                        {laboratorios.map((lab) => <option key={lab.id} value={lab.id}>
                            {lab.nombre} — {lab.ubicacion}
                        </option>)}
                    </select>
                    {errores.laboratorioId && <span className="reserva-error" id="error-laboratorioId" role="alert">{errores.laboratorioId}</span>}
                </div>
                {[
                    ["fecha", "Fecha", "date"],
                    ["horaInicio", "Hora inicial", "time"],
                    ["horaFin", "Hora final", "time"],
                ].map(([campo, etiqueta, tipo]) => (
                    <div className="reserva-campo" key={campo}>
                        <label htmlFor={`reserva-${campo}`}>{etiqueta}</label>
                        <input id={`reserva-${campo}`} type={tipo} value={datos[campo]}
                            disabled={bloqueado} required
                            aria-invalid={Boolean(errores[campo])}
                            aria-describedby={errores[campo] ? `error-${campo}` : undefined}
                            onChange={(e) => cambiar(campo, e.target.value)} />
                        {errores[campo] && <span className="reserva-error" id={`error-${campo}`} role="alert">{errores[campo]}</span>}
                    </div>
                ))}
                <button type="submit" className="reserva-registrar" disabled={bloqueado || exito}>
                    {enviando ? "Registrando..." : "Registrar reserva"}
                </button>
            </form>
            {error && <p className="labs-error" role="alert">{error}</p>}
            {exito && <p className="reserva-exito" role="status">{confirmacion}</p>}
        </section>
    );
}

export default Reserva;
