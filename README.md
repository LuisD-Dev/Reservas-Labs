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

La base de datos se crea con las **migraciones de EF Core** (code-first); no hace falta ejecutar scripts a mano.

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

Las migraciones se aplican con la herramienta `dotnet-ef`, que debe ser de la versión 10 (la misma de EF Core en el proyecto). Para instalarla o actualizarla:

```powershell
dotnet tool install --global dotnet-ef
dotnet tool update --global dotnet-ef
```

Con SQL Server en ejecución, crear o actualizar la base de datos:

```powershell
cd ".\Backend\SistemaReservas.API\SistemaReservas.API"
dotnet ef database update
```

> **Importante:** si la base de datos se creó antes con `database/database.sql`, se debe recrear con las migraciones. Esto borra los datos locales:
>
> ```powershell
> dotnet ef database drop -f
> dotnet ef database update
> ```

Al ejecutar el backend en ambiente de desarrollo, `EfDatabaseSeeder` agrega los datos de prueba que falten (no borra nada):

- Los [usuarios de prueba](#usuarios-de-prueba).
- Los laboratorios de Software y de Redes.
- Si no hay horarios de hoy en adelante, horarios para mañana y pasado mañana en cada laboratorio: de 08:00 a 12:00 "Disponible" y de 13:00 a 15:00 "No disponible".

Para revisiones o despliegues manuales, el script SQL generado a partir de las migraciones está en:

```text
Backend/SistemaReservas.API/SistemaReservas.API/Migrations/InitialCreate.sql
```

Si se agrega una migración, el script se regenera desde la carpeta del backend con:

```powershell
dotnet ef migrations script -o Migrations/InitialCreate.sql
```

El archivo `database/database.sql` se conserva solo como referencia histórica; ya no se usa para crear la base de datos.

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

# HU4 - Registrar una reserva

El usuario autenticado registra una reserva indicando laboratorio, fecha y horario. Tareas: AB#62, AB#63, AB#64 y AB#66.

## Pruebas principales HU4

- Campos obligatorios: sin datos, datos completos y laboratorio inválido.
- El servicio guarda la reserva con el usuario autenticado y no guarda si hay errores de validación.
- Confirmación en pantalla con laboratorio, fecha y horario de la reserva.

# HU5 - Validar horario de reserva

La reserva solo se acepta si la fecha y el horario son válidos. Tareas: AB#65, AB#67, AB#68, AB#69, AB#70 y AB#71.

## Pruebas principales HU5

- Hora final: requerida, igual o menor que la inicial, y válida.
- Fecha: ayer se rechaza; hoy y mañana se aceptan.
- Horario de funcionamiento (07:00 a 21:00 [confirmar]): dentro del rango, justo de apertura a cierre, antes de abrir y después de cerrar.
- Duración máxima (2 horas [confirmar]): menor, exacta al máximo (válida) y un minuto de más (inválida).
- Frontend: los mismos casos en `validarReserva`, con el motivo de rechazo bajo el campo.

# HU6 - Evitar conflictos entre reservas

No se permite reservar un laboratorio que ya tiene una reserva activa en ese horario. Tareas: AB#74 y AB#75.

## Pruebas principales HU6

- Traslape: parcial por el inicio o por el final, contenida, contenedora e idéntica (hay conflicto); consecutivas y separadas (no hay conflicto).
- Servicio: rechaza la reserva traslapada con el mensaje de conflicto, acepta la consecutiva y no consulta conflictos si los datos son inválidos.

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

### Pruebas

```powershell
dotnet test Backend/SistemaReservas.API/SistemaReservas.API.slnx
cd ".\frontend"; npm.cmd test
```

Detalle de las pruebas y la cobertura: [Wiki – Pruebas de unidad – Taller V](https://dev.azure.com/mazunigaal/Laboratorio%20OBLD/_wiki/wikis/Laboratorio-OBLD.wiki/3/Pruebas-de-unidad-%E2%80%93-Taller-V)

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
