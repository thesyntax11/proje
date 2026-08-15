using System;
using System.Threading;
using System.Threading.Tasks;
using System.Media;

namespace ChaosVisualAudioSimulation
{
    /// <summary>
    /// SES MOTORU
    /// İki katmanlı işitsel kaos üretir:
    ///   1) Windows sistem sesleri (SystemSounds.*) — giderek hızlanan tempo
    ///   2) Anakart beep sesleri (Console.Beep) — rastgele frekanslı kaotik bip
    ///
    /// GÜVENLİK NOTU: Ses üretimi tamamen yereldir. Hiçbir ses/dosya/veri
    /// okunmaz, yazılmaz veya ağa gönderilmez.
    /// </summary>
    internal sealed class AudioEngine : IDisposable
    {
        private CancellationTokenSource? _cts;
        private Task? _loopTask;
        private readonly Random _rnd = new Random();

        // Tempo, 320ms'den 40ms'ye kadar hızlanır.
        private const int MinIntervalMs = 40;
        private const int MaxIntervalMs = 320;

        public void Start()
        {
            if (_loopTask != null && !_loopTask.IsCompleted) return;

            _cts = new CancellationTokenSource();
            CancellationToken token = _cts.Token;
            _loopTask = Task.Run(() => Loop(token), token);
        }

        public void Stop()
        {
            _cts?.Cancel();
            try { _loopTask?.Wait(1500); } catch { /* süre aşımı önemsiz */ }
            _cts?.Dispose();
            _cts = null;
            _loopTask = null;
        }

        private void Loop(CancellationToken token)
        {
            // Tempo giderek artar.
            double intensity = 0.0;
            while (!token.IsCancellationRequested)
            {
                int interval = (int)(MaxIntervalMs - (MaxIntervalMs - MinIntervalMs) * intensity);
                intensity = Math.Min(1.0, intensity + 0.02);

                try
                {
                    if (token.WaitHandle.WaitOne(interval)) break;

                    PlayRandomSystemSound();
                    if (_rnd.Next(0, 3) == 0)
                    {
                        PlayRandomBeep();
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        // ------------------------------------------------------------------
        // Windows sistem sesleri ("dıng / dınnn" hata sesleri)
        // ------------------------------------------------------------------
        private void PlayRandomSystemSound()
        {
            switch (_rnd.Next(0, 5))
            {
                case 0: SystemSounds.Hand.Play(); break;
                case 1: SystemSounds.Asterisk.Play(); break;
                case 2: SystemSounds.Exclamation.Play(); break;
                case 3: SystemSounds.Beep.Play(); break;
                case 4: SystemSounds.Question.Play(); break;
            }
        }

        // ------------------------------------------------------------------
        // Anakart beep — rastgele frekanslı kaotik bip
        // ------------------------------------------------------------------
        private void PlayRandomBeep()
        {
            int frequency = _rnd.Next(200, 3000);
            int duration = _rnd.Next(30, 220);
            Console.Beep(frequency, duration);
        }

        public void Dispose() => Stop();
    }
}
