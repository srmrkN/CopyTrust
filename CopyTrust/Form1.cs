using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using Gma.System.MouseKeyHook;
using Microsoft.Win32;

namespace CopyTrust
{
    public partial class Form1 : Form
    {
        private IKeyboardMouseEvents _globalHook;
        private string _lastClipboardText;
        private NotifyIcon _notifyIcon;
        private ToolStripMenuItem _autoStartMenuItem;

        public Form1()
        {
            InitializeComponent();

            // Инициализация иконки в трее
            _notifyIcon = new NotifyIcon
            {
                Icon = SystemIcons.Information, // Используем стандартную иконку
                Visible = true,
                Text = "CopyTrust"
            };

            // Контекстное меню для иконки в трее
            var contextMenu = new ContextMenuStrip();

            // Пункт для управления автозапуском
            _autoStartMenuItem = new ToolStripMenuItem("Автозапуск");
            _autoStartMenuItem.CheckOnClick = true;
            _autoStartMenuItem.Checked = IsAutoStartEnabled();
            _autoStartMenuItem.Click += ToggleAutoStart;
            contextMenu.Items.Add(_autoStartMenuItem);

            contextMenu.Items.Add("Выход", null, ExitApplication);
            _notifyIcon.ContextMenuStrip = contextMenu;

            // Инициализация глобального хука
            _globalHook = Hook.GlobalEvents();
            _globalHook.KeyDown += OnKeyDown;

            // Скрытие окна приложения
            this.WindowState = FormWindowState.Minimized;
            this.ShowInTaskbar = false;

            // Уведомление о запуске программы
            ShowNotification("CopyTrust", "Программа запущена и работает в фоновом режиме.");
        }

        private async void OnKeyDown(object sender, KeyEventArgs e)
        {
            // Проверяем, нажаты ли Ctrl + C
            if (e.Control && e.KeyCode == Keys.C)
            {
                // Ждем 100 мс, чтобы буфер обмена успел обновиться
                await Task.Delay(100);

                // Проверяем буфер обмена
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
            // Показываем уведомление в трее
            _notifyIcon.BalloonTipTitle = title;
            _notifyIcon.BalloonTipText = message;
            _notifyIcon.BalloonTipIcon = ToolTipIcon.None; // Отключаем системный звук
            _notifyIcon.ShowBalloonTip(1000); // Уведомление показывается 1 секунду
        }

        private void ToggleAutoStart(object sender, EventArgs e)
        {
            // Включаем или выключаем автозапуск
            SetAutoStart(_autoStartMenuItem.Checked);
        }

        private void ExitApplication(object sender, EventArgs e)
        {
            // Освобождаем ресурсы и закрываем приложение
            _globalHook.KeyDown -= OnKeyDown;
            _globalHook.Dispose();
            _notifyIcon.Visible = false;
            Application.Exit();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            // Освобождаем ресурсы при закрытии формы
            _globalHook.KeyDown -= OnKeyDown;
            _globalHook.Dispose();
            _notifyIcon.Dispose();
            base.OnFormClosed(e);
        }

        // Метод для управления автозапуском
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

        // Метод для проверки, включен ли автозапуск
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