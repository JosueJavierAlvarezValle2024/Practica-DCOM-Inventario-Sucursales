# Base de datos — Inventory Sucursales

Ejecuta `01_create_inventory_sucursales.sql` en SQL Server Management Studio o con `sqlcmd` desde una cuenta con permiso para crear bases de datos:

```powershell
sqlcmd -S .\SQLEXPRESS -E -i .\database\01_create_inventory_sucursales.sql
```

El script se puede volver a ejecutar: crea la base y las tablas solo si no existen, e inserta los datos iniciales una única vez.

Usuarios iniciales (solo para desarrollo):

| Usuario | Contraseña | Rol |
| --- | --- | --- |
| `admin` | `Admin123!` | Administrador |
| `encargado.mat` | `Encargado123!` | Encargado de Matriz |
| `cajero.nte` | `Cajero123!` | Cajero de Sucursal Norte |

Las contraseñas se almacenan como SHA-256 únicamente para la práctica. Antes de un uso real se reemplazarán por hashes con sal, por ejemplo BCrypt o PBKDF2.
