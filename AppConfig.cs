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

        // ------------------------------------------------------------------
        // Jumpscare (korku patlaması) ayarları
        // ------------------------------------------------------------------

        /// <summary>Jumpscare aktif mi?</summary>
        public const bool ScareEnabled = true;

        /// <summary>
        /// Jumpscare tekrar etsin mi? false = YALNIZCA BİR KEZ, aniden çıkar ve
        /// bir daha gelmez (varsayılan ve önerilen). true = aralıklarla tekrarlar.
        /// </summary>
        public const bool ScareRepeat = false;

        /// <summary>Jumpscare anında çığlık benzeri ses çalınsın mı?</summary>
        public const bool ScareScream = true;

        /// <summary>Jumpscare süresi (ms) — 3500 = ~3.5 saniye.</summary>
        public const int ScareDurationMs = 3500;

        /// <summary>İlk jumpscare'in gecikmesi (saniye).</summary>
        public const int ScareFirstDelaySeconds = 12;

        /// <summary>İki jumpscare arasındaki minimum boşluk (saniye).</summary>
        public const int ScareMinGapSeconds = 18;

        /// <summary>İki jumpscare arasındaki maksimum boşluk (saniye).</summary>
        public const int ScareMaxGapSeconds = 42;

        // ------------------------------------------------------------------
        // Kaos yoğunluğu / popup ayarları
        // ------------------------------------------------------------------

        /// <summary>Görsel motorun tick aralığı (ms). Düşük = daha yoğun/akıcı kaos.</summary>
        public const int VisualIntervalMs = 20;

        /// <summary>Ekranı dolduran sahte uyarı pencereleri aktif mi?</summary>
        public const bool PopupsEnabled = true;

        /// <summary>Uyarı pencereleri açılma aralığı (ms). Düşük = daha hızlı fışkırır.</summary>
        public const int PopupIntervalMs = 900;

        /// <summary>Ekranda aynı anda bulunabilecek maksimum uyarı penceresi.</summary>
        public const int MaxPopups = 20;

        // ------------------------------------------------------------------
        // Uygulama açma (AppSpam) ayarları
        // ------------------------------------------------------------------

        /// <summary>Windows uygulamalarını otomatik açma aktif mi?</summary>
        public const bool AppSpamEnabled = true;

        /// <summary>Uygulama açma motorunun tick aralığı (ms).</summary>
        public const int AppSpamIntervalMs = 1500;

        /// <summary>Açılacak tarayıcı sekmesi sayısı aralığı (min).</summary>
        public const int BrowserTabMin = 10;

        /// <summary>Açılacak tarayıcı sekmesi sayısı aralığı (max).</summary>
        public const int BrowserTabMax = 20;

        /// <summary>Açılacak Not Defteri sayısı aralığı (min).</summary>
        public const int NotepadMin = 4;

        /// <summary>Açılacak Not Defteri sayısı aralığı (max).</summary>
        public const int NotepadMax = 8;

        /// <summary>
        /// Tarayıcıda açılacak sekmenin URL'si. Zararsız bir sayfa kullanılır;
        /// istersen "about:blank" yaparak tamamen ağ dışı bırakabilirsin.
        /// </summary>
        public const string BrowserUrl = "https://www.bing.com";
    }
}
