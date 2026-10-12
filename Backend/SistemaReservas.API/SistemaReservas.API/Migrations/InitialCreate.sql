IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
CREATE TABLE [Laboratorios] (
    [Id] int NOT NULL IDENTITY,
    [Nombre] nvarchar(100) NOT NULL,
    [Ubicacion] nvarchar(150) NOT NULL,
    [Capacidad] int NOT NULL,
    [Estado] nvarchar(30) NOT NULL,
    CONSTRAINT [PK_Laboratorios] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_Laboratorios_Capacidad] CHECK (Capacidad > 0),
    CONSTRAINT [CK_Laboratorios_Estado] CHECK (Estado IN ('Habilitado','Fuera de servicio'))
);

CREATE TABLE [Usuarios] (
    [Id] int NOT NULL IDENTITY,
    [Nombre] nvarchar(100) NOT NULL,
    [Username] nvarchar(50) NOT NULL,
    [PasswordHash] nvarchar(255) NOT NULL,
    [Rol] nvarchar(20) NOT NULL,
    [IntentosFallidos] int NOT NULL DEFAULT 0,
    [BloqueadoHasta] datetime2 NULL,
    CONSTRAINT [PK_Usuarios] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_Usuarios_Rol] CHECK (Rol IN ('Administrador','Usuario'))
);

CREATE TABLE [Disponibilidades] (
    [Id] int NOT NULL IDENTITY,
    [LaboratorioId] int NOT NULL,
    [Fecha] date NOT NULL,
    [HoraInicio] time NOT NULL,
    [HoraFin] time NOT NULL,
    [Estado] nvarchar(30) NOT NULL,
    CONSTRAINT [PK_Disponibilidades] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_Disponibilidades_Estado] CHECK (Estado IN ('Disponible','No disponible')),
    CONSTRAINT [FK_Disponibilidades_Laboratorios_LaboratorioId] FOREIGN KEY ([LaboratorioId]) REFERENCES [Laboratorios] ([Id]) ON DELETE NO ACTION
);

CREATE INDEX [IX_Disponibilidades_LaboratorioId] ON [Disponibilidades] ([LaboratorioId]);

CREATE UNIQUE INDEX [IX_Usuarios_Username] ON [Usuarios] ([Username]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261005163907_InitialCreate', N'10.0.12');

COMMIT;
GO

