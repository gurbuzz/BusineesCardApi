# BusinessCardAPI

Bu proje, kartvizit bilgilerinin (örneğin isim, soy isim, unvan, organizasyon, telefon, e-posta, adres ve web adresi gibi) yapay zeka kullanılarak ayrıştırılması amacıyla geliştirilmiş basit bir .NET web uygulamasıdır. Uygulama, kullanıcı tarafından gönderilen metin içerisindeki dil ve yazım hatalarını düzelttikten sonra, bu düzenlenmiş metni yapay zeka modeline göndererek doğru ve eksiksiz JSON çıktısı oluşturur.

## Özellikler

- **Kart Bilgisi Ayrıştırma:** Düzeltilmiş metni alıp, ilgili kart bilgilerini JSON formatında sunar.
- **REST API:** Kart işleme için `/api/v1/cards/process` uç noktasını sağlar.
- **API Güvenliği:** API kullanımı için API anahtarı doğrulaması yapılır.

## Kurulum

Proje .NET 8 kullanılarak geliştirilmiştir.

```bash
dotnet restore
dotnet run
```

## Ollama Kurulumu

Ollama'yı aşağıdaki gibi kurabilirsiniz:

```bash
curl -fsSL https://ollama.com/install.sh | sh
```

Kurulumdan sonra, Llama3.2 yapay zeka modelini indirmek için:

```bash
ollama run llama3.2:latest
```

Bu işlemden sonra ollama üzeriden kullanacağımız yapay zeka  modeli localhost:11434/api/generate serverinde aktif hale gelecektir.

## Kullanım

Endpoint:

```http
POST /api/v1/cards/process
```

### Örnek İstek

```json
{
  "ApiKey": "YOUR_API_KEY",
  "Message": "Ata gurbz, backend developer, atatech, 05000000000, ata@example.com, Mersin yenixehir"
}
```

### Örnek Yanıt

```json
{
  "cardData": {
    "name": "Ata",
    "surname": "Gürbüz",
    "titles": "Backend Developer",
    "organization": "",
    "phone": "05000000000",
    "email": [""],
    "address": "Mersin Yenişehir",
    "webAddress": ""
  }
}
```
