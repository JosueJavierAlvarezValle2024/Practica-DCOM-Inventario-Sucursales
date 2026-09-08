using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Inventory.Contracts;

namespace Inventory.Client
{
    public sealed class MainForm : Form
    {
        private const string GatewayProgId = "InventorySucursales.InventoryGateway";
        private readonly TextBox txtServer = new TextBox { Name = "txtServer", Text = ".", Width = 260 };
        private readonly TextBox txtUsername = new TextBox { Name = "txtUsername", Text = "admin", Width = 200 };
        private readonly TextBox txtPassword = new TextBox { Name = "txtPassword", Text = "Admin123!", Width = 200, UseSystemPasswordChar = true };
        private readonly Label lblSession = new Label { Name = "lblSession", Text = "Sin sesión iniciada.", AutoSize = true };
        private readonly TextBox txtClientName = new TextBox { Name = "txtClientName", Text = Environment.MachineName, Width = 260 };
        private readonly TextBox txtFirstValue = new TextBox { Name = "txtFirstValue", Text = "10", Width = 120 };
        private readonly TextBox txtSecondValue = new TextBox { Name = "txtSecondValue", Text = "20", Width = 120 };
        private readonly TextBox txtResult = new TextBox { Name = "txtResult", ReadOnly = true, Multiline = true, Height = 65, Width = 420 };
        private readonly Button btnCatalogs = new Button { Name = "btnCatalogs", Text = "Administrar catálogos", AutoSize = true, Enabled = false };
        private readonly Button btnInventory = new Button { Name = "btnInventory", Text = "Inventario", AutoSize = true, Enabled = false };
        private readonly Button btnTransfers = new Button { Name = "btnTransfers", Text = "Transferencias", AutoSize = true, Enabled = false };
        private readonly Button btnReports = new Button { Name = "btnReports", Text = "Reportes", AutoSize = true, Enabled = false };
        private readonly Button btnAudit = new Button { Name = "btnAudit", Text = "Bitácora", AutoSize = true, Enabled = false };
        private readonly Button btnSales = new Button { Name = "btnSales", Text = "Ventas", AutoSize = true, Enabled = false };
        private int _currentUserId;
        private int _currentBranchId = 1;

        public MainForm()
        {
            Text = "Inventory Sucursales - Cliente DCOM";
            StartPosition = FormStartPosition.CenterScreen;
            Width = 520;
            Height = 680;

            Button btnLogin = new Button { Name = "btnLogin", Text = "Iniciar sesión", AutoSize = true };
            Button btnPing = new Button { Name = "btnPing", Text = "Probar Ping", AutoSize = true };
            Button btnSum = new Button { Name = "btnSum", Text = "Probar Suma", AutoSize = true };
            btnLogin.Click += BtnLogin_Click;
            btnPing.Click += BtnPing_Click;
            btnSum.Click += BtnSum_Click;
            btnCatalogs.Click += (sender, args) => new CatalogForm(CreateGateway).ShowDialog(this);
            btnInventory.Click += (sender, args) => new InventoryForm(CreateGateway, _currentUserId, _currentBranchId).ShowDialog(this);
            btnTransfers.Click += (sender, args) => new TransferForm(CreateGateway, _currentUserId, _currentBranchId).ShowDialog(this);
            btnReports.Click += (sender, args) => new ReportForm(CreateGateway).ShowDialog(this);
            btnAudit.Click += (sender,args) => new AuditForm(CreateGateway).ShowDialog(this);
            btnSales.Click += (sender,args) => new SaleForm(CreateGateway, _currentUserId, _currentBranchId).ShowDialog(this);

            FlowLayoutPanel loginLayout = CreatePanel();
            loginLayout.Controls.Add(CreateRow("Servidor DCOM (. = local o nombre/IP):", txtServer));
            loginLayout.Controls.Add(CreateRow("Usuario:", txtUsername));
            loginLayout.Controls.Add(CreateRow("Contraseña:", txtPassword));
            loginLayout.Controls.Add(btnLogin);
            loginLayout.Controls.Add(lblSession);

            FlowLayoutPanel testLayout = CreatePanel();
            testLayout.Controls.Add(CreateRow("Nombre del cliente:", txtClientName));
            testLayout.Controls.Add(btnPing);
            testLayout.Controls.Add(CreateRow("Primer valor:", txtFirstValue));
            testLayout.Controls.Add(CreateRow("Segundo valor:", txtSecondValue));
            testLayout.Controls.Add(btnSum);
            testLayout.Controls.Add(new Label { Text = "Resultado:", AutoSize = true, Padding = new Padding(0, 10, 0, 0) });
            testLayout.Controls.Add(txtResult);

            FlowLayoutPanel root = CreatePanel();
            root.Dock = DockStyle.Fill;
            root.Padding = new Padding(18);
            root.AutoScroll = true;
            root.AutoSize = false;
            root.WrapContents = false;
            root.Controls.Add(CreateGroup("Acceso al sistema", loginLayout));
            root.Controls.Add(CreateGroup("Pruebas de comunicación DCOM", testLayout));
            root.Controls.Add(btnCatalogs);
            root.Controls.Add(btnInventory);
            root.Controls.Add(btnTransfers);
            root.Controls.Add(btnReports);
            root.Controls.Add(btnAudit);
            root.Controls.Add(btnSales);
            Controls.Add(root);
        }

