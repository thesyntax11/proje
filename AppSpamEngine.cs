using System;
using System.Diagnostics;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

namespace ChaosVisualAudioSimulation
{
    /// <summary>
    /// UYGULAMA AÇMA MOTORU
    /// Kaos sırasında Windows'un yerleşik uygulamalarını açarak sistemi doldurur:
    ///   - Hava Durumu (msnweather:) ile başlar,
    ///   - 10-20 tarayıcı sekmesi,
    ///   - çok sayıda Not Defteri,
    ///   - Hesap Makinesi vb.
    /// Sekmeler/pencere'ler VM'yi aniden kilitlememek için kademeli açılır.
    ///
    /// GÜVENLİK: Yalnızca Process.Start ile yerleşik uygulamalar açılır.
    /// Hiçbir veri toplanmaz/gönderilmez, dosya/registry/sistem ayarı değişmez.
    /// Açılan uygulamalar normal şekilde kapatılabilir; kill-switch tüm
    /// uygulamayı sonlandırır.
    /// </summary>
    internal sealed class AppSpamEngine : IDisposable
    {
        private readonly Timer _timer;
        private readonly Random _rnd = new Random();

        private bool _started;
        private int _pendingTabs;
        private int _pendingPads;

        public AppSpamEngine(int intervalMs)
        {
            _timer = new Timer { Interval = intervalMs };
            _timer.Tick += OnTick;
        }

        public void Start() => _timer.Start();

        public void Stop() => _timer.Stop();

        private void OnTick(object? sender, EventArgs e)
        {
            if (!_started)
            {
                _started = true;
                // Hava durumundan başla, ardından sekme ve not defteri yığını hazırla.
                OpenWeather();
                OpenCalculator();
                _pendingTabs = _rnd.Next(AppConfig.BrowserTabMin, AppConfig.BrowserTabMax + 1);
                _pendingPads = _rnd.Next(AppConfig.NotepadMin, AppConfig.NotepadMax + 1);
                return;
            }

            // Tarayıcı sekmelerini kademeli aç (tek tick'te hepsini patlatma).
            if (_pendingTabs > 0)
            {
                int n = Math.Min(_pendingTabs, _rnd.Next(1, 4));
                for (int i = 0; i < n; i++)
                {
                    OpenBrowserTab();
                }
                _pendingTabs -= n;
            }

            // Not defterlerini kademeli aç.
            if (_pendingPads > 0)
            {
                OpenNotepad();
                _pendingPads--;
            }

            // Ayrıca rastgele ekstra uygulama.
            if (_rnd.Next(0, 3) == 0)
            {
                OpenRandomApp();
            }
        }

        private void OpenRandomApp()
        {
            switch (_rnd.Next(0, 4))
            {
                case 0: OpenNotepad(); break;
                case 1: OpenCalculator(); break;
                case 2: OpenWeather(); break;
                case 3: OpenBrowserTab(); break;
            }
        }

        private void OpenNotepad() => TryOpen("notepad.exe");
        private void OpenCalculator() => TryOpen("calc.exe");

        /// <summary>Windows Hava Durumu uygulaması (msnweather: protokolü).</summary>
        private void OpenWeather() => TryOpen("msnweather:");

        /// <summary>Varsayılan tarayıcıda yeni sekme açar (URL AppConfig'ten).</summary>
        private void OpenBrowserTab() => TryOpen(AppConfig.BrowserUrl);

        private void TryOpen(string target)
        {
            try
            {
                Process.Start(new ProcessStartInfo(target) { UseShellExecute = true });
            }
            catch
            {
                // Uygulama yoksa/bulunamazsa (örn. Weather kaldırılmış) sessizce geç.
            }
        }

        public void Dispose()
        {
            _timer.Tick -= OnTick;
            _timer.Dispose();
        }
    }
}
