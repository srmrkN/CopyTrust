using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using Gma.System.MouseKeyHook;
using Microsoft.Win32;

namespace CopyTrust
{
    public partial class Form1 : Form
    {
        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr intPtr, int nIndex);

        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TOOLWINDOW = 0x00000080;

        private IKeyboardMouseEvents _globalHook;
        private string _lastClipboardText;
        private NotifyIcon _notifyIcon;
        private ToolStripMenuItem _autoStartMenuItem;
        private bool _isPrivateModeEnabled = true;

        public Form1()
        {
            InitializeComponent();
            this.Load += Form1_Load;

            _notifyIcon = new NotifyIcon
            {
                Icon = SystemIcons.Information,
                Visible = true,
                Text = "CopyTrust"
            };

            var contextMenu = new ContextMenuStrip();

            var privateModeMenuItem = new ToolStripMenuItem("Приватный режим");
            privateModeMenuItem.CheckOnClick = true;
            privateModeMenuItem.Checked = _isPrivateModeEnabled;
            privateModeMenuItem.Click += TogglePrivateMode;
            contextMenu.Items.Add(privateModeMenuItem);

            _autoStartMenuItem = new ToolStripMenuItem("Автозапуск");
            _autoStartMenuItem.CheckOnClick = true;
            _autoStartMenuItem.Checked = IsAutoStartEnabled();
            _autoStartMenuItem.Click += ToggleAutoStart;
            contextMenu.Items.Add(_autoStartMenuItem);

            contextMenu.Items.Add("Выход", null, ExitApplication);
            _notifyIcon.ContextMenuStrip = contextMenu;

            _globalHook = Hook.GlobalEvents();
            _globalHook.KeyDown += OnKeyDown;

            this.WindowState = FormWindowState.Minimized;
            this.ShowInTaskbar = false;

            ShowNotification("CopyTrust", "Программа запущена и работает в фоновом режиме.");
        }

        private void TogglePrivateMode(object sender, EventArgs e)
        {
            _isPrivateModeEnabled = !_isPrivateModeEnabled;

            var menuItem = (ToolStripMenuItem)sender;

            menuItem.Checked = _isPrivateModeEnabled;

            ShowNotification("CopyTrust", _isPrivateModeEnabled ? "Включен приватный режим" : "Приватный режим выключен");
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            SetWindowLong(this.Handle, GWL_EXSTYLE, GetWindowLong(this.Handle, GWL_EXSTYLE) | WS_EX_TOOLWINDOW);
        }

        private async void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.C)
            {
                await Task.Delay(100);

                if (Clipboard.ContainsText())
                {
                    string currentText = Clipboard.GetText();

                    if (currentText != _lastClipboardText)
                    {
                        _lastClipboardText = currentText;
                        ShowNotification("Скопировано в буфер обмена", currentText);
                    }
                }
                else if (Clipboard.ContainsImage())
                {
                    ShowNotification("Скопировано в буфер обмена", "Изображение скопировано");
                }
            }
        }

        private void ShowNotification(string title, string message)
        {

            if (_isPrivateModeEnabled && message != "Программа запущена и работает в фоновом режиме." && message != "Включен приватный режим")
            {
                _notifyIcon.BalloonTipTitle = title;
                _notifyIcon.BalloonTipText = "Содержимое скрыто";
                _notifyIcon.BalloonTipIcon = ToolTipIcon.None;
                _notifyIcon.ShowBalloonTip(100);
            }
            else
            {
                _notifyIcon.BalloonTipTitle = title;
                _notifyIcon.BalloonTipText = message;
                _notifyIcon.BalloonTipIcon = ToolTipIcon.None;
                _notifyIcon.ShowBalloonTip(100);
            }
        }

        private void ToggleAutoStart(object sender, EventArgs e)
        {
            SetAutoStart(_autoStartMenuItem.Checked);
        }

        private void ExitApplication(object sender, EventArgs e)
        {
            _globalHook.KeyDown -= OnKeyDown;
            _globalHook.Dispose();
            _notifyIcon.Visible = false;
            Application.Exit();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _globalHook.KeyDown -= OnKeyDown;
            _globalHook.Dispose();
            _notifyIcon.Dispose();
            base.OnFormClosed(e);
        }

        public void SetAutoStart(bool enabled)
        {
            const string appName = "CopyTrust";
            const string runKey = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run";

            using (var key = Registry.CurrentUser.OpenSubKey(runKey, true))
            {
                if (enabled)
                {
                    key.SetValue(appName, Application.ExecutablePath);
                }
                else
                {
                    key.DeleteValue(appName, false);
                }
            }
        }

        private bool IsAutoStartEnabled()
        {
            const string appName = "CopyTrust";
            const string runKey = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run";

            using (var key = Registry.CurrentUser.OpenSubKey(runKey, false))
            {
                return key.GetValue(appName) != null;
            }
        }
    }
}