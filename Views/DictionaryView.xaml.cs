using System;
using System.Windows;
using System.Windows.Controls;
using System.Data;
using PipeRuleConfigurator.ViewModels;
using PipeRuleConfigurator.Common;
using PipeRuleConfigurator.Models;

namespace PipeRuleConfigurator.Views
{
    public partial class DictionaryView : UserControl
    {
        public DictionaryView()
        {
            InitializeComponent();
            this.DataContextChanged += DictionaryView_DataContextChanged;
            if (this.DataContext is DictionaryViewModel vm) InjectInputProvider(vm);
        }

        private void DictionaryView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is DictionaryViewModel vm) InjectInputProvider(vm);
        }

        private void InjectInputProvider(DictionaryViewModel vm)
        {
            vm.InputProvider = (title) =>
            {
                var dialog = new SimpleInputDialog(title);
                dialog.Owner = Window.GetWindow(this);
                if (dialog.ShowDialog() == true) return dialog.InputText;
                return null;
            };
        }

        private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (this.DataContext is DictionaryViewModel vm)
            {
                if (e.NewValue is TreeItem selectedItem)
                {
                    vm.SelectedTreeItem = selectedItem;
                }
            }
        }

        private void DataGrid_BeginningEdit(object sender, DataGridBeginningEditEventArgs e)
        {
            string colName = e.Column.Header as string;
            // 处理 header 可能包含 * 号的情况
            if (string.IsNullOrEmpty(colName)) colName = e.Column.SortMemberPath;

            // "状态" 列永远允许编辑
            if (colName.StartsWith("状态")) return;

            if (e.Row.Item is DataRowView drv)
            {
                string status = drv["状态"]?.ToString();
                if (status == "禁用") e.Cancel = true;
            }
        }

        // 【新增】行编辑结束时触发校验
        private void DataGrid_RowEditEnding(object sender, DataGridRowEditEndingEventArgs e)
        {
            this.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (e.Row.Item is DataRowView drv && this.DataContext is DictionaryViewModel vm)
                {
                    vm.ValidateRow(drv.Row);
                }
            }), System.Windows.Threading.DispatcherPriority.ContextIdle);
        }
    }
}