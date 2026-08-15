namespace ChaosVisualAudioSimulation
{
    /// <summary>
    /// Merkezi yapılandırma. Yayıncı/geliştirici buradaki değerleri
    /// tek yerden değiştirir.
    /// </summary>
    internal static class AppConfig
    {
        /// <summary>
        /// GİZLİ ACİL KAPANIŞ KOMBİNASYONU:
        /// CTRL + SHIFT + ALT + K
        /// (RegisterHotKey ile sisteme kaydedilir; uygulama odakta olmasa
        ///  bile çalışır. MOD_NOREPEAT sayesinde basılı tutunca tekrar
        ///  tetiklenmez.)
        /// </summary>
        public const uint KillModifiers =
            NativeMethods.MOD_CONTROL |
            NativeMethods.MOD_SHIFT |
            NativeMethods.MOD_ALT |
            NativeMethods.MOD_NOREPEAT;

        public const uint KillVirtualKey = 0x4B; // 'K' tuşu

        /// <summary>
        /// Kaos başlamadan önceki bekleme süresi (saniye).
        /// Operatöre hazırlanması için kısa bir süre tanır.
        /// </summary>
        public const int StartupDelaySeconds = 3;
    }
}
