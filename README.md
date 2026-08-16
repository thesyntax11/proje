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
- **Masaüstü .txt spam:** Uygulama, Masaüstüne **yalnızca yeni, zararsız ve
  silinebilir `.txt` dosyaları** bırakır (hiçbir dosyayı silmez/değiştirmez).
  İstersen hepsini seçip silebilirsin; `AppConfig.FileSpamEnabled` ile
  kapatılabilir.

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

## Açılış Sekansı

Animasyonlar **hemen başlamaz**:

1. Önce bir **CMD** penceresi açılır,
2. Ardından bir **Windows uyarısı** gösterilir,
3. Tamam'a basınca animasyonlar başlar (kara duvar kağıdı + aşağı akan
   renkli akıntılar).

## Özellikler

| Motor | Efektler |
|-------|----------|
| Görsel | Kara duvar kağıdı + aşağı akan renkli akıntılar, Invert Colors, Screen Shake/Glitch, Tunnel/Zoom, Icon Spammer + İkon Fırtınası, Window Jumper, Pixel Melter, Renk Flaşları |
| Ses | Windows sistem sesleri (`SystemSounds.*`) çok katmanlı, `Console.Beep` bass/glitch/statik çızırtı, "mikrofon patlatma" geri beslemesi |
| Jumpscare | Tek seferlik 3–4 sn korku görseli (zoom + flaş) + çığlık |
| Popup | Ekranı dolduran sahte Windows hata/uyarı pencereleri |
| Uygulama | Hava Durumu, tarayıcı sekmeleri (10-20), Not Defteri, Hesap Makinesi |
| Dosya | Masaüstüne yüzlerce zararsız .txt dosyası (ekranı doldurur) |

## Jumpscare (korku patlaması)

Kaos **kesintisiz** devam ederken, rastgele aralıklarla (~18–42 sn) yalnızca
**3–4 saniyelik** bir korku patlaması üzerine biner: tam ekran korku görseli
içe doğru zoom yaparak, titreme/flaş ve yükselen beep + sistem sesleriyle
görünür. **7/24 kalmaz**, Matrix yağmuru gibi sürekli bir katman değildir.

- Korku görseli: proje köküne **`scare.png`** (veya `scare.jpg`) koyun;
  derlemede otomatik gömülür ve yüklenir.
- Görsel yoksa kırmızı/siyah glitch fallback'i çalışır.
- Tüm jumpscare ayarları `AppConfig.cs` içinde (`ScareEnabled`,
  `ScareDurationMs`, boşluk aralıkları, `ScareScream`) değiştirilebilir.

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
| `VisualEngine.cs` | GDI görsel kaos motoru (kara duvar kağıdı + akıntılar) |
| `AudioEngine.cs` | İşitsel kaos motoru (sistem sesleri + beep + bass + çızırtı) |
| `JumpscareEngine.cs` | Tek seferlik korku patlaması motoru |
| `PopupEngine.cs` | Ekranı dolduran sahte uyarı pencereleri motoru |
| `AppSpamEngine.cs` | Windows uygulamalarını açan motor |
| `DesktopFileSpamEngine.cs` | Masaüstüne .txt dosyası bırakan motor |
| `MainForm.cs` | Açılış sekansı, hotkey dinleme, kapanış kontrolü |
| `AppConfig.cs` | Merkezi yapılandırma |
