import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { expect, test, vi } from 'vitest';
import Login from './Login';
import { respuesta } from '../test/fixtures';

test('expone campos accesibles y rechaza el envío vacío sin peticiones', async () => {
    const user = userEvent.setup();
    render(<Login />);
    expect(screen.getByRole('textbox', { name: 'Usuario' })).toHaveAttribute('maxLength', '50');
    expect(screen.getByLabelText('Contraseña')).toHaveAttribute('type', 'password');
    await user.click(screen.getByRole('button', { name: 'Iniciar sesión' }));
    expect(screen.getByRole('alert')).toHaveTextContent('Usuario y contraseña son obligatorios.');
    expect(fetch).not.toHaveBeenCalled();
});

test('mostrar y ocultar contraseña cambia tipo y nombre accesible mediante teclado', async () => {
    const user = userEvent.setup();
    render(<Login />);
    await user.tab();
    await user.tab();
    await user.tab();
    expect(screen.getByRole('button', { name: 'Mostrar contraseña' })).toHaveFocus();
    await user.keyboard('{Enter}');
    expect(screen.getByLabelText('Contraseña')).toHaveAttribute('type', 'text');
    await user.click(screen.getByRole('button', { name: 'Ocultar contraseña' }));
    expect(screen.getByLabelText('Contraseña')).toHaveAttribute('type', 'password');
});

test('envía credenciales sin alterar caracteres o contraseña y entrega la sesión', async () => {
    const user = userEvent.setup();
    const onLoginExitoso = vi.fn();
    const sesion = { usuarioId: 3, nombre: 'José', rol: 'Usuario', token: 'token-prueba', expiraUtc: '2099-01-01T00:00:00Z' };
    fetch.mockResolvedValue(respuesta(sesion));
    render(<Login onLoginExitoso={onLoginExitoso} />);
    await user.type(screen.getByLabelText('Usuario'), 'José+@dominio');
    await user.type(screen.getByLabelText('Contraseña'), '  clave con espacios  ');
    await user.click(screen.getByRole('button', { name: 'Iniciar sesión' }));
    await waitFor(() => expect(onLoginExitoso).toHaveBeenCalledWith(sesion));
    expect(fetch).toHaveBeenCalledTimes(1);
    const [url, opciones] = fetch.mock.calls[0];
    expect(url).toBe('http://localhost:5282/api/auth/login');
    expect(opciones.method).toBe('POST');
    expect(JSON.parse(opciones.body)).toEqual({ Username: 'José+@dominio', Password: '  clave con espacios  ' });
});

test('presenta el error de autenticación del servidor mediante una alerta', async () => {
    const user = userEvent.setup();
    const onLoginExitoso = vi.fn();
    fetch.mockResolvedValue(respuesta({ mensaje: 'Cuenta bloqueada temporalmente.' }, 423));
    render(<Login onLoginExitoso={onLoginExitoso} />);
    await user.type(screen.getByLabelText('Usuario'), 'usuario');
    await user.type(screen.getByLabelText('Contraseña'), 'incorrecta');
    await user.click(screen.getByRole('button', { name: 'Iniciar sesión' }));
    expect(await screen.findByRole('alert')).toHaveTextContent('Cuenta bloqueada temporalmente.');
    expect(onLoginExitoso).not.toHaveBeenCalled();
});
