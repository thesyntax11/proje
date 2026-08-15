using System;
using System.Threading;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

namespace ChaosVisualAudioSimulation
{
    /// <summary>
    /// UYARI PENÇERESİ MOTORU
    /// Ekranı sahte Windows hata/uyarı MessageBox pencereleriyle doldurur.
    /// Her pencere ayrı bir arka plan thread'inde açılır (UI thread kilitlenmez);
    /// aktif pencere sayısı sınırlandırılarak sistem sağlığı korunur.
    ///
    /// GÜVENLİK: Bu pencereler tamamen sanal/harmless'tır. Hiçbir dosya,
    /// kayıt defteri, ağ veya sistem ayarına dokunulmaz. Tek tek kapatılabilir
    /// ya da kill-switch (CTRL+SHIFT+ALT+K) ile tüm uygulama sonlandırılır.
    /// </summary>
    internal sealed class PopupEngine : IDisposable
    {
        private readonly Timer _timer;
        private readonly Random _rnd = new Random();
        private int _active; // thread-safe sayaç

        private static readonly MessageBoxIcon[] Icons =
        {
            MessageBoxIcon.Error,
            MessageBoxIcon.Error,
            MessageBoxIcon.Warning,
            MessageBoxIcon.Warning,
            MessageBoxIcon.Question,
            MessageBoxIcon.Information
        };

        private static readonly string[] Titles =
        {
            "Kritik Sistem Hatası",
            "DİKKAT!",
            "Uyarı",
            "Bellek Yetersiz",
            "Ekran sürücüsü durdu",
            "SYSTEM_FAULT",
            "FATAL ERROR",
            "ÖNEMLİ!"
        };

        private static readonly string[] Messages =
        {
            "Sistem çöküyor... Lütfen panik yapmayın. 😱",
            "Ekran kontrolü kaybedildi!",
            "Grafik kartı aşırı ısındı (şaka şaka).",
            "Bellek yetersiz: yeterince kaos var!",
            "Kritik hata: çok fazla hata penceresi açıldı.",
            "Mavi ekran yaklaşıyor... şaka, hiçbir şey olmuyor.",
            "Fare imleci nerede? Aramıyoruz, sadece şov yapıyoruz.",
            "Bu bir tatbikattır. Gerçek bir hata değildir.",
            "Piksel eritici devrede!",
            "Kaos seviyesi: MAKSİMUM"
        };

        public PopupEngine(int intervalMs)
        {
            _timer = new Timer { Interval = intervalMs };
            _timer.Tick += OnTick;
        }

        public void Start() => _timer.Start();

        public void Stop() => _timer.Stop();

        private void OnTick(object? sender, EventArgs e)
        {
            // Aktif pencere sınırını aşma; ekranı doldur ama sistemi boğma.
            if (Volatile.Read(ref _active) >= AppConfig.MaxPopups) return;

            Interlocked.Increment(ref _active);
            var t = new Thread(ShowPopup)
            {
                IsBackground = true,
                Name = "ChaosPopup"
            };
            t.Start();
        }

        private void ShowPopup()
        {
            try
            {
                var icon = Icons[_rnd.Next(Icons.Length)];
                string title = Titles[_rnd.Next(Titles.Length)];
                string msg = Messages[_rnd.Next(Messages.Length)];

                MessageBox.Show(
                    msg, title,
                    MessageBoxButtons.OK, icon,
                    MessageBoxDefaultButton.Button1,
                    MessageBoxOptions.DefaultDesktopOnly);
            }
            catch
            {
                // pencere oluşturulamazsa sessizce geç
            }
            finally
            {
                Interlocked.Decrement(ref _active);
            }
        }

        public void Dispose()
        {
            _timer.Tick -= OnTick;
            _timer.Dispose();
        }
    }
}