        private static FlowLayoutPanel CreatePanel()
        {
            return new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false };
        }

        private static GroupBox CreateGroup(string title, Control content)
        {
            GroupBox group = new GroupBox { Text = title, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Padding = new Padding(12), Width = 460 };
            group.Controls.Add(content);
            return group;
        }

        private static Control CreateRow(string label, Control input)
        {
            FlowLayoutPanel row = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight };
            row.Controls.Add(new Label { Text = label, AutoSize = true, Width = 205, Padding = new Padding(0, 6, 0, 0) });
            row.Controls.Add(input);
            return row;
        }

        private void BtnLogin_Click(object sender, EventArgs e)
        {
            try
            {
                AuthenticationResult session = CreateGateway().Authenticate(txtUsername.Text, txtPassword.Text);
                if (!session.IsAuthenticated)
                {
                    lblSession.Text = session.Message;
                    lblSession.ForeColor = System.Drawing.Color.Firebrick;
                    return;
                }

                lblSession.Text = $"Sesión: {session.FullName} | Rol: {session.Role} | Sucursal: {session.BranchName}";
                lblSession.ForeColor = System.Drawing.Color.DarkGreen;
                txtResult.Text = session.Message;
                btnCatalogs.Enabled = string.Equals(session.Role, "Administrador", StringComparison.OrdinalIgnoreCase);
                _currentUserId = session.UserId;
                if (session.BranchId.HasValue) _currentBranchId = session.BranchId.Value;
                btnInventory.Enabled = string.Equals(session.Role, "Administrador", StringComparison.OrdinalIgnoreCase) || string.Equals(session.Role, "Encargado", StringComparison.OrdinalIgnoreCase);
                btnTransfers.Enabled = btnInventory.Enabled;
                btnReports.Enabled = true;
                btnAudit.Enabled = btnCatalogs.Enabled;
                btnSales.Enabled = string.Equals(session.Role, "Administrador", StringComparison.OrdinalIgnoreCase) || string.Equals(session.Role, "Cajero", StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                lblSession.Text = "No fue posible comunicarse con el servidor DCOM.";
                lblSession.ForeColor = System.Drawing.Color.Firebrick;
                txtResult.Text = ex.Message;
            }
        }

        private void BtnPing_Click(object sender, EventArgs e)
        {
            try
            {
                txtResult.Text = CreateGateway().Ping(txtClientName.Text);
            }
            catch (COMException ex)
            {
                txtResult.Text = $"Error DCOM (0x{ex.ErrorCode:X8}): {ex.Message}";
            }
            catch (Exception ex)
            {
                txtResult.Text = $"No se pudo conectar: {ex.Message}";
            }
        }

        private void BtnSum_Click(object sender, EventArgs e)
        {
            if (!decimal.TryParse(txtFirstValue.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out decimal firstValue) ||
                !decimal.TryParse(txtSecondValue.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out decimal secondValue))
            {
                MessageBox.Show("Captura dos números válidos.", "Datos inválidos", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                txtResult.Text = $"Resultado remoto: {CreateGateway().Sum(firstValue, secondValue):N2}";
            }
            catch (Exception ex)
            {
                txtResult.Text = $"No se pudo ejecutar la operación remota: {ex.Message}";
            }
        }

        private IInventoryGateway CreateGateway()
        {
            string serverName = string.IsNullOrWhiteSpace(txtServer.Text) ? "." : txtServer.Text.Trim();
            // El punto representa una prueba COM local; no debe forzar activación DCOM remota.
            Type gatewayType = serverName == "."
                ? Type.GetTypeFromProgID(GatewayProgId, true)
                : Type.GetTypeFromProgID(GatewayProgId, serverName, true);
            return (IInventoryGateway)Activator.CreateInstance(gatewayType);
        }
    }
}
