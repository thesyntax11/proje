# Chaos Visual & Audio Simulation (MEMZ-Style Harmless Prank)

C# (Windows Forms / .NET 8) ile yazılmış, görsel ve işitsel açıdan yoğun,
**tamamen zararsız** bir "ekran kaosu" simülasyonu. Eğitim, siber güvenlik
yayınları ve sanal makine görsel performans testleri için tasarlanmıştır.

> ⚠️ **ETİK & GÜVENLİK:** Bu proje **yalnızca kendi makinanızda veya izinli
> bir sanal makinede** çalıştırılmalıdır. Başkalarının sistemlerinde
> çalıştırmayın.

---

## Güvenlik Garantileri

- **Sıfır ağ / sıfır veri:** Kodda `System.Net`, `Socket`, `WebClient` veya
  herhangi bir ağ kütüphanesi yoktur. Hiçbir veri toplanmaz, gönderilmez.
- **Kalıcı zarar yok:** Dosya silme, şifreleme, Registry değişikliği,
  MBR/Boot müdahalesi veya System32 erişimi yapılmaz.
- **Temizlenebilir yapı:** Uygulama durdurulduğunda ekran
  `InvalidateRect` / `RedrawWindow` ile anında ilk haline döner.

## Gizli Acil Kapanış (Kill-Switch)

```
CTRL + SHIFT + ALT + K
```

Bu kombinasyon `RegisterHotKey` Win32 API'si ile sisteme kaydedilir ve
uygulama odakta olmasa bile çalışır. Kombinasyon ve diğer ayarlar
`AppConfig.cs` içinden değiştirilebilir.

Normal kapanış yolları (Alt+F4, X butonu, Görev Yöneticisi'nden "Görevi
sonlandır" hariç) bilinçli olarak engellenmiştir; tek temiz çıkış yolu
kill-switch'tir. Gerekirse Görev Yöneticisi'nden de sonlandırılabilir ve
ekran kendiliğinden normale döner (çizimler kalıcı değildir).

## Özellikler

| Motor | Efektler |
|-------|----------|
| Görsel | Invert Colors (`PatBlt`/`DSTINVERT`), Screen Shake/Glitch, Tunnel/Zoom, Icon Spammer, Window Jumper, Pixel Melter |
| Ses | Windows sistem sesleri (`SystemSounds.*`) — hızlanan tempo, anakart `Console.Beep` ritimleri |

## Derleme ve Çalıştırma

```bash
# .NET 8 SDK gerekir
dotnet build -c Release
dotnet run --project ChaosVisualAudioSimulation.csproj
```

> Görsel efektler doğrudan masaüstü DC'sine çizilir; tam ekran overlay form
> görev çubuğunda ve Alt+Tab'da görünmez.

## Proje Yapısı

| Dosya | Sorumluluk |
|-------|------------|
| `Program.cs` | Ana giriş noktası, DPI ayarı, `EmergencyStop()` |
| `NativeMethods.cs` | Tüm Win32 P/Invoke tanımları (`user32.dll` / `gdi32.dll`) |
| `VisualEngine.cs` | GDI görsel kaos motoru |
| `AudioEngine.cs` | İşitsel kaos motoru (sistem sesleri + beep) |
| `MainForm.cs` | Overlay form, hotkey dinleme, kapanış kontrolü |
| `AppConfig.cs` | Merkezi yapılandırma |
