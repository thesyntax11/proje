using System;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

namespace ChaosVisualAudioSimulation
{
    /// <summary>
    /// CHAOS DIRECTOR — tüm motorların üstündeki yönetmen.
    /// Kaosu 0% -> 100% arasında zamanla tırmandırır ve 6 fazda aşamalı
    /// yükseltir (progressive escalation). Tüm motorlar şiddetlerini buradaki
    /// <see cref="Level"/> değerinden okur; böylece tek merkezden kontrol edilir.
    ///
    ///   PHASE 0 -> Normal
    ///   PHASE 1 -> Something is wrong
    ///   PHASE 2 -> Glitches
    ///   PHASE 3 -> Windows chaos
    ///   PHASE 4 -> Insanity
    ///   PHASE 5 -> Final
    ///
    /// GÜVENLİK: Yalnızca zamanlayıcı + sayaçtır. Ağ/veri/disk/registry yok.
    /// </summary>
    internal static class ChaosDirector
    {
        public static double Level { get; private set; }   // 0.0 .. 1.0
        public static int Phase { get; private set; }      // 0 .. 5
        public static bool Running { get; private set; }

        /// <summary>Faz değiştiğinde tetiklenir: (yeniFaz, o anki seviye).</summary>
        public static event Action<int, double>? PhaseChanged;

        private static readonly Timer _timer = new Timer { Interval = 250 };
        private static DateTime _started;

        // Faz eşikleri (Level oranı). Her faz bir öncekinden daha şiddetlidir.
        private static readonly double[] PhaseThresholds =
            { 0.00, 0.05, 0.18, 0.37, 0.64, 0.92 };

        private static readonly string[] PhaseNames =
        {
            "NORMAL",
            "SOMETHING IS WRONG",
            "GLITCHES",
            "WINDOWS CHAOS",
            "INSANITY",
            "FINAL"
        };

        static ChaosDirector()
        {
            _timer.Tick += OnTick;
        }

        public static void Start()
        {
            _started = DateTime.UtcNow;
            Level = 0.0;
            Phase = 0;
            Running = true;
            _timer.Start();
            PhaseChanged?.Invoke(0, 0.0);
        }

        public static void Stop()
        {
            Running = false;
            _timer.Stop();
        }

        private static void OnTick(object? sender, EventArgs e)
        {
            if (!Running) return;

            double elapsed = (DateTime.UtcNow - _started).TotalSeconds;
            Level = Math.Clamp(elapsed / AppConfig.FullChaosSeconds, 0.0, 1.0);

            int newPhase = 0;
            for (int i = PhaseThresholds.Length - 1; i >= 0; i--)
            {
                if (Level >= PhaseThresholds[i]) { newPhase = i; break; }
            }

            if (newPhase != Phase)
            {
                Phase = newPhase;
                PhaseChanged?.Invoke(Phase, Level);
            }
        }

        /// <summary>Fazın yüzde olarak okunabilir hali (örn. "37%").</summary>
        public static string LevelPercent => ((int)(Level * 100)) + "%";

        public static string PhaseName(int phase)
        {
            if (phase < 0 || phase >= PhaseNames.Length) return "???";
            return PhaseNames[phase];
        }
    }
}
