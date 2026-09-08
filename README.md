# Inventory Sucursales — práctica de Programación Cliente-Servidor

Aplicación académica en C# y Windows Forms para administrar inventario y ventas de sucursales mediante componentes COM/DCOM y SQL Server. Incluye acceso por usuario, catálogos, inventario, transferencias, ventas, reportes y consulta de bitácora.

La demostración documentada es **local**, usando `.` como servidor: acredita activación COM local y operaciones sobre SQL Server. La comunicación DCOM entre computadoras queda pendiente de comprobación.

Consulta [CONTEXTO.md](CONTEXTO.md) para el objetivo, arquitectura, explicación de módulos, relación de siete evidencias y límites del prototipo. Las imágenes y el informe Word se conservan localmente dentro de `evidencias/`, excluida de Git.

Esta solución contiene la base cliente-servidor del sistema de inventario para sucursales:

- `Inventory.Contracts`: contrato COM que ambos equipos deben tener.
- `Inventory.DcomServer`: componente COM alojable por DCOM; expone `Ping` y `Sum`.
- `Inventory.Client`: WinForms que invoca el componente local o remoto.

Todos los proyectos están configurados para **.NET Framework 4.8 y x64**. Cliente y servidor deben usar la misma arquitectura.

## 1. Compilar

Abre `InventarioSucursalesDCOM.sln` en Visual Studio como administrador y compila la solución en `Release | x64`.

## 2. Registrar el servidor (solo en la PC servidor)

Abre **Developer Command Prompt for Visual Studio** como administrador. Desde la carpeta de la solución ejecuta:

```powershell
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\RegAsm.exe .\src\Inventory.DcomServer\bin\x64\Release\net48\Inventory.DcomServer.dll /codebase /tlb
```

El componente quedará registrado como `InventorySucursales.InventoryGateway`. Para desregistrarlo:

```powershell
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\RegAsm.exe .\src\Inventory.DcomServer\bin\x64\Release\net48\Inventory.DcomServer.dll /unregister
```

## 3. Configurar DCOM (solo en la PC servidor)

1. Presiona `Win + R`, escribe `dcomcnfg` y acepta.
2. Ve a **Component Services → Computers → My Computer → DCOM Config**.
3. Abre **Inventory Sucursales DCOM Server** → **Properties**.
4. En **Security**, selecciona **Customize** en *Launch and Activation Permissions* y en *Access Permissions*; agrega el usuario que ejecutará el cliente y concede **Remote Launch**, **Remote Activation** y **Remote Access**.
5. En **Identity**, durante la práctica selecciona *The launching user*.

No modifiques la seguridad global de DCOM. Estas reglas quedan limitadas a esta aplicación.

## 4. Ejecutar el cliente

En cada PC cliente, compila o copia `Inventory.Client.exe` junto con `Inventory.Contracts.dll`. En el campo **Servidor DCOM** captura el nombre de equipo o IP de la PC servidor. Usa `.` para probar localmente en el servidor.

Primero pulsa **Probar Ping**. Si responde con el nombre del equipo, el componente atendió la llamada. Con `.` la activación es COM local; el nombre mostrado no demuestra por sí solo comunicación remota. Después pulsa **Probar Suma**.

> La red debe permitir RPC/DCOM y ambos equipos deben poder resolver el nombre/IP. Para una demostración escolar, realiza primero la prueba local y después la remota; configura el firewall institucional únicamente si está autorizado.

## Etapa 2: base de datos

El esquema de SQL Server, los índices, las reglas de integridad y los datos de prueba se encuentran en `database/01_create_inventory_sucursales.sql`.

Para crearla en la instancia local SQL Server Express:

```powershell
sqlcmd -S .\SQLEXPRESS -E -i .\database\01_create_inventory_sucursales.sql
```

Consulta [database/README.md](database/README.md) para los usuarios iniciales de demostración. El acceso ADO.NET está implementado en `InventoryGateway`.

## Etapa 3: acceso y roles

El método remoto `Authenticate(usuario, contraseña)` valida al usuario en SQL Server y devuelve su rol y sucursal. La cadena de conexión se resuelve **en la PC servidor**, por lo que el cliente nunca recibe credenciales de base de datos.

Por defecto, el servidor busca `InventorySucursales` en `.\SQLEXPRESS` con autenticación integrada de Windows. Si la instancia es diferente, crea en la PC servidor la variable de entorno de máquina `INVENTORY_DB_CONNECTION`; por ejemplo:

```text
Data Source=SERVIDOR\SQLEXPRESS;Initial Catalog=InventorySucursales;Integrated Security=True;
```

Después de recompilar el servidor, vuelve a registrarlo con `RegAsm` antes de probar el login DCOM. Para la demostración local, utiliza `admin` / `Admin123!`.
