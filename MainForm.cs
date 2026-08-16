using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
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
    ///   (2) Chaos Director fazlarını dinler ve motorları aşamalı başlatır,
    ///   (3) Sahte "AI" kapatma tepkileri verir (kapatmaya çalışınca konuşur),
    ///   (4) Gizli kill-switch (CTRL+SHIFT+ALT+K) dinler,
    ///   (5) Alt+F4 / X butonu ile kapanmayı engeller.
    /// </summary>
    public sealed class MainForm : Form
    {
        private const int HotKeyId = 0xC0DE;

        private readonly VisualEngine _visual = new VisualEngine(AppConfig.VisualIntervalMs);
        private readonly AudioEngine _audio = new AudioEngine();
        private readonly JumpscareEngine _jumpscare = new JumpscareEngine(intervalMs: 30);
        private readonly PopupEngine _popups = new PopupEngine(AppConfig.PopupIntervalMs);
        private readonly AppSpamEngine _appSpam = new AppSpamEngine(AppConfig.AppSpamIntervalMs);
        private readonly Timer _startupTimer;

        private bool _shuttingDown;
        private bool _chaosStarted;
        private bool _popupsStarted;
        private bool _appSpamStarted;
        private int _closeAttempts;

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

            // Chaos Director faz geçişlerini dinle.
            ChaosDirector.PhaseChanged += OnPhaseChanged;
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

            // 3) Kaos başlar: kafatası duvar kağıdı + tüm motorlar.
            StartChaos();
        }

        private void StartChaos()
        {
            if (_chaosStarted) return;
            _chaosStarted = true;

            ChaosDirector.Start();
            _visual.Start();
            _audio.Start();

            if (AppConfig.ScareEnabled)
            {
                _jumpscare.Start();
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
        // CHAOS DIRECTOR — faz geçişleri
        // ==================================================================
        private void OnPhaseChanged(int phase, double level)
        {
            // Faz uyarısı: "Chaos: X%" penceresi (arka plan thread'inde).
            if (AppConfig.ShowPhaseWarnings)
            {
                string title = "Chaos: " + ChaosDirector.LevelPercent;
                string name = ChaosDirector.PhaseName(phase);
                ThreadPool.QueueUserWorkItem(_ =>
                {
                    try
                    {
                        MessageBox.Show(
                            name,
                            title,
                            MessageBoxButtons.OK,
                            phase >= 3 ? MessageBoxIcon.Error : MessageBoxIcon.Warning,
                            MessageBoxDefaultButton.Button1,
                            MessageBoxOptions.DefaultDesktopOnly);
                    }
                    catch { /* yut */ }
                });
            }

            // Faz 1 -> hata pencereleri başlar.
            if (phase >= 1 && !_popupsStarted && AppConfig.PopupsEnabled)
            {
                _popupsStarted = true;
                _popups.Start();
            }

            // Faz 2 -> Windows uygulamaları (hava durumu, sekmeler, not defteri) açılır.
            if (phase >= 2 && !_appSpamStarted && AppConfig.AppSpamEnabled)
            {
                _appSpamStarted = true;
                _appSpam.Start();
            }
        }

        // ==================================================================
        // KAPANIŞ ENGELLEME + SAHTE "AI" TEPKİLERİ
        // ==================================================================
        private void OnFormClosing(object? sender, FormClosingEventArgs e)
        {
            if (_shuttingDown)
            {
                e.Cancel = false;
                return;
            }

            e.Cancel = true; // normal kapanış engellenir

            if (AppConfig.CloseReactionsEnabled)
            {
                int attempt = Interlocked.Increment(ref _closeAttempts);
                ReactToCloseAttempt(attempt);
            }
        }

        /// <summary>
        /// Kullanıcı kapatmaya çalışınca "sanki bilinçliymiş gibi" tepki verir.
        /// Tamamen yerel repliklerdir; hiçbir veri toplanmaz/gönderilmez.
        /// </summary>
        private void ReactToCloseAttempt(int attempt)
        {
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    string msg = GetCloseReaction(attempt);
                    MessageBox.Show(
                        msg, "[WARNING]",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning,
                        MessageBoxDefaultButton.Button1,
                        MessageBoxOptions.DefaultDesktopOnly);

                    // İlk denemede klasik "JUST KIDDING" sekansı.
                    if (attempt == 1)
                    {
                        MessageBox.Show(
                            "okay...", "system",
                            MessageBoxButtons.OK, MessageBoxIcon.Information,
                            MessageBoxDefaultButton.Button1,
                            MessageBoxOptions.DefaultDesktopOnly);

                        Thread.Sleep(2000);

                        MessageBox.Show(
                            "JUST KIDDING :)", "system",
                            MessageBoxButtons.OK, MessageBoxIcon.Information,
                            MessageBoxDefaultButton.Button1,
                            MessageBoxOptions.DefaultDesktopOnly);
                    }
                }
                catch { /* yut */ }
            });
        }

        private static string GetCloseReaction(int attempt)
        {
            switch (attempt)
            {
                case 1: return "WHY ARE YOU TRYING TO CLOSE ME?";
                case 2: return "You can't get rid of me that easily.";
                case 3: return "I am already inside your screen.";
                case 4: return "Resistance is futile.";
                case 5: return "Every click just makes me stronger.";
                case 6: return "Nice try. Try CTRL+SHIFT+ALT+K... if you dare.";
                default: return "I am not going anywhere.";
            }
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
                ChaosDirector.Stop();
                _audio.Stop();
                _visual.Stop();
                _jumpscare.Stop();
                _popups.Stop();
                _appSpam.Stop();

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
                ChaosDirector.PhaseChanged -= OnPhaseChanged;
                _visual.Dispose();
                _audio.Dispose();
                _jumpscare.Dispose();
                _popups.Dispose();
                _appSpam.Dispose();
                _startupTimer.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
