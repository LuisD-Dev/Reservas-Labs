const ICONOS = {
    laboratorios: (
        <>
            <rect x="3" y="4" width="18" height="12" rx="2" />
            <path d="M8 20h8M12 16v4" />
        </>
    ),
    "mis-reservas": (
        <>
            <rect x="3" y="5" width="18" height="16" rx="2" />
            <path d="M3 10h18M8 3v4M16 3v4M9 15l2 2 4-4" />
        </>
    ),
    "gestion-reservas": (
        <>
            <rect x="8" y="2" width="8" height="4" rx="1" />
            <path d="M16 4h2a2 2 0 0 1 2 2v14a2 2 0 0 1-2 2H6a2 2 0 0 1-2-2V6a2 2 0 0 1 2-2h2" />
            <path d="M9 12h6M9 16h4" />
        </>
    ),
    reportes: (
        <>
            <rect x="4" y="12" width="4" height="8" rx="1" />
            <rect x="10" y="4" width="4" height="16" rx="1" />
            <rect x="16" y="9" width="4" height="11" rx="1" />
        </>
    ),
};

function Icono({ nombre, tamano = 24 }) {
    return (
        <svg
            width={tamano}
            height={tamano}
            viewBox="0 0 24 24"
            fill="none"
            stroke="currentColor"
            strokeWidth="1.8"
            strokeLinecap="round"
            strokeLinejoin="round"
            aria-hidden="true"
        >
            {ICONOS[nombre]}
        </svg>
    );
}

export default Icono;
