import { useState } from 'react';
import Login from './components/Login';
import Dashboard from './components/Dashboard';
import './App.css';
import './components/Dashboard.css';

function usuarioGuardado() {
    const datos = localStorage.getItem('usuario');
    return datos ? JSON.parse(datos) : null;
}

function App() {
    const [usuarioLogueado, setUsuarioLogueado] = useState(usuarioGuardado);

    function handleCerrarSesion() {
        localStorage.removeItem('usuario');
        setUsuarioLogueado(null);
    }

    if (!usuarioLogueado) {
        return (
            <Login
                onLoginExitoso={(usuario) => {
                    localStorage.setItem('usuario', JSON.stringify(usuario));
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