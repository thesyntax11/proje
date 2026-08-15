using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Media;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

namespace ChaosVisualAudioSimulation
{
    /// <summary>
    /// JUMPSCARE MOTORU
    /// Rastgele aralıklarla, yalnızca 3-4 saniyelik "korku patlaması" üretir:
    ///   - Tam ekran korku görseli, içe doğru zoom + titreme/flaş ile,
    ///   - Çığlık benzeri yükselen beep + sistem sesi patlaması ile.
    ///
    /// DİĞER MOTORLARI DURDURMAZ: ana kaos animasyonu ve sesler aynen devam
    /// eder; jumpscare sadece kısa süreliğine üzerine binerek baskın gelir.
    /// Sürekli kalmaz (7/24 değildir) ve Matrix yağmuru gibi değildir.
    ///
    /// GÜVENLİK: Görsel yalnızca ekran DC'sine çizilir (kalıcı değildir).
    /// Dosya/ağ/registry erişimi yoktur.
    /// </summary>
    internal sealed class JumpscareEngine : IDisposable
    {
        private readonly Timer _timer;
        private readonly Random _rnd = new Random();
        private readonly int _screenW;
        private readonly int _screenH;

        private Image? _scareImage;
        private bool _scaring;
        private DateTime _scareStarted;
        private DateTime _nextScare;

        public JumpscareEngine(int intervalMs)
        {
            _screenW = NativeMethods.GetSystemMetrics(NativeMethods.SM_CXVIRTUALSCREEN);
            _screenH = NativeMethods.GetSystemMetrics(NativeMethods.SM_CYVIRTUALSCREEN);
            if (_screenW <= 0 || _screenH <= 0)
            {
                Rectangle primary = Screen.PrimaryScreen?.Bounds ?? new Rectangle(0, 0, 1920, 1080);
                _screenW = primary.Width;
                _screenH = primary.Height;
            }

            LoadScareImage();

            _nextScare = DateTime.UtcNow.AddSeconds(AppConfig.ScareFirstDelaySeconds);

            _timer = new Timer { Interval = intervalMs };
            _timer.Tick += OnTick;
        }

        public void Start() => _timer.Start();

        public void Stop() => _timer.Stop();

        // ==================================================================
        // KORKU GÖRSELİNİ GÖMÜLÜ KAYNAKTAN YÜKLE
        // Görsel dosyası: scare.png / scare.jpg (projeye EmbeddedResource
        // olarak eklenir). Bulunamazsa kırmızı/siyah glitch fallback çalışır.
        // ==================================================================
        private void LoadScareImage()
        {
            try
            {
                Assembly asm = Assembly.GetExecutingAssembly();
                foreach (string name in asm.GetManifestResourceNames())
                {
                    bool match = name.EndsWith("scare.png", StringComparison.OrdinalIgnoreCase)
                              || name.EndsWith("scare.jpg", StringComparison.OrdinalIgnoreCase)
                              || name.EndsWith("scare.jpeg", StringComparison.OrdinalIgnoreCase);

                    if (!match) continue;

                    using Stream s = asm.GetManifestResourceStream(name)!;
                    using var ms = new MemoryStream();
                    s.CopyTo(ms);
                    ms.Position = 0;

                    // Kopyalayarak stream'den bağımsız bir Bitmap üret.
                    using var tmp = Image.FromStream(ms);
                    _scareImage = new Bitmap(tmp);
                    return;
                }
            }
            catch
            {
                _scareImage = null; // fallback'e düş
            }
        }

        // ==================================================================
        // ZAMANLAYICI — her tick: ya bir sonraki scare'i bekle ya da animasyon
        // ==================================================================
        private void OnTick(object? sender, EventArgs e)
        {
            DateTime now = DateTime.UtcNow;

            if (!_scaring)
            {
                if (now >= _nextScare)
                {
                    BeginScare(now);
                }
                return;
            }

            AnimateScare(now);
        }

        private void BeginScare(DateTime now)
        {
            _scaring = true;
            _scareStarted = now;

            if (AppConfig.ScareScream)
            {
                PlayShriek();
            }
        }

        private void AnimateScare(DateTime now)
        {
            double elapsedMs = (now - _scareStarted).TotalMilliseconds;

            // 3-4 saniye doldu -> scare biter, bir sonraki için boşluk planla.
            if (elapsedMs >= AppConfig.ScareDurationMs)
            {
                _scaring = false;
                int gap = _rnd.Next(AppConfig.ScareMinGapSeconds, AppConfig.ScareMaxGapSeconds + 1);
                _nextScare = now.AddSeconds(gap);
                return;
            }

            double progress = elapsedMs / AppConfig.ScareDurationMs;

            IntPtr hdc = NativeMethods.GetDC(IntPtr.Zero);
            try
            {
                // Rastgele flaş: siyaha karartma / ters çevirme (dehşet hissi).
                int flash = _rnd.Next(5);
                if (flash == 0)
                {
                    NativeMethods.PatBlt(hdc, 0, 0, _screenW, _screenH, NativeMethods.ROP_BLACKNESS);
                }
                else if (flash == 1)
                {
                    NativeMethods.PatBlt(hdc, 0, 0, _screenW, _screenH, NativeMethods.ROP_DSTINVERT);
                }

                if (_scareImage != null)
                {
                    // Zoom: 1.0 -> 1.4 arası salınımlı (yüze doğru hamle hissi).
                    double zoom = 1.0 + 0.4 * Math.Abs(Math.Sin(progress * Math.PI * 3.0));
                    DrawImageCentered(hdc, zoom);
                }
                else
                {
                    DrawFallback(hdc);
                }
            }
            finally
            {
                NativeMethods.ReleaseDC(IntPtr.Zero, hdc);
            }
        }

        // ==================================================================
        // GÖRSEL ÇİZİM
        // ==================================================================
        private void DrawImageCentered(IntPtr hdc, double zoom)
        {
            using Graphics g = Graphics.FromHdc(hdc);
            g.InterpolationMode = InterpolationMode.Low; // hız için

            int w = (int)(_screenW * zoom);
            int h = (int)(_screenH * zoom);
            int x = (_screenW - w) / 2;
            int y = (_screenH - h) / 2;

            g.DrawImage(_scareImage!, x, y, w, h);
        }

        private void DrawFallback(IntPtr hdc)
        {
            // Görsel gömülü değilse: kırmızı/siyah glitch + tarama şeritleri.
            using Graphics g = Graphics.FromHdc(hdc);
            using var red = new SolidBrush(Color.FromArgb(180, 160, 0, 0));

            g.Clear(Color.Black);
            int lines = 18 + _rnd.Next(14);
            for (int i = 0; i < lines; i++)
            {
                int y = _rnd.Next(0, _screenH);
                int h = _rnd.Next(2, 70);
                g.FillRectangle(red, 0, y, _screenW, h);
            }
        }

        // ==================================================================
        // SES — çığlık benzeri yükselen beep + sistem sesleri patlaması
        // (UI thread'i kilitlememek için arka plana alınır)
        // ==================================================================
        private void PlayShriek()
        {
            Task.Run(() =>
            {
                try
                {
                    int freq = 500;
                    while (freq <= 2600)
                    {
                        Console.Beep(freq, 45);
                        freq += 150;
                    }
                    Console.Beep(3000, 130);

                    SystemSounds.Exclamation.Play();
                    SystemSounds.Hand.Play();
                }
                catch
                {
                    // Beep cihazı yoksa sessizce geç.
                }
            });
        }

        public void Dispose()
        {
            _timer.Tick -= OnTick;
            _timer.Dispose();
            _scareImage?.Dispose();
        }
    }
}
