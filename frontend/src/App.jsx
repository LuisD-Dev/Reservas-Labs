import { useState } from 'react'
import Login from './components/Login'
import './App.css'

function usuarioGuardado() {
    const datos = localStorage.getItem('usuario')
    return datos ? JSON.parse(datos) : null
}

function App() {
    const [usuarioLogueado, setUsuarioLogueado] = useState(usuarioGuardado)

    function handleCerrarSesion() {
        localStorage.removeItem('usuario')
        setUsuarioLogueado(null)
    }

    if (!usuarioLogueado) {
        return (
            <Login 
                onLoginExitoso={(usuario) => {
                    localStorage.setItem('usuario', JSON.stringify(usuario))
                    setUsuarioLogueado(usuario)
                }} 
            />
        )
    }

    return (
        <section id="center">
            <h1>Bienvenido, {usuarioLogueado.nombre}</h1>
            <p>Rol: {usuarioLogueado.rol}</p>

            {usuarioLogueado.rol === 'Admin' && (
                <div className="panel-admin">
                    <h3>Panel de Control del Administrador</h3>
                </div>
            )}

            {usuarioLogueado.rol === 'Cliente' && (
                <div className="panel-cliente">
                    <h3>Mis Reservas</h3>
                </div>
            )}

            <button type="button" onClick={handleCerrarSesion}>
                Cerrar sesión
            </button>
        </section>
    )
}

export default App