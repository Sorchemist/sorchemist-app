# Sorchemist — Aqualf'in Laboratuvarı

Kimyayı büyüyle harmanlayan, izometrik bir köyde geçen mobil simya oyunu (Unity 6.3 LTS, Android).

![Core Tests](https://github.com/Sorchemist/sorchemist-app/actions/workflows/core-tests.yml/badge.svg)

## Hızlı başlangıç
1. Repoyu klonla: `git clone https://github.com/Sorchemist/sorchemist-app.git`
2. Unity Hub → **Add project from disk** → bu klasör → Unity 6.3 ile aç.
3. **Play**'e bas. Sahneye hiçbir şey eklemen gerekmez; oyun kendini kodla kurar.

## Belgeler
- [KURULUM.md](KURULUM.md) — Unity kurulumundan Play Store'a adım adım rehber
- [YOL_HARITASI.md](YOL_HARITASI.md) — v0.1'den v1.0'a geliştirme planı

## Klasörler
| Klasör | İçerik |
|---|---|
| `Assets/Sorchemist/Scripts/Core` | Oyun kuralları (saf C#, Unity'den bağımsız) |
| `Assets/Sorchemist/Scripts/Game` | Başlatma, kayıt, ses, paylaşım |
| `Assets/Sorchemist/Scripts/UI` | Ekranlar, minioyunlar, efektler |
| `Assets/Sorchemist/Scripts/Editor` | Unity'deki "Sorchemist" menüsü (Android ayarları) |
| `Tests` | Çekirdek testleri: `dotnet run --project Tests/Sorchemist.CoreTests.csproj` |
