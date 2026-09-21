# Reservas-Labs
Proyecto de Pruebas de Software 
## Definición de Hecho

Una Historia de Usuario se considera completada cuando:

- Cumple todos los criterios de aceptación establecidos.
- El código compila y se ejecuta correctamente.
- La funcionalidad fue probada por el equipo.
- Los casos de prueba definidos fueron ejecutados.
- No existen errores conocidos que impidan utilizar la funcionalidad.
- Los commits correspondientes están registrados en Azure Repos.
- Los commits están relacionados con el PBI correspondiente.
- La funcionalidad se encuentra integrada en la rama principal.
- La documentación fue actualizada.
- La funcionalidad puede ser demostrada correctamente.

---

# Sistema de Reservas de Laboratorios

Proyecto desarrollado para el curso **ISW-622 - Pruebas de Software** de la Universidad Técnica Nacional.

El sistema permite administrar progresivamente la reserva de laboratorios, aplicando pruebas de software, control de versiones y aseguramiento de calidad.

## Tecnologías

- **Frontend:** React + Vite
- **Backend:** C# / ASP.NET Core Web API
- **Base de datos:** SQL Server
- **Control de versiones:** Git, GitHub y Azure DevOps
- **Pruebas:** xUnit, Postman y Azure Test Plans

## Arquitectura

```text
React + Vite
localhost:5173
      ↓
ASP.NET Core Web API
localhost:5282
      ↓
SQL Server
LaboratorioOBLD
```

## Requisitos

Antes de ejecutar el proyecto se requiere:

- .NET SDK
- Node.js y npm
- SQL Server
- SQL Server Management Studio
- Git
- Visual Studio o Visual Studio Code

# Ejecución del proyecto

## 1. Base de datos

Abrir **SQL Server Management Studio** y ejecutar:

```text
database/database.sql
```

La base de datos utilizada es:

```text
LaboratorioOBLD
```

La conexión se encuentra en:

```text
Backend/SistemaReservas.API/SistemaReservas.API/appsettings.json
```

Configuración actual:

```json
"DefaultConnection": "Server=localhost;Database=LaboratorioOBLD;Trusted_Connection=True;TrustServerCertificate=True;"
```

> Si la instancia de SQL Server utiliza otro nombre, se debe modificar `Server` en la cadena de conexión.

---

## 2. Ejecutar Backend

Desde la raíz del proyecto:

```powershell
cd ".\Backend\SistemaReservas.API\SistemaReservas.API"
dotnet run
```

El backend se ejecutará normalmente en:

```text
http://localhost:5282
```

Para comprobar la conexión con SQL Server:

```text
http://localhost:5282/db-test
```

Debe responder:

```text
Conexión con SQL Server exitosa.
```

La terminal del backend debe permanecer abierta.

---

## 3. Ejecutar Frontend

Abrir otra terminal:

```powershell
cd ".\frontend"
npm.cmd install
npm.cmd run dev
```

Abrir en el navegador:

```text
http://localhost:5173
```

> `npm.cmd install` solo es necesario la primera vez o cuando cambien las dependencias.

## Usuarios de prueba

En ambiente de desarrollo se crean automáticamente los siguientes usuarios si no existen:

| Usuario | Contraseña | Rol |
|---|---|---|
| `admin` | `1234` | Administrador |
| `usuario` | `1234` | Usuario |

Las contraseñas se almacenan utilizando hash y no como texto plano.

# HU1 - Inicio de sesión

Actualmente se encuentra implementado:

- Inicio de sesión.
- Validación de credenciales.
- Roles `Administrador` y `Usuario`.
- Identificación del usuario autenticado.
- Contraseñas mediante hash.
- Control de intentos fallidos.
- Bloqueo temporal después de 5 intentos incorrectos.

## Pruebas principales HU1

| Caso | Resultado esperado |
|---|---|
| Credenciales válidas | Permite acceso |
| Contraseña incorrecta | Rechaza acceso |
| Usuario inexistente | Rechaza acceso |
| Login Administrador | Identifica rol Administrador |
| Login Usuario | Identifica rol Usuario |
| 5 intentos incorrectos | Bloquea temporalmente al usuario |
| Login correcto | Reinicia intentos fallidos |

# Ejecución rápida

Una vez configurado el proyecto, normalmente solo se necesitan dos terminales:

### Terminal 1

```powershell
cd ".\Backend\SistemaReservas.API\SistemaReservas.API"
dotnet run
```

### Terminal 2

```powershell
cd ".\frontend"
npm.cmd run dev
```

Luego ingresar a:

```text
http://localhost:5173
```

# Problemas comunes

### `npm.ps1 cannot be loaded`

Utilizar:

```powershell
npm.cmd install
npm.cmd run dev
```

### Error `ENOENT package.json`

El comando de npm debe ejecutarse dentro de:

```text
Reservas-Labs/frontend
```

### El frontend abre en `5174`

El backend actualmente permite CORS desde:

```text
http://localhost:5173
```

Por lo tanto, se recomienda liberar el puerto `5173` antes de ejecutar Vite.

### No conecta a SQL Server

Comprobar:

- SQL Server está iniciado.
- Existe `LaboratorioOBLD`.
- La cadena de conexión es correcta.
- El usuario de Windows tiene acceso a SQL Server.

# Repositorios

**GitHub**

```text
https://github.com/LuisD-Dev/Reservas-Labs
```

**Repositorio académico principal:** Azure DevOps / Azure Repos.

# Notas

Las credenciales y configuraciones actuales son únicamente para fines académicos, desarrollo y pruebas.

El proyecto continuará ampliándose durante los siguientes Sprints del curso.
