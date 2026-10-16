using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using SmartMicrogrid.API.Data;
using SmartMicrogrid.API.Models.Common;
using SmartMicrogrid.API.Models.M1;
using SmartMicrogrid.API.Models.M2;
using SmartMicrogrid.API.Models.M4;
using SmartMicrogrid.API.Models.Transactions;
using Xunit;

namespace SmartMicrogrid.API.Tests;

public class DbSeederTests
{
    private static readonly DateTime Now = new(2026, 10, 16, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void DatasetCoversEveryModelAndHasStableUniqueIds()
    {
        var data = DbSeeder.CreateDemoData(Now);
        var rerun = DbSeeder.CreateDemoData(Now.AddDays(3));
        Assert.Equal(7, data.Count);
        Assert.Equal(18, data[MongoCollections.Users].Count);
        Assert.Equal(10, data[MongoCollections.Microgrids].Count);
        Assert.Equal(18, data[MongoCollections.EnergySlots].Count);
        Assert.Equal(16, data[MongoCollections.Reservations].Count);
        Assert.Equal(12, data[MongoCollections.Transactions].Count);
        Assert.Single(data[MongoCollections.SystemConfiguration]);
        Assert.NotEmpty(data[MongoCollections.SystemActivity]);
        foreach (var (name, documents) in data)
        {
            var ids = documents.Select(d => d["_id"].ToString()).ToList();
            Assert.Equal(ids.Count, ids.Distinct().Count());
            Assert.Equal(ids, rerun[name].Select(d => d["_id"].ToString()).ToList());
        }
    }

    [Fact]
    public void RecordsHaveValidForeignKeysAndMatchingTransactionPayloads()
    {
        var data = DbSeeder.CreateDemoData(Now);
        var users = data[MongoCollections.Users].Select(d => BsonSerializer.Deserialize<User>(d)).ToList();
        var nodes = data[MongoCollections.Microgrids].Select(d => BsonSerializer.Deserialize<MicrogridNode>(d)).ToDictionary(n => n.Id!);
        var slots = data[MongoCollections.EnergySlots].Select(d => BsonSerializer.Deserialize<EnergySlot>(d)).ToDictionary(s => s.Id!);
        var reservations = data[MongoCollections.Reservations].Select(d => BsonSerializer.Deserialize<Reservation>(d)).ToDictionary(r => r.Id!);
        var transactions = data[MongoCollections.Transactions].Select(d => BsonSerializer.Deserialize<Transaction>(d)).ToList();
        foreach (var node in nodes.Values)
            Assert.Contains(users, u => u.Id == node.OperatorId && u.Role == Role.MicrogridOperator);
        foreach (var slot in slots.Values) Assert.True(nodes.ContainsKey(slot.MicrogridNodeId));
        foreach (var reservation in reservations.Values)
        {
            Assert.Contains(users, u => u.Nic == reservation.ProsumerId && u.Role == Role.Prosumer);
            Assert.Equal(reservation.MicrogridNodeId, slots[reservation.EnergySlotId].MicrogridNodeId);
        }
        Assert.Equal(transactions.Count, transactions.Select(t => t.ReservationId).Distinct().Count());
        foreach (var transaction in transactions)
        {
            var reservation = reservations[transaction.ReservationId];
            Assert.Equal(reservation.ProsumerId, transaction.ProsumerId);
            Assert.Equal(reservation.MicrogridNodeId, transaction.MicrogridNodeId);
            Assert.Equal(reservation.EnergySlotId, transaction.EnergySlotId);
            Assert.Equal(reservation.EnergyAmount, transaction.EnergyAmount);
            if (!string.IsNullOrEmpty(transaction.QrCodeData))
                Assert.Equal($"SMART-MICROGRID|TRANSACTION|{transaction.Id}|{transaction.TransactionCode}", transaction.QrCodeData);
            if (transaction.VerifiedBy != null)
                Assert.Equal(nodes[transaction.MicrogridNodeId].OperatorId, transaction.VerifiedBy);
        }
        foreach (var record in data[MongoCollections.SystemActivity])
        {
            var activity = BsonSerializer.Deserialize<SystemActivity>(record);
            if (activity.UserId != null) Assert.Contains(users, u => u.Id == activity.UserId);
        }
        Assert.False(BsonSerializer.Deserialize<SystemConfiguration>(data[MongoCollections.SystemConfiguration].Single()).MaintenanceMode);
    }

    [Fact]
    public void OffersAreBookableWithinSevenDaysAndPastSlotsAreExpired()
    {
        var data = DbSeeder.CreateDemoData(Now);
        var slots = data[MongoCollections.EnergySlots].Select(d => BsonSerializer.Deserialize<EnergySlot>(d)).ToList();
        var available = slots.Where(s => s.Status == "Available").ToList();
        Assert.NotEmpty(available);
        foreach (var slot in slots)
        {
            Assert.True(slot.StartTime < slot.EndTime);
            Assert.InRange(slot.AvailableAmount, 0, slot.EnergyAmount);
            Assert.InRange(slot.PricePerUnit, 0m, 10000m);
            if (slot.EndTime <= Now) Assert.Equal("Expired", slot.Status);
        }
        Assert.All(available, slot =>
        {
            Assert.True(slot.StartTime > Now && slot.StartTime <= Now.AddDays(7));
            Assert.True(slot.AvailableAmount > 0);
        });
        Assert.Contains(slots, slot => slot.PricePerUnit == 25.00m);
    }
}
