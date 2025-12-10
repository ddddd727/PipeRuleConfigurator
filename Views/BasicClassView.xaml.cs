using System.Windows;
using System.Windows.Controls;
using System.Data;
using PipeRuleConfigurator.ViewModels;
using PipeRuleConfigurator.Common; // 引用公共类

namespace PipeRuleConfigurator.Views
{
    public partial class BasicClassView : UserControl
    {
        public BasicClassView()
        {
            InitializeComponent();
            this.DataContextChanged += BasicClassView_DataContextChanged;
            if (this.DataContext is BasicClassViewModel vm) InjectInputProvider(vm);
        }

        private void BasicClassView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is BasicClassViewModel vm) InjectInputProvider(vm);
        }

        private void InjectInputProvider(BasicClassViewModel vm)
        {
            vm.InputProvider = (title) =>
            {
                // 直接使用公共弹窗类
                var dialog = new SimpleInputDialog(title);
                dialog.Owner = Window.GetWindow(this);
                if (dialog.ShowDialog() == true) return dialog.InputText;
                return null;
            };
        }

        private void DataGrid_BeginningEdit(object sender, DataGridBeginningEditEventArgs e)
        {
            // 复用之前的业务逻辑：禁用状态下不可编辑
            string colName = e.Column.Header as string;
            if (string.IsNullOrEmpty(colName)) colName = e.Column.SortMemberPath;

            if (colName == "状态") return;

            if (e.Row.Item is DataRowView drv)
            {
                string status = drv["状态"]?.ToString();
                if (status == "禁用") e.Cancel = true;
            }
        }
    }
}