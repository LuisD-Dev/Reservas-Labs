import { useEffect, useState } from "react";
import { apiFetch } from "../services/api";
import "./Laboratorios.css";

// si la API responde 401, cierra la sesion por su cuenta.

function claseEstado(estado) {
    const texto = (estado ?? "").toLowerCase();
    if (texto === "habilitado") return "estado-disponible";
    if (texto === "fuera de servicio") return "estado-mantenimiento";
    return "estado-otro";
}

function Laboratorios({ onVolver }) {
    const [laboratorios, setLaboratorios] = useState([]);
    const [cargando, setCargando] = useState(true);
    const [error, setError] = useState("");
    const [busqueda, setBusqueda] = useState("");

    async function cargar() {
        setCargando(true);
        setError("");

        try {
            const respuesta = await apiFetch("/api/laboratorios");

            if (respuesta.status === 401) {
                setError("Tu sesión venció. Inicia sesión de nuevo.");
            } else if (respuesta.status === 403) {
                setError("Tu usuario no tiene permiso para ver los laboratorios.");
            } else if (!respuesta.ok) {
                setError("No se pudo cargar la lista de laboratorios.");
            } else {
                setLaboratorios(await respuesta.json());
            }
        } catch {
            setError("No se pudo conectar con el servidor. Verifica que la API esté en ejecución.");
        } finally {
            setCargando(false);
        }
    }

    useEffect(() => {
        cargar();
    }, []);

    const texto = busqueda.trim().toLowerCase();
    const visibles = laboratorios.filter(
        (lab) =>
            lab.nombre.toLowerCase().includes(texto) ||
            lab.ubicacion.toLowerCase().includes(texto)
    );

    return (
        <section className="labs-section">
            <button className="labs-volver" onClick={onVolver}>
                Volver al inicio
            </button>

            <div className="labs-encabezado">
                <h1>Laboratorios OBLD</h1>
                <p>Consulta la ubicación, la capacidad y el estado de cada laboratorio.</p>
            </div>

            <input
                className="labs-buscar"
                type="search"
                placeholder="Buscar por nombre o ubicación"
                value={busqueda}
                onChange={(e) => setBusqueda(e.target.value)}
                aria-label="Buscar laboratorio"
            />

            {cargando && <p className="labs-mensaje">Cargando laboratorios...</p>}

            {error && (
                <div className="labs-error" role="alert">
                    <p>{error}</p>
                    <button onClick={cargar}>Reintentar</button>
                </div>
            )}

            {!cargando && !error && visibles.length === 0 && (
                <p className="labs-mensaje">
                    {laboratorios.length === 0
                        ? "Todavía no hay laboratorios registrados."
                        : "Ningún laboratorio coincide con la búsqueda."}
                </p>
            )}

            {!cargando && !error && visibles.length > 0 && (
                <div className="labs-grid">
                    {visibles.map((lab) => (
                        <article key={lab.id} className="labs-card">
                            <div className="labs-card-top">
                                <h3>{lab.nombre}</h3>
                                <span className={`labs-estado ${claseEstado(lab.estado)}`}>
                                    {lab.estado}
                                </span>
                            </div>
                            <dl>
                                <div>
                                    <dt>Ubicación</dt>
                                    <dd>{lab.ubicacion}</dd>
                                </div>
                                <div>
                                    <dt>Capacidad</dt>
                                    <dd>{lab.capacidad} personas</dd>
                                </div>
                            </dl>
                        </article>
                    ))}
                </div>
            )}
        </section>
    );
}

export default Laboratorios;
