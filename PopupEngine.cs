using System;
using System.Threading;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

namespace ChaosVisualAudioSimulation
{
    /// <summary>
    /// UYARI PENÇERESİ MOTORU — MEMZ tarzı, giderek agresifleşen sürüm.
    /// Ekranı iç içe binen sahte Windows hata/uyarı pencereleriyle doldurur;
    /// zamanla yoğunluk arttıkça daha çok ve daha korkutucu pencereler açılır.
    ///
    /// GÜVENLİK: Bu pencereler tamamen sanal/harmless'tır. Hiçbir dosya,
    /// kayıt defteri, ağ veya sistem ayarına dokunulmaz. Tek tek kapatılabilir
    /// ya da kill-switch (CTRL+SHIFT+ALT+K) ile tüm uygulama sonlandırılır.
    /// </summary>
    internal sealed class PopupEngine : IDisposable
    {
        private readonly Timer _timer;
        private readonly Random _rnd = new Random();
        private int _active;   // thread-safe sayaç
        private double _intensity; // zamanla 0 -> 1

        private static readonly MessageBoxIcon[] Icons =
        {
            MessageBoxIcon.Error,
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
            "SYSTEM_FAULT",
            "FATAL ERROR",
            "KERNEL PANIC",
            "MEMORY DUMP",
            "ÖNEMLİ!",
            "SON UYARI",
            "GÖREV BAŞARISIZ",
            "ÇIKIŞ YOK"
        };

        // Korkutucu mesajlar: başta hafif, ilerledikçe ürkütücü.
        private static readonly string[] Messages =
        {
            "Sistem çöküyor... Lütfen panik yapmayın. 😱",
            "Ekran kontrolü kaybedildi!",
            "Kritik hata: çok fazla hata penceresi açıldı.",
            "Mavi ekran yaklaşıyor... şaka, hiçbir şey olmuyor.",
            "Piksel eritici devrede!",
            "Kaos seviyesi: MAKSİMUM",

            // Korkutucu / agresif katman
            "KAÇIŞ YOK.",
            "I AM INSIDE YOUR SCREEN",
            "SİSTEMİN KONTROLÜ BİZDE",
            "HER ŞEY KAYBEDİLDİ",
            "BUNU SEN BAŞLATTIN",
            "GERİ DÖNÜŞ YOK",
            "DOSYALARIN... ŞAKA. HAYIR, CİDDİ.",
            "SENİ İZLİYORUM",
            "ÇIKIŞ YOK, SADECE KAOS VAR",
            "BENİ KAPATAMAZSIN"
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
            // Yoğunluk zamanla artar -> iç içe daha çok pencere.
            _intensity = Math.Min(1.0, _intensity + 0.02);

            int burst = _rnd.Next(1, 4) + (int)(_intensity * 6);
            for (int i = 0; i < burst; i++)
            {
                if (Volatile.Read(ref _active) >= AppConfig.MaxPopups) return;

                Interlocked.Increment(ref _active);
                var t = new Thread(ShowPopup)
                {
                    IsBackground = true,
                    Name = "ChaosPopup"
                };
                t.Start();
            }
        }

        private void ShowPopup()
        {
            try
            {
                var icon = Icons[_rnd.Next(Icons.Length)];
                string title = Titles[_rnd.Next(Titles.Length)];

                // Yoğunluk arttıkça korkutucu mesajların seçilme olasılığı artar.
                bool scary = _rnd.NextDouble() < _intensity;
                int idx;
                if (scary)
                {
                    idx = 6 + _rnd.Next(Messages.Length - 6); // korkutucu bölüm
                }
                else
                {
                    idx = _rnd.Next(6); // hafif bölüm
                }
                string msg = Messages[idx];

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
