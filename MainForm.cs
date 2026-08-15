using System;
using System.Drawing;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

namespace ChaosVisualAudioSimulation
{
    /// <summary>
    /// ANA FORM — tam ekran, GÖRÜNMEZ (TransparencyKey), görev çubuğunda ve
    /// Alt+Tab'da görünmeyen, tıklama-geçirgen overlay.
    ///
    /// Görsel efektler doğrudan masaüstü DC'sine (GetDC(NULL)) çizilir; bu form
    /// şeffaf olduğu için efektler masaüstü üzerinde görünür. Form yalnızca:
    ///   (1) gizli kill-switch hotkey'ini (CTRL+SHIFT+ALT+K) dinler,
    ///   (2) Alt+F4 / X butonu ile kapanmayı engeller,
    ///   (3) kaos motorlarını barındırır ve zamanlar.
    /// </summary>
    public sealed class MainForm : Form
    {
        private const int HotKeyId = 0xC0DE;

        private readonly VisualEngine _visual = new VisualEngine(AppConfig.VisualIntervalMs);
        private readonly AudioEngine _audio = new AudioEngine();
        private readonly JumpscareEngine _jumpscare = new JumpscareEngine(intervalMs: 30);
        private readonly PopupEngine _popups = new PopupEngine(AppConfig.PopupIntervalMs);
        private readonly Timer _startupTimer;
        private bool _shuttingDown;

        public MainForm()
        {
            ConfigureWindow();

            // Başlangıç gecikmesi: operatöre hazırlanma süresi.
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
            ShowInTaskbar = false;          // Görev çubuğunda gizli
            TopMost = true;
            StartPosition = FormStartPosition.Manual;

            // Formu tamamen görünmez yap: arka plan rengi = şeffaflık anahtarı.
            // Böylece masaüstüne çizilen efektler overlay'in arkasında değil,
            // üstünde/net biçimde görünür.
            BackColor = Color.Black;
            TransparencyKey = Color.Black;

            // Tüm sanal ekranı (çoklu monitör dahil) kapla.
            int x = NativeMethods.GetSystemMetrics(NativeMethods.SM_XVIRTUALSCREEN);
            int y = NativeMethods.GetSystemMetrics(NativeMethods.SM_YVIRTUALSCREEN);
            int w = Math.Max(1, NativeMethods.GetSystemMetrics(NativeMethods.SM_CXVIRTUALSCREEN));
            int h = Math.Max(1, NativeMethods.GetSystemMetrics(NativeMethods.SM_CYVIRTUALSCREEN));
            Bounds = new Rectangle(x, y, w, h);

            // Kapanma engelleri + yedek klavye dinleyicisi.
            FormClosing += OnFormClosing;
            KeyDown += OnKeyDown;
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                // Alt+Tab'da gizle.
                cp.ExStyle |= NativeMethods.WS_EX_TOOLWINDOW;
                // Tıklamaları alttaki pencereye geçir (zararsız overlay).
                cp.ExStyle |= NativeMethods.WS_EX_TRANSPARENT;
                return cp;
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            // Kill-switch hotkey'ini sisteme kaydet (uygulama odakta olmasa bile çalışır).
            NativeMethods.RegisterHotKey(Handle, HotKeyId, AppConfig.KillModifiers, AppConfig.KillVirtualKey);
        }

        // ==================================================================
        // BAŞLANGIÇ / KAPANIŞ
        // ==================================================================
        private void OnStartupTick(object? sender, EventArgs e)
        {
            _startupTimer.Stop();
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
        }

        /// <summary>
        /// Alt+F4 ve normal kapanışı engelle. Sadece kill-switch ile çıkılır.
        /// </summary>
        private void OnFormClosing(object? sender, FormClosingEventArgs e)
        {
            if (_shuttingDown)
            {
                e.Cancel = false; // acil kapanışa izin ver
                return;
            }
            e.Cancel = true; // normal kapanış tamamen engellenir
        }

        // ==================================================================
        // KILL-SWITCH
        // ==================================================================
        private void OnKeyDown(object? sender, KeyEventArgs e)
        {
            // Form odaklıyken de aynı kombinasyon yedek olarak çalışsın.
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
                return; // mesajı yut
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

                // Ekranı anında ilk haline döndür.
                _visual.RestoreScreen();

                NativeMethods.UnregisterHotKey(Handle, HotKeyId);
            }
            catch
            {
                // Temizlik hataları kapanışı engellemesin.
            }

            Close(); // _shuttingDown=true olduğu için FormClosing engellenmez.
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _visual.Dispose();
                _audio.Dispose();
                _jumpscare.Dispose();
                _popups.Dispose();
                _startupTimer.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
