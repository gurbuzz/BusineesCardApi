# Bus-neesCardAp-BusinessCardAPI

BusinessCardAPI, metin içerisindeki kişisel bilgileri ayrıştırarak bir BusinessCard nesnesine dönüştüren bir RESTful API'dir. Proje, Ollama (Llama tabanlı LLM) kullanarak metinden ilgili bilgileri çıkarır.

🚀 Özellikler

Metin içerisindeki ad, ünvan, organizasyon, telefon, e-posta, adres ve web adresi bilgilerini tespit eder.

Ollama LLM modeli ile doğal dil işleme tabanlı veri ayrıştırma.

Swagger UI ile API endpoint'lerini test etme.

Proje, http://localhost:5001 adresinde çalışacaktır.

Swagger UI'yi görmek için:

http://localhost:5001/swagger

📌 API Endpoint'leri

**1️⃣ **POST /api/v1/cards/process
