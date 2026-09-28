CREATE DATABASE LaboratorioOBLD;
GO

USE LaboratorioOBLD;
GO

CREATE TABLE Usuarios
(
    Id INT IDENTITY(1,1) PRIMARY KEY,

    Nombre NVARCHAR(100) NOT NULL,

    Username NVARCHAR(50) NOT NULL UNIQUE,

    PasswordHash NVARCHAR(255) NOT NULL,

    Rol NVARCHAR(20) NOT NULL,

    IntentosFallidos INT NOT NULL DEFAULT 0,

    BloqueadoHasta DATETIME2 NULL,

    CONSTRAINT CK_Usuarios_Rol
        CHECK (Rol IN ('Administrador', 'Usuario'))
);
GO
CREATE TABLE Laboratorios
(
    Id INT IDENTITY(1,1) PRIMARY KEY,

    Nombre NVARCHAR(100) NOT NULL,

    Ubicacion NVARCHAR(150) NOT NULL,

    Capacidad INT NOT NULL,

    Estado NVARCHAR(30) NOT NULL,

    CONSTRAINT CK_Laboratorios_Capacidad
        CHECK (Capacidad > 0),

    CONSTRAINT CK_Laboratorios_Estado
        CHECK (Estado IN ('Habilitado', 'Fuera de servicio'))
);
GO