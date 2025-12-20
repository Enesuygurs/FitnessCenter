using FitnessCenter.Models.ViewModels;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace FitnessCenter.Services
{
    // AI servisi arayüzü
    public interface IAIService
    {
        Task<(string textRecommendation, string? imageUrl)> GetFitnessRecommendationAsync(AIRecommendationViewModel model);
    }

    // Google Gemini AI servisi
    public class AIService : IAIService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AIService> _logger;
        
        // Fotoğraf analizinden elde edilen bilgiler
        private string? _photoAnalysisResult;
        private string? _base64Photo;

        public AIService(HttpClient httpClient, IConfiguration configuration, ILogger<AIService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        // Fitness önerisi ve görsel üret
        public async Task<(string textRecommendation, string? imageUrl)> GetFitnessRecommendationAsync(AIRecommendationViewModel model)
        {
            // API anahtarlarını ortam değişkeninden al
            var geminiApiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
            
            string textRecommendation;
            _photoAnalysisResult = null;
            _base64Photo = null;
            
            // Fotoğraf varsa base64'e çevir (sadece görsel üretimi için)
            if (model.Photo != null && model.Photo.Length > 0)
            {
                // Fotoğrafı base64'e çevir ve sakla
                using (var ms = new MemoryStream())
                {
                    await model.Photo.CopyToAsync(ms);
                    _base64Photo = Convert.ToBase64String(ms.ToArray());
                }
            }
            
            // Gemini sadece metin önerisi üretsin (fotoğraf analizi yapmasın)
            textRecommendation = await GetRecommendationWithoutPhoto(model, geminiApiKey);
            
            // Hedef vücut görseli üret - Replicate ve Pollinations sorumlu
            var replicateToken = Environment.GetEnvironmentVariable("REPLICATE_API_TOKEN");
            string? imageUrl;
            
            // Replicate API varsa ve fotoğraf yüklendiyse gerçek dönüşüm yap
            if (!string.IsNullOrEmpty(replicateToken) && !string.IsNullOrEmpty(_base64Photo))
            {
                imageUrl = await GenerateImageWithReplicate(model, _base64Photo, replicateToken);
            }
            else
            {
                // Replicate yoksa Pollinations.ai kullan (fallback)
                imageUrl = await GenerateTargetBodyImage(model, _photoAnalysisResult);
            }
            
            return (textRecommendation, imageUrl);
        }

        // Replicate API ile görsel üret (fotoğrafı kullanarak)
        private async Task<string?> GenerateImageWithReplicate(AIRecommendationViewModel model, string base64Photo, string replicateToken)
        {
            try
            {
                _logger.LogInformation("Replicate API ile görsel üretiliyor...");
                
                // Hedef prompt oluştur
                var targetPrompt = BuildReplicatePrompt(model);
                
                // Fotoğrafı data URI formatına çevir
                var imageDataUri = $"data:image/jpeg;base64,{base64Photo}";
                
                // Replicate API - Flux Dev img2img modeli kullan (daha iyi sonuçlar)
                // Model: black-forest-labs/flux-dev
                var requestBody = new
                {
                    input = new
                    {
                        image = imageDataUri,
                        prompt = targetPrompt + ", professional fitness photography, 8k uhd, highly detailed, photorealistic",
                        guidance = 3.5,
                        num_outputs = 1,
                        aspect_ratio = "3:4",
                        output_format = "jpg",
                        output_quality = 70,
                        prompt_strength = 0.45, // Orijinal fotoğrafa daha sadık (düşük = daha benzer)
                        num_inference_steps = 28
                    }
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Token {replicateToken}");

                var response = await _httpClient.PostAsync(
                    "https://api.replicate.com/v1/models/black-forest-labs/flux-dev/predictions",
                    content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation($"Replicate yanıtı: {responseContent}");
                    
                    var result = JsonDocument.Parse(responseContent);
                    var predictionId = result.RootElement.GetProperty("id").GetString();
                    
                    // Sonucu bekle
                    if (!string.IsNullOrEmpty(predictionId))
                    {
                        return await WaitForReplicateResult(predictionId, replicateToken);
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Replicate API Hatası: {response.StatusCode} - {errorContent}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Replicate API çağrısında hata");
            }

            // Hata durumunda Pollinations.ai'a fallback
            _logger.LogWarning("Replicate başarısız oldu, Pollinations.ai'a geçiliyor...");
            return await GenerateTargetBodyImage(model, _photoAnalysisResult);
        }

        // Replicate sonucunu bekle
        private async Task<string?> WaitForReplicateResult(string predictionId, string replicateToken)
        {
            _logger.LogInformation($"Replicate prediction {predictionId} bekleniyor...");
            
            for (int i = 0; i < 60; i++) // 2 dakika bekle (60 x 2 saniye)
            {
                await Task.Delay(2000);
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Token {replicateToken}");

                var response = await _httpClient.GetAsync($"https://api.replicate.com/v1/predictions/{predictionId}");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonDocument.Parse(content);
                    var status = result.RootElement.GetProperty("status").GetString();
                    
                    _logger.LogInformation($"Replicate status: {status} (deneme {i + 1}/60)");

                    if (status == "succeeded")
                    {
                        var output = result.RootElement.GetProperty("output");
                        if (output.ValueKind == JsonValueKind.Array && output.GetArrayLength() > 0)
                        {
                            var imageUrl = output[0].GetString();
                            _logger.LogInformation($"Replicate görsel başarıyla üretildi: {imageUrl}");
                            return imageUrl;
                        }
                        else if (output.ValueKind == JsonValueKind.String)
                        {
                            var imageUrl = output.GetString();
                            _logger.LogInformation($"Replicate görsel başarıyla üretildi: {imageUrl}");
                            return imageUrl;
                        }
                    }
                    else if (status == "failed" || status == "canceled")
                    {
                        _logger.LogError($"Replicate başarısız: {status}");
                        if (result.RootElement.TryGetProperty("error", out var error))
                        {
                            _logger.LogError($"Replicate hatası: {error.GetString()}");
                        }
                        break;
                    }
                }
                else
                {
                    _logger.LogError($"Replicate status sorgulaması başarısız: {response.StatusCode}");
                }
            }
            
            _logger.LogError("Replicate timeout - görsel üretilemedi");
            return null;
        }

        // Replicate için prompt oluştur
        private string BuildReplicatePrompt(AIRecommendationViewModel model)
        {
            var sb = new StringBuilder();
            
            // Cinsiyet
            var gender = model.Gender?.ToLower() ?? "";
            var isMale = gender.Contains("erkek");
            var isFemale = gender.Contains("kadın");
            
            if (isMale)
            {
                sb.Append("athletic muscular man, fit male body, ");
            }
            else if (isFemale)
            {
                sb.Append("athletic fit woman, toned female body, ");
            }
            else
            {
                sb.Append("athletic fit person, ");
            }
            
            // Hedef
            var goal = model.FitnessGoal?.ToLower() ?? "";
            if (goal.Contains("muscle") || goal.Contains("kas"))
            {
                sb.Append("very muscular, defined muscles, six pack abs, bodybuilder physique, ");
            }
            else if (goal.Contains("weight") || goal.Contains("kilo"))
            {
                sb.Append("lean slim body, low body fat, toned physique, ");
            }
            else
            {
                sb.Append("healthy fit body, balanced physique, ");
            }
            
            sb.Append("professional fitness photography, gym environment, good lighting, high quality, realistic");
            
            return sb.ToString();
        }

        // Fotoğrafı Gemini Vision ile analiz et
        private async Task<string?> AnalyzePhotoWithGemini(AIRecommendationViewModel model, string? apiKey, string base64Image)
        {
            if (string.IsNullOrEmpty(apiKey))
            {
                return null;
            }

            try
            {
                // Analiz prompt'u - vücut özelliklerini tespit et
                var analysisPrompt = @"Bu fotoğrafı analiz et ve şu bilgileri JSON formatında döndür:
{
    ""bodyType"": ""ince/normal/kaslı/kilolu"",
    ""estimatedBodyFat"": ""düşük/orta/yüksek"",
    ""muscleDefinition"": ""az/orta/yüksek"",
    ""skinTone"": ""açık/orta/koyu"",
    ""hairColor"": ""sarı/kahverengi/siyah/kızıl"",
    ""hairLength"": ""kısa/orta/uzun"",
    ""apparentAge"": ""genç/orta yaşlı/yaşlı"",
    ""physicalFeatures"": ""kısa açıklama""
}
Sadece JSON döndür, başka bir şey yazma.";

                var requestBody = new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new object[]
                            {
                                new { text = analysisPrompt },
                                new 
                                { 
                                    inline_data = new 
                                    { 
                                        mime_type = "image/jpeg",
                                        data = base64Image 
                                    } 
                                }
                            }
                        }
                    },
                    generationConfig = new
                    {
                        temperature = 0.3,
                        maxOutputTokens = 500
                    }
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _httpClient.DefaultRequestHeaders.Clear();

                var response = await _httpClient.PostAsync(
                    $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent?key={apiKey}", 
                    content);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var result = JsonDocument.Parse(responseContent);
                    
                    var analysisResult = result.RootElement
                        .GetProperty("candidates")[0]
                        .GetProperty("content")
                        .GetProperty("parts")[0]
                        .GetProperty("text")
                        .GetString();

                    _logger.LogInformation($"Fotoğraf analiz sonucu: {analysisResult}");
                    return analysisResult;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fotoğraf analizi sırasında hata");
            }

            return null;
        }

        // Fotoğraf analizi ile öneri al
        private async Task<string> GetRecommendationWithPhotoAnalysis(AIRecommendationViewModel model, string? apiKey, string? photoAnalysis, string base64Image)
        {
            if (string.IsNullOrEmpty(apiKey))
            {
                return GenerateDemoRecommendation(model);
            }

            try
            {
                var prompt = BuildPrompt(model);
                if (!string.IsNullOrEmpty(photoAnalysis))
                {
                    prompt += $"\n\nFotoğraf Analiz Sonucu:\n{photoAnalysis}\n\nBu analiz sonucuna göre önerilerini kişiselleştir.";
                }

                // Gemini Vision API istek formatı
                var requestBody = new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new object[]
                            {
                                new { text = "Sen bir profesyonel fitness ve beslenme danışmanısın. Türkçe yanıt ver. Kullanıcının verdiği bilgilere ve fotoğrafına göre kişiselleştirilmiş egzersiz ve diyet önerileri sun.\n\n" + prompt },
                                new 
                                { 
                                    inline_data = new 
                                    { 
                                        mime_type = "image/jpeg",
                                        data = base64Image 
                                    } 
                                }
                            }
                        }
                    },
                    generationConfig = new
                    {
                        temperature = 0.7,
                        maxOutputTokens = 4096
                    }
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _httpClient.DefaultRequestHeaders.Clear();

                // Gemini API'ye istek gönder
                var response = await _httpClient.PostAsync(
                    $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent?key={apiKey}", 
                    content);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var result = JsonDocument.Parse(responseContent);
                    
                    // Gemini yanıtını ayrıştır
                    var recommendation = result.RootElement
                        .GetProperty("candidates")[0]
                        .GetProperty("content")
                        .GetProperty("parts")[0]
                        .GetProperty("text")
                        .GetString();

                    return recommendation ?? "Öneri alınamadı.";
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Gemini API Hatası: {response.StatusCode} - {errorContent}");
                    return GenerateDemoRecommendation(model);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Gemini Vision API çağrısında hata");
                return GenerateDemoRecommendation(model);
            }
        }

        // Fotoğrafsız öneri
        private async Task<string> GetRecommendationWithoutPhoto(AIRecommendationViewModel model, string? apiKey)
        {
            // API anahtarı yoksa demo öneri döndür
            if (string.IsNullOrEmpty(apiKey))
            {
                return GenerateDemoRecommendation(model);
            }

            try
            {
                var prompt = BuildPrompt(model);

                // Gemini API istek formatı
                var requestBody = new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new[]
                            {
                                new { text = "Sen bir profesyonel fitness ve beslenme danışmanısın. Türkçe yanıt ver. Kullanıcının verdiği bilgilere göre kişiselleştirilmiş egzersiz ve diyet önerileri sun.\n\n" + prompt }
                            }
                        }
                    },
                    generationConfig = new
                    {
                        temperature = 0.7,
                        maxOutputTokens = 4096
                    }
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _httpClient.DefaultRequestHeaders.Clear();

                // Gemini API'ye istek gönder
                var response = await _httpClient.PostAsync(
                    $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent?key={apiKey}", 
                    content);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var result = JsonDocument.Parse(responseContent);
                    
                    // Gemini yanıtını ayrıştır
                    var recommendation = result.RootElement
                        .GetProperty("candidates")[0]
                        .GetProperty("content")
                        .GetProperty("parts")[0]
                        .GetProperty("text")
                        .GetString();

                    return recommendation ?? "Öneri alınamadı.";
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Gemini API Hatası: {response.StatusCode} - {errorContent}");
                    return GenerateDemoRecommendation(model);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Gemini API çağrısında hata");
                return GenerateDemoRecommendation(model);
            }
        }

        // Hedef vücut görseli üret (Pollinations.ai - ücretsiz)
        private async Task<string?> GenerateTargetBodyImage(AIRecommendationViewModel model, string? photoAnalysis)
        {
            try
            {
                // Görsel prompt oluştur (fotoğraf analizi varsa kullan)
                var imagePrompt = BuildImagePrompt(model, photoAnalysis);
                
                // Her seferinde farklı görsel üretmek için seed ekle
                var seed = DateTime.Now.Ticks.ToString();
                
                // Pollinations.ai URL'i oluştur (URL encode)
                var encodedPrompt = Uri.EscapeDataString(imagePrompt);
                var imageUrl = $"https://image.pollinations.ai/prompt/{encodedPrompt}?width=512&height=768&model=flux&seed={seed}&nologo=true&enhance=true";
                
                _logger.LogInformation($"Görsel URL oluşturuldu: {imageUrl}");
                
                return imageUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Görsel üretiminde hata");
                return null;
            }
        }

        // Görsel prompt oluştur (fotoğraf analizi ile zenginleştirilmiş)
        private string BuildImagePrompt(AIRecommendationViewModel model, string? photoAnalysis)
        {
            var sb = new StringBuilder();
            
            // Fotoğraf analizinden bilgileri çıkar
            string skinTone = "medium skin tone";
            string hairColor = "dark hair";
            string hairLength = "";
            string currentBodyType = "";
            
            if (!string.IsNullOrEmpty(photoAnalysis))
            {
                try
                {
                    // JSON'dan bilgileri çıkarmaya çalış
                    var analysis = photoAnalysis.ToLower();
                    
                    // Cilt tonu
                    if (analysis.Contains("\"skintone\"") || analysis.Contains("\"skin_tone\"") || analysis.Contains("cilt"))
                    {
                        if (analysis.Contains("açık") || analysis.Contains("light") || analysis.Contains("fair"))
                            skinTone = "fair light skin";
                        else if (analysis.Contains("koyu") || analysis.Contains("dark"))
                            skinTone = "dark brown skin";
                        else
                            skinTone = "medium olive skin";
                    }
                    
                    // Saç rengi
                    if (analysis.Contains("sarı") || analysis.Contains("blonde"))
                        hairColor = "blonde hair";
                    else if (analysis.Contains("kızıl") || analysis.Contains("red"))
                        hairColor = "red hair";
                    else if (analysis.Contains("kahverengi") || analysis.Contains("brown"))
                        hairColor = "brown hair";
                    else
                        hairColor = "black hair";
                    
                    // Saç uzunluğu
                    if (analysis.Contains("uzun") || analysis.Contains("long"))
                        hairLength = "long ";
                    else if (analysis.Contains("kısa") || analysis.Contains("short"))
                        hairLength = "short ";
                    else
                        hairLength = "medium length ";
                    
                    // Mevcut vücut tipi (hedef için ters çevireceğiz)
                    if (analysis.Contains("kilolu") || analysis.Contains("overweight") || analysis.Contains("yüksek"))
                        currentBodyType = "overweight";
                    else if (analysis.Contains("ince") || analysis.Contains("thin") || analysis.Contains("zayıf"))
                        currentBodyType = "thin";
                    else
                        currentBodyType = "normal";
                }
                catch
                {
                    // Analiz parse edilemezse varsayılanları kullan
                }
            }
            
            // Cinsiyet bazlı detaylı tanım
            var gender = model.Gender?.ToLower() ?? "";
            var isMale = gender.Contains("erkek") && !gender.Contains("kadın");
            var isFemale = gender.Contains("kadın");
            
            if (isMale)
            {
                sb.Append($"professional fitness photography of athletic man, {skinTone}, {hairLength}{hairColor}, ");
                sb.Append("masculine features, handsome face, ");
            }
            else if (isFemale)
            {
                sb.Append($"professional fitness photography of athletic woman, {skinTone}, {hairLength}{hairColor}, ");
                sb.Append("feminine features, beautiful face, ");
            }
            else
            {
                sb.Append($"professional fitness photography of athletic person, {skinTone}, {hairColor}, ");
            }
            
            // Hedef bazlı vücut tipi
            var goal = model.FitnessGoal?.ToLower() ?? "";
            if (goal.Contains("kas") || goal.Contains("musclegain") || goal.Contains("muscle"))
            {
                if (isMale)
                {
                    sb.Append("very muscular body, strong biceps and chest, six pack abs, bodybuilder physique, ");
                }
                else if (isFemale)
                {
                    sb.Append("toned athletic body, defined muscles, fit abs, strong feminine physique, ");
                }
                else
                {
                    sb.Append("muscular and toned body, strong physique, ");
                }
            }
            else if (goal.Contains("kilo") || goal.Contains("weightloss") || goal.Contains("weight"))
            {
                if (isMale)
                {
                    sb.Append("lean muscular body, slim waist, defined abs, fit athletic male, ");
                }
                else if (isFemale)
                {
                    sb.Append("slim toned body, lean athletic figure, fit waist, graceful feminine physique, ");
                }
                else
                {
                    sb.Append("lean and fit body, slim physique, ");
                }
            }
            else
            {
                if (isMale)
                {
                    sb.Append("balanced athletic male body, healthy muscular build, fit physique, ");
                }
                else if (isFemale)
                {
                    sb.Append("balanced athletic female body, healthy toned figure, fit physique, ");
                }
                else
                {
                    sb.Append("healthy and toned body, balanced physique, ");
                }
            }
            
            // Genel özellikler
            sb.Append("confident pose in gym, professional studio lighting, ");
            sb.Append("high quality, detailed, realistic, 8k resolution");
            
            return sb.ToString();
        }

        // Prompt oluştur
        private string BuildPrompt(AIRecommendationViewModel model)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Kullanıcı bilgileri:");
            
            if (model.Height.HasValue)
                sb.AppendLine($"- Boy: {model.Height} cm");
            
            if (model.Weight.HasValue)
                sb.AppendLine($"- Kilo: {model.Weight} kg");
            
            if (model.Age.HasValue)
                sb.AppendLine($"- Yaş: {model.Age}");
            
            if (!string.IsNullOrEmpty(model.Gender))
                sb.AppendLine($"- Cinsiyet: {model.Gender}");
            
            if (!string.IsNullOrEmpty(model.BodyType))
                sb.AppendLine($"- Vücut Tipi: {model.BodyType}");
            
            if (!string.IsNullOrEmpty(model.FitnessGoal))
                sb.AppendLine($"- Hedef: {model.FitnessGoal}");
            
            if (!string.IsNullOrEmpty(model.ActivityLevel))
                sb.AppendLine($"- Aktivite Seviyesi: {model.ActivityLevel}");
            
            if (!string.IsNullOrEmpty(model.HealthConditions))
                sb.AppendLine($"- Sağlık Durumu: {model.HealthConditions}");

            sb.AppendLine();
            sb.AppendLine("Bu bilgilere göre kullanıcıya:");
            sb.AppendLine("1. Haftalık egzersiz programı öner");
            sb.AppendLine("2. Günlük diyet önerileri ver");
            sb.AppendLine("3. Spor salonumuzda hangi hizmetleri (Fitness, Yoga, Pilates, Kişisel Antrenman) tercih etmesi gerektiğini öner");
            sb.AppendLine("4. Genel sağlık ve fitness tavsiyeleri ver");

            return sb.ToString();
        }

        // Demo öneri oluştur (API yoksa)
        private string GenerateDemoRecommendation(AIRecommendationViewModel model)
        {
            var sb = new StringBuilder();
            
            // VKİ hesapla
            double? bmi = null;
            string bmiCategory = "";
            if (model.Height.HasValue && model.Weight.HasValue)
            {
                var heightInMeters = model.Height.Value / 100.0;
                bmi = model.Weight.Value / (heightInMeters * heightInMeters);
                
                if (bmi < 18.5) bmiCategory = "Zayıf";
                else if (bmi < 25) bmiCategory = "Normal";
                else if (bmi < 30) bmiCategory = "Fazla Kilolu";
                else bmiCategory = "Obez";
            }

            sb.AppendLine("## 🏋️ Kişisel Fitness Öneriniz");
            sb.AppendLine();

            if (bmi.HasValue)
            {
                sb.AppendLine($"### 📊 Vücut Kütle İndeksi (BMI): {bmi:F1} ({bmiCategory})");
                sb.AppendLine();
            }

            sb.AppendLine("### 💪 Haftalık Egzersiz Programı");
            sb.AppendLine();

            var goal = model.FitnessGoal?.ToLower() ?? "";
            
            if (goal.Contains("kilo") && goal.Contains("ver"))
            {
                sb.AppendLine("**Kilo Verme Odaklı Program:**");
                sb.AppendLine("- **Pazartesi:** 45 dk Kardio + 20 dk Core çalışması");
                sb.AppendLine("- **Salı:** HIIT Antrenmanı (30 dk)");
                sb.AppendLine("- **Çarşamba:** Yoga veya Pilates (60 dk)");
                sb.AppendLine("- **Perşembe:** 40 dk Kardio + Alt vücut antrenmanı");
                sb.AppendLine("- **Cuma:** HIIT Antrenmanı (30 dk)");
                sb.AppendLine("- **Cumartesi:** Uzun tempolu yürüyüş veya bisiklet (45-60 dk)");
                sb.AppendLine("- **Pazar:** Dinlenme veya hafif esneme");
            }
            else if (goal.Contains("kas") || goal.Contains("geliştir"))
            {
                sb.AppendLine("**Kas Geliştirme Odaklı Program:**");
                sb.AppendLine("- **Pazartesi:** Göğüs + Triceps (60 dk)");
                sb.AppendLine("- **Salı:** Sırt + Biceps (60 dk)");
                sb.AppendLine("- **Çarşamba:** Bacak günü (60 dk)");
                sb.AppendLine("- **Perşembe:** Omuz + Core (45 dk)");
                sb.AppendLine("- **Cuma:** Kol ve aksesuar kasları (45 dk)");
                sb.AppendLine("- **Cumartesi:** Tam vücut antrenmanı (60 dk)");
                sb.AppendLine("- **Pazar:** Dinlenme");
            }
            else
            {
                sb.AppendLine("**Genel Kondisyon Programı:**");
                sb.AppendLine("- **Pazartesi:** Tam vücut kuvvet antrenmanı (45 dk)");
                sb.AppendLine("- **Salı:** Kardio (30 dk) + Esneme (15 dk)");
                sb.AppendLine("- **Çarşamba:** Yoga veya Pilates (60 dk)");
                sb.AppendLine("- **Perşembe:** HIIT veya Fonksiyonel antrenman (30 dk)");
                sb.AppendLine("- **Cuma:** Üst vücut + Core (45 dk)");
                sb.AppendLine("- **Cumartesi:** Aktif dinlenme - yürüyüş veya hafif aktivite");
                sb.AppendLine("- **Pazar:** Dinlenme");
            }

            sb.AppendLine();
            sb.AppendLine("### 🥗 Günlük Beslenme Önerileri");
            sb.AppendLine();
            sb.AppendLine("**Kahvaltı:** Yulaf ezmesi, yumurta, tam tahıllı ekmek, meyve");
            sb.AppendLine();
            sb.AppendLine("**Ara Öğün:** Yoğurt veya bir avuç kuruyemiş");
            sb.AppendLine();
            sb.AppendLine("**Öğle:** Izgara tavuk/balık, bulgur pilavı, bol sebze");
            sb.AppendLine();
            sb.AppendLine("**Ara Öğün:** Meyve veya protein bar");
            sb.AppendLine();
            sb.AppendLine("**Akşam:** Hafif protein (ton balığı, yumurta), salata");
            sb.AppendLine();
            sb.AppendLine("**Günlük su tüketimi:** En az 2-3 litre");

            sb.AppendLine();
            sb.AppendLine("### 🎯 Önerilen Hizmetlerimiz");
            sb.AppendLine();

            if (goal.Contains("kilo") && goal.Contains("ver"))
            {
                sb.AppendLine("1. **Kilo Verme Programı** - Kardio ve direnç kombinasyonu");
                sb.AppendLine("2. **Kişisel Antrenman** - Birebir takip ile maksimum verim");
                sb.AppendLine("3. **Pilates** - Core güçlendirme ve esneklik");
            }
            else if (goal.Contains("kas") || goal.Contains("geliştir"))
            {
                sb.AppendLine("1. **Kas Geliştirme Programı** - Yoğun kuvvet antrenmanı");
                sb.AppendLine("2. **Kişisel Antrenman** - Doğru teknik ve maksimum verim");
                sb.AppendLine("3. **Fitness** - Genel kas gelişimi");
            }
            else
            {
                sb.AppendLine("1. **Fitness** - Genel kondisyon geliştirme");
                sb.AppendLine("2. **Yoga** - Esneklik ve rahatlama");
                sb.AppendLine("3. **Pilates** - Core güçlendirme");
            }

            sb.AppendLine();
            sb.AppendLine("### 💡 Genel Tavsiyeler");
            sb.AppendLine();
            sb.AppendLine("- Antrenman öncesi mutlaka ısının, sonrasında esneme yapın");
            sb.AppendLine("- Yeterli uyku alın (7-8 saat)");
            sb.AppendLine("- İlerlemelerinizi takip edin ve motivasyonunuzu yüksek tutun");
            sb.AppendLine("- Profesyonel antrenörlerimizden destek almaktan çekinmeyin");
            sb.AppendLine();
            sb.AppendLine("*Bu öneriler genel niteliktedir. Kişisel antrenman seansı ile daha detaylı bir program oluşturabiliriz.*");

            return sb.ToString();
        }
    }
}
