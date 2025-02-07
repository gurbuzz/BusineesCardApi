# Bus-neesCardAp-BusinessCardAPI

BusinessCardAPI, metin içerisindeki kişisel bilgileri ayrıştırarak bir BusinessCard nesnesine dönüştüren bir RESTful API'dir. Proje, Ollama (Llama tabanlı LLM) kullanarak metinden ilgili bilgileri çıkarır.

🚀 Özellikler

Metin içerisindeki ad, ünvan, organizasyon, telefon, e-posta, adres ve web adresi bilgilerini tespit eder.

Ollama LLM modeli ile doğal dil işleme tabanlı veri ayrıştırma.

Swagger UI ile API endpoint'lerini test etme.

ASP.NET Core 6 ve Dependency Injection (DI) kullanılarak modüler yapı.

🛠 Kurulum

1️⃣ Projeyi İndir

git clone 
cd BusinessCardAPI

2️⃣ Gerekli Paketleri Yükle

dotnet restore

3️⃣ Ollama Sunucusunu Çalıştır

Projede Llama3.2 modelini kullanıyoruz. Ollama'nın çalıştığından emin olun:

ollama run  Llama3.2

Alternatif olarak Ollama'yı sistemine yükleyip kullanabilirsin.

4️⃣ Projeyi Çalıştır

dotnet run

Proje, http://localhost:5001 adresinde çalışacaktır.

Swagger UI'yi görmek için:

http://localhost:5001/swagger

📌 API Endpoint'leri

**1️⃣ **POST /api/v1/cards/process

Belirtilen metinden ilgili alanları çıkartır.

✅ Request Body

{
   "message": "Your Card Message"
}

✅ **Response Body **

{
  "cardData": {
    "fullname": "Fahri Babacan",
    "titles": "Doçenti & Avukat",
    "organization": "Mersin Üniversitesi",
    "phone": "#90 324 361 00",
    "email": ["fahriozgungurE@gmail.com"],
    "address": "Çiftlikköy Kampüsü, Mersin, Türkiye",
    "webAddress": "Not available"
  }
}

**2️⃣ **POST /api/v1/cards/raw

LLM'den gelen ham cevabı döndürür. İlerleyen süreçlerde Card yapısını tekrar parse etmek için kullanılabilir. 

✅ Response Body

{
  "rawText": "Here is the extracted information:\n\n- Full name: ..."
}

🏗 Geliştirme

Yeni Model Entegrasyonu: OllamaClient.cs içindeki model adı güncellenebilir.

Hata Yönetimi: Daha kapsamlı hata yönetimi için LLMService.cs geliştirilebilir.

Unit Test: BusinessCardParser.cs için test senaryoları eklenebilir.