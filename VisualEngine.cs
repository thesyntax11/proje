using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

namespace ChaosVisualAudioSimulation
{
    /// <summary>
    /// GÖRSEL MOTORU — yoğunlaştırılmış sürüm.
    /// Her tick'te birden fazla rastgele "şok" efekti (invert / shake / glitch /
    /// melter / renk flaşı / ikon fırtınası) + sürekli efektler (tünel, ikon
    /// spam, pencere zıplatma) çalıştırılır. Böylece ekran sürekli "çıldırır".
    ///
    /// Tüm çizimler doğrudan masaüstü DC'sine yapılır; uygulama durdurulduğunda
    /// <see cref="RestoreScreen"/> ile ekran anında ilk haline döndürülür.
    /// </summary>
    internal sealed class VisualEngine : IDisposable
    {
        private readonly Timer _timer;
        private readonly Random _rnd = new Random();
        private readonly int _screenW;
        private readonly int _screenH;

        // Sürekli efektlerin yumuşak durumları
        private double _tunnelScale = 1.0;
        private int _tunnelDirection = -1;

        // Zamanla 0 -> 1'e tırmanan yoğunluk: kaos giderek BİRİKİR ve çıldırır.
        private double _intensity;

        // İmleç izi için önceki konum (hata logosu çizimi).
        private int _lastCursorX = int.MinValue;
        private int _lastCursorY = int.MinValue;

        // Rastgele kaydırılan ikonlar için yerleşik ikon kimlikleri
        private static readonly int[] IconIds =
        {
            NativeMethods.IDI_ERROR,
            NativeMethods.IDI_QUESTION,
            NativeMethods.IDI_WARNING,
            NativeMethods.IDI_INFORMATION,
            NativeMethods.IDI_SHIELD
        };

        // Aşağı akan renkli akıntılar için hazır neon fırçalar (tahsis önlenir).
        private static readonly SolidBrush[] FlowBrushes = BuildFlowBrushes();

        // Korkutucu ekran mesajları (MEMZ tarzı, ekrana yazılan metinler).
        private static readonly string[] ScaryTexts =
        {
            "I AM INSIDE YOUR SCREEN",
            "THERE IS NO ESCAPE",
            "CHAOS IS ETERNAL",
            "SYSTEM CORRUPTED",
            "ERROR: YOU",
            "RUN WHILE YOU CAN",
            "IT IS TOO LATE",
            "YOU CANNOT CLOSE ME",
            "KAÇIŞ YOK",
            "SENİ İZLİYORUM",
            "HER ŞEY BİTTİ",
            "GERİ DÖNÜŞ YOK"
        };

        private static readonly Color[] ScaryColors =
        {
            Color.Red, Color.FromArgb(255, 255, 0, 0), Color.White,
            Color.Lime, Color.FromArgb(255, 255, 30, 30)
        };

        private static SolidBrush[] BuildFlowBrushes()
        {
            var colors = new Color[]
            {
                Color.Red, Color.Lime, Color.Cyan, Color.Yellow,
                Color.Magenta, Color.Orange, Color.HotPink, Color.DodgerBlue,
                Color.LawnGreen, Color.Turquoise, Color.Violet, Color.Gold,
                Color.DeepSkyBlue, Color.SpringGreen, Color.Coral, Color.MediumPurple
            };
            var brushes = new SolidBrush[colors.Length];
            for (int i = 0; i < colors.Length; i++)
            {
                brushes[i] = new SolidBrush(colors[i]);
            }
            return brushes;
        }

        public VisualEngine(int intervalMs)
        {
            _screenW = NativeMethods.GetSystemMetrics(NativeMethods.SM_CXVIRTUALSCREEN);
            _screenH = NativeMethods.GetSystemMetrics(NativeMethods.SM_CYVIRTUALSCREEN);
            if (_screenW <= 0 || _screenH <= 0)
            {
                Rectangle primary = Screen.PrimaryScreen?.Bounds ?? new Rectangle(0, 0, 1920, 1080);
                _screenW = primary.Width;
                _screenH = primary.Height;
            }

            _timer = new Timer { Interval = intervalMs };
            _timer.Tick += OnTick;
        }

        public void Start()
        {
            BlackenScreen(); // kara zemin: ekranı baştan siyaha boya
            _timer.Start();
        }

        public void Stop() => _timer.Stop();

        /// <summary>
        /// Tek bir kaos dilimi. UI thread üzerinde, Timer.Tick içinden çağrılır.
        /// </summary>
        private void OnTick(object? sender, EventArgs e)
        {
            // Yoğunluğu HIZLA artır: ekran giderek çıldırır.
            _intensity = Math.Min(1.0, _intensity + 0.008);

            // ---- ANA EFEKT: kara duvar kağıdı + aşağı akan renkli akıntılar ----
            FlowDown();

            // ---- Rastgele şok efektleri — yoğunlukla birlikte çoğalır ----
            int shocks = _rnd.Next(4, 8) + (int)(_intensity * 8);
            for (int i = 0; i < shocks; i++)
            {
                ApplyShock();
            }

            // ---- Sürekli efektler ----
            DrawTunnel();
            SpawnIcons();
            CursorTrail();   // fare, hata logoları çizerek iz bırakır
            JumpWindows();

            // İkon fırtınası: yoğunluk arttıkça çok daha sık.
            if (_rnd.NextDouble() < 0.06 + _intensity * 0.25)
            {
                IconStorm();
            }

            // Korkutucu ekran mesajı: yoğunluk arttıkça sıklaşır.
            if (_rnd.NextDouble() < 0.01 + _intensity * 0.05)
            {
                ScaryText();
            }
        }

        private void ApplyShock()
        {
            switch (_rnd.Next(0, 7))
            {
                case 0: InvertScreen(); break;
                case 1: ShakeScreen(); break;
                case 2: GlitchScreen(); break;
                case 3: MeltScreen(); break;
                case 4: ColorFlash(); break;
                case 5: InvertScreen(); ShakeScreen(); break;
                case 6: GlitchScreen(); ColorFlash(); break;
            }
        }

        // ==================================================================
        // EFEKT: KORKUTUCU EKRAN MESAJI — MEMZ tarzı ekrana yazılan metin
        // ==================================================================
        private void ScaryText()
        {
            IntPtr hdc = NativeMethods.GetDC(IntPtr.Zero);
            try
            {
                using Graphics g = Graphics.FromHdc(hdc);
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

                string text = ScaryTexts[_rnd.Next(ScaryTexts.Length)];
                Color color = ScaryColors[_rnd.Next(ScaryColors.Length)];
                float size = 28f + (float)(_rnd.NextDouble() * 70);

                using var font = new Font("Consolas", size, FontStyle.Bold);
                using var brush = new SolidBrush(color);

                SizeF sz = g.MeasureString(text, font);
                int x = _rnd.Next(0, Math.Max(1, _screenW - (int)sz.Width));
                int y = _rnd.Next(0, Math.Max(1, _screenH - (int)sz.Height));

                // Ara sıra eğik (dönmüş) metin — daha kaotik.
                float angle = _rnd.NextDouble() < 0.4 ? (float)(_rnd.Next(-45, 46)) : 0f;

                if (angle != 0f)
                {
                    g.TranslateTransform(x + sz.Width / 2, y + sz.Height / 2);
                    g.RotateTransform(angle);
                    g.DrawString(text, font, brush, -sz.Width / 2, -sz.Height / 2);
                    g.ResetTransform();
                }
                else
                {
                    g.DrawString(text, font, brush, x, y);
                }
            }
            finally
            {
                NativeMethods.ReleaseDC(IntPtr.Zero, hdc);
            }
        }

        // ==================================================================
        // EFEKT 1: INVERT COLORS — PatBlt(DSTINVERT)
        // ==================================================================
        private void InvertScreen()
        {
            IntPtr hdc = NativeMethods.GetDC(IntPtr.Zero);
            try
            {
                int y = _rnd.Next(0, 3) == 0 ? 0 : _rnd.Next(0, _screenH);
                int h = _rnd.Next(0, 3) == 0 ? _screenH : _rnd.Next(_screenH / 8, _screenH / 2);
                NativeMethods.PatBlt(hdc, 0, y, _screenW, Math.Min(h, _screenH - y), NativeMethods.ROP_DSTINVERT);
            }
            finally
            {
                NativeMethods.ReleaseDC(IntPtr.Zero, hdc);
            }
        }

        // ==================================================================
        // EFEKT 2: SCREEN SHAKE — daha şiddetli kayma
        // ==================================================================
        private void ShakeScreen()
        {
            int mag = 1 + (int)(_intensity * 2);
            int dx = _rnd.Next(-120 * mag, 121 * mag);
            int dy = _rnd.Next(-90 * mag, 91 * mag);
            if (dx == 0 && dy == 0) return;

            IntPtr hdc = NativeMethods.GetDC(IntPtr.Zero);
            try
            {
                int xSrc = Math.Max(0, -dx);
                int ySrc = Math.Max(0, -dy);
                int w = _screenW - Math.Abs(dx);
                int h = _screenH - Math.Abs(dy);
                int xDst = Math.Max(0, dx);
                int yDst = Math.Max(0, dy);

                NativeMethods.BitBlt(hdc, xDst, yDst, w, h, hdc, xSrc, ySrc, NativeMethods.ROP_SRCCOPY);
            }
            finally
            {
                NativeMethods.ReleaseDC(IntPtr.Zero, hdc);
            }
        }

        // ==================================================================
        // EFEKT 3: GLITCH — daha çok şerit, daha geniş kayma
        // ==================================================================
        private void GlitchScreen()
        {
            IntPtr hdc = NativeMethods.GetDC(IntPtr.Zero);
            try
            {
                int strips = _rnd.Next(6, 16) + (int)(_intensity * 16);
                for (int i = 0; i < strips; i++)
                {
                    int x = _rnd.Next(0, _screenW);
                    int stripW = _rnd.Next(_screenW / 25, _screenW / 5);
                    int shift = _rnd.Next(-160 - (int)(_intensity * 160), 161 + (int)(_intensity * 160));
                    int y = _rnd.Next(0, _screenH);
                    int h = _rnd.Next(_screenH / 25, _screenH / 5);

                    int xSrc = Math.Max(0, -shift) + x;
                    NativeMethods.BitBlt(
                        hdc, x + shift, y, stripW, h,
                        hdc, Math.Min(xSrc, _screenW - 1), y, NativeMethods.ROP_SRCCOPY);
                }
            }
            finally
            {
                NativeMethods.ReleaseDC(IntPtr.Zero, hdc);
            }
        }

        // ==================================================================
        // EFEKT 4: PIXEL MELTER — şeritleri aşağı akıt
        // ==================================================================
        private void MeltScreen()
        {
            IntPtr hdc = NativeMethods.GetDC(IntPtr.Zero);
            try
            {
                int columns = _rnd.Next(8, 20) + (int)(_intensity * 20);
                for (int i = 0; i < columns; i++)
                {
                    int x = _rnd.Next(0, _screenW);
                    int w = _rnd.Next(_screenW / 30, _screenW / 8);
                    int srcY = _rnd.Next(0, _screenH);
                    int h = _rnd.Next(_screenH / 8, _screenH / 2);
                    int drop = _rnd.Next(15, 140);

                    NativeMethods.BitBlt(
                        hdc, x, Math.Min(srcY + drop, _screenH - h), w, Math.Max(1, h - drop),
                        hdc, x, srcY, NativeMethods.ROP_SRCCOPY);
                }
            }
            finally
            {
                NativeMethods.ReleaseDC(IntPtr.Zero, hdc);
            }
        }

        // ==================================================================
        // EFEKT 5: COLOR FLASH — rastgele yarı saydam renk patlaması
        // ==================================================================
        private void ColorFlash()
        {
            IntPtr hdc = NativeMethods.GetDC(IntPtr.Zero);
            try
            {
                // Dikdörtgen bant kaldırıldı: yalnızca tam ekran renk flaşı.
                using Graphics g = Graphics.FromHdc(hdc);
                Color c = Color.FromArgb(
                    _rnd.Next(40, 150),
                    _rnd.Next(256),
                    _rnd.Next(256),
                    _rnd.Next(256));
                using var b = new SolidBrush(c);
                g.FillRectangle(b, 0, 0, _screenW, _screenH);
            }
            finally
            {
                NativeMethods.ReleaseDC(IntPtr.Zero, hdc);
            }
        }

        // ==================================================================
        // EFEKT: KARA DUVAR KAĞIDI — ekranı tamamen siyaha boya
        // ==================================================================
        private void BlackenScreen()
        {
            IntPtr hdc = NativeMethods.GetDC(IntPtr.Zero);
            try
            {
                NativeMethods.PatBlt(hdc, 0, 0, _screenW, _screenH, NativeMethods.ROP_BLACKNESS);
            }
            finally
            {
                NativeMethods.ReleaseDC(IntPtr.Zero, hdc);
            }
        }

        // ==================================================================
        // EFEKT: FLOW DOWN — ekran sürekli aşağı akar; üstten renkli akıntılar
        // süzülür (kara zemin üzerinde renkli yağmur hissi)
        // ==================================================================
        private void FlowDown()
        {
            IntPtr hdc = NativeMethods.GetDC(IntPtr.Zero);
            try
            {
                // Tüm ekranı aşağı kaydır -> her şey aşağı akar.
                int drop = 5 + (int)(_intensity * 15);
                NativeMethods.BitBlt(hdc, 0, drop, _screenW, _screenH - drop,
                    hdc, 0, 0, NativeMethods.ROP_SRCCOPY);

                // Üst şeridi siyaha boya (kara duvar kağıdı).
                NativeMethods.PatBlt(hdc, 0, 0, _screenW, drop, NativeMethods.ROP_BLACKNESS);

                // Üstten aşağı süzülen renkli dikey akıntılar.
                using Graphics g = Graphics.FromHdc(hdc);
                int streaks = 30 + (int)(_intensity * 120);
                for (int i = 0; i < streaks; i++)
                {
                    int x = _rnd.Next(0, _screenW);
                    int w = _rnd.Next(2, 14);
                    int h = _rnd.Next(_screenH / 8, _screenH / 3);
                    SolidBrush brush = FlowBrushes[_rnd.Next(FlowBrushes.Length)];
                    g.FillRectangle(brush, x, 0, w, h);
                }
            }
            finally
            {
                NativeMethods.ReleaseDC(IntPtr.Zero, hdc);
            }
        }

        // ==================================================================
        // EFEKT 5: TUNNEL / ZOOM — ekranı kendi üzerine küçülen çerçevelerle çiz
        // ==================================================================
        private void DrawTunnel()
        {
            _tunnelScale += 0.06 * _tunnelDirection;
            if (_tunnelScale <= 0.5) { _tunnelScale = 0.5; _tunnelDirection = 1; }
            else if (_tunnelScale >= 1.0) { _tunnelScale = 1.0; _tunnelDirection = -1; }

            IntPtr hdc = NativeMethods.GetDC(IntPtr.Zero);
            IntPtr memDc = IntPtr.Zero;
            IntPtr bmp = IntPtr.Zero;
            IntPtr old = IntPtr.Zero;
            try
            {
                memDc = NativeMethods.CreateCompatibleDC(hdc);
                bmp = NativeMethods.CreateCompatibleBitmap(hdc, _screenW, _screenH);
                old = NativeMethods.SelectObject(memDc, bmp);

                NativeMethods.BitBlt(memDc, 0, 0, _screenW, _screenH, hdc, 0, 0, NativeMethods.ROP_SRCCOPY);

                int layers = 8;
                for (int i = 0; i < layers; i++)
                {
                    double s = _tunnelScale - i * 0.09;
                    if (s <= 0.05) break;
                    int w = (int)(_screenW * s);
                    int h = (int)(_screenH * s);
                    int x = (_screenW - w) / 2;
                    int y = (_screenH - h) / 2;
                    NativeMethods.StretchBlt(
                        hdc, x, y, w, h,
                        memDc, 0, 0, _screenW, _screenH, NativeMethods.ROP_SRCCOPY);
                }
            }
            finally
            {
                if (old != IntPtr.Zero) NativeMethods.SelectObject(memDc, old);
                if (bmp != IntPtr.Zero) NativeMethods.DeleteObject(bmp);
                if (memDc != IntPtr.Zero) NativeMethods.DeleteDC(memDc);
                NativeMethods.ReleaseDC(IntPtr.Zero, hdc);
            }
        }

        // ==================================================================
        // EFEKT 6: ICON SPAMMER — fare konumu + rastgele noktalara ikon
        // ==================================================================
        private void SpawnIcons()
        {
            IntPtr hdc = NativeMethods.GetDC(IntPtr.Zero);
            try
            {
                NativeMethods.GetCursorPos(out NativeMethods.POINT cur);
                DrawRandomIcon(hdc, cur.X - 16, cur.Y - 16);
                DrawRandomIcon(hdc, cur.X + _rnd.Next(-60, 61), cur.Y + _rnd.Next(-60, 61));

                int extra = _rnd.Next(2, 6) + (int)(_intensity * 10);
                for (int i = 0; i < extra; i++)
                {
                    int x = _rnd.Next(0, _screenW - 32);
                    int y = _rnd.Next(0, _screenH - 32);
                    DrawRandomIcon(hdc, x, y);
                }
            }
            finally
            {
                NativeMethods.ReleaseDC(IntPtr.Zero, hdc);
            }
        }

        /// <summary>
        /// İmleç izi: fare hareket ettikçe ardında kırmızı hata (error) logoları
        /// bırakır — imleç "çizerek" gezmiş gibi görünür.
        /// </summary>
        private void CursorTrail()
        {
            IntPtr hdc = NativeMethods.GetDC(IntPtr.Zero);
            try
            {
                NativeMethods.GetCursorPos(out NativeMethods.POINT cur);

                if (_lastCursorX != int.MinValue)
                {
                    // Önceki konum ile şimdiki arasına error logosu serpiştir.
                    int steps = 10;
                    for (int i = 1; i <= steps; i++)
                    {
                        int x = _lastCursorX + ((cur.X - _lastCursorX) * i / steps);
                        int y = _lastCursorY + ((cur.Y - _lastCursorY) * i / steps);
                        DrawErrorIcon(hdc, x - 16, y - 16);
                    }
                }

                // İmlecin tam altına da bir tane.
                DrawErrorIcon(hdc, cur.X - 16, cur.Y - 16);

                _lastCursorX = cur.X;
                _lastCursorY = cur.Y;
            }
            finally
            {
                NativeMethods.ReleaseDC(IntPtr.Zero, hdc);
            }
        }

        /// <summary>Yerleşik kırmızı hata (X) logosunu çizer.</summary>
        private void DrawErrorIcon(IntPtr hdc, int x, int y)
        {
            IntPtr icon = NativeMethods.LoadIcon(IntPtr.Zero, new IntPtr(NativeMethods.IDI_ERROR));
            if (icon != IntPtr.Zero)
            {
                NativeMethods.DrawIcon(hdc, x, y, icon);
            }
        }

        /// <summary>Ekranı kısa süreliğine yüzlerce ikonla doldurur.</summary>
        private void IconStorm()
        {
            IntPtr hdc = NativeMethods.GetDC(IntPtr.Zero);
            try
            {
                int n = _rnd.Next(80, 220) + (int)(_intensity * 300);
                for (int i = 0; i < n; i++)
                {
                    int x = _rnd.Next(0, _screenW - 32);
                    int y = _rnd.Next(0, _screenH - 32);
                    DrawRandomIcon(hdc, x, y);
                }
            }
            finally
            {
                NativeMethods.ReleaseDC(IntPtr.Zero, hdc);
            }
        }

        private void DrawRandomIcon(IntPtr hdc, int x, int y)
        {
            int id = IconIds[_rnd.Next(IconIds.Length)];
            IntPtr icon = NativeMethods.LoadIcon(IntPtr.Zero, new IntPtr(id));
            if (icon != IntPtr.Zero)
            {
                NativeMethods.DrawIcon(hdc, x, y, icon);
            }
        }

        // ==================================================================
        // EFEKT 7: WINDOW JUMPER — daha çok pencereyi daha geniş zıplat
        // ==================================================================
        private void JumpWindows()
        {
            var handles = new List<IntPtr>(48);
            NativeMethods.EnumWindows((hWnd, lParam) =>
            {
                if (handles.Count >= 48) return false;
                if (hWnd == IntPtr.Zero) return true;
                if (!NativeMethods.IsWindowVisible(hWnd)) return true;

                int len = NativeMethods.GetWindowTextLength(hWnd);
                if (len == 0) return true;

                handles.Add(hWnd);
                return true;
            }, IntPtr.Zero);

            int count = Math.Min(8, handles.Count);
            for (int i = 0; i < count; i++)
            {
                IntPtr hWnd = handles[_rnd.Next(handles.Count)];
                int x = _rnd.Next(-400, _screenW - 100);
                int y = _rnd.Next(-300, _screenH - 80);
                NativeMethods.SetWindowPos(
                    hWnd, IntPtr.Zero, x, y, 0, 0,
                    NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOZORDER);
            }
        }

        // ==================================================================
        // EKRANI GERİ YÜKLE — uygulama kapanırken çağrılır
        // ==================================================================
        public void RestoreScreen()
        {
            IntPtr desktop = NativeMethods.GetDesktopWindow();
            NativeMethods.InvalidateRect(desktop, IntPtr.Zero, true);
            NativeMethods.RedrawWindow(
                desktop, IntPtr.Zero, IntPtr.Zero,
                NativeMethods.RDW_INVALIDATE | NativeMethods.RDW_ERASE |
                NativeMethods.RDW_ALLCHILDREN | NativeMethods.RDW_UPDATENOW);
        }

        public void Dispose()
        {
            _timer.Tick -= OnTick;
            _timer.Dispose();
        }
    }
}
