# Stock Control Prototype (ASP.NET Core)

Bu proje bir stok kontrol prototipidir. Başlangıçta amaçladığım işletmelerin şuanda kullanamacağı ama bir prototip olan stok takip, kontor, yenileme, ekleme kısımlarının yağıldığı bir uygulama. Yeni güncellemede yapmayı planladığım şeyler arasında işletmelerin kullanabileceği hale getirmek (Şuanda aşırı basit halde.) ve tasarım değişikliğine gitmek.

## Ozellikler

- Login sistemi (Admin/User rolleri)
  * Neden role-based yetkilendirme?
  - Stok giris/cikis, urun duzenleme ve kategori yonetimi gibi kritik islemler sadece Admin tarafinda sinirlanir.
  - User rolunun katalog/urun inceleme ile sinirli kalmasi, yetki hatalarini ve veri guvenligi riskini azaltir.
  - Isletme tarafinda sorumluluk ayrimini netlestirir ve panel kullanimini sadeleştirir.
- Kategori bazli katalog listeleme
- Admin panelinde urun ekleme, duzenleme, silme
- Urun girislerinde kucuk fotograf yukleme
- Stok giris/cikis islemleri (sadece Admin)
- Hareket gecmisi
- SQLite veritabani baglantisi
  * Neden SQLite?
  - Prototip asamasinda kurulum maliyeti dusuk ve sifir ek servis gerektirir.
  - Tek dosya veritabani yapisi sayesinde yerel gelistirme ve demo sureci hizli ilerler.
  - Entity Framework Core ile kolay entegre olur ve daha sonra SQL Server/PostgreSQL'e gecis icin uygun bir baslangic sunar.





## Calistirma

```bash
dotnet restore
dotnet run
```

Uygulama acildiginda varsayilan olarak login sayfasina yonlenir.

## Test kullanicilari

- Admin: admin123 / admin123
- User: user123 / user123

## Not

Eski veritabani ile uyumsuzluk yasarsaniz `stockcontrol.db` dosyasini silip tekrar calistirin.
