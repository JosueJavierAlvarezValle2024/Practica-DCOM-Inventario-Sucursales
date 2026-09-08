# Contexto de la práctica: Inventory Sucursales

## Propósito académico

Proyecto de la unidad 4 de Programación Cliente-Servidor. La actividad plantea desarrollar aplicaciones bajo el modelo de componentes distribuidos de Microsoft (DCOM) mediante un lenguaje visual. Se eligió C# con Windows Forms y .NET Framework 4.8 para construir un sistema de inventario y ventas de sucursales.

El caso de uso consiste en administrar catálogos comunes, consultar existencias por sucursal, registrar entradas y salidas, solicitar transferencias y confirmar ventas. Una base central permite conservar las operaciones y consultar reportes.

## Arquitectura implementada

- `Inventory.Client`: interfaz WinForms, captura de datos y presentación de resultados.
- `Inventory.Contracts`: interfaces y objetos compartidos para invocar al componente.
- `Inventory.DcomServer`: clase `InventoryGateway`, lógica de negocio y acceso ADO.NET a SQL Server mediante consultas parametrizadas.
- `InventorySucursales`: base SQL Server con 12 tablas, claves, restricciones e inventario por sucursal/producto.

El flujo lógico es: cliente WinForms → componente COM → ADO.NET → SQL Server. La clase se registra mediante RegAsm y dispone de CLSID, ProgID y AppID para su configuración COM/DCOM.

## Alcance comprobado

La demostración se realizó en una sola computadora usando `.` en el campo servidor. En ese caso, el cliente solicita activación COM local. Con el registro `InprocServer32`, el componente puede cargarse dentro del proceso cliente; la separación es lógica y por proyectos, no una prueba de procesos o computadoras independientes. Un nombre/IP utiliza la ruta de activación remota del cliente, todavía pendiente de validación.

Las evidencias locales muestran Ping, autenticación administrativa, listado de categorías, existencias y alerta de stock mínimo, interfaz de ventas, un reporte con venta confirmada y una entrada registrada en bitácora. No se afirma que las capturas acrediten una conexión DCOM de red.

## Relación de las siete evidencias

| Archivo local | Contexto y resultado visible |
| --- | --- |
| Evidencia1.png | Ping responde desde el equipo local; también se observan los accesos a los módulos. |
| Evidencia2.png | Login correcto de Administrador General, con rol Administrador. |
| Evidencia3.png | Catálogo CATEGORY con tres registros; muestra campos y botones de mantenimiento. |
| Evidencia4.png | Sucursal 2: agua 35, arroz 11 y detergente 5; el detergente está debajo del mínimo 8. |
| Evidencia5.png | Ventana de venta de sucursal 2 con captura de producto/cantidad y carrito. No muestra una confirmación de venta. |
| Evidencia6.png | Reporte SALES con una venta Confirmada de Sucursal Norte por 37.70. Quantity representa una venta en esta consulta, no el total de artículos. |
| Evidencia7.png | Bitácora con usuario admin, Sucursal Matriz, acción Entrada y cantidad 5. |

El documento `evidencias/Informe_Inventory_Sucursales.docx` incorpora las siete imágenes y su explicación. Esa carpeta completa es material local de entrega y está excluida de Git. Este contexto y el código sí se versionan.

## Funcionamiento de los módulos

Catálogos permite consultar, crear, editar y desactivar categorías, sucursales y productos. «Nuevo» limpia los campos y «Guardar» persiste los datos. Inventario consulta existencias y registra entradas, salidas o ajustes de productos ya presentes en la sucursal; la actualización y el movimiento se ejecutan en una transacción, junto con su registro en Bitacora.

Transferencias crea una solicitud de un producto y permite autorizarla o rechazarla. Autorizar aplica de inmediato el descuento en origen y el incremento en destino, dejando el estado Recibida. Ventas recibe productos y cantidades; toma precios de la base, calcula subtotal, IVA fijo del 16 % para la práctica y total, y registra venta, detalle y movimientos dentro de una transacción.

Reportes ofrece SALES, LOWSTOCK, MOVEMENTS y TRANSFERS. Bitácora consulta los registros de auditoría por fechas. Las siete capturas no contienen una demostración de transferencia ni todos los reportes.

## Estado y trabajo futuro

Es un prototipo académico. El login consulta usuarios y activa botones según su rol, pero los métodos de negocio aún reciben identificadores del cliente sin una sesión autenticada que autorice cada operación en el servidor. El hash de contraseña es SHA-256 sin sal y las credenciales iniciales son datos de demostración. Falta reforzar esas medidas antes de un despliegue real.

También queda pendiente comprobar el transporte DCOM entre equipos, resolver la advertencia de exportación COM de `BranchId` nullable y ampliar la auditoría a catálogos, ventas y transferencias. Las consultas LOWSTOCK, MOVEMENTS y TRANSFERS usan un cero SQL entero que debe convertirse a decimal para el lector `GetDecimal` cuando existan filas; la captura de reporte aportada valida únicamente SALES. El contexto documenta el estado actual sin presentar esas mejoras como terminadas.

## Entrega

El repositorio contiene fuente C#, solución, scripts SQL, instrucciones y este contexto. Las capturas y el informe de Word se conservan en `evidencias/` para la entrega escolar. No se incluyen compilados ni archivos de datos SQL Server. Véase `README.md` para ejecutar y `TESTING.md` para el alcance de pruebas.
