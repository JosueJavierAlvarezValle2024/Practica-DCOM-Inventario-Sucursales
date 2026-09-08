using System;
using System.Data;
using System.Data.SqlClient;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using Inventory.Contracts;
using Microsoft.Win32;

namespace Inventory.DcomServer
{
    [ComVisible(true)]
    [Guid("D9CB4E2B-1E41-47B0-BB6E-0D4F4A8960E5")]
    [ProgId("InventorySucursales.InventoryGateway")]
    [ClassInterface(ClassInterfaceType.None)]
    [ComDefaultInterface(typeof(IInventoryGateway))]
    public class InventoryGateway : IInventoryGateway
    {
        internal const string AppId = "{61E19C80-EF12-47CF-A2E4-659BCF7D68A5}";

        public string Ping(string clientName)
        {
            string safeClientName = string.IsNullOrWhiteSpace(clientName) ? "Cliente sin nombre" : clientName.Trim();
            return $"Servidor DCOM disponible en {Environment.MachineName}. Solicitud recibida de: {safeClientName}.";
        }

        public decimal Sum(decimal firstValue, decimal secondValue)
        {
            return firstValue + secondValue;
        }

        public AuthenticationResult Authenticate(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                return new AuthenticationResult { Message = "Captura usuario y contraseña." };
            }

            const string query = @"
SELECT u.UsuarioId, u.NombreCompleto, r.Nombre AS Rol, s.SucursalId, s.Nombre AS Sucursal
FROM dbo.Usuarios AS u
INNER JOIN dbo.Roles AS r ON r.RolId = u.RolId
LEFT JOIN dbo.Sucursales AS s ON s.SucursalId = u.SucursalId
WHERE u.NombreUsuario = @Username
  AND u.PasswordHash = @PasswordHash
  AND u.Activo = 1
  AND r.Activo = 1
  AND (s.SucursalId IS NULL OR s.Activa = 1);";

