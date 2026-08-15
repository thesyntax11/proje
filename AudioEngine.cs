using System;
using System.Media;
using System.Threading;
using System.Threading.Tasks;

namespace ChaosVisualAudioSimulation
{
    /// <summary>
    /// SES MOTORU — yoğunlaştırılmış sürüm.
    /// İki katmanlı işitsel kaos üretir:
    ///   1) Windows sistem sesleri (SystemSounds.*) — giderek hızlanan tempo
    ///   2) Anakart beep sesleri (Console.Beep) — kaotik frekans ve hızlı
    ///      "dıt-dıt-dıtttt" ritim patlamaları.
    ///
    /// GÜVENLİK: Tamamen yereldir; hiçbir dosya/ağ/registry erişimi yoktur.
    /// </summary>
    internal sealed class AudioEngine : IDisposable
    {
        private CancellationTokenSource? _cts;
        private Task? _loopTask;
        private readonly Random _rnd = new Random();

        // Tempo, 240ms'den 25ms'ye kadar hızlanır.
        private const int MinIntervalMs = 25;
        private const int MaxIntervalMs = 240;

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
            double intensity = 0.0;
            while (!token.IsCancellationRequested)
            {
                int interval = (int)(MaxIntervalMs - (MaxIntervalMs - MinIntervalMs) * intensity);
                intensity = Math.Min(1.0, intensity + 0.02);

                try
                {
                    if (token.WaitHandle.WaitOne(interval)) break;

                    PlayRandomSystemSound();

                    // Beep sıklığını artır: çoğu dilimde "dıtttt" gelsin.
                    if (_rnd.Next(0, 2) == 0)
                    {
                        PlayRandomBeep();
                    }
                    // Ara sıra hızlı bip patlaması (dıt-dıt-dıtttt).
                    if (_rnd.Next(0, 5) == 0)
                    {
                        PlayBeepBurst();
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
            int frequency = _rnd.Next(150, 3500);
            int duration = _rnd.Next(20, 200);
            Console.Beep(frequency, duration);
        }

        // ------------------------------------------------------------------
        // Hızlı bip patlaması: "dıt-dıt-dıtttt"
        // ------------------------------------------------------------------
        private void PlayBeepBurst()
        {
            try
            {
                int count = _rnd.Next(3, 9);
                int baseFreq = _rnd.Next(400, 1800);
                for (int i = 0; i < count; i++)
                {
                    int freq = baseFreq + _rnd.Next(-200, 400);
                    int dur = _rnd.Next(15, 70);
                    Console.Beep(freq, dur);
                }
            }
            catch
            {
                // beep cihazı yoksa sessizce geç
            }
        }

        public void Dispose() => Stop();
    }
}
