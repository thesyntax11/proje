using System;
using System.Media;
using System.Threading;
using System.Threading.Tasks;

namespace ChaosVisualAudioSimulation
{
    /// <summary>
    /// SES MOTORU — MAKSİMUM yoğunluk sürümü.
    /// Çok katmanlı işitsel kaos üretir:
    ///   1) Windows sistem sesleri (SystemSounds.*) — çoklu katman, hızlanan tempo
    ///   2) Anakart beep (Console.Beep) — kaotik frekans, bass, glitch, çızırtı
    ///   3) "Mikrofon patlatma" hissi — ani yükselen tiz + geri besleme taklidi
    ///
    /// GÜVENLİK: Tamamen yereldir; ağ/veri erişimi yoktur. Sistem ses ayarını
    /// (master volume) DEĞİŞTİRMEZ — yalnızca çok yoğun ses üretir. Uygulama
    /// kapanınca sesler de kesilir.
    /// </summary>
    internal sealed class AudioEngine : IDisposable
    {
        private CancellationTokenSource? _cts;
        private Task? _loopTask;
        private readonly Random _rnd = new Random();

        // Tempo, 180ms'den 8ms'ye kadar hızlanır (sürekli ses hissi).
        private const int MinIntervalMs = 8;
        private const int MaxIntervalMs = 180;

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
            while (!token.IsCancellationRequested)
            {
                // Tempo, Chaos Director'ın seviyesine göre hızlanır.
                double intensity = ChaosDirector.Level;
                int interval = (int)(MaxIntervalMs - (MaxIntervalMs - MinIntervalMs) * intensity);

                try
                {
                    if (token.WaitHandle.WaitOne(interval)) break;

                    // Sistem sesleri: çok katmanlı (üst üste binen "dıng/dınnn").
                    PlayRandomSystemSound();
                    if (_rnd.Next(0, 2) == 0) PlayRandomSystemSound();
                    if (intensity > 0.5 && _rnd.Next(0, 2) == 0) PlayRandomSystemSound();

                    // Beep neredeyse her dilimde.
                    if (_rnd.Next(0, 3) != 0) PlayRandomBeep();

                    // Bass: düşük frekanslı derin uğultu.
                    if (_rnd.Next(0, 3) == 0) PlayBass();

                    // Hızlı bip patlaması.
                    if (_rnd.Next(0, 4) == 0) PlayBeepBurst();

                    // Frekans çıldırması.
                    if (_rnd.Next(0, 4) == 0) PlayGlitch();

                    // Statik çızırtı — yoğun.
                    if (_rnd.Next(0, 2) == 0) PlayStatic();

                    // "Mikrofon patlatma" hissi: ani tiz + geri besleme.
                    if (_rnd.Next(0, 4) == 0) PlayFeedback();
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
            try
            {
                int frequency = _rnd.Next(150, 4000);
                int duration = _rnd.Next(15, 180);
                Console.Beep(frequency, duration);
            }
            catch { /* beep cihazı yok */ }
        }

        // ------------------------------------------------------------------
        // Bass — düşük frekanslı derin uğultu (subwoofer hissi)
        // ------------------------------------------------------------------
        private void PlayBass()
        {
            try
            {
                int frequency = _rnd.Next(40, 160);
                int duration = _rnd.Next(60, 250);
                Console.Beep(frequency, duration);
            }
            catch { /* beep cihazı yok */ }
        }

        // ------------------------------------------------------------------
        // "Mikrofon patlatma" / geri besleme — ani yükselen tiz dalgalanması
        // ------------------------------------------------------------------
        private void PlayFeedback()
        {
            try
            {
                // Ani yüksek tiz (patlama).
                Console.Beep(_rnd.Next(2200, 3800), _rnd.Next(60, 140));

                // Ardından düzensiz geri besleme dalgalanması.
                int n = _rnd.Next(5, 12);
                int f = _rnd.Next(1800, 3200);
                for (int i = 0; i < n; i++)
                {
                    f += _rnd.Next(-400, 400);
                    f = Math.Clamp(f, 800, 4000);
                    Console.Beep(f, _rnd.Next(20, 60));
                }
            }
            catch { /* beep cihazı yok */ }
        }

        // ------------------------------------------------------------------
        // Ses "bug"ı — frekanslar tamamen çıldırır
        // ------------------------------------------------------------------
        private void PlayGlitch()
        {
            try
            {
                int n = _rnd.Next(8, 20);
                for (int i = 0; i < n; i++)
                {
                    int freq = _rnd.Next(40, 4000);
                    int dur = _rnd.Next(8, 40);
                    Console.Beep(freq, dur);
                }
            }
            catch { /* beep cihazı yok */ }
        }

        // ------------------------------------------------------------------
        // Statik çızırtı — yoğun, kısa rastgele bip fırtınası
        // ------------------------------------------------------------------
        private void PlayStatic()
        {
            try
            {
                int n = _rnd.Next(40, 90);
                for (int i = 0; i < n; i++)
                {
                    int freq = _rnd.Next(30, 3500);
                    int dur = _rnd.Next(2, 12);
                    Console.Beep(freq, dur);
                }
            }
            catch { /* beep cihazı yok */ }
        }

        // ------------------------------------------------------------------
        // Hızlı bip patlaması: "dıt-dıt-dıtttt"
        // ------------------------------------------------------------------
        private void PlayBeepBurst()
        {
            try
            {
                int count = _rnd.Next(4, 12);
                int baseFreq = _rnd.Next(400, 1800);
                for (int i = 0; i < count; i++)
                {
                    int freq = baseFreq + _rnd.Next(-300, 500);
                    int dur = _rnd.Next(12, 60);
                    Console.Beep(freq, dur);
                }
            }
            catch { /* beep cihazı yok */ }
        }

        public void Dispose() => Stop();
    }
}
