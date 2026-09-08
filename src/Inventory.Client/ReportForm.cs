using System; using System.Windows.Forms; using Inventory.Contracts;
namespace Inventory.Client
{
 internal sealed class ReportForm:Form
 {
  private readonly Func<IInventoryGateway> _factory; private readonly ComboBox cmb=new ComboBox{DropDownStyle=ComboBoxStyle.DropDownList}; private readonly DateTimePicker start=new DateTimePicker(); private readonly DateTimePicker end=new DateTimePicker(); private readonly DataGridView grid=new DataGridView{Dock=DockStyle.Fill,ReadOnly=true,AutoGenerateColumns=true};
  public ReportForm(Func<IInventoryGateway> factory){_factory=factory;Text="Reportes";Width=760;Height=450;cmb.Items.AddRange(new[]{"SALES","LOWSTOCK","MOVEMENTS","TRANSFERS"});cmb.SelectedIndex=0;start.Value=DateTime.Today.AddDays(-30);end.Value=DateTime.Today;var button=new Button{Text="Generar",AutoSize=true};button.Click+=(s,e)=>{try{grid.DataSource=_factory().GetReport(cmb.Text,start.Value,end.Value);}catch(Exception ex){MessageBox.Show(ex.Message);}};var top=new FlowLayoutPanel{Dock=DockStyle.Top,AutoSize=true,Padding=new Padding(8)};top.Controls.AddRange(new Control[]{cmb,start,end,button});Controls.Add(grid);Controls.Add(top);}
 }
}
