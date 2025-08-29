 Open Banking Projesi
 Proje Hakkında
Bu proje, açık bankacılık kapsamında kullanıcıların farklı bankalardaki hesaplarını tek bir platform üzerinden görüntüleyebilmesini, bakiye ve işlem hareketlerini takip edebilmesini ve para transferi gerçekleştirebilmesini sağlamaktadır.

Backend tarafında .NET Core API, frontend tarafında ise Angular framework kullanılmıştır. Veritabanı olarak MySQL tercih edilmiştir.

  Özellikler
- Kullanıcı kayıt ve giriş (JWT tabanlı kimlik doğrulama)
- Dashboard ekranı üzerinden hesapların görüntülenmesi
- Hesap detaylarının modal penceresinde gösterilmesi
- Hesap hareketlerinin listelenmesi
- Para transferi işlemleri (IBAN doğrulama + bakiye kontrolü)
- Logout ve güvenli oturum yönetimi (Access + Refresh Token)

  Kullanılan Teknolojiler
- Backend: ASP.NET Core 8.0, Entity Framework Core, MySQL
- Frontend: Angular 16+, TypeScript, RxJS, Angular Material
- Kimlik Doğrulama: JWT (Access + Refresh Token)
- Versiyon Kontrol: Git & GitHub

  Proje Yapısı
/backend
   /Controllers
   /Services
   /Models
   /Dtos

/frontend
   /src/app
       /core
       /features
       /services
       /components

  Kurulum
Backend
- Repoyu klonla:
- git clone https://github.com/kullaniciAdi/proje-adi.git
- cd backend
Gerekli bağımlılıkları yükle:
- dotnet restore
Veritabanı migration çalıştır:
- dotnet ef database update
Projeyi çalıştır:
- dotnet run
Frontend
Frontend dizinine geç:
- cd frontend
Bağımlılıkları yükle:
- npm install
Uygulamayı başlat:
- ng serve --proxy-config proxy.conf.json -o

Ekran Görüntüleri
<img width="1919" height="929" alt="Ekran görüntüsü 2025-08-28 153001" src="https://github.com/user-attachments/assets/e2d60b91-e778-495e-9a11-3aaf76152173" />
<img width="1901" height="925" alt="Ekran görüntüsü 2025-08-28 153009" src="https://github.com/user-attachments/assets/59c9b555-d598-4fe6-9a32-646e0bf35acb" />
<img width="1885" height="925" alt="Ekran görüntüsü 2025-08-28 153029" src="https://github.com/user-attachments/assets/866480bd-a854-4e89-a826-fa4b6c4afdf6" />
<img width="1894" height="928" alt="Ekran görüntüsü 2025-08-28 153036" src="https://github.com/user-attachments/assets/31b0e16e-bc94-4950-b14a-c33877c34705" />
<img width="1895" height="923" alt="Ekran görüntüsü 2025-08-28 153050" src="https://github.com/user-attachments/assets/f20e2d96-5e2d-442a-8a7a-b63d25362081" />
<img width="1896" height="930" alt="Ekran görüntüsü 2025-08-28 153057" src="https://github.com/user-attachments/assets/6a6bdf14-a9e4-4a73-8afb-a7409a1b3bb7" />
<img width="1892" height="928" alt="Ekran görüntüsü 2025-08-28 153106" src="https://github.com/user-attachments/assets/109dcfed-392f-48fc-90dd-5efbd7ee57d4" />

Test Senaryoları
- Swagger üzerinden tüm endpointler test edilmiştir.
- 400, 401 ve 404 hata senaryoları kontrol edilmiştir.
- Null IBAN girildiğinde doğru hata mesajı dönmesi sağlanmıştır.

👤 Geliştirici
- Ad Soyad: Berat Soylu
- Trakya Üniversitesi - Bilgisayar Mühendisliği (Staj Projesi)
