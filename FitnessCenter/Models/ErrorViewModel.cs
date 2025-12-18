namespace FitnessCenter.Models;

// Hata sayfası görünüm modeli
public class ErrorViewModel
{
    public string? RequestId { get; set; }

    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
}
