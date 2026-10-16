using MongoDB.Driver;
using SmartMicrogrid.API.Helpers;
using SmartMicrogrid.API.Models.Transactions;

namespace SmartMicrogrid.API.Data;

public static partial class DbSeeder
{
    /// <summary>Repairs only the known legacy sample payloads; never rotates a valid issued QR.</summary>
    public static async Task MigrateTransactionQrPayloadsAsync(MongoDbContext context)
    {
        var records = await context.Transactions.Find(t => t.QrCodeData != "" && t.TransactionCode != "").ToListAsync();
        long repaired = 0;
        foreach (var transaction in records)
        {
            if (!IsLegacyQrPayload(transaction)) continue;
            var filter = Builders<Transaction>.Filter.Eq(t => t.Id, transaction.Id) &
                Builders<Transaction>.Filter.Eq(t => t.QrCodeData, transaction.QrCodeData);
            var result = await context.Transactions.UpdateOneAsync(filter, Builders<Transaction>.Update.Set(
                t => t.QrCodeData, TransactionQrPayload.Create(transaction.Id, transaction.TransactionCode)));
            repaired += result.ModifiedCount;
        }
        Console.WriteLine($"Transaction QR migration: {repaired} legacy sample payloads repaired; valid issued QR codes preserved.");
    }

    public static bool IsLegacyQrPayload(Transaction transaction)
    {
        var parts = transaction.QrCodeData.Split('|');
        return parts.Length == 4 && parts[0] == "SMART-MICROGRID" && parts[1] == "TRANSACTION" &&
            parts[3] == transaction.TransactionCode && parts[2] is "pending" or "qrgenerated" or "verified" or "completed" or "verificationpending";
    }
}
