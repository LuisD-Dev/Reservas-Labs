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

# Pruebas unitarias

El proyecto cuenta con pruebas unitarias automatizadas para el **backend** (xUnit) y para la lógica del **frontend** (`node --test`). Las pruebas no necesitan la base de datos, ni levantar la API o la interfaz: cada regla de negocio se prueba de forma aislada (**NFR2 – Mantenibilidad / Testeabilidad**).

## Organización del backend

Para poder probar la lógica sin SQL Server, el backend está separado en capas:

| Capa | Carpeta | Responsabilidad |
|---|---|---|
| Controladores | `Controllers/` | Reciben la petición HTTP y devuelven la respuesta |
| Servicios | `Services/` | Coordinan cada caso de uso (`AuthService`, `TokenService`, `LaboratorioService`, `DisponibilidadService`) |
| Reglas de negocio | `Reglas/` | Clases sin base de datos, HTTP ni reloj: `ReglasBloqueo` (HU1), `ReglasDisponibilidad` (HU3) y `ReglasRoles` |
| Acceso a datos | `Repositories/` | Consultas a SQL Server detrás de interfaces: `IUsuarioRepository`, `ILaboratorioRepository`, `IDisponibilidadRepository` |

- Los servicios reciben los repositorios por **interfaz**; en las pruebas se reemplazan por repositorios falsos en memoria (carpeta `Fakes/`).
- La hora actual se obtiene de **`TimeProvider`**; en las pruebas se usa `FakeTimeProvider` para simular el paso del tiempo (por ejemplo, que venza un bloqueo de 5 minutos) sin esperar en tiempo real.

## Pruebas del backend (xUnit)

Proyecto: `Backend/SistemaReservas.API/SistemaReservas.Tests` (incluido en la solución `SistemaReservas.API.slnx`).

**Ejecutar desde la raíz del repositorio:**

```powershell
dotnet test Backend/SistemaReservas.API/SistemaReservas.API.slnx
```

También se pueden ejecutar en Visual Studio desde **Prueba → Explorador de pruebas → Ejecutar todas las pruebas**.

**Medir la cobertura (coverlet):**

```powershell
dotnet test Backend/SistemaReservas.API/SistemaReservas.API.slnx --collect:"XPlat Code Coverage"
```

El reporte se genera en `SistemaReservas.Tests/TestResults/` (carpeta ignorada por git).

**Herramientas:** xUnit 2.9.3, Microsoft.NET.Test.Sdk, Microsoft.Extensions.TimeProvider.Testing (`FakeTimeProvider`) y coverlet.collector. Los dobles de prueba están escritos a mano, sin librerías de mocks.

| Clase de prueba | Pruebas | Qué valida | HU / NFR |
|---|---|---|---|
| `Reglas/ReglasBloqueoTests` | 16 | Límite de 5 intentos y 5 minutos; bloqueo vigente, vencido y justo al vencer; cuándo bloquear (4.º vs. 5.º intento) y cuándo reiniciar el contador; segundos restantes redondeados hacia arriba y nunca negativos | HU1 |
| `Reglas/ReglasDisponibilidadTests` | 26 | Campos obligatorios; fecha pasada y fecha de hoy; hora final igual o menor que la inicial; horario que cubre el rango y sus límites; laboratorio fuera de servicio; fecha sin horarios; ningún horario cubre el rango; filtra por fecha y ordena por hora. Usa los mismos casos y mensajes que las pruebas del frontend | HU3 |
| `Reglas/ReglasRolesTests` | 8 | Los roles coinciden con la base de datos (`Administrador`, `Usuario`); distingue mayúsculas y rechaza vacío, nulo y roles desconocidos | NFR1 |
| `Services/AuthServiceTests` | 12 | Datos vacíos sin consultar la BD; usuario inexistente y contraseña incorrecta con el mismo resultado; login correcto; intentos 1 a 4; bloqueo en el 5.º; sigue bloqueado a los 4:59 aun con la contraseña correcta; a los 5:00 vuelve a entrar; reinicio del contador tras un login correcto; contraseña guardada como hash | HU1 |
| `Services/TokenServiceTests` | 9 | Claims del usuario (id, nombre, rol, `jti`); issuer y audience; expiración según `ExpiraHoras` (8 h por defecto); error si falta la clave; firma válida con la misma clave y rechazada con otra; `jti` distinto en cada token | NFR1 |
| `Services/LaboratorioServiceTests` | 1 | Devuelve los laboratorios que entrega el repositorio | HU2 |
| `Services/DisponibilidadServiceTests` | 1 | Devuelve los horarios del laboratorio solicitado | HU3 |

**Resultado (Sprint 2):** 73 pruebas, 73 correctas. Cobertura de líneas: **100 % en `Services/`** y **100 % en `Reglas/`** (meta del plan de calidad: ≥ 80 % en la capa de servicios).

## Pruebas del frontend (`node --test`)

**Ejecutar desde la carpeta `frontend`:**

```powershell
npm.cmd test
```

| Archivo | Qué valida | HU / NFR |
|---|---|---|
| `src/utils/disponibilidad.test.js` | Validación del formulario de consulta, normalización de fechas y horas de la API, formato de fechas sin cambio de día por zona horaria, evaluación de disponibilidad y laboratorio fuera de servicio | HU3 |
| `src/services/api.test.js` | Nombre y rol se leen del token; el token JWT se envía en cada petición; ante un 401 o una sesión vencida se cierra la sesión | NFR1 |

## Criterio

Todas las pruebas deben pasar antes de integrar cambios a `develop`.

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
