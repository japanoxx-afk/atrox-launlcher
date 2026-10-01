using System;
using System.Linq;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Process.NET;
using Process.NET.Windows;
using Process.NET.Memory;
using Overlay.NET.Directx;

namespace AtroxLauncher
{
    partial class MainForm
    {
        [DllImport("User32.dll")]
        static extern bool RegisterHotKey(IntPtr hWnd, int id, KeyModifiers fsModifiers, Keys vk);

        [DllImport("User32.dll")]
        static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        const int HOTKEY_ID = 0x79DD;
        const int WM_HOTKEY = 0x0312;

        AtroxOverlayDrawer AtroxOverlayDrawer;

        protected override void WndProc(ref System.Windows.Forms.Message message)
        {
            switch (message.Msg)
            {
                case WM_HOTKEY:
                {
                    if (message.LParam.ToKeys() == Keys.F11)
                    {
                        ToggleOverlay();
                    }
                    break;
                }
            }
            base.WndProc(ref message);
        }

        void MainForm_Shown(object sender, EventArgs e)
        {
            RegisterHotKey(Handle, HOTKEY_ID, KeyModifiers.None, Keys.F11);
        }

        void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (AtroxOverlayDrawer != null)
            {
                DisposeOverlay();
            }
            UnregisterHotKey(Handle, HOTKEY_ID);
        }

        void ToggleOverlay()
        {
            if (!(AtroxOverlayDrawer?.IsEnabled ?? false))
            {
                StartOverlay();
            }
            else
            {
                StopOverlay();
            }
        }

        void StartOverlay()
        {
            if (AtroxOverlayDrawer?.IsEnabled ?? false) return;

            if (AtroxOverlayDrawer == null)
            {
                var process = System.Diagnostics.Process.GetProcessesByName("Atrox").FirstOrDefault();
                if (process == null) return;

                var processSharp = new ProcessSharp(process, MemoryType.Remote);
                processSharp.ProcessExited += (sender, e) =>
                {
                    DisposeOverlay();
                };

                AtroxOverlayDrawer = new AtroxOverlayDrawer(processSharp.WindowFactory.MainWindow, WindowModeCheckBox.CheckState == CheckState.Checked);
            }

            AtroxOverlayDrawer?.Enable();
            Task.Run(async () =>
            {
                while (AtroxOverlayDrawer?.IsEnabled ?? false)
                {
                    AtroxOverlayDrawer?.Update();
                    await Task.Delay(1000 / 60);
                }
            });
        }

        void StopOverlay()
        {
            if (!(AtroxOverlayDrawer?.IsEnabled ?? false)) return;
            AtroxOverlayDrawer?.Disable();
        }

        void DisposeOverlay()
        {
            if (AtroxOverlayDrawer == null) return;
            var drawer = AtroxOverlayDrawer;
            AtroxOverlayDrawer = null;
            drawer?.Dispose();
        }
    }

    enum KeyModifiers
    {
        None = 0,
        Alt = 1,
        Control = 2,
        Shift = 4,
        Windows = 8
    }

    static class IntPrtExtension
    {
        public static Keys ToKeys(this IntPtr thiz)
        {
            return (Keys)(((int)thiz >> 16) & 0xFFFF);
        }

        public static KeyModifiers ToKeyModifiers(this IntPtr thiz)
        {
            return (KeyModifiers)((int)thiz & 0xFFFF);
        }
    }

    class AtroxOverlayDrawer : DirectXOverlayPlugin
    {
        private bool HasBorder;
        private int Brush;

        public AtroxOverlayDrawer(IWindow targetWindow, bool hasBorder) : base()
        {
            Initialize(targetWindow);
            OverlayWindow = new DirectXOverlayWindow(targetWindow.Handle, false);
            HasBorder = hasBorder;
            Brush = OverlayWindow.Graphics.CreateBrush(0x7F000000);
        }

        public override void Enable()
        {
            if (IsEnabled) return;
            base.Enable();
        }

        public override void Disable()
        {
            if (!IsEnabled) return;
            ClearScreen();
            base.Disable();
        }

        public override void Dispose()
        {
            Disable();
            base.Dispose();
        }

        private void ClearScreen()
        {
            OverlayWindow.Graphics.BeginScene();
            {
                OverlayWindow.Graphics.ClearScene();
            }
            OverlayWindow.Graphics.EndScene();
        }

        public override void Update()
        {
            if (!TargetWindow.IsActivated && OverlayWindow.IsVisible)
            {
                ClearScreen();
                OverlayWindow.Hide();
            }
            else if (TargetWindow.IsActivated && !OverlayWindow.IsVisible)
            {
                OverlayWindow.Show();
            }

            OverlayWindow.Update();
            OverlayWindow.Graphics.BeginScene();
            {
                var borderSize = HasBorder ? 2 : 0;
                OverlayWindow.Graphics.ClearScene();
                OverlayWindow.Graphics.FillRectangle(borderSize, OverlayWindow.Height - 108, 241, 108 - borderSize, Brush);
                OverlayWindow.Graphics.FillRectangle(OverlayWindow.Width - 241 - borderSize, OverlayWindow.Height - 108, 241, 108 - borderSize, Brush);
            }
            OverlayWindow.Graphics.EndScene();
        }
    }
}
