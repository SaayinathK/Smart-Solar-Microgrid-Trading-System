namespace SmartMicrogrid.API.Helpers;

/// <summary>The single QR payload contract used by web, Android and demo data.</summary>
public static class TransactionQrPayload
{
    public static string Create(string transactionId, string transactionCode)
    {
        if (string.IsNullOrWhiteSpace(transactionId) || string.IsNullOrWhiteSpace(transactionCode) ||
            transactionId.Contains('|') || transactionCode.Contains('|'))
            throw new ArgumentException("A transaction ID and code are required for the transaction QR.");
        return $"SMART-MICROGRID|TRANSACTION|{transactionId}|{transactionCode}";
    }
}
