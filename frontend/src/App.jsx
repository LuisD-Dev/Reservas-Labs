import { useState } from 'react'
import Login from './components/Login'
import './App.css'

function usuarioGuardado() {
    const datos = sessionStorage.getItem('usuario')
    return datos ? JSON.parse(datos) : null
}

function App() {
    const [usuarioLogueado, setUsuarioLogueado] = useState(usuarioGuardado)

    function handleCerrarSesion() {
        sessionStorage.removeItem('usuario')
        setUsuarioLogueado(null)
    }

    if (!usuarioLogueado) {
        return <Login onLoginExitoso={(usuario) => setUsuarioLogueado(usuario)} />
    }

    return (
        <section id="center">
            <h1>Bienvenido, {usuarioLogueado.nombre}</h1>
            <p>Rol: {usuarioLogueado.rol}</p>
            <button type="button" onClick={handleCerrarSesion}>
                Cerrar sesión
            </button>
        </section>
    )
}

export default App
