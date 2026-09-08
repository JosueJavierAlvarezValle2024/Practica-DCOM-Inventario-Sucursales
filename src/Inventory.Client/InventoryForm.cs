using System;
using System.Windows.Forms;
using Inventory.Contracts;

namespace Inventory.Client
{
    internal sealed class InventoryForm : Form
    {
        private readonly Func<IInventoryGateway> _gatewayFactory;
        private readonly int _userId;
        private readonly TextBox txtBranch = new TextBox { Width = 55 };
        private readonly ComboBox cmbMovement = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 120 };
        private readonly TextBox txtQuantity = new TextBox { Width = 60 };
        private readonly TextBox txtNotes = new TextBox { Width = 220 };
        private readonly DataGridView dgvInventory = new DataGridView { Dock = DockStyle.Fill, ReadOnly = true, AutoGenerateColumns = true, SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false };

        public InventoryForm(Func<IInventoryGateway> gatewayFactory, int userId, int branchId)
        {
            _gatewayFactory = gatewayFactory; _userId = userId; txtBranch.Text = branchId.ToString();
            Text = "Inventario por sucursal"; Width = 780; Height = 480; StartPosition = FormStartPosition.CenterParent;
            cmbMovement.Items.AddRange(new[] { "Entrada", "Salida", "AjusteEntrada", "AjusteSalida" }); cmbMovement.SelectedIndex = 0;
            Button btnRefresh = new Button { Text = "Consultar", AutoSize = true }; Button btnMove = new Button { Text = "Registrar movimiento", AutoSize = true };
            btnRefresh.Click += (sender, args) => LoadInventory(); btnMove.Click += BtnMove_Click;
            FlowLayoutPanel top = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(8) };
            top.Controls.AddRange(new Control[] { new Label { Text = "Sucursal ID:", AutoSize = true, Padding = new Padding(0, 6, 0, 0) }, txtBranch, btnRefresh, new Label { Text = "Movimiento:", AutoSize = true, Padding = new Padding(8, 6, 0, 0) }, cmbMovement, new Label { Text = "Cantidad:", AutoSize = true, Padding = new Padding(8, 6, 0, 0) }, txtQuantity, txtNotes, btnMove });
            Controls.Add(dgvInventory); Controls.Add(top); LoadInventory();
        }

        private void LoadInventory()
        {
            if (!int.TryParse(txtBranch.Text, out int branchId)) return;
            try { dgvInventory.DataSource = _gatewayFactory().GetInventory(branchId); }
            catch (Exception ex) { MessageBox.Show(ex.Message, "Inventario", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        private void BtnMove_Click(object sender, EventArgs e)
        {
            if (!(dgvInventory.CurrentRow?.DataBoundItem is InventoryItem item) || !int.TryParse(txtBranch.Text, out int branchId) || !int.TryParse(txtQuantity.Text, out int quantity)) { MessageBox.Show("Selecciona un producto y captura una cantidad válida."); return; }
            try { _gatewayFactory().RegisterInventoryMovement(_userId, branchId, item.ProductId, cmbMovement.Text, quantity, txtNotes.Text); txtQuantity.Clear(); txtNotes.Clear(); LoadInventory(); }
            catch (Exception ex) { MessageBox.Show(ex.Message, "Movimiento rechazado", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
        }
    }
}
