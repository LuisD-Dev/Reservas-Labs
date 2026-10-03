import { useState } from "react";
import "./Disponibilidad.css";
import { hoyComoTexto, validarConsulta } from "../utils/disponibilidad";

// HU3 - #38 Pantalla para consultar si un laboratorio está disponible en una
// fecha y un rango de horas. Recibe la lista de laboratorios que ya cargó la
// pantalla de Laboratorios y el laboratorio desde el que se abrió.

function Disponibilidad({ laboratorios, laboratorioInicialId, onVolver }) {
    const [laboratorioId, setLaboratorioId] = useState(
        laboratorioInicialId ? String(laboratorioInicialId) : ""
    );
    const [fecha, setFecha] = useState("");
    const [horaInicio, setHoraInicio] = useState("");
    const [horaFin, setHoraFin] = useState("");
    const [errores, setErrores] = useState({});
    const [consulta, setConsulta] = useState(null);

    const laboratorio = laboratorios.find((lab) => String(lab.id) === laboratorioId);

    function limpiarResultado() {
        setConsulta(null);
    }

    function handleSubmit(e) {
        e.preventDefault();
        const nuevosErrores = validarConsulta({ laboratorioId, fecha, horaInicio, horaFin });
        setErrores(nuevosErrores);

        if (Object.keys(nuevosErrores).length > 0) {
            setConsulta(null);
            return;
        }

        setConsulta({ laboratorioId: Number(laboratorioId), fecha, horaInicio, horaFin });
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

                <button type="submit" className="disp-consultar">
                    Consultar
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

            {consulta && (
                <div className="disp-resultado" aria-live="polite">
                    <p>
                        Consulta para el {consulta.fecha}, de {consulta.horaInicio} a {consulta.horaFin}.
                    </p>
                </div>
            )}
        </section>
    );
}

export default Disponibilidad;
