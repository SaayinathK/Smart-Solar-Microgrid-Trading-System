using MongoDB.Bson;
using MongoDB.Driver;
using SmartMicrogrid.API.Data;
using SmartMicrogrid.API.Models.M1;
using SmartMicrogrid.API.Models.M2;
using SmartMicrogrid.API.Repositories.Interfaces;

namespace SmartMicrogrid.API.Repositories.Implementation;

public class ReservationRepository : IReservationRepository
{
    private readonly MongoDbContext _context;
    public ReservationRepository(MongoDbContext context) => _context = context;

    public async Task<Reservation> CreateAsync(Reservation reservation)
    {
        await _context.Reservations.InsertOneAsync(reservation);
        return reservation;
    }
    public async Task<Reservation?> GetByIdAsync(string id)
    {
        if (!ObjectId.TryParse(id, out _)) return null;
        return await _context.Reservations.Find(r => r.Id == id).FirstOrDefaultAsync();
    }

    public Task<List<Reservation>> GetAsync(string? prosumerId, string? status, string? nodeId, int page, int pageSize)
    {
        var f = Builders<Reservation>.Filter; var filter = f.Empty;
        if (!string.IsNullOrWhiteSpace(prosumerId)) filter &= f.Eq(r => r.ProsumerId, prosumerId);
        if (!string.IsNullOrWhiteSpace(status)) filter &= f.Eq(r => r.Status, status);
        if (!string.IsNullOrWhiteSpace(nodeId) && ObjectId.TryParse(nodeId, out _)) filter &= f.Eq(r => r.MicrogridNodeId, nodeId);
        return _context.Reservations.Find(filter).SortByDescending(r => r.CreatedAt).Skip((page - 1) * pageSize).Limit(pageSize).ToListAsync();
    }

    public async Task<bool> TransitionAsync(string id, string expectedStatus, string newStatus, string changedBy, string? reason = null)
    {
        if (!ObjectId.TryParse(id, out _)) return false;
        var evt = new ReservationStatusEvent { From = expectedStatus, To = newStatus, ChangedBy = changedBy, ChangedAt = DateTime.UtcNow, Reason = reason };
        var update = Builders<Reservation>.Update.Set(r => r.Status, newStatus).Set(r => r.UpdatedAt, DateTime.UtcNow).Push(r => r.StatusHistory, evt);
        var result = await _context.Reservations.UpdateOneAsync(r => r.Id == id && r.Status == expectedStatus, update);
        return result.ModifiedCount == 1;
    }
    public async Task<bool> UpdateBookingAsync(Reservation updated, string expectedStatus)
    {
        if (!ObjectId.TryParse(updated.Id, out _)) return false;
        var change = Builders<Reservation>.Update
            .Set(r => r.EnergySlotId, updated.EnergySlotId)
            .Set(r => r.MicrogridNodeId, updated.MicrogridNodeId)
            .Set(r => r.EnergyAmount, updated.EnergyAmount)
            .Set(r => r.ReservationDate, updated.ReservationDate)
            .Set(r => r.StartTime, updated.StartTime)
            .Set(r => r.EndTime, updated.EndTime)
            .Set(r => r.UpdatedAt, DateTime.UtcNow);
        var result = await _context.Reservations.UpdateOneAsync(r => r.Id == updated.Id && r.Status == expectedStatus, change);
        return result.ModifiedCount == 1;
    }
    public Task<long> CountAsync(string? status = null, string? prosumerId = null)
    {
        var f = Builders<Reservation>.Filter; var filter = f.Empty;
        if (!string.IsNullOrWhiteSpace(status)) filter &= f.Eq(r => r.Status, status);
        if (!string.IsNullOrWhiteSpace(prosumerId)) filter &= f.Eq(r => r.ProsumerId, prosumerId);
        return _context.Reservations.CountDocumentsAsync(filter);
    }
    public Task<List<Reservation>> GetApprovedEndedAsync(DateTime now) => _context.Reservations.Find(r => r.Status == "Approved" && r.EndTime <= now).ToListAsync();
    public Task<bool> HasActiveForNodeAsync(string nodeId) => _context.Reservations.Find(r => r.MicrogridNodeId == nodeId && (r.Status == "Pending" || r.Status == "Approved")).AnyAsync();
    public Task<bool> MarkCompletedAsync(string id, string changedBy) => TransitionAsync(id, "Approved", "Completed", changedBy);
    public async Task<double> SumActiveEnergyAsync(string? prosumerId = null)
    {
        var f = Builders<Reservation>.Filter; var filter = f.In(r => r.Status, new[] { "Pending", "Approved" });
        if (!string.IsNullOrWhiteSpace(prosumerId)) filter &= f.Eq(r => r.ProsumerId, prosumerId);
        var rows = await _context.Reservations.Find(filter).Project(r => r.EnergyAmount).ToListAsync(); return rows.Sum();
    }
    public Task<long> CountCompletedSinceAsync(DateTime since, string? prosumerId = null)
    {
        var filter = Builders<Reservation>.Filter.Eq(r => r.Status, "Completed") & Builders<Reservation>.Filter.Gte(r => r.UpdatedAt, since);
        if (!string.IsNullOrWhiteSpace(prosumerId)) filter &= Builders<Reservation>.Filter.Eq(r => r.ProsumerId, prosumerId);
        return _context.Reservations.CountDocumentsAsync(filter);
    }
}
