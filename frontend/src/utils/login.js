export function crearCredenciales(usuario, contrasena) {
    return { Username: usuario.trim(), Password: contrasena };
}
