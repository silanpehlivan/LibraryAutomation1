namespace LibraryAutomation1.Models
{
    public class ErrorViewModel
    {
        // Ýstek Kimliði
        public string? RequestId { get; set; } // Hata oluþtuðunda bu hataya özel olarak atanan benzersiz bir kimlik. Null olabilir.

        // RequestId'nin gösterilip gösterilmeyeceðini belirleyen yardýmcý özellik.
        // Eðer RequestId boþ deðilse (yani bir deðeri varsa), 'true' döner ve gösterilmesi gerektiðini belirtir.
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}