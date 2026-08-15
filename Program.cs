using System;
using System.Windows.Forms;

namespace ChaosVisualAudioSimulation
{
    /// <summary>
    /// Uygulamanın ana giriş noktası.
    /// Tek sorumluluğu: DPI farkındalığını açmak, ana formu başlatmak
    /// ve acil durumda (kill-switch) tüm sistemi güvenle kapatmak.
    /// </summary>
    internal static class Program
    {
        private static MainForm? _mainForm;

        [STAThread]
        private static void Main()
        {
            // GDI blitlerinin fiziksel piksel ile birebir eşleşmesi için
            // süreç DPI farkındalığını etkinleştir.
            try { NativeMethods.SetProcessDPIAware(); }
            catch { /* kritik değil */ }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

            _mainForm = new MainForm();
            Application.Run(_mainForm);
        }

        /// <summary>
        /// Güvenli acil kapanış (kill-switch) çağrısı.
        /// Tüm motorları durdurur, ekranı yeniden çizer ve süreci sonlandırır.
        /// UI thread üzerinden çağrılması güvenlidir (WndProc içinden gelir).
        /// </summary>
        internal static void EmergencyStop()
        {
            if (_mainForm != null)
            {
                _mainForm.SafeShutdown();
            }
            else
            {
                // Form henüz oluşmadıysa doğrudan çık.
                Environment.Exit(0);
            }
        }
    }
}