            try
            {
                using (SqlConnection connection = new SqlConnection(GetConnectionString()))
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.Add("@Username", SqlDbType.NVarChar, 50).Value = username.Trim();
                    command.Parameters.Add("@PasswordHash", SqlDbType.VarChar, 64).Value = ComputeSha256(password);
                    connection.Open();

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (!reader.Read())
                        {
                            return new AuthenticationResult { Message = "Usuario o contraseña incorrectos." };
                        }

                        AuthenticationResult result = new AuthenticationResult
                        {
                            IsAuthenticated = true,
                            Message = "Inicio de sesión correcto.",
                            UserId = reader.GetInt32(reader.GetOrdinal("UsuarioId")),
                            FullName = reader.GetString(reader.GetOrdinal("NombreCompleto")),
                            Role = reader.GetString(reader.GetOrdinal("Rol")),
                            BranchId = reader.IsDBNull(reader.GetOrdinal("SucursalId")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("SucursalId")),
                            BranchName = reader.IsDBNull(reader.GetOrdinal("Sucursal")) ? "Todas las sucursales" : reader.GetString(reader.GetOrdinal("Sucursal"))
                        };

                        reader.Close();
                        using (SqlCommand updateCommand = new SqlCommand("UPDATE dbo.Usuarios SET UltimoAccesoEn = SYSDATETIME() WHERE UsuarioId = @UserId", connection))
                        {
                            updateCommand.Parameters.Add("@UserId", SqlDbType.Int).Value = result.UserId;
                            updateCommand.ExecuteNonQuery();
                        }

                        return result;
                    }
                }
            }
            catch (SqlException)
            {
                return new AuthenticationResult { Message = "No fue posible validar el usuario en el servidor de datos." };
            }
        }

        private static string GetConnectionString()
        {
            string configuredConnection = Environment.GetEnvironmentVariable("INVENTORY_DB_CONNECTION", EnvironmentVariableTarget.Machine);
            if (!string.IsNullOrWhiteSpace(configuredConnection))
            {
                return configuredConnection;
            }

            return "Data Source=.\\SQLEXPRESS;Initial Catalog=InventorySucursales;Integrated Security=True;";
        }

        private static string ComputeSha256(string value)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                // HASHBYTES sobre NVARCHAR en SQL Server usa UTF-16 little-endian.
                byte[] hash = sha256.ComputeHash(Encoding.Unicode.GetBytes(value));
                StringBuilder builder = new StringBuilder(hash.Length * 2);
                foreach (byte item in hash)
                {
                    builder.Append(item.ToString("X2"));
                }

                return builder.ToString();
            }
        }

        public CatalogItem[] GetCatalog(string catalogType)
        {
            string query;
            switch (NormalizeCatalogType(catalogType))
            {
                case "CATEGORY":
                    query = "SELECT CategoriaId AS Id, CAST('' AS nvarchar(30)) AS Code, Nombre, ISNULL(Descripcion, '') AS Description, CAST('' AS nvarchar(250)) AS Address, CAST('' AS nvarchar(20)) AS Phone, 0 AS CategoryId, CAST(0 AS decimal(12,2)) AS Price, 0 AS MinimumStock, Activa AS Active FROM dbo.Categorias ORDER BY Nombre";
                    break;
                case "BRANCH":
                    query = "SELECT SucursalId AS Id, Clave AS Code, Nombre, CAST('' AS nvarchar(300)) AS Description, Direccion AS Address, ISNULL(Telefono, '') AS Phone, 0 AS CategoryId, CAST(0 AS decimal(12,2)) AS Price, 0 AS MinimumStock, Activa AS Active FROM dbo.Sucursales ORDER BY Nombre";
                    break;
                case "PRODUCT":
                    query = "SELECT ProductoId AS Id, Codigo AS Code, Nombre, ISNULL(Descripcion, '') AS Description, CAST('' AS nvarchar(250)) AS Address, CAST('' AS nvarchar(20)) AS Phone, CategoriaId AS CategoryId, Precio, ExistenciaMinima AS MinimumStock, Activo AS Active FROM dbo.Productos ORDER BY Nombre";
                    break;
                default:
                    throw new ArgumentException("Catálogo no válido.");
            }

            var items = new System.Collections.Generic.List<CatalogItem>();
            using (SqlConnection connection = new SqlConnection(GetConnectionString()))
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                connection.Open();
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        items.Add(new CatalogItem
                        {
                            Id = reader.GetInt32(0), Code = reader.GetString(1), Name = reader.GetString(2), Description = reader.GetString(3),
                            Address = reader.GetString(4), Phone = reader.GetString(5), CategoryId = reader.GetInt32(6),
                            Price = reader.GetDecimal(7), MinimumStock = reader.GetInt32(8), Active = reader.GetBoolean(9)
                        });
                    }
                }
            }

            return items.ToArray();
        }

        public CatalogItem SaveCatalog(string catalogType, CatalogItem item)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.Name)) throw new ArgumentException("El nombre es obligatorio.");
            string type = NormalizeCatalogType(catalogType);
            string commandText;

            if (type == "CATEGORY")
                commandText = item.Id == 0 ? "INSERT dbo.Categorias(Nombre, Descripcion) VALUES(@Name, @Description); SELECT CONVERT(int, SCOPE_IDENTITY());" : "UPDATE dbo.Categorias SET Nombre=@Name, Descripcion=@Description WHERE CategoriaId=@Id; SELECT @Id;";
            else if (type == "BRANCH")
                commandText = item.Id == 0 ? "INSERT dbo.Sucursales(Clave, Nombre, Direccion, Telefono) VALUES(@Code, @Name, @Address, @Phone); SELECT CONVERT(int, SCOPE_IDENTITY());" : "UPDATE dbo.Sucursales SET Clave=@Code, Nombre=@Name, Direccion=@Address, Telefono=@Phone WHERE SucursalId=@Id; SELECT @Id;";
            else if (type == "PRODUCT")
                commandText = item.Id == 0 ? "INSERT dbo.Productos(CategoriaId, Codigo, Nombre, Descripcion, Precio, ExistenciaMinima) VALUES(@CategoryId, @Code, @Name, @Description, @Price, @MinimumStock); SELECT CONVERT(int, SCOPE_IDENTITY());" : "UPDATE dbo.Productos SET CategoriaId=@CategoryId, Codigo=@Code, Nombre=@Name, Descripcion=@Description, Precio=@Price, ExistenciaMinima=@MinimumStock WHERE ProductoId=@Id; SELECT @Id;";
            else throw new ArgumentException("Catálogo no válido.");

            if ((type == "BRANCH" || type == "PRODUCT") && string.IsNullOrWhiteSpace(item.Code)) throw new ArgumentException("La clave o código es obligatorio.");
            if (type == "PRODUCT" && (item.CategoryId <= 0 || item.Price < 0 || item.MinimumStock < 0)) throw new ArgumentException("Los datos del producto no son válidos.");

            using (SqlConnection connection = new SqlConnection(GetConnectionString()))
            using (SqlCommand command = new SqlCommand(commandText, connection))
            {
                command.Parameters.Add("@Id", SqlDbType.Int).Value = item.Id;
                command.Parameters.Add("@Code", SqlDbType.NVarChar, 30).Value = (object)item.Code?.Trim() ?? DBNull.Value;
                command.Parameters.Add("@Name", SqlDbType.NVarChar, 150).Value = item.Name.Trim();
                command.Parameters.Add("@Description", SqlDbType.NVarChar, 300).Value = (object)item.Description?.Trim() ?? DBNull.Value;
                command.Parameters.Add("@Address", SqlDbType.NVarChar, 250).Value = (object)item.Address?.Trim() ?? DBNull.Value;
                command.Parameters.Add("@Phone", SqlDbType.NVarChar, 20).Value = (object)item.Phone?.Trim() ?? DBNull.Value;
                command.Parameters.Add("@CategoryId", SqlDbType.Int).Value = item.CategoryId;
                command.Parameters.Add("@Price", SqlDbType.Decimal).Value = item.Price;
                command.Parameters["@Price"].Precision = 12; command.Parameters["@Price"].Scale = 2;
                command.Parameters.Add("@MinimumStock", SqlDbType.Int).Value = item.MinimumStock;
                connection.Open();
                item.Id = (int)command.ExecuteScalar();
            }
            return item;
        }

        public void DeactivateCatalog(string catalogType, int id)
        {
            string type = NormalizeCatalogType(catalogType);
            string query = type == "CATEGORY" ? "UPDATE dbo.Categorias SET Activa=0 WHERE CategoriaId=@Id" : type == "BRANCH" ? "UPDATE dbo.Sucursales SET Activa=0 WHERE SucursalId=@Id" : type == "PRODUCT" ? "UPDATE dbo.Productos SET Activo=0 WHERE ProductoId=@Id" : throw new ArgumentException("Catálogo no válido.");
            using (SqlConnection connection = new SqlConnection(GetConnectionString()))
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                command.Parameters.Add("@Id", SqlDbType.Int).Value = id;
                connection.Open();
                command.ExecuteNonQuery();
            }
        }

        private static string NormalizeCatalogType(string catalogType)
        {
            return (catalogType ?? string.Empty).Trim().ToUpperInvariant();
        }

        public InventoryItem[] GetInventory(int branchId)
        {
            const string query = @"SELECT p.ProductoId, p.Codigo, p.Nombre, i.Existencia, p.ExistenciaMinima
FROM dbo.Inventario i INNER JOIN dbo.Productos p ON p.ProductoId=i.ProductoId
WHERE i.SucursalId=@BranchId AND p.Activo=1 ORDER BY p.Nombre;";
            var items = new System.Collections.Generic.List<InventoryItem>();
            using (SqlConnection connection = new SqlConnection(GetConnectionString()))
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                command.Parameters.Add("@BranchId", SqlDbType.Int).Value = branchId;
                connection.Open();
                using (SqlDataReader reader = command.ExecuteReader())
                    while (reader.Read()) items.Add(new InventoryItem { ProductId = reader.GetInt32(0), Code = reader.GetString(1), ProductName = reader.GetString(2), Stock = reader.GetInt32(3), MinimumStock = reader.GetInt32(4), LowStock = reader.GetInt32(3) <= reader.GetInt32(4) });
            }
            return items.ToArray();
        }

        public void RegisterInventoryMovement(int userId, int branchId, int productId, string movementType, int quantity, string notes)
        {
            string type = (movementType ?? string.Empty).Trim();
            if (quantity <= 0 || (type != "Entrada" && type != "Salida" && type != "AjusteEntrada" && type != "AjusteSalida")) throw new ArgumentException("Movimiento no válido.");
            int delta = type == "Entrada" || type == "AjusteEntrada" ? quantity : -quantity;
            using (SqlConnection connection = new SqlConnection(GetConnectionString()))
            {
                connection.Open();
                using (SqlTransaction transaction = connection.BeginTransaction(IsolationLevel.Serializable))
                {
                    try
                    {
                        using (SqlCommand update = new SqlCommand("UPDATE dbo.Inventario SET Existencia=Existencia+@Delta, ActualizadoEn=SYSDATETIME() WHERE SucursalId=@BranchId AND ProductoId=@ProductId AND Existencia+@Delta>=0", connection, transaction))
                        {
                            update.Parameters.Add("@Delta", SqlDbType.Int).Value = delta; update.Parameters.Add("@BranchId", SqlDbType.Int).Value = branchId; update.Parameters.Add("@ProductId", SqlDbType.Int).Value = productId;
                            if (update.ExecuteNonQuery() == 0) throw new InvalidOperationException("Existencia insuficiente o producto no registrado en esta sucursal.");
                        }
                        using (SqlCommand movement = new SqlCommand("INSERT dbo.MovimientosInventario(SucursalId,ProductoId,UsuarioId,Tipo,Cantidad,Observaciones) VALUES(@BranchId,@ProductId,@UserId,@Type,@Quantity,@Notes)", connection, transaction))
                        {
                            movement.Parameters.Add("@BranchId", SqlDbType.Int).Value = branchId; movement.Parameters.Add("@ProductId", SqlDbType.Int).Value = productId; movement.Parameters.Add("@UserId", SqlDbType.Int).Value = userId; movement.Parameters.Add("@Type", SqlDbType.NVarChar, 30).Value = type; movement.Parameters.Add("@Quantity", SqlDbType.Int).Value = quantity; movement.Parameters.Add("@Notes", SqlDbType.NVarChar, 300).Value = (object)notes ?? DBNull.Value; movement.ExecuteNonQuery();
                        }
                        using (SqlCommand audit = new SqlCommand("INSERT dbo.Bitacora(UsuarioId,SucursalId,Accion,Entidad,EntidadId,Detalle) VALUES(@User,@Branch,@Action,'Inventario',@Product,@Detail)", connection, transaction))
                        { audit.Parameters.Add("@User",SqlDbType.Int).Value=userId; audit.Parameters.Add("@Branch",SqlDbType.Int).Value=branchId; audit.Parameters.Add("@Action",SqlDbType.NVarChar,100).Value=type; audit.Parameters.Add("@Product",SqlDbType.Int).Value=productId; audit.Parameters.Add("@Detail",SqlDbType.NVarChar,500).Value=$"Cantidad: {quantity}. {notes}"; audit.ExecuteNonQuery(); }
                        transaction.Commit();
                    }
                    catch { transaction.Rollback(); throw; }
                }
            }
        }

        public int CreateTransfer(int userId, int originBranchId, int destinationBranchId, int productId, int quantity, string notes)
        {
            if (originBranchId == destinationBranchId || quantity <= 0) throw new ArgumentException("Los datos de transferencia no son válidos.");
            using (SqlConnection connection = new SqlConnection(GetConnectionString()))
            {
                connection.Open(); using (SqlTransaction tx = connection.BeginTransaction())
                {
                    try
                    {
                        int transferId;
                        using (SqlCommand header = new SqlCommand("INSERT dbo.Transferencias(SucursalOrigenId,SucursalDestinoId,UsuarioSolicitaId,Observaciones) VALUES(@Origin,@Destination,@User,@Notes); SELECT CONVERT(int,SCOPE_IDENTITY());", connection, tx))
                        {
                            header.Parameters.Add("@Origin", SqlDbType.Int).Value = originBranchId; header.Parameters.Add("@Destination", SqlDbType.Int).Value = destinationBranchId; header.Parameters.Add("@User", SqlDbType.Int).Value = userId; header.Parameters.Add("@Notes", SqlDbType.NVarChar, 300).Value = (object)notes ?? DBNull.Value; transferId = (int)header.ExecuteScalar();
                        }
                        using (SqlCommand detail = new SqlCommand("INSERT dbo.DetalleTransferencia(TransferenciaId,ProductoId,Cantidad) VALUES(@Transfer,@Product,@Quantity)", connection, tx))
                        { detail.Parameters.Add("@Transfer", SqlDbType.Int).Value = transferId; detail.Parameters.Add("@Product", SqlDbType.Int).Value = productId; detail.Parameters.Add("@Quantity", SqlDbType.Int).Value = quantity; detail.ExecuteNonQuery(); }
                        tx.Commit(); return transferId;
                    }
                    catch { tx.Rollback(); throw; }
                }
            }
        }

        public void ResolveTransfer(int userId, int transferId, bool approve)
        {
            using (SqlConnection connection = new SqlConnection(GetConnectionString()))
            {
                connection.Open(); using (SqlTransaction tx = connection.BeginTransaction(IsolationLevel.Serializable))
                {
                    try
                    {
                        int origin, destination, product, quantity;
                        using (SqlCommand read = new SqlCommand("SELECT t.SucursalOrigenId,t.SucursalDestinoId,d.ProductoId,d.Cantidad FROM dbo.Transferencias t INNER JOIN dbo.DetalleTransferencia d ON d.TransferenciaId=t.TransferenciaId WHERE t.TransferenciaId=@Id AND t.Estado='Pendiente'", connection, tx))
                        { read.Parameters.Add("@Id", SqlDbType.Int).Value = transferId; using (SqlDataReader r = read.ExecuteReader()) { if (!r.Read()) throw new InvalidOperationException("La transferencia no está pendiente."); origin=r.GetInt32(0); destination=r.GetInt32(1); product=r.GetInt32(2); quantity=r.GetInt32(3); } }
                        if (approve)
                        {
                            using (SqlCommand debit = new SqlCommand("UPDATE dbo.Inventario SET Existencia=Existencia-@Q WHERE SucursalId=@B AND ProductoId=@P AND Existencia>=@Q", connection, tx))
                            { debit.Parameters.Add("@Q", SqlDbType.Int).Value=quantity; debit.Parameters.Add("@B",SqlDbType.Int).Value=origin; debit.Parameters.Add("@P",SqlDbType.Int).Value=product; if (debit.ExecuteNonQuery()==0) throw new InvalidOperationException("La sucursal origen no tiene existencia suficiente."); }
                            using (SqlCommand credit = new SqlCommand("UPDATE dbo.Inventario SET Existencia=Existencia+@Q WHERE SucursalId=@B AND ProductoId=@P; IF @@ROWCOUNT=0 INSERT dbo.Inventario(SucursalId,ProductoId,Existencia) VALUES(@B,@P,@Q)", connection, tx))
                            { credit.Parameters.Add("@Q",SqlDbType.Int).Value=quantity; credit.Parameters.Add("@B",SqlDbType.Int).Value=destination; credit.Parameters.Add("@P",SqlDbType.Int).Value=product; credit.ExecuteNonQuery(); }
                        }
                        using (SqlCommand status = new SqlCommand("UPDATE dbo.Transferencias SET Estado=@State,UsuarioAutorizaId=@User,FechaResolucion=SYSDATETIME() WHERE TransferenciaId=@Id", connection, tx))
                        { status.Parameters.Add("@State",SqlDbType.NVarChar,20).Value=approve ? "Recibida" : "Rechazada"; status.Parameters.Add("@User",SqlDbType.Int).Value=userId; status.Parameters.Add("@Id",SqlDbType.Int).Value=transferId; status.ExecuteNonQuery(); }
                        tx.Commit();
                    }
                    catch { tx.Rollback(); throw; }
                }
            }
        }

        public TransferItem[] GetTransfers()
        {
            const string query = "SELECT t.TransferenciaId,so.Nombre,sd.Nombre,t.Estado,d.ProductoId,p.Nombre,d.Cantidad FROM dbo.Transferencias t INNER JOIN dbo.Sucursales so ON so.SucursalId=t.SucursalOrigenId INNER JOIN dbo.Sucursales sd ON sd.SucursalId=t.SucursalDestinoId INNER JOIN dbo.DetalleTransferencia d ON d.TransferenciaId=t.TransferenciaId INNER JOIN dbo.Productos p ON p.ProductoId=d.ProductoId ORDER BY t.FechaSolicitud DESC";
            var result=new System.Collections.Generic.List<TransferItem>(); using(SqlConnection c=new SqlConnection(GetConnectionString())) using(SqlCommand cmd=new SqlCommand(query,c)){c.Open();using(SqlDataReader r=cmd.ExecuteReader())while(r.Read())result.Add(new TransferItem{TransferId=r.GetInt32(0),Origin=r.GetString(1),Destination=r.GetString(2),Status=r.GetString(3),ProductId=r.GetInt32(4),ProductName=r.GetString(5),Quantity=r.GetInt32(6)});} return result.ToArray();
        }

        public ReportRow[] GetReport(string reportType, DateTime startDate, DateTime endDate)
        {
            string type=(reportType??string.Empty).ToUpperInvariant(); string query;
            if(type=="LOWSTOCK") query="SELECT p.Nombre, s.Nombre, 0 AS Amount, i.Existencia, i.ActualizadoEn FROM dbo.Inventario i INNER JOIN dbo.Productos p ON p.ProductoId=i.ProductoId INNER JOIN dbo.Sucursales s ON s.SucursalId=i.SucursalId WHERE i.Existencia<=p.ExistenciaMinima";
            else if(type=="MOVEMENTS") query="SELECT p.Nombre, m.Tipo, 0 AS Amount, m.Cantidad, m.Fecha FROM dbo.MovimientosInventario m INNER JOIN dbo.Productos p ON p.ProductoId=m.ProductoId WHERE m.Fecha>=@Start AND m.Fecha<DATEADD(day,1,@End)";
            else if(type=="SALES") query="SELECT s.Nombre, v.Estado, v.Total, 1 AS Quantity, v.Fecha FROM dbo.Ventas v INNER JOIN dbo.Sucursales s ON s.SucursalId=v.SucursalId WHERE v.Fecha>=@Start AND v.Fecha<DATEADD(day,1,@End)";
            else if(type=="TRANSFERS") query="SELECT CONCAT(so.Nombre,' → ',sd.Nombre), t.Estado, 0 AS Amount, d.Cantidad, t.FechaSolicitud FROM dbo.Transferencias t INNER JOIN dbo.Sucursales so ON so.SucursalId=t.SucursalOrigenId INNER JOIN dbo.Sucursales sd ON sd.SucursalId=t.SucursalDestinoId INNER JOIN dbo.DetalleTransferencia d ON d.TransferenciaId=t.TransferenciaId WHERE t.FechaSolicitud>=@Start AND t.FechaSolicitud<DATEADD(day,1,@End)";
            else throw new ArgumentException("Reporte no válido.");
            var rows=new System.Collections.Generic.List<ReportRow>(); using(SqlConnection c=new SqlConnection(GetConnectionString()))using(SqlCommand cmd=new SqlCommand(query,c)){cmd.Parameters.Add("@Start",SqlDbType.DateTime2).Value=startDate.Date;cmd.Parameters.Add("@End",SqlDbType.DateTime2).Value=endDate.Date;c.Open();using(SqlDataReader r=cmd.ExecuteReader())while(r.Read())rows.Add(new ReportRow{Item=r.GetString(0),Detail=r.GetString(1),Amount=r.GetDecimal(2),Quantity=r.GetInt32(3),Date=r.GetDateTime(4)});}return rows.ToArray();
        }

        public AuditRecord[] GetAuditLog(DateTime startDate, DateTime endDate)
        {
            const string query="SELECT b.Fecha,ISNULL(u.NombreUsuario,'Sistema'),ISNULL(s.Nombre,'Global'),b.Accion,b.Entidad,ISNULL(b.Detalle,'') FROM dbo.Bitacora b LEFT JOIN dbo.Usuarios u ON u.UsuarioId=b.UsuarioId LEFT JOIN dbo.Sucursales s ON s.SucursalId=b.SucursalId WHERE b.Fecha>=@Start AND b.Fecha<DATEADD(day,1,@End) ORDER BY b.Fecha DESC";
            var records=new System.Collections.Generic.List<AuditRecord>(); using(SqlConnection c=new SqlConnection(GetConnectionString()))using(SqlCommand cmd=new SqlCommand(query,c)){cmd.Parameters.Add("@Start",SqlDbType.DateTime2).Value=startDate.Date;cmd.Parameters.Add("@End",SqlDbType.DateTime2).Value=endDate.Date;c.Open();using(SqlDataReader r=cmd.ExecuteReader())while(r.Read())records.Add(new AuditRecord{Date=r.GetDateTime(0),User=r.GetString(1),Branch=r.GetString(2),Action=r.GetString(3),Entity=r.GetString(4),Detail=r.GetString(5)});}return records.ToArray();
        }

        public int RegisterSale(int userId, int branchId, SaleLine[] lines)
        {
            if (lines == null || lines.Length == 0) throw new ArgumentException("La venta debe contener productos.");
            using (SqlConnection c = new SqlConnection(GetConnectionString())) { c.Open(); using (SqlTransaction tx = c.BeginTransaction(IsolationLevel.Serializable)) { try {
                decimal subtotal=0m; foreach(var line in lines){ if(line==null||line.ProductId<=0||line.Quantity<=0) throw new ArgumentException("Detalle de venta no válido."); using(SqlCommand price=new SqlCommand("SELECT Precio FROM dbo.Productos WHERE ProductoId=@P AND Activo=1",c,tx)){price.Parameters.Add("@P",SqlDbType.Int).Value=line.ProductId; object value=price.ExecuteScalar(); if(value==null) throw new InvalidOperationException("Producto no disponible."); subtotal+=(decimal)value*line.Quantity;} using(SqlCommand stock=new SqlCommand("UPDATE dbo.Inventario SET Existencia=Existencia-@Q WHERE SucursalId=@B AND ProductoId=@P AND Existencia>=@Q",c,tx)){stock.Parameters.Add("@Q",SqlDbType.Int).Value=line.Quantity;stock.Parameters.Add("@B",SqlDbType.Int).Value=branchId;stock.Parameters.Add("@P",SqlDbType.Int).Value=line.ProductId;if(stock.ExecuteNonQuery()==0)throw new InvalidOperationException("Existencia insuficiente.");}}
                decimal iva=Math.Round(subtotal*.16m,2), total=subtotal+iva; int saleId; using(SqlCommand sale=new SqlCommand("INSERT dbo.Ventas(SucursalId,UsuarioId,Subtotal,Iva,Total) VALUES(@B,@U,@S,@I,@T);SELECT CONVERT(int,SCOPE_IDENTITY())",c,tx)){sale.Parameters.Add("@B",SqlDbType.Int).Value=branchId;sale.Parameters.Add("@U",SqlDbType.Int).Value=userId;sale.Parameters.Add("@S",SqlDbType.Decimal).Value=subtotal;sale.Parameters.Add("@I",SqlDbType.Decimal).Value=iva;sale.Parameters.Add("@T",SqlDbType.Decimal).Value=total;saleId=(int)sale.ExecuteScalar();}
                foreach(var line in lines){using(SqlCommand detail=new SqlCommand("INSERT dbo.DetalleVenta(VentaId,ProductoId,Cantidad,PrecioUnitario) SELECT @V,@P,@Q,Precio FROM dbo.Productos WHERE ProductoId=@P; INSERT dbo.MovimientosInventario(SucursalId,ProductoId,UsuarioId,Tipo,Cantidad,Referencia) VALUES(@B,@P,@U,'Venta',@Q,CONCAT('Venta #',@V))",c,tx)){detail.Parameters.Add("@V",SqlDbType.Int).Value=saleId;detail.Parameters.Add("@P",SqlDbType.Int).Value=line.ProductId;detail.Parameters.Add("@Q",SqlDbType.Int).Value=line.Quantity;detail.Parameters.Add("@B",SqlDbType.Int).Value=branchId;detail.Parameters.Add("@U",SqlDbType.Int).Value=userId;detail.ExecuteNonQuery();}}
                tx.Commit();return saleId; } catch {tx.Rollback();throw;} } }
        }

        [ComRegisterFunction]
        public static void Register(Type type)
        {
            // RegAsm registra la clase; aquí se asocia a un AppID para configurarla en DCOMCNFG.
            using (RegistryKey clsidKey = Registry.ClassesRoot.OpenSubKey($@"CLSID\{{{type.GUID}}}", true))
            {
                clsidKey?.SetValue("AppID", AppId);
            }

            using (RegistryKey appIdKey = Registry.ClassesRoot.CreateSubKey($@"AppID\{AppId}"))
            {
                appIdKey.SetValue(null, "Inventory Sucursales DCOM Server");
                // Un DLL COM se ejecuta en el surrogate de Windows, habilitando activación remota DCOM.
                appIdKey.SetValue("DllSurrogate", string.Empty);
            }
        }

        [ComUnregisterFunction]
        public static void Unregister(Type type)
        {
            using (RegistryKey clsidKey = Registry.ClassesRoot.OpenSubKey($@"CLSID\{{{type.GUID}}}", true))
            {
                clsidKey?.DeleteValue("AppID", false);
            }

            Registry.ClassesRoot.DeleteSubKeyTree($@"AppID\{AppId}", false);
        }
    }
}
