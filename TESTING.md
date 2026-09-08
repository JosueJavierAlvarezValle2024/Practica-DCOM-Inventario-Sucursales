# Pruebas finales

Estado de entrega: se documentaron siete evidencias locales (véase `CONTEXTO.md`). Esta guía contiene casos propuestos; no es una certificación de que todos se ejecutaron. Las pruebas entre dos equipos siguen pendientes. Con `.` se activa COM local y el componente puede ejecutarse dentro del proceso cliente.

## Prueba local

1. Compila `Release | x64`.
2. Registra `Inventory.DcomServer.dll` con el comando de `README.md` ejecutado como administrador.
3. Abre `Inventory.Client.exe`, escribe `.` como servidor e inicia sesión como `admin` / `Admin123!`.
4. Comprueba `Ping`, catálogos, inventario, transferencias, reportes y bitácora.
5. Inicia sesión como `cajero.nte` / `Cajero123!`; valida que Ventas esté habilitado y Catálogos no.

## Prueba entre dos equipos

| Equipo | Función |
| --- | --- |
| PC servidor | SQL Server Express, `Inventory.Contracts.dll`, `Inventory.DcomServer.dll`, registro RegAsm y permisos DCOM. |
| PC cliente | `Inventory.Client.exe` e `Inventory.Contracts.dll`; no instala SQL Server. |

1. En la PC servidor, ejecuta `dcomcnfg` y otorga Remote Launch, Remote Activation y Remote Access al usuario cliente para **Inventory Sucursales DCOM Server**.
2. En el cliente, escribe el nombre de red o IP de la PC servidor en el campo **Servidor DCOM**.
3. Pulsa **Probar Ping**: debe responder con el nombre de la PC servidor.
4. Inicia sesión y realiza una entrada/salida o venta; confirma en la PC servidor que cambió el inventario y aparece la bitácora.

## Casos que deben rechazarse

- Login con contraseña incorrecta.
- Salida o venta con existencia insuficiente.
- Transferencia hacia la misma sucursal.
- Producto, categoría o sucursal sin los campos obligatorios.

No modifiques la seguridad global de DCOM. Configura únicamente el componente de esta práctica.
