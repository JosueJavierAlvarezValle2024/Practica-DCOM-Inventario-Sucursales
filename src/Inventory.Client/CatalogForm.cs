using System;
using System.Globalization;
using System.Windows.Forms;
using Inventory.Contracts;

namespace Inventory.Client
{
    internal sealed class CatalogForm : Form
    {
        private readonly Func<IInventoryGateway> _gatewayFactory;
        private readonly ComboBox cmbType = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 180 };
        private readonly DataGridView dgvItems = new DataGridView { Dock = DockStyle.Fill, ReadOnly = true, AutoGenerateColumns = true, SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false };
        private readonly TextBox txtCode = new TextBox { Width = 150 };
        private readonly TextBox txtName = new TextBox { Width = 220 };
        private readonly TextBox txtDescription = new TextBox { Width = 220 };
        private readonly TextBox txtAddress = new TextBox { Width = 220 };
        private readonly TextBox txtPhone = new TextBox { Width = 150 };
        private readonly TextBox txtCategoryId = new TextBox { Width = 70 };
        private readonly TextBox txtPrice = new TextBox { Width = 90 };
        private readonly TextBox txtMinimumStock = new TextBox { Width = 70 };
        private int _selectedId;

        public CatalogForm(Func<IInventoryGateway> gatewayFactory)
        {
            _gatewayFactory = gatewayFactory;
            Text = "Catálogos administrativos"; Width = 950; Height = 620; StartPosition = FormStartPosition.CenterParent;
            cmbType.Items.AddRange(new[] { "CATEGORY", "BRANCH", "PRODUCT" }); cmbType.SelectedIndex = 0;
            cmbType.SelectedIndexChanged += (sender, args) => { ClearForm(); LoadItems(); };
            dgvItems.SelectionChanged += DgvItems_SelectionChanged;

            Button btnNew = new Button { Text = "Nuevo", AutoSize = true };
            Button btnSave = new Button { Text = "Guardar", AutoSize = true };
            Button btnDeactivate = new Button { Text = "Desactivar", AutoSize = true };
            btnNew.Click += (sender, args) => ClearForm();
            btnSave.Click += BtnSave_Click;
            btnDeactivate.Click += BtnDeactivate_Click;

            FlowLayoutPanel form = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 125, AutoScroll = true };
            form.Controls.AddRange(new Control[] { Row("Tipo", cmbType), Row("Código/clave", txtCode), Row("Nombre", txtName), Row("Descripción", txtDescription), Row("Dirección", txtAddress), Row("Teléfono", txtPhone), Row("Categoría ID", txtCategoryId), Row("Precio", txtPrice), Row("Mínimo", txtMinimumStock), btnNew, btnSave, btnDeactivate });
            Controls.Add(dgvItems); Controls.Add(form);
            LoadItems();
        }

        private static Control Row(string label, Control input)
        {
            FlowLayoutPanel row = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, AutoSize = true, Margin = new Padding(6) };
            row.Controls.Add(new Label { Text = label, AutoSize = true }); row.Controls.Add(input); return row;
        }

        private void LoadItems()
        {
            try { dgvItems.DataSource = _gatewayFactory().GetCatalog(cmbType.Text); }
            catch (Exception ex) { MessageBox.Show(ex.Message, "Catálogos", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        private void DgvItems_SelectionChanged(object sender, EventArgs e)
        {
            if (!(dgvItems.CurrentRow?.DataBoundItem is CatalogItem item)) return;
            _selectedId = item.Id; txtCode.Text = item.Code; txtName.Text = item.Name; txtDescription.Text = item.Description;
            txtAddress.Text = item.Address; txtPhone.Text = item.Phone; txtCategoryId.Text = item.CategoryId == 0 ? string.Empty : item.CategoryId.ToString();
            txtPrice.Text = item.Price.ToString(CultureInfo.CurrentCulture); txtMinimumStock.Text = item.MinimumStock == 0 ? string.Empty : item.MinimumStock.ToString();
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (!decimal.TryParse(string.IsNullOrWhiteSpace(txtPrice.Text) ? "0" : txtPrice.Text, out decimal price) || !int.TryParse(string.IsNullOrWhiteSpace(txtCategoryId.Text) ? "0" : txtCategoryId.Text, out int categoryId) || !int.TryParse(string.IsNullOrWhiteSpace(txtMinimumStock.Text) ? "0" : txtMinimumStock.Text, out int minimumStock))
            { MessageBox.Show("Precio, categoría y mínimo deben ser números válidos."); return; }
            try
            {
                _gatewayFactory().SaveCatalog(cmbType.Text, new CatalogItem { Id = _selectedId, Code = txtCode.Text, Name = txtName.Text, Description = txtDescription.Text, Address = txtAddress.Text, Phone = txtPhone.Text, CategoryId = categoryId, Price = price, MinimumStock = minimumStock });
                ClearForm(); LoadItems();
            }
            catch (Exception ex) { MessageBox.Show(ex.Message, "No fue posible guardar", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        private void BtnDeactivate_Click(object sender, EventArgs e)
        {
            if (_selectedId == 0) return;
            try { _gatewayFactory().DeactivateCatalog(cmbType.Text, _selectedId); ClearForm(); LoadItems(); }
            catch (Exception ex) { MessageBox.Show(ex.Message, "No fue posible desactivar", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        private void ClearForm()
        {
            _selectedId = 0; txtCode.Clear(); txtName.Clear(); txtDescription.Clear(); txtAddress.Clear(); txtPhone.Clear(); txtCategoryId.Clear(); txtPrice.Clear(); txtMinimumStock.Clear(); dgvItems.ClearSelection();
        }
    }
}
