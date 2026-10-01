import { useEffect, useState } from 'react';
import Login from './components/Login';
import Dashboard from './components/Dashboard';
import {
    EVENTO_SESION_EXPIRADA,
    cerrarSesion,
    guardarSesion,
    obtenerSesion,
} from './services/api';
import './App.css';
import './components/Dashboard.css';

function App() {
    // NFR1 - #46 Solo se restaura una sesión con token vigente.
    const [usuarioLogueado, setUsuarioLogueado] = useState(obtenerSesion);

    // Si la API responde 401 (token vencido o inválido), se vuelve al login.
    useEffect(() => {
        const alExpirar = () => setUsuarioLogueado(null);
        window.addEventListener(EVENTO_SESION_EXPIRADA, alExpirar);
        return () => window.removeEventListener(EVENTO_SESION_EXPIRADA, alExpirar);
    }, []);

    function handleCerrarSesion() {
        cerrarSesion();
        setUsuarioLogueado(null);
    }

    if (!usuarioLogueado) {
        return (
            <Login
                onLoginExitoso={(usuario) => {
                    guardarSesion(usuario);
                    setUsuarioLogueado(usuario);
                }}
            />
        );
    }

    return (
        <Dashboard
            usuario={usuarioLogueado}
            onCerrarSesion={handleCerrarSesion}
        />
    );
}

export default App;