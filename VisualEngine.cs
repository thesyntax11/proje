using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace ChaosVisualAudioSimulation
{
    /// <summary>
    /// GÖRSEL MOTORU
    /// Tüm GDI/ekran kaos efektlerini tek bir Timer tetiklemesiyle üretir.
    /// Her tick'te:
    ///   1) Rastgele seçilen bir "şok" efekti (invert / shake / glitch / melter)
    ///   2) Sabit devam eden sürekli efektler (tünel, ikon spam, pencere zıplatma)
    /// çalıştırılır.
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

        // Rastgele kaydırılan ikonlar için yerleşik ikon kimlikleri
        private static readonly int[] IconIds =
        {
            NativeMethods.IDI_ERROR,
            NativeMethods.IDI_QUESTION,
            NativeMethods.IDI_WARNING,
            NativeMethods.IDI_INFORMATION,
            NativeMethods.IDI_SHIELD
        };

        public VisualEngine(int intervalMs)
        {
            _screenW = NativeMethods.GetSystemMetrics(NativeMethods.SM_CXVIRTUALSCREEN);
            _screenH = NativeMethods.GetSystemMetrics(NativeMethods.SM_CYVIRTUALSCREEN);
            if (_screenW <= 0 || _screenH <= 0)
            {
                // Sanal ekran metrikleri alınamazsa birincil ekran boyutunu kullan.
                Rectangle primary = Screen.PrimaryScreen?.Bounds ?? new Rectangle(0, 0, 1920, 1080);
                _screenW = primary.Width;
                _screenH = primary.Height;
            }

            _timer = new Timer { Interval = intervalMs };
            _timer.Tick += OnTick;
        }

        public void Start() => _timer.Start();

        public void Stop() => _timer.Stop();

        /// <summary>
        /// Tek bir kaos dilimi. UI thread üzerinde, Timer.Tick içinden çağrılır.
        /// </summary>
        private void OnTick(object? sender, EventArgs e)
        {
            // ---- 1) Rastgele şok efekti (her dilimde bir tanesi) ----
            switch (_rnd.Next(0, 6))
            {
                case 0: InvertScreen(); break;
                case 1: ShakeScreen(); break;
                case 2: GlitchScreen(); break;
                case 3: MeltScreen(); break;
                case 4: InvertScreen(); ShakeScreen(); break; // kombine kaos
                case 5: GlitchScreen(); MeltScreen(); break;
            }

            // ---- 2) Sürekli efektler ----
            DrawTunnel();
            SpawnIcons();
            JumpWindows();
        }

        // ==================================================================
        // EFEKT 1: INVERT COLORS — PatBlt(DSTINVERT)
        // ==================================================================
        private void InvertScreen()
        {
            IntPtr hdc = NativeMethods.GetDC(IntPtr.Zero);
            try
            {
                // Ritmik: ekranın tamamını ya da rastgele yatay bandı ters çevir.
                int y = _rnd.Next(0, 3) == 0 ? 0 : _rnd.Next(0, _screenH);
                int h = _rnd.Next(0, 3) == 0 ? _screenH : _rnd.Next(_screenH / 8, _screenH / 3);
                NativeMethods.PatBlt(hdc, 0, y, _screenW, Math.Min(h, _screenH - y), NativeMethods.ROP_DSTINVERT);
            }
            finally
            {
                NativeMethods.ReleaseDC(IntPtr.Zero, hdc);
            }
        }

        // ==================================================================
        // EFEKT 2: SCREEN SHAKE — masaüstünü kendine kaydırarak kopyala
        // ==================================================================
        private void ShakeScreen()
        {
            int dx = _rnd.Next(-40, 41);
            int dy = _rnd.Next(-30, 31);
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
        // EFEKT 3: GLITCH — rastgele dikey şeritleri yatay kaydır
        // ==================================================================
        private void GlitchScreen()
        {
            IntPtr hdc = NativeMethods.GetDC(IntPtr.Zero);
            try
            {
                int strips = _rnd.Next(3, 9);
                for (int i = 0; i < strips; i++)
                {
                    int x = _rnd.Next(0, _screenW);
                    int stripW = _rnd.Next(_screenW / 20, _screenW / 6);
                    int shift = _rnd.Next(-80, 81);
                    int y = _rnd.Next(0, _screenH);
                    int h = _rnd.Next(_screenH / 20, _screenH / 6);

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
                int columns = _rnd.Next(4, 12);
                for (int i = 0; i < columns; i++)
                {
                    int x = _rnd.Next(0, _screenW);
                    int w = _rnd.Next(_screenW / 30, _screenW / 10);
                    int srcY = _rnd.Next(0, _screenH);
                    int h = _rnd.Next(_screenH / 8, _screenH / 2);
                    int drop = _rnd.Next(10, 80);

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
        // EFEKT 5: TUNNEL / ZOOM — ekranı kendi üzerine giderek küçülen
        //          çerçeveler halinde çiz (tünel hissi)
        // ==================================================================
        private void DrawTunnel()
        {
            // Yumuşak zoom oranını ileri-geri salındır.
            _tunnelScale += 0.05 * _tunnelDirection;
            if (_tunnelScale <= 0.55) { _tunnelScale = 0.55; _tunnelDirection = 1; }
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

                // Önce ekranın anlık görüntüsünü belleğe al.
                NativeMethods.BitBlt(memDc, 0, 0, _screenW, _screenH, hdc, 0, 0, NativeMethods.ROP_SRCCOPY);

                // Sonra aynı görüntüyü kademeli küçülterek ekrana yapıştır.
                int layers = 4;
                for (int i = 0; i < layers; i++)
                {
                    double s = _tunnelScale - i * 0.12;
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
                // Fareyi takip eden bir ikon.
                NativeMethods.GetCursorPos(out NativeMethods.POINT cur);
                DrawRandomIcon(hdc, cur.X - 16, cur.Y - 16);

                // Ekranın rastgele noktalarına 1-3 ikon daha.
                int extra = _rnd.Next(1, 4);
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
        // EFEKT 7: WINDOW JUMPER — diğer pencereleri rastgele zıplat
        // ==================================================================
        private void JumpWindows()
        {
            // Pencere tutamaçlarını topla (tek seferde sınırlı sayıda).
            var handles = new List<IntPtr>(32);
            NativeMethods.EnumWindows((hWnd, lParam) =>
            {
                if (handles.Count >= 32) return false;
                if (hWnd == IntPtr.Zero) return true;
                if (!NativeMethods.IsWindowVisible(hWnd)) return true;

                int len = NativeMethods.GetWindowTextLength(hWnd);
                if (len == 0) return true; // başlıksız/tool pencerelerini atla

                handles.Add(hWnd);
                return true;
            }, IntPtr.Zero);

            // Birkaç tanesini rastgele koordinata taşı.
            int count = Math.Min(3, handles.Count);
            for (int i = 0; i < count; i++)
            {
                IntPtr hWnd = handles[_rnd.Next(handles.Count)];
                int x = _rnd.Next(-100, _screenW - 100);
                int y = _rnd.Next(-50, _screenH - 50);
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
