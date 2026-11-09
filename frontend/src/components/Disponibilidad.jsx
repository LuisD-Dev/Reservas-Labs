import { filtrarLaboratoriosHabilitados } from "../utils/laboratorios";
import { useState } from "react";
import "./Formularios.css";
import "./Disponibilidad.css";
import { apiFetch } from "../services/api";
import {
    interpretarDisponibilidad,
    formatearFecha,
    hoyComoTexto,
    validarConsulta,
} from "../utils/disponibilidad";

// HU3 - #38 Pantalla para consultar si un laboratorio está disponible en una
// fecha y un rango de horas. Recibe la lista de laboratorios que ya cargó la
// pantalla de Laboratorios y el laboratorio desde el que se abrió.

function Disponibilidad({ laboratorios = [], laboratorioInicialId, onVolver, onContinuarReserva }) {
    const habilitados = filtrarLaboratoriosHabilitados(laboratorios);
    const [seleccionId, setLaboratorioId] = useState(
        habilitados.some(lab => String(lab.id) === String(laboratorioInicialId)) ? String(laboratorioInicialId) : ""
    );
    const [fecha, setFecha] = useState("");
    const [horaInicio, setHoraInicio] = useState("");
    const [horaFin, setHoraFin] = useState("");
    const [errores, setErrores] = useState({});
    const [consulta, setConsulta] = useState(null);
    const [resultado, setResultado] = useState(null);
    const [cargando, setCargando] = useState(false);
    const [errorServidor, setErrorServidor] = useState("");

    const laboratorio = habilitados.find((lab) => String(lab.id) === seleccionId);
    const laboratorioId = laboratorio ? seleccionId : "";

    function limpiarResultado() {
        setConsulta(null);
        setResultado(null);
        setErrorServidor("");
    }

    function limpiarErrores(...campos) {
        setErrores((anteriores) => {
            const siguientes = { ...anteriores };
            campos.forEach((campo) => delete siguientes[campo]);
            return siguientes;
        });
    }

    // HU3 - #39 Consulta los horarios del laboratorio en la API y evalúa el rango.
    async function consultar(datos) {
        if (!habilitados.some(lab => String(lab.id) === String(datos.laboratorioId))) return;
        setCargando(true);
        setErrorServidor("");
        setResultado(null);

        try {
            const respuesta = await apiFetch(`/api/laboratorios/${datos.laboratorioId}/disponibilidad/verificar`, {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ fecha: datos.fecha, horaInicio: `${datos.horaInicio}:00`, horaFin: `${datos.horaFin}:00` }),
            });

            if (respuesta.status === 401) {
                setErrorServidor("Tu sesión venció. Inicia sesión de nuevo.");
            } else if (respuesta.status === 403) {
                setErrorServidor("Tu usuario no tiene permiso para consultar la disponibilidad.");
            } else if (respuesta.status === 400) {
                const cuerpo = await respuesta.json();
                setErrores(cuerpo.errores ?? cuerpo.errors ?? {});
            } else if (!respuesta.ok) {
                setErrorServidor("No se pudo consultar la disponibilidad. Intenta de nuevo.");
            } else {
                const datosServidor = await respuesta.json();
                setResultado(interpretarDisponibilidad(datosServidor));
            }
        } catch {
            setErrorServidor("No se pudo conectar con el servidor. Verifica que la API esté en ejecución.");
        } finally {
            setCargando(false);
        }
    }

    function handleSubmit(e) {
        e.preventDefault();
        const nuevosErrores = validarConsulta({ laboratorioId, fecha, horaInicio, horaFin });
        setErrores(nuevosErrores);

        if (Object.keys(nuevosErrores).length > 0) {
            limpiarResultado();
            return;
        }

        const datos = { laboratorioId: Number(laboratorioId), fecha, horaInicio, horaFin };
        setConsulta(datos);
        consultar(datos);
    }

    return (
        <section className="disp-section">
            <button className="labs-volver" onClick={onVolver}>
                Volver a laboratorios
            </button>

            <div className="labs-encabezado">
                <h1>Consultar disponibilidad</h1>
                <p>Elige un laboratorio, la fecha y el horario que necesitas.</p>
            </div>

            <form className="disp-form" onSubmit={handleSubmit} noValidate>
                <div className="disp-campo disp-campo-ancho">
                    <label htmlFor="disp-laboratorio">Laboratorio</label>
                    <select
                        id="disp-laboratorio"
                        aria-invalid={Boolean(errores.laboratorioId)}
                        aria-describedby={errores.laboratorioId ? "disp-error-laboratorioId" : undefined}
                        value={laboratorioId}
                        disabled={cargando}
                        onChange={(e) => {
                            setLaboratorioId(e.target.value);
                            limpiarErrores("laboratorioId");
                            limpiarResultado();
                        }}
                    >
                        <option value="">Selecciona un laboratorio</option>
                        {habilitados.map((lab) => (
                            <option key={lab.id} value={lab.id}>
                                {lab.nombre} — {lab.ubicacion}
                            </option>
                        ))}
                    </select>
                    {errores.laboratorioId && <span id="disp-error-laboratorioId" className="disp-error" role="alert">{errores.laboratorioId}</span>}
                </div>

                <div className="disp-campo">
                    <label htmlFor="disp-fecha">Fecha</label>
                    <input
                        id="disp-fecha"
                        aria-invalid={Boolean(errores.fecha)}
                        aria-describedby={errores.fecha ? "disp-error-fecha" : undefined}
                        type="date"
                        min={hoyComoTexto()}
                        value={fecha}
                        disabled={cargando}
                        onChange={(e) => {
                            setFecha(e.target.value);
                            limpiarErrores("fecha");
                            limpiarResultado();
                        }}
                    />
                    {errores.fecha && <span id="disp-error-fecha" className="disp-error" role="alert">{errores.fecha}</span>}
                </div>

                <div className="disp-campo">
                    <label htmlFor="disp-inicio">Hora inicial</label>
                    <input
                        id="disp-inicio"
                        aria-invalid={Boolean(errores.horaInicio)}
                        aria-describedby={errores.horaInicio ? "disp-error-horaInicio" : undefined}
                        type="time"
                        value={horaInicio}
                        disabled={cargando}
                        onChange={(e) => {
                            const nuevaHoraInicio = e.target.value;
                            setHoraInicio(nuevaHoraInicio);
                            limpiarErrores(
                                "horaInicio",
                                ...(horaFin && nuevaHoraInicio && horaFin > nuevaHoraInicio ? ["horaFin"] : [])
                            );
                            limpiarResultado();
                        }}
                    />
                    {errores.horaInicio && <span id="disp-error-horaInicio" className="disp-error" role="alert">{errores.horaInicio}</span>}
                </div>

                <div className="disp-campo">
                    <label htmlFor="disp-fin">Hora final</label>
                    <input
                        id="disp-fin"
                        aria-invalid={Boolean(errores.horaFin)}
                        aria-describedby={errores.horaFin ? "disp-error-horaFin" : undefined}
                        type="time"
                        value={horaFin}
                        disabled={cargando}
                        onChange={(e) => {
                            setHoraFin(e.target.value);
                            limpiarErrores("horaFin");
                            limpiarResultado();
                        }}
                    />
                    {errores.horaFin && <span id="disp-error-horaFin" className="disp-error" role="alert">{errores.horaFin}</span>}
                </div>

                <button type="submit" className="disp-consultar" disabled={cargando}>
                    {cargando ? "Consultando..." : "Consultar"}
                </button>
            </form>

            {laboratorio && (
                <div className="disp-lab-info">
                    <strong>{laboratorio.nombre}</strong>
                    <span>{laboratorio.ubicacion}</span>
                    <span>Capacidad: {laboratorio.capacidad} personas</span>
                    <span>Estado: {laboratorio.estado}</span>
                </div>
            )}

            {errorServidor && consulta && laboratorio && (
                <div className="labs-error" role="alert">
                    <p>{errorServidor}</p>
                    <button onClick={() => consultar(consulta)} disabled={cargando}>
                        {cargando ? "Consultando..." : "Reintentar"}
                    </button>
                </div>
            )}

            {consulta && resultado && laboratorio && (
                <div className="disp-resultado" aria-live="polite">
                    <div className={`disp-veredicto ${resultado.disponible ? "disp-si" : "disp-no"}`}>
                        {resultado.disponible ? "Disponible" : "No disponible"}
                    </div>
                    <p className="disp-detalle">
                        {laboratorio?.nombre}, {formatearFecha(consulta.fecha)}, de {consulta.horaInicio} a {consulta.horaFin}.{" "}
                        {resultado.motivo}
                    </p>

                    {resultado.disponible && (
                        <button
                            className="disp-reservar"
                            onClick={() => onContinuarReserva?.(consulta)}
                            disabled={!onContinuarReserva}
                            title={onContinuarReserva ? "" : "El registro de reservas se habilita en la HU4"}
                        >
                            Continuar con la reserva
                        </button>
                    )}

                    <h3>Horarios del {formatearFecha(consulta.fecha)}</h3>
                    {resultado.horariosDelDia.length === 0 ? (
                        <p className="labs-mensaje">No hay horarios registrados para esta fecha.</p>
                    ) : (
                        <ul className="disp-horarios">
                            {resultado.horariosDelDia.map((h) => (
                                <li key={`${h.id}-${h.horaInicio}-${h.horaFin}`}>
                                    <span>{h.horaInicio} – {h.horaFin}</span>
                                    <span className={h.estado === "Disponible" ? "disp-chip-si" : "disp-chip-no"}>
                                        {h.estado}
                                    </span>
                                </li>
                            ))}
                        </ul>
                    )}
                </div>
            )}
        </section>
    );
}

export default Disponibilidad;
