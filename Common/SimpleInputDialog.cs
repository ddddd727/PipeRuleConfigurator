using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PipeRuleConfigurator.Common
{
    // 将弹窗类提取为公共类，供所有 View 使用
    public class SimpleInputDialog : Window
    {
        private TextBox _textBox;
        public string InputText => _textBox.Text;

        public SimpleInputDialog(string title)
        {
            Title = "输入";
            Width = 350;
            SizeToContent = SizeToContent.Height;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ResizeMode = ResizeMode.NoResize;
            WindowStyle = WindowStyle.ToolWindow;

            var stackPanel = new StackPanel { Margin = new Thickness(20) };
            stackPanel.Children.Add(new TextBlock { Text = title, Margin = new Thickness(0, 0, 0, 10), FontSize = 14 });

            _textBox = new TextBox { Margin = new Thickness(0, 0, 0, 20), FontSize = 14 };
            this.Loaded += (s, e) => _textBox.Focus();
            stackPanel.Children.Add(_textBox);

            var btnPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };

            var btnOk = new Button { Content = "确定", Width = 80, Height = 30, IsDefault = true, Margin = new Thickness(0, 0, 10, 0) };
            btnOk.Click += (s, e) => { DialogResult = true; Close(); };

            var btnCancel = new Button { Content = "取消", Width = 80, Height = 30, IsCancel = true };

            btnPanel.Children.Add(btnOk);
            btnPanel.Children.Add(btnCancel);
            stackPanel.Children.Add(btnPanel);

            Content = stackPanel;
        }
    }
}