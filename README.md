	CineTrack
CineTrack, film ve kitap tutkunlarının içerikleri keşfetmesini, takip etmesini ve arkadaşlarıyla etkileşime girmesini sağlayan sosyal tabanlı bir takip platformudur. Kullanıcılar izledikleri filmleri veya okudukları kitapları puanlayabilir, inceleme yazabilir, özel listeler oluşturabilir ve takip ettikleri kişilerin aktivitelerini "Feed" (Akış) üzerinden görüntüleyebilirler.

	Proje Hakkında
Bu proje, modern .NET 9.0 teknolojileri kullanılarak, N-Tier (Katmanlı) mimariye uygun olarak geliştirilmiştir. Proje, iş mantığını ve veri erişimini yöneten bir Web API ve kullanıcı arayüzünü sunan bir ASP.NET Core MVC uygulamasından oluşur.

Öne Çıkan Özellikler
	Kapsamlı Keşif: TMDB ve Google Books entegrasyonu ile geniş kapsamlı film ve kitap arama/filtreleme.

	Sosyal Etkileşim: Arkadaş takip sistemi, aktivite akışı (Feed), beğeniler ve yorumlar.

	İçerik Yönetimi: İzledim/Okudum işaretleme, puanlama ve detaylı inceleme yazma.

seçenekli Listeler: "İzlenecekler", "Okunacaklar" ve kullanıcıya özel oluşturulabilir listeler.

	Profil Yönetimi: Kullanıcı biyografisi, avatar yönetimi ve takipçi/takip edilen istatistikleri.

	Güvenlik: JWT (JSON Web Token) tabanlı kimlik doğrulama ve Session yönetimi.

	Teknolojiler ve Mimari
Proje, temiz kod prensipleri ve modülerlik gözetilerek aşağıdaki teknolojilerle inşa edilmiştir:

Backend: .NET 9.0, ASP.NET Core Web API

Frontend (UI): ASP.NET Core MVC, Bootstrap 5, jQuery

Veritabanı: Microsoft SQL Server, Entity Framework Core (Code First)

Authentication: JWT (JSON Web Tokens)

External APIs:

The Movie Database (TMDB) - Film verileri için

Google Books API - Kitap verileri için

Proje Yapısı
CineTrack.WebAPI: Veri servislerini ve iş mantığını dışarıya sunan RESTful API katmanı.

CineTrack.MvcUI: Kullanıcı arayüzünü sağlayan ve API ile haberleşen istemci katmanı.

CineTrack.DataAccess: Veritabanı bağlamı (DbContext), varlıklar (Entities) ve konfigürasyonlar.

CineTrack.Shared: Katmanlar arası ortak kullanılan DTO (Data Transfer Objects) nesneleri.

	Kurulum ve Çalıştırma
Projeyi yerel makinenizde çalıştırmak için aşağıdaki adımları izleyin:

Gereksinimler
.NET 9.0 SDK

SQL Server (LocalDB veya MSSQL)

Visual Studio 2022 veya VS Code

Adım 1: Depoyu Klonlayın
Bash

git clone https://github.com/kullaniciadi/CineTrack.git
cd CineTrack
Adım 2: Konfigürasyon
CineTrack.WebAPI/appsettings.json dosyasını açın ve gerekli alanları doldurun:

JSON

"ConnectionStrings": {
  "CineTrackDb": "Server=YOUR_SERVER;Database=CineTrackDB;Trusted_Connection=True;MultipleActiveResultSets=true;"
},
"ExternalApis": {
  "TMDb": {
    "ApiKey": "BURAYA_TMDB_API_KEY_GELECEK"
  }
}
Adım 3: Veritabanını Oluşturun
Terminali CineTrack.DataAccess dizininde açın veya Package Manager Console üzerinden şu komutu çalıştırın:

Bash

dotnet ef database update --startup-project ../CineTrack.WebAPI
Adım 4: Projeyi Başlatın
Solution ayarlarından Multiple Startup Projects seçeneğini kullanarak hem CineTrack.WebAPI hem de CineTrack.MvcUI projelerini aynı anda başlatın.

API: http://localhost:5011 (Backend servisleri burada çalışır)

UI: http://localhost:5215 (Tarayıcıda bu adresi açın)

	Katkı Verenler
Bu projenin geliştirilmesinde emeği geçenler:

mustafa-kaplan1

mbudak42
