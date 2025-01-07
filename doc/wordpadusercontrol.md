以下是一个 WPF `UserControl` 的示例，包装了嵌入多个 WordPad 窗口并接管其菜单条的功能。这个 `UserControl` 提供了启动和管理 WordPad 窗口的功能，并将其嵌入到 WPF 窗口中。

### 1. 创建 UserControl.xaml

首先，在 WPF 项目中创建一个新的 `UserControl`，并将以下内容添加到 `UserControl.xaml` 和 `UserControl.xaml.cs` 中。

#### UserControl.xaml

```xml
<UserControl x:Class="WordPadEmbeddingDemo.WordPadEmbedControl"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             mc:Ignorable="d"
             Height="450" Width="800">
    <Grid Name="MainGrid">
        <!-- 这里可以添加 UI 元素以进行其他交互 -->
    </Grid>
</UserControl>
```

在这个 `UserControl.xaml` 中，我们使用了一个 `Grid` 来容纳嵌入的 WordPad 窗口。你可以根据需要添加其他 UI 控件来进行交互。

### 2. UserControl.xaml.cs 代码

接下来，在 `UserControl.xaml.cs` 中实现嵌入 WordPad 窗口的功能。

```csharp
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;

namespace WordPadEmbeddingDemo
{
    public partial class WordPadEmbedControl : UserControl
    {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern int SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        private const int GWL_STYLE = -16;
        private const int WS_CHILD = 0x40000000;
        private const int SW_HIDE = 0;
        private const uint WM_COMMAND = 0x0111;

        public WordPadEmbedControl()
        {
            InitializeComponent();
        }

        public void EmbedWordPad()
        {
            var process = Process.Start("wordpad.exe");
            process.WaitForInputIdle(); // 等待 WordPad 启动并准备好

            IntPtr wordPadHandle = process.MainWindowHandle;
            if (wordPadHandle != IntPtr.Zero)
            {
                IntPtr parentHandle = new WindowInteropHelper(Application.Current.MainWindow).Handle;
                SetParent(wordPadHandle, parentHandle); // 将 WordPad 窗口嵌入到当前 WPF 窗口

                int style = GetWindowLong(wordPadHandle, GWL_STYLE);
                SetWindowLong(wordPadHandle, GWL_STYLE, style | WS_CHILD); // 设置为子窗口

                // 隐藏菜单和工具栏
                HideWordPadMenus(wordPadHandle);
            }
        }

        private void HideWordPadMenus(IntPtr wordPadHandle)
        {
            IntPtr menuHandle = FindWindowEx(wordPadHandle, IntPtr.Zero, null, null); // 查找菜单或工具栏窗口
            if (menuHandle != IntPtr.Zero)
            {
                ShowWindow(menuHandle, SW_HIDE); // 隐藏菜单栏
            }
        }

        public void ExecuteWordPadCommand(IntPtr wordPadHandle, int commandId)
        {
            SendMessage(wordPadHandle, WM_COMMAND, (IntPtr)commandId, IntPtr.Zero); // 执行菜单命令
        }

        public void EmbedMultipleWordPads(int count)
        {
            for (int i = 0; i < count; i++) // 启动多个 WordPad 实例
            {
                EmbedWordPad();
            }
        }

        // 示例：在 UserControl 加载时自动嵌入 WordPad
        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            EmbedWordPad(); // 嵌入一个 WordPad 窗口
        }
    }
}
```

### 3. 说明

- `EmbedWordPad()` 方法启动一个 WordPad 实例并将其嵌入到 WPF 窗口中。
- `HideWordPadMenus()` 方法隐藏 WordPad 的菜单栏和工具栏。
- `ExecuteWordPadCommand()` 方法模拟菜单命令的执行，可以通过传递菜单项的 `commandId` 来实现。
- `EmbedMultipleWordPads()` 方法启动并嵌入多个 WordPad 实例。
- `UserControl_Loaded()` 是 `UserControl` 加载时的事件，自动调用 `EmbedWordPad` 启动并嵌入一个 WordPad 窗口。

### 4. 使用这个 `UserControl`

你可以在主窗口中使用这个 `UserControl`，如下所示：

#### MainWindow.xaml

```xml
<Window x:Class="WordPadEmbeddingDemo.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:local="clr-namespace:WordPadEmbeddingDemo"
        Title="WordPad Embed Demo" Height="450" Width="800">
    <Grid>
        <local:WordPadEmbedControl Name="WordPadControl" HorizontalAlignment="Stretch" VerticalAlignment="Stretch"/>
    </Grid>
</Window>
```

#### MainWindow.xaml.cs

```csharp
namespace WordPadEmbeddingDemo
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
    }
}
```

### 总结

这个 `UserControl` 允许你在 WPF 应用中嵌入 WordPad 窗口，并控制其显示和菜单。你可以轻松地通过 `EmbedWordPad()` 方法嵌入一个或多个 WordPad 窗口，同时隐藏其菜单栏和工具栏，甚至通过 `SendMessage` 接管菜单命令。
