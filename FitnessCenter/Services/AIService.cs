using FitnessCenter.Models.ViewModels;
using System.Text;
using System.Text.Json;

namespace FitnessCenter.Services
{
    public interface IAIService
    {
        Task<string> GetFitnessRecommendationAsync(AIRecommendationViewModel model);
    }

    public class AIService : IAIService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AIService> _logger;

        public AIService(HttpClient httpClient, IConfiguration configuration, ILogger<AIService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<string> GetFitnessRecommendationAsync(AIRecommendationViewModel model)
        {
            // .env dosyasından API key'i al
            var apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
            
            // If no API key, return a demo recommendation
            if (string.IsNullOrEmpty(apiKey))
            {
                return GenerateDemoRecommendation(model);
            }

            try
            {
                var prompt = BuildPrompt(model);

                // Gemini API request format
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

                // Gemini API endpoint
                var response = await _httpClient.PostAsync(
                    $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent?key={apiKey}", 
                    content);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var result = JsonDocument.Parse(responseContent);
                    
                    // Parse Gemini response
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
                    _logger.LogError($"Gemini API Error: {response.StatusCode} - {errorContent}");
                    return GenerateDemoRecommendation(model);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling Gemini API");
                return GenerateDemoRecommendation(model);
            }
        }

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

        private string GenerateDemoRecommendation(AIRecommendationViewModel model)
        {
            var sb = new StringBuilder();
            
            // Calculate BMI if possible
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
