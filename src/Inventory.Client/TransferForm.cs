using System;
using System.Windows.Forms;
using Inventory.Contracts;

namespace Inventory.Client
{
    internal sealed class TransferForm : Form
    {
        private readonly Func<IInventoryGateway> _gatewayFactory;
        private readonly int _userId;
        private readonly TextBox txtOrigin = new TextBox { Width = 45 };
        private readonly TextBox txtDestination = new TextBox { Width = 45 };
        private readonly TextBox txtProduct = new TextBox { Width = 55 };
        private readonly TextBox txtQuantity = new TextBox { Width = 55 };
        private readonly TextBox txtNotes = new TextBox { Width = 180 };
        private readonly DataGridView dgvTransfers = new DataGridView { Dock = DockStyle.Fill, ReadOnly = true, AutoGenerateColumns = true, SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false };

        public TransferForm(Func<IInventoryGateway> gatewayFactory, int userId, int branchId)
        {
            _gatewayFactory = gatewayFactory; _userId = userId; txtOrigin.Text = branchId.ToString();
            Text = "Transferencias entre sucursales"; Width = 850; Height = 480; StartPosition = FormStartPosition.CenterParent;
            Button btnCreate = new Button { Text = "Solicitar", AutoSize = true }; Button btnApprove = new Button { Text = "Autorizar", AutoSize = true }; Button btnReject = new Button { Text = "Rechazar", AutoSize = true }; Button btnRefresh = new Button { Text = "Actualizar", AutoSize = true };
            btnCreate.Click += BtnCreate_Click; btnApprove.Click += (s,e) => Resolve(true); btnReject.Click += (s,e) => Resolve(false); btnRefresh.Click += (s,e) => LoadTransfers();
            FlowLayoutPanel top = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(8) };
            top.Controls.AddRange(new Control[] { LabelOf("Origen"), txtOrigin, LabelOf("Destino"), txtDestination, LabelOf("Producto ID"), txtProduct, LabelOf("Cantidad"), txtQuantity, txtNotes, btnCreate, btnApprove, btnReject, btnRefresh });
            Controls.Add(dgvTransfers); Controls.Add(top); LoadTransfers();
        }

        private static Label LabelOf(string text) => new Label { Text = text, AutoSize = true, Padding = new Padding(4, 6, 0, 0) };
        private void LoadTransfers() { try { dgvTransfers.DataSource = _gatewayFactory().GetTransfers(); } catch (Exception ex) { MessageBox.Show(ex.Message); } }
        private void BtnCreate_Click(object sender, EventArgs e)
        {
            if (!int.TryParse(txtOrigin.Text, out int origin) || !int.TryParse(txtDestination.Text, out int destination) || !int.TryParse(txtProduct.Text, out int product) || !int.TryParse(txtQuantity.Text, out int quantity)) { MessageBox.Show("Captura IDs y cantidad válidos."); return; }
            try { int id = _gatewayFactory().CreateTransfer(_userId, origin, destination, product, quantity, txtNotes.Text); MessageBox.Show($"Solicitud #{id} creada."); LoadTransfers(); } catch (Exception ex) { MessageBox.Show(ex.Message, "Transferencia", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
        }
        private void Resolve(bool approve)
        {
            if (!(dgvTransfers.CurrentRow?.DataBoundItem is TransferItem item)) { MessageBox.Show("Selecciona una transferencia."); return; }
            try { _gatewayFactory().ResolveTransfer(_userId, item.TransferId, approve); LoadTransfers(); } catch (Exception ex) { MessageBox.Show(ex.Message, "Transferencia", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
        }
    }
}
