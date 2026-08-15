using System;
using System.Runtime.InteropServices;
using System.Text;

namespace ChaosVisualAudioSimulation
{
    /// <summary>
    /// Tüm Win32 P/Invoke tanımları ve sabitleri tek bir yerde toplanır.
    /// Kullanılan kütüphaneler: user32.dll (pencere/ekran/ikon/hotkey) ve
    /// gdi32.dll (ekran çizim / BitBlt / PatBlt / StretchBlt).
    ///
    /// GÜVENLİK NOTU: Burada System.Net, Socket, WebClient veya herhangi bir
    /// ağ/veri/disk/registry API'si BULUNMAZ. Sadece görsel ve pencere API'leri.
    /// </summary>
    internal static class NativeMethods
    {
        // ------------------------------------------------------------------
        // user32.dll — ekran / pencere
        // ------------------------------------------------------------------
        [DllImport("user32.dll")]
        internal static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        internal static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("user32.dll")]
        internal static extern IntPtr GetDesktopWindow();

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SetWindowPos(
            IntPtr hWnd, IntPtr hWndInsertAfter,
            int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        internal static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        internal static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        internal static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        internal static extern IntPtr LoadIcon(IntPtr hInstance, IntPtr lpIconName);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool DrawIcon(IntPtr hDC, int x, int y, IntPtr hIcon);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool RedrawWindow(
            IntPtr hWnd, IntPtr lprcUpdate, IntPtr hrgnUpdate, uint flags);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool InvalidateRect(IntPtr hWnd, IntPtr lpRect, [MarshalAs(UnmanagedType.Bool)] bool bErase);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SetProcessDPIAware();

        [DllImport("user32.dll")]
        internal static extern int GetSystemMetrics(int nIndex);

        // ------------------------------------------------------------------
        // gdi32.dll — çizim
        // ------------------------------------------------------------------
        [DllImport("gdi32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool PatBlt(IntPtr hdc, int x, int y, int width, int height, uint rop);

        [DllImport("gdi32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool BitBlt(
            IntPtr hdcDest, int xDest, int yDest, int w, int h,
            IntPtr hdcSrc, int xSrc, int ySrc, uint rop);

        [DllImport("gdi32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool StretchBlt(
            IntPtr hdcDest, int xDest, int yDest, int wDest, int hDest,
            IntPtr hdcSrc, int xSrc, int ySrc, int wSrc, int hSrc, uint rop);

        [DllImport("gdi32.dll")]
        internal static extern IntPtr CreateCompatibleDC(IntPtr hdc);

        [DllImport("gdi32.dll")]
        internal static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int width, int height);

        [DllImport("gdi32.dll")]
        internal static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);

        [DllImport("gdi32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool DeleteObject(IntPtr hObject);

        [DllImport("gdi32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool DeleteDC(IntPtr hdc);

        // ------------------------------------------------------------------
        // Yapılar ve delegate
        // ------------------------------------------------------------------
        internal delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        internal struct POINT
        {
            public int X;
            public int Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;

            public int Width => Right - Left;
            public int Height => Bottom - Top;
        }

        // ------------------------------------------------------------------
        // SystemMetrics sabitleri
        // ------------------------------------------------------------------
        internal const int SM_XVIRTUALSCREEN = 76;
        internal const int SM_YVIRTUALSCREEN = 77;
        internal const int SM_CXVIRTUALSCREEN = 78;
        internal const int SM_CYVIRTUALSCREEN = 79;

        // ------------------------------------------------------------------
        // Raster operation (ROP) kodları
        // ------------------------------------------------------------------
        internal const uint ROP_SRCCOPY = 0x00CC0020;   // kaynağı kopyala
        internal const uint ROP_DSTINVERT = 0x00550009; // hedefi ters çevir
        internal const uint ROP_SRCINVERT = 0x00660046; // XOR
        internal const uint ROP_BLACKNESS = 0x00000042; // siyaha boya

        // ------------------------------------------------------------------
        // SetWindowPos bayrakları
        // ------------------------------------------------------------------
        internal const uint SWP_NOSIZE = 0x0001;
        internal const uint SWP_NOZORDER = 0x0004;

        // ------------------------------------------------------------------
        // Hotkey (kill-switch) bayrakları
        // ------------------------------------------------------------------
        internal const uint MOD_ALT = 0x0001;
        internal const uint MOD_CONTROL = 0x0002;
        internal const uint MOD_SHIFT = 0x0004;
        internal const uint MOD_WIN = 0x0008;
        internal const uint MOD_NOREPEAT = 0x4000;

        internal const int WM_HOTKEY = 0x0312;

        // ------------------------------------------------------------------
        // Yerleşik sistem ikonları (IDI_*)
        // ------------------------------------------------------------------
        internal const int IDI_ERROR = 32513;
        internal const int IDI_QUESTION = 32514;
        internal const int IDI_WARNING = 32515;
        internal const int IDI_INFORMATION = 32516;
        internal const int IDI_SHIELD = 32518;

        // ------------------------------------------------------------------
        // RedrawWindow bayrakları (temizlik / ekran geri yükleme)
        // ------------------------------------------------------------------
        internal const uint RDW_INVALIDATE = 0x0001;
        internal const uint RDW_ERASE = 0x0004;
        internal const uint RDW_ALLCHILDREN = 0x0080;
        internal const uint RDW_UPDATENOW = 0x0100;

        // ------------------------------------------------------------------
        // Genişletilmiş pencere stilleri
        // ------------------------------------------------------------------
        internal const int WS_EX_TOOLWINDOW = 0x00000080;  // Alt+Tab'da gizle
        internal const int WS_EX_TRANSPARENT = 0x00000020; // tıklama geçirgenliği
    }
}
