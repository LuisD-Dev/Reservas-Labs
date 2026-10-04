import { useState } from "react";
import "./Disponibilidad.css";
import { apiFetch } from "../services/api";
import { evaluarDisponibilidad, hoyComoTexto, validarConsulta } from "../utils/disponibilidad";

// HU3 - #38 Pantalla para consultar si un laboratorio está disponible en una
// fecha y un rango de horas. Recibe la lista de laboratorios que ya cargó la
// pantalla de Laboratorios y el laboratorio desde el que se abrió.

function Disponibilidad({ laboratorios, laboratorioInicialId, onVolver, onContinuarReserva }) {
    const [laboratorioId, setLaboratorioId] = useState(
        laboratorioInicialId ? String(laboratorioInicialId) : ""
    );
    const [fecha, setFecha] = useState("");
    const [horaInicio, setHoraInicio] = useState("");
    const [horaFin, setHoraFin] = useState("");
    const [errores, setErrores] = useState({});
    const [consulta, setConsulta] = useState(null);
    const [resultado, setResultado] = useState(null);
    const [cargando, setCargando] = useState(false);
    const [errorServidor, setErrorServidor] = useState("");

    const laboratorio = laboratorios.find((lab) => String(lab.id) === laboratorioId);

    function limpiarResultado() {
        setConsulta(null);
        setResultado(null);
        setErrorServidor("");
    }

    // HU3 - #39 Consulta los horarios del laboratorio en la API y evalúa el rango.
    async function consultar(datos) {
        setCargando(true);
        setErrorServidor("");
        setResultado(null);

        try {
            const respuesta = await apiFetch(`/api/laboratorios/${datos.laboratorioId}/disponibilidad`);

            if (respuesta.status === 401) {
                setErrorServidor("Tu sesión venció. Inicia sesión de nuevo.");
            } else if (respuesta.status === 403) {
                setErrorServidor("Tu usuario no tiene permiso para consultar la disponibilidad.");
            } else if (!respuesta.ok) {
                setErrorServidor("No se pudo consultar la disponibilidad. Intenta de nuevo.");
            } else {
                const horarios = await respuesta.json();
                setResultado(evaluarDisponibilidad(horarios, datos, laboratorio));
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
                        value={laboratorioId}
                        onChange={(e) => {
                            setLaboratorioId(e.target.value);
                            limpiarResultado();
                        }}
                    >
                        <option value="">Selecciona un laboratorio</option>
                        {laboratorios.map((lab) => (
                            <option key={lab.id} value={lab.id}>
                                {lab.nombre} — {lab.ubicacion}
                            </option>
                        ))}
                    </select>
                    {errores.laboratorioId && <span className="disp-error">{errores.laboratorioId}</span>}
                </div>

                <div className="disp-campo">
                    <label htmlFor="disp-fecha">Fecha</label>
                    <input
                        id="disp-fecha"
                        type="date"
                        min={hoyComoTexto()}
                        value={fecha}
                        onChange={(e) => {
                            setFecha(e.target.value);
                            limpiarResultado();
                        }}
                    />
                    {errores.fecha && <span className="disp-error">{errores.fecha}</span>}
                </div>

                <div className="disp-campo">
                    <label htmlFor="disp-inicio">Hora inicial</label>
                    <input
                        id="disp-inicio"
                        type="time"
                        value={horaInicio}
                        onChange={(e) => {
                            setHoraInicio(e.target.value);
                            limpiarResultado();
                        }}
                    />
                    {errores.horaInicio && <span className="disp-error">{errores.horaInicio}</span>}
                </div>

                <div className="disp-campo">
                    <label htmlFor="disp-fin">Hora final</label>
                    <input
                        id="disp-fin"
                        type="time"
                        value={horaFin}
                        onChange={(e) => {
                            setHoraFin(e.target.value);
                            limpiarResultado();
                        }}
                    />
                    {errores.horaFin && <span className="disp-error">{errores.horaFin}</span>}
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

            {errorServidor && (
                <div className="labs-error" role="alert">
                    <p>{errorServidor}</p>
                    <button onClick={() => consultar(consulta)}>Reintentar</button>
                </div>
            )}

            {consulta && resultado && (
                <div className="disp-resultado" aria-live="polite">
                    <div className={`disp-veredicto ${resultado.disponible ? "disp-si" : "disp-no"}`}>
                        {resultado.disponible ? "Disponible" : "No disponible"}
                    </div>
                    <p className="disp-detalle">
                        {laboratorio?.nombre}, {consulta.fecha}, de {consulta.horaInicio} a {consulta.horaFin}.{" "}
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

                    <h3>Horarios del {consulta.fecha}</h3>
                    {resultado.horariosDelDia.length === 0 ? (
                        <p className="labs-mensaje">No hay horarios registrados para esta fecha.</p>
                    ) : (
                        <ul className="disp-horarios">
                            {resultado.horariosDelDia.map((h) => (
                                <li key={h.id}>
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
