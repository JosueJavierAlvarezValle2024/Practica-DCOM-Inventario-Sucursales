/*
    Inventory Sucursales - esquema inicial SQL Server
    Ejecutar con una cuenta que pueda crear bases de datos.
*/
USE master;
GO

IF DB_ID(N'InventorySucursales') IS NULL
BEGIN
    CREATE DATABASE InventorySucursales;
END;
GO

USE InventorySucursales;
GO

-- Requerido por la columna calculada persistida de DetalleVenta.
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.Roles', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Roles
    (
        RolId       INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Roles PRIMARY KEY,
        Nombre      NVARCHAR(50) NOT NULL,
        Descripcion NVARCHAR(200) NOT NULL,
        Activo      BIT NOT NULL CONSTRAINT DF_Roles_Activo DEFAULT (1),
        CreadoEn    DATETIME2(0) NOT NULL CONSTRAINT DF_Roles_CreadoEn DEFAULT (SYSDATETIME()),
        CONSTRAINT UQ_Roles_Nombre UNIQUE (Nombre)
    );
END;
GO

IF OBJECT_ID(N'dbo.Sucursales', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Sucursales
    (
        SucursalId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Sucursales PRIMARY KEY,
        Clave      NVARCHAR(10) NOT NULL,
        Nombre     NVARCHAR(100) NOT NULL,
        Direccion  NVARCHAR(250) NOT NULL,
        Telefono   NVARCHAR(20) NULL,
        Activa     BIT NOT NULL CONSTRAINT DF_Sucursales_Activa DEFAULT (1),
        CreadoEn   DATETIME2(0) NOT NULL CONSTRAINT DF_Sucursales_CreadoEn DEFAULT (SYSDATETIME()),
        CONSTRAINT UQ_Sucursales_Clave UNIQUE (Clave)
    );
END;
GO

IF OBJECT_ID(N'dbo.Usuarios', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Usuarios
    (
        UsuarioId       INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Usuarios PRIMARY KEY,
        RolId           INT NOT NULL,
        SucursalId      INT NULL,
        NombreCompleto  NVARCHAR(150) NOT NULL,
        NombreUsuario   NVARCHAR(50) NOT NULL,
        PasswordHash    VARCHAR(64) NOT NULL,
        Activo          BIT NOT NULL CONSTRAINT DF_Usuarios_Activo DEFAULT (1),
        UltimoAccesoEn  DATETIME2(0) NULL,
        CreadoEn        DATETIME2(0) NOT NULL CONSTRAINT DF_Usuarios_CreadoEn DEFAULT (SYSDATETIME()),
        CONSTRAINT UQ_Usuarios_NombreUsuario UNIQUE (NombreUsuario),
        CONSTRAINT FK_Usuarios_Roles FOREIGN KEY (RolId) REFERENCES dbo.Roles(RolId),
        CONSTRAINT FK_Usuarios_Sucursales FOREIGN KEY (SucursalId) REFERENCES dbo.Sucursales(SucursalId)
    );
END;
GO

IF OBJECT_ID(N'dbo.Categorias', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Categorias
    (
        CategoriaId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Categorias PRIMARY KEY,
        Nombre      NVARCHAR(100) NOT NULL,
        Descripcion NVARCHAR(250) NULL,
        Activa      BIT NOT NULL CONSTRAINT DF_Categorias_Activa DEFAULT (1),
        CreadoEn    DATETIME2(0) NOT NULL CONSTRAINT DF_Categorias_CreadoEn DEFAULT (SYSDATETIME()),
        CONSTRAINT UQ_Categorias_Nombre UNIQUE (Nombre)
    );
END;
GO

IF OBJECT_ID(N'dbo.Productos', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Productos
    (
        ProductoId       INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Productos PRIMARY KEY,
        CategoriaId      INT NOT NULL,
        Codigo           NVARCHAR(30) NOT NULL,
        Nombre           NVARCHAR(150) NOT NULL,
        Descripcion      NVARCHAR(300) NULL,
        Precio           DECIMAL(12,2) NOT NULL,
        ExistenciaMinima INT NOT NULL CONSTRAINT DF_Productos_ExistenciaMinima DEFAULT (0),
        Activo           BIT NOT NULL CONSTRAINT DF_Productos_Activo DEFAULT (1),
        CreadoEn         DATETIME2(0) NOT NULL CONSTRAINT DF_Productos_CreadoEn DEFAULT (SYSDATETIME()),
        CONSTRAINT UQ_Productos_Codigo UNIQUE (Codigo),
        CONSTRAINT CK_Productos_Precio CHECK (Precio >= 0),
        CONSTRAINT CK_Productos_ExistenciaMinima CHECK (ExistenciaMinima >= 0),
        CONSTRAINT FK_Productos_Categorias FOREIGN KEY (CategoriaId) REFERENCES dbo.Categorias(CategoriaId)
    );
END;
GO

IF OBJECT_ID(N'dbo.Inventario', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Inventario
    (
        InventarioId     INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Inventario PRIMARY KEY,
        SucursalId       INT NOT NULL,
        ProductoId       INT NOT NULL,
        Existencia       INT NOT NULL CONSTRAINT DF_Inventario_Existencia DEFAULT (0),
        ActualizadoEn    DATETIME2(0) NOT NULL CONSTRAINT DF_Inventario_ActualizadoEn DEFAULT (SYSDATETIME()),
        RowVersion       ROWVERSION NOT NULL,
        CONSTRAINT UQ_Inventario_Sucursal_Producto UNIQUE (SucursalId, ProductoId),
        CONSTRAINT CK_Inventario_Existencia CHECK (Existencia >= 0),
        CONSTRAINT FK_Inventario_Sucursales FOREIGN KEY (SucursalId) REFERENCES dbo.Sucursales(SucursalId),
        CONSTRAINT FK_Inventario_Productos FOREIGN KEY (ProductoId) REFERENCES dbo.Productos(ProductoId)
    );
END;
GO

IF OBJECT_ID(N'dbo.Ventas', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Ventas
    (
        VentaId        INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Ventas PRIMARY KEY,
        SucursalId     INT NOT NULL,
        UsuarioId      INT NOT NULL,
        Fecha          DATETIME2(0) NOT NULL CONSTRAINT DF_Ventas_Fecha DEFAULT (SYSDATETIME()),
        Subtotal       DECIMAL(12,2) NOT NULL,
        Iva            DECIMAL(12,2) NOT NULL,
        Total          DECIMAL(12,2) NOT NULL,
        Estado         NVARCHAR(20) NOT NULL CONSTRAINT DF_Ventas_Estado DEFAULT (N'Confirmada'),
        CONSTRAINT CK_Ventas_Importes CHECK (Subtotal >= 0 AND Iva >= 0 AND Total >= 0),
        CONSTRAINT CK_Ventas_Estado CHECK (Estado IN (N'Confirmada', N'Cancelada')),
        CONSTRAINT FK_Ventas_Sucursales FOREIGN KEY (SucursalId) REFERENCES dbo.Sucursales(SucursalId),
        CONSTRAINT FK_Ventas_Usuarios FOREIGN KEY (UsuarioId) REFERENCES dbo.Usuarios(UsuarioId)
    );
END;
GO

IF OBJECT_ID(N'dbo.DetalleVenta', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DetalleVenta
    (
        DetalleVentaId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_DetalleVenta PRIMARY KEY,
        VentaId        INT NOT NULL,
        ProductoId     INT NOT NULL,
        Cantidad       INT NOT NULL,
        PrecioUnitario DECIMAL(12,2) NOT NULL,
        Importe        AS CONVERT(DECIMAL(12,2), Cantidad * PrecioUnitario) PERSISTED,
        CONSTRAINT CK_DetalleVenta_Cantidad CHECK (Cantidad > 0),
        CONSTRAINT CK_DetalleVenta_Precio CHECK (PrecioUnitario >= 0),
        CONSTRAINT FK_DetalleVenta_Ventas FOREIGN KEY (VentaId) REFERENCES dbo.Ventas(VentaId),
        CONSTRAINT FK_DetalleVenta_Productos FOREIGN KEY (ProductoId) REFERENCES dbo.Productos(ProductoId)
    );
END;
GO

IF OBJECT_ID(N'dbo.Transferencias', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Transferencias
    (
        TransferenciaId    INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Transferencias PRIMARY KEY,
        SucursalOrigenId   INT NOT NULL,
        SucursalDestinoId  INT NOT NULL,
        UsuarioSolicitaId  INT NOT NULL,
        UsuarioAutorizaId  INT NULL,
        FechaSolicitud     DATETIME2(0) NOT NULL CONSTRAINT DF_Transferencias_FechaSolicitud DEFAULT (SYSDATETIME()),
        FechaResolucion    DATETIME2(0) NULL,
        Estado             NVARCHAR(20) NOT NULL CONSTRAINT DF_Transferencias_Estado DEFAULT (N'Pendiente'),
        Observaciones      NVARCHAR(300) NULL,
        CONSTRAINT CK_Transferencias_Sucursales CHECK (SucursalOrigenId <> SucursalDestinoId),
        CONSTRAINT CK_Transferencias_Estado CHECK (Estado IN (N'Pendiente', N'Autorizada', N'Rechazada', N'Enviada', N'Recibida', N'Cancelada')),
        CONSTRAINT FK_Transferencias_Origen FOREIGN KEY (SucursalOrigenId) REFERENCES dbo.Sucursales(SucursalId),
        CONSTRAINT FK_Transferencias_Destino FOREIGN KEY (SucursalDestinoId) REFERENCES dbo.Sucursales(SucursalId),
        CONSTRAINT FK_Transferencias_Solicita FOREIGN KEY (UsuarioSolicitaId) REFERENCES dbo.Usuarios(UsuarioId),
        CONSTRAINT FK_Transferencias_Autoriza FOREIGN KEY (UsuarioAutorizaId) REFERENCES dbo.Usuarios(UsuarioId)
    );
END;
GO

IF OBJECT_ID(N'dbo.DetalleTransferencia', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DetalleTransferencia
    (
        DetalleTransferenciaId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_DetalleTransferencia PRIMARY KEY,
        TransferenciaId        INT NOT NULL,
        ProductoId             INT NOT NULL,
        Cantidad               INT NOT NULL,
        CONSTRAINT UQ_DetalleTransferencia UNIQUE (TransferenciaId, ProductoId),
        CONSTRAINT CK_DetalleTransferencia_Cantidad CHECK (Cantidad > 0),
        CONSTRAINT FK_DetalleTransferencia_Transferencias FOREIGN KEY (TransferenciaId) REFERENCES dbo.Transferencias(TransferenciaId),
        CONSTRAINT FK_DetalleTransferencia_Productos FOREIGN KEY (ProductoId) REFERENCES dbo.Productos(ProductoId)
    );
END;
GO

IF OBJECT_ID(N'dbo.MovimientosInventario', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.MovimientosInventario
    (
        MovimientoInventarioId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_MovimientosInventario PRIMARY KEY,
        SucursalId             INT NOT NULL,
        ProductoId             INT NOT NULL,
        UsuarioId              INT NOT NULL,
        Tipo                   NVARCHAR(30) NOT NULL,
        Cantidad               INT NOT NULL,
        Referencia             NVARCHAR(80) NULL,
        Observaciones          NVARCHAR(300) NULL,
        Fecha                  DATETIME2(0) NOT NULL CONSTRAINT DF_MovimientosInventario_Fecha DEFAULT (SYSDATETIME()),
        CONSTRAINT CK_MovimientosInventario_Tipo CHECK (Tipo IN (N'Entrada', N'Salida', N'AjusteEntrada', N'AjusteSalida', N'Venta', N'TransferenciaSalida', N'TransferenciaEntrada')),
        CONSTRAINT CK_MovimientosInventario_Cantidad CHECK (Cantidad > 0),
        CONSTRAINT FK_MovimientosInventario_Sucursales FOREIGN KEY (SucursalId) REFERENCES dbo.Sucursales(SucursalId),
        CONSTRAINT FK_MovimientosInventario_Productos FOREIGN KEY (ProductoId) REFERENCES dbo.Productos(ProductoId),
        CONSTRAINT FK_MovimientosInventario_Usuarios FOREIGN KEY (UsuarioId) REFERENCES dbo.Usuarios(UsuarioId)
    );
END;
GO

IF OBJECT_ID(N'dbo.Bitacora', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Bitacora
    (
        BitacoraId  BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Bitacora PRIMARY KEY,
        UsuarioId   INT NULL,
        SucursalId  INT NULL,
        Accion      NVARCHAR(100) NOT NULL,
        Entidad     NVARCHAR(80) NOT NULL,
        EntidadId   INT NULL,
        Detalle     NVARCHAR(500) NULL,
        Fecha       DATETIME2(0) NOT NULL CONSTRAINT DF_Bitacora_Fecha DEFAULT (SYSDATETIME()),
        CONSTRAINT FK_Bitacora_Usuarios FOREIGN KEY (UsuarioId) REFERENCES dbo.Usuarios(UsuarioId),
        CONSTRAINT FK_Bitacora_Sucursales FOREIGN KEY (SucursalId) REFERENCES dbo.Sucursales(SucursalId)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Usuarios') AND name = N'IX_Usuarios_SucursalId')
    CREATE INDEX IX_Usuarios_SucursalId ON dbo.Usuarios(SucursalId);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Productos') AND name = N'IX_Productos_CategoriaId')
    CREATE INDEX IX_Productos_CategoriaId ON dbo.Productos(CategoriaId);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Inventario') AND name = N'IX_Inventario_ProductoId')
    CREATE INDEX IX_Inventario_ProductoId ON dbo.Inventario(ProductoId);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Ventas') AND name = N'IX_Ventas_Sucursal_Fecha')
    CREATE INDEX IX_Ventas_Sucursal_Fecha ON dbo.Ventas(SucursalId, Fecha DESC);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.MovimientosInventario') AND name = N'IX_MovimientosInventario_Sucursal_Fecha')
    CREATE INDEX IX_MovimientosInventario_Sucursal_Fecha ON dbo.MovimientosInventario(SucursalId, Fecha DESC);
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE Nombre = N'Administrador')
BEGIN
    INSERT INTO dbo.Roles (Nombre, Descripcion) VALUES
    (N'Administrador', N'Administra catálogos, usuarios y reportes globales.'),
    (N'Encargado', N'Administra inventario y transferencias de su sucursal.'),
    (N'Cajero', N'Registra ventas de su sucursal.');
END;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Sucursales WHERE Clave = N'MAT')
BEGIN
    INSERT INTO dbo.Sucursales (Clave, Nombre, Direccion, Telefono) VALUES
    (N'MAT', N'Sucursal Matriz', N'Av. Principal 100, Monclova, Coahuila', N'866-000-0001'),
    (N'NTE', N'Sucursal Norte', N'Blvd. Norte 250, Monclova, Coahuila', N'866-000-0002');
END;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Categorias WHERE Nombre = N'Abarrotes')
BEGIN
    INSERT INTO dbo.Categorias (Nombre, Descripcion) VALUES
    (N'Abarrotes', N'Productos de consumo general.'),
    (N'Bebidas', N'Bebidas no alcohólicas.'),
    (N'Limpieza', N'Productos para limpieza del hogar.');
END;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Productos WHERE Codigo = N'750100000001')
BEGIN
    INSERT INTO dbo.Productos (CategoriaId, Codigo, Nombre, Descripcion, Precio, ExistenciaMinima) VALUES
    ((SELECT CategoriaId FROM dbo.Categorias WHERE Nombre = N'Abarrotes'), N'750100000001', N'Arroz 1 kg', N'Arroz blanco bolsa de 1 kg.', 32.50, 10),
    ((SELECT CategoriaId FROM dbo.Categorias WHERE Nombre = N'Bebidas'), N'750100000002', N'Agua purificada 1 L', N'Botella de agua purificada.', 15.00, 20),
    ((SELECT CategoriaId FROM dbo.Categorias WHERE Nombre = N'Limpieza'), N'750100000003', N'Detergente 900 ml', N'Detergente líquido para ropa.', 48.90, 8);
END;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Usuarios WHERE NombreUsuario = N'admin')
BEGIN
    INSERT INTO dbo.Usuarios (RolId, SucursalId, NombreCompleto, NombreUsuario, PasswordHash) VALUES
    ((SELECT RolId FROM dbo.Roles WHERE Nombre = N'Administrador'), NULL, N'Administrador General', N'admin', CONVERT(VARCHAR(64), HASHBYTES('SHA2_256', N'Admin123!'), 2)),
    ((SELECT RolId FROM dbo.Roles WHERE Nombre = N'Encargado'), (SELECT SucursalId FROM dbo.Sucursales WHERE Clave = N'MAT'), N'Elena Martínez', N'encargado.mat', CONVERT(VARCHAR(64), HASHBYTES('SHA2_256', N'Encargado123!'), 2)),
    ((SELECT RolId FROM dbo.Roles WHERE Nombre = N'Cajero'), (SELECT SucursalId FROM dbo.Sucursales WHERE Clave = N'NTE'), N'Carlos Ruiz', N'cajero.nte', CONVERT(VARCHAR(64), HASHBYTES('SHA2_256', N'Cajero123!'), 2));
END;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Inventario)
BEGIN
    INSERT INTO dbo.Inventario (SucursalId, ProductoId, Existencia) VALUES
    ((SELECT SucursalId FROM dbo.Sucursales WHERE Clave = N'MAT'), (SELECT ProductoId FROM dbo.Productos WHERE Codigo = N'750100000001'), 45),
    ((SELECT SucursalId FROM dbo.Sucursales WHERE Clave = N'MAT'), (SELECT ProductoId FROM dbo.Productos WHERE Codigo = N'750100000002'), 70),
    ((SELECT SucursalId FROM dbo.Sucursales WHERE Clave = N'MAT'), (SELECT ProductoId FROM dbo.Productos WHERE Codigo = N'750100000003'), 18),
    ((SELECT SucursalId FROM dbo.Sucursales WHERE Clave = N'NTE'), (SELECT ProductoId FROM dbo.Productos WHERE Codigo = N'750100000001'), 12),
    ((SELECT SucursalId FROM dbo.Sucursales WHERE Clave = N'NTE'), (SELECT ProductoId FROM dbo.Productos WHERE Codigo = N'750100000002'), 35),
    ((SELECT SucursalId FROM dbo.Sucursales WHERE Clave = N'NTE'), (SELECT ProductoId FROM dbo.Productos WHERE Codigo = N'750100000003'), 5);
END;
GO

SELECT N'Base de datos preparada.' AS Resultado,
       (SELECT COUNT(*) FROM dbo.Usuarios) AS Usuarios,
       (SELECT COUNT(*) FROM dbo.Productos) AS Productos,
       (SELECT COUNT(*) FROM dbo.Inventario) AS RegistrosInventario;
GO
