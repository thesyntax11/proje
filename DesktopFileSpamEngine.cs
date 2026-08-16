using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

namespace ChaosVisualAudioSimulation
{
    /// <summary>
    /// MASAÜSTÜ DOSYA SPAM MOTORU
    /// Masaüstüne zararsız, silinebilir .txt dosyaları bırakır; ekran ikonlarla
    /// dolar. Dosyalar tamamen güvenli metinlerdir:
    ///   - Hiçbir veri toplanmaz/gönderilmez,
    ///   - Hiçbir dosya silinmez/değiştirilmez (yalnızca YENİ dosya oluşturulur),
    ///   - Kullanıcı istediğinde hepsini seçip silebilir.
    ///
    /// GÜVENLİK: Yalnızca kullanıcının Masaüstü klasörüne yeni .txt dosyaları
    /// yazar. Ağ, registry, sistem dosyaları YOK.
    /// </summary>
    internal sealed class DesktopFileSpamEngine : IDisposable
    {
        private readonly Timer _timer;
        private readonly Random _rnd = new Random();
        private readonly string _desktopPath;

        private int _created; // thread-safe sayaç

        private static readonly string[] FileContents =
        {
            "Bu bir tatbikattır. Gerçek bir virüs değildir. :)\r\n",
            "KAOS SEVİYESİ: MAKSİMUM\r\n",
            "Sistem çöküyor... şaka, hiçbir şey olmuyor.\r\n",
            "Bu dosyayı bulduysan, oyunun bir parçasısın.\r\n",
            "Sil beni. Ama bil ki geri gelebilirim... şaka.\r\n",
            "HAHAHA\r\n",
            "Windows bu dosyayı kendi oluşturmadı.\r\n",
            "Ekranı doldurmak için buradayım.\r\n",
            "Bu bir zararsız prank dosyasıdır.\r\n",
            "CTRL+SHIFT+ALT+K ile her şey durur.\r\n"
        };

        private static readonly string[] FileStems =
        {
            "KAOS", "hata", "ERROR", "sistem", "beni_sil", "şaka",
            "KAOS_%", "glitch", "dikkat", "uyari", "HAHA", "chaos"
        };

        public DesktopFileSpamEngine(int intervalMs)
        {
            _desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            _timer = new Timer { Interval = intervalMs };
            _timer.Tick += OnTick;
        }

        public void Start() => _timer.Start();

        public void Stop() => _timer.Stop();

        private void OnTick(object? sender, EventArgs e)
        {
            if (Volatile.Read(ref _created) >= AppConfig.MaxDesktopFiles) return;

            // Kaos seviyesi arttıkça daha hızlı dosya bırakır.
            double level = ChaosDirector.Level;
            int burst = 2 + (int)(level * 8);

            for (int i = 0; i < burst; i++)
            {
                if (Volatile.Read(ref _created) >= AppConfig.MaxDesktopFiles) break;
                Interlocked.Increment(ref _created);
                CreateOneFile();
            }
        }

        private void CreateOneFile()
        {
            try
            {
                string stem = FileStems[_rnd.Next(FileStems.Length)].Replace("%", _rnd.Next(0, 99999).ToString());
                string name = stem + "_" + _rnd.Next(0, 99999) + ".txt";
                string path = Path.Combine(_desktopPath, name);

                string content = FileContents[_rnd.Next(FileContents.Length)];
                File.WriteAllText(path, content);
            }
            catch
            {
                // yazma izni yoksa / dosya varsa sessizce geç
            }
        }

        public void Dispose()
        {
            _timer.Tick -= OnTick;
            _timer.Dispose();
        }
    }
}
