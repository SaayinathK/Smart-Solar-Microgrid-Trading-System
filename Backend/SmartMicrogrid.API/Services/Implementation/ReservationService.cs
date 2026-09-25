using MongoDB.Driver;
using SmartMicrogrid.API.Data;
using SmartMicrogrid.API.DTOs.M2;
using SmartMicrogrid.API.Models.Common;
using SmartMicrogrid.API.Models.M1;
using SmartMicrogrid.API.Models.M2;
using SmartMicrogrid.API.Repositories.Interfaces;
using SmartMicrogrid.API.Services.Interfaces;

namespace SmartMicrogrid.API.Services.Implementation;

public class ReservationService : IReservationService
{
    private readonly IReservationRepository _reservations;
    private readonly MongoDbContext _db;
    private readonly IUserRepository _users;
    public ReservationService(IReservationRepository reservations, MongoDbContext db, IUserRepository users) { _reservations = reservations; _db = db; _users = users; }

    public async Task<ReservationResponseDto> CreateAsync(CreateReservationDto dto, string actorId, bool staff, string? operatorId = null)
    {
        var actor = staff ? null : await _users.GetByIdAsync(actorId);
        var prosumerId = (staff ? dto.ProsumerId : actor?.Nic)?.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(prosumerId)) throw new ArgumentException("A prosumer NIC is required.");
        var user = await _users.GetByNicAsync(prosumerId);
        if (user == null || user.Role != Role.Prosumer) throw new ArgumentException("Prosumer account was not found.");
        if (!user.IsActive) throw new UnauthorizedAccessException("Deactivated prosumers cannot book energy.");
        if (!staff && user.Id != actorId) throw new UnauthorizedAccessException("Prosumer account does not match the authenticated user.");
        if (!MongoDB.Bson.ObjectId.TryParse(dto.EnergySlotId, out _)) throw new ArgumentException("Invalid energy slot ID.");
        if (dto.EnergyAmount <= 0 || double.IsNaN(dto.EnergyAmount) || double.IsInfinity(dto.EnergyAmount)) throw new ArgumentException("Energy amount must be greater than zero.");
        var now = DateTime.UtcNow; var slotId = dto.EnergySlotId;
        var slots = _db.EnergySlots;
        var slotFilter = Builders<EnergySlot>.Filter.Eq(s => s.Id, slotId) & Builders<EnergySlot>.Filter.Gte(s => s.AvailableAmount, dto.EnergyAmount) & Builders<EnergySlot>.Filter.In(s => s.Status, new[] { "Available", "PartiallyReserved" }) & Builders<EnergySlot>.Filter.Gt(s => s.EndTime, now);
        var slotUpdate = Builders<EnergySlot>.Update.Inc(s => s.AvailableAmount, -dto.EnergyAmount).Set(s => s.UpdatedAt, now);
        var slot = await slots.FindOneAndUpdateAsync(slotFilter, slotUpdate, new FindOneAndUpdateOptions<EnergySlot> { ReturnDocument = ReturnDocument.After });
        if (slot == null) throw new InvalidOperationException("The energy slot is no longer available or has insufficient capacity.");
        try
        {
            if (slot.StartTime <= now || slot.StartTime > now.AddDays(7)) throw new ArgumentException("Reservations must start in the future and within 7 days.");
            var node = await _db.Microgrids.Find(n => n.Id == slot.MicrogridNodeId).FirstOrDefaultAsync();
            if (node == null || !node.IsActive || !string.Equals(node.Status, "Active", StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("The microgrid for this energy slot is not active.");
            if (!string.IsNullOrWhiteSpace(operatorId)) await EnsureOperatorAccessAsync(node.Id!, slot.Id!, operatorId);
            var reservation = new Reservation { ProsumerId = prosumerId, MicrogridNodeId = slot.MicrogridNodeId, EnergySlotId = slotId, EnergyAmount = dto.EnergyAmount, ReservationDate = (dto.ReservationDate ?? slot.StartTime).ToUniversalTime(), StartTime = slot.StartTime, EndTime = slot.EndTime, CreatedAt = now, UpdatedAt = now, Status = "Pending", StatusHistory = { new ReservationStatusEvent { To = "Pending", ChangedBy = actorId, ChangedAt = now } } };
            return Map(await _reservations.CreateAsync(reservation));
        }
        catch
        {
            await RestoreCapacityAsync(slotId, dto.EnergyAmount); throw;
        }
    }

    public async Task<ReservationResponseDto?> GetByIdAsync(string id, string? operatorId = null) { var r = await _reservations.GetByIdAsync(id); if (r != null && !string.IsNullOrWhiteSpace(operatorId)) await EnsureOperatorAccessAsync(r.MicrogridNodeId, r.EnergySlotId, operatorId); return r == null ? null : Map(r); }
    public async Task<ReservationResponseDto> UpdateAsync(string id, string? energySlotId, double energyAmount, string actorId, bool staff, string? operatorId = null)
    {
        var current = await _reservations.GetByIdAsync(id) ?? throw new KeyNotFoundException("Reservation not found.");
        if (!string.IsNullOrWhiteSpace(operatorId)) await EnsureOperatorAccessAsync(current.MicrogridNodeId, current.EnergySlotId, operatorId);
        if (!staff && (await _users.GetByIdAsync(actorId))?.Nic != current.ProsumerId) throw new UnauthorizedAccessException("You can only change your own reservations.");
        if (current.Status is not ("Pending" or "Approved")) throw new ArgumentException("Only pending or approved reservations can be changed.");
        if (current.StartTime < DateTime.UtcNow.AddHours(12)) throw new ArgumentException("Reservations can only be changed at least 12 hours before start time.");
        if (energyAmount <= 0 || double.IsNaN(energyAmount) || double.IsInfinity(energyAmount)) throw new ArgumentException("Energy amount must be greater than zero.");
        var previousSlotId = current.EnergySlotId;
        var previousAmount = current.EnergyAmount;
        var targetSlotId = string.IsNullOrWhiteSpace(energySlotId) ? current.EnergySlotId : energySlotId.Trim();
        if (!MongoDB.Bson.ObjectId.TryParse(targetSlotId, out _)) throw new ArgumentException("Invalid energy slot ID.");
        var movingSlot = targetSlotId != current.EnergySlotId;
        var targetSlot = movingSlot ? await _db.EnergySlots.Find(s => s.Id == targetSlotId).FirstOrDefaultAsync() : null;
        if (movingSlot && targetSlot == null) throw new KeyNotFoundException("Target energy slot not found.");
        if (targetSlot != null && !string.IsNullOrWhiteSpace(operatorId)) await EnsureOperatorAccessAsync(targetSlot.MicrogridNodeId, targetSlot.Id!, operatorId);
        var targetStart = targetSlot?.StartTime ?? current.StartTime;
        var targetEnd = targetSlot?.EndTime ?? current.EndTime;
        if (targetStart <= DateTime.UtcNow || targetStart > DateTime.UtcNow.AddDays(7))
            throw new ArgumentException("Reservations must start in the future and within 7 days.");

        var reservedAmount = movingSlot ? energyAmount : energyAmount - current.EnergyAmount;
        var changedCapacity = false;
        if (Math.Abs(reservedAmount) >= 0.000001)
        {
            var capacityFilter = Builders<EnergySlot>.Filter.Eq(s => s.Id, targetSlotId);
            if (reservedAmount > 0)
                capacityFilter &= Builders<EnergySlot>.Filter.Gte(s => s.AvailableAmount, reservedAmount) & Builders<EnergySlot>.Filter.In(s => s.Status, new[] { "Available", "PartiallyReserved" }) & Builders<EnergySlot>.Filter.Gt(s => s.EndTime, DateTime.UtcNow);
            var capacityChange = await _db.EnergySlots.UpdateOneAsync(capacityFilter,
                Builders<EnergySlot>.Update.Inc(s => s.AvailableAmount, -reservedAmount).Set(s => s.UpdatedAt, DateTime.UtcNow));
            if (capacityChange.ModifiedCount != 1) throw new InvalidOperationException("The slot does not have enough remaining capacity.");
            changedCapacity = true;
        }
        current.EnergySlotId = targetSlotId;
        current.MicrogridNodeId = targetSlot?.MicrogridNodeId ?? current.MicrogridNodeId;
        current.EnergyAmount = energyAmount;
        current.ReservationDate = targetStart;
        current.StartTime = targetStart;
        current.EndTime = targetEnd;
        if (!await _reservations.UpdateBookingAsync(current, current.Status))
        {
            if (changedCapacity) await RestoreCapacityAsync(targetSlotId, reservedAmount);
            throw new InvalidOperationException("Reservation status changed concurrently. Refresh and try again.");
        }
        if (movingSlot) await RestoreCapacityAsync(previousSlotId, previousAmount);
        return await GetByIdAsync(id) ?? throw new KeyNotFoundException("Reservation not found.");
    }
    public async Task<List<ReservationResponseDto>> GetAsync(string? prosumerId, string? status, string? nodeId, int page, int pageSize, string? operatorId = null)
    {
        var allowedNodeIds = string.IsNullOrWhiteSpace(operatorId) ? null : await GetOperatorMicrogridIdsAsync(operatorId);
        if (allowedNodeIds is { Count: 0 } || (allowedNodeIds != null && !string.IsNullOrWhiteSpace(nodeId) && !allowedNodeIds.Contains(nodeId))) return new List<ReservationResponseDto>();
        return (await _reservations.GetAsync(prosumerId, status, nodeId, page, pageSize, allowedNodeIds)).Select(Map).ToList();
    }
    public async Task<ReservationSummaryDto> SummaryAsync(string? prosumerId = null, string? operatorId = null)
    {
        var start = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var allowedNodeIds = string.IsNullOrWhiteSpace(operatorId) ? null : await GetOperatorMicrogridIdsAsync(operatorId);
        var approved = allowedNodeIds is { Count: 0 } ? new List<Reservation>() : await _reservations.GetAsync(prosumerId, "Approved", null, 1, 10000, allowedNodeIds);
        return new ReservationSummaryDto { PendingCount = allowedNodeIds is { Count: 0 } ? 0 : await _reservations.CountAsync("Pending", prosumerId, allowedNodeIds), ApprovedFutureCount = approved.LongCount(r => r.StartTime > DateTime.UtcNow), CompletedThisMonthCount = allowedNodeIds is { Count: 0 } ? 0 : await _reservations.CountCompletedSinceAsync(start, prosumerId, allowedNodeIds), TotalEnergyReserved = allowedNodeIds is { Count: 0 } ? 0 : await _reservations.SumActiveEnergyAsync(prosumerId, allowedNodeIds) };
    }

    public async Task<ReservationResponseDto> TransitionAsync(string id, string action, string actorId, bool staff, string? reason = null, string? operatorId = null)
    {
        var current = await _reservations.GetByIdAsync(id) ?? throw new KeyNotFoundException("Reservation not found.");
        if (!string.IsNullOrWhiteSpace(operatorId)) await EnsureOperatorAccessAsync(current.MicrogridNodeId, current.EnergySlotId, operatorId);
        if (!staff && (await _users.GetByIdAsync(actorId))?.Nic != current.ProsumerId) throw new UnauthorizedAccessException("You can only change your own reservations.");
        string expected; string next;
        switch (action.ToLowerInvariant())
        {
            case "approve" when staff: expected = "Pending"; next = "Approved"; break;
            case "reject" when staff: expected = "Pending"; next = "Rejected"; break;
            case "cancel" when current.Status == "Pending": expected = "Pending"; next = "Cancelled"; break;
            case "cancel" when current.Status == "Approved":
                if (current.StartTime < DateTime.UtcNow.AddHours(12)) throw new ArgumentException("Approved reservations can only be cancelled at least 12 hours before start time.");
                expected = "Approved"; next = "Cancelled"; break;
            default: throw new ArgumentException("This reservation status transition is not allowed.");
        }
        if (!await _reservations.TransitionAsync(id, expected, next, actorId, reason)) throw new InvalidOperationException("Reservation status changed concurrently. Refresh and try again.");
        if (next is "Rejected" or "Cancelled") await RestoreCapacityAsync(current.EnergySlotId, current.EnergyAmount);
        return await GetByIdAsync(id) ?? throw new KeyNotFoundException("Reservation not found.");
    }

    public async Task<ReservationResponseDto> MarkCompletedAsync(string id, string actorId, string? operatorId = null)
    {
        var current = await _reservations.GetByIdAsync(id) ?? throw new KeyNotFoundException("Reservation not found.");
        if (!string.IsNullOrWhiteSpace(operatorId)) await EnsureOperatorAccessAsync(current.MicrogridNodeId, current.EnergySlotId, operatorId);
        if (current.Status != "Approved") throw new ArgumentException("Only approved reservations can be completed.");
        if (!await _reservations.MarkCompletedAsync(id, actorId)) throw new InvalidOperationException("Reservation was already changed.");
        return await GetByIdAsync(id) ?? throw new KeyNotFoundException("Reservation not found.");
    }
    public Task<bool> HasActiveForNodeAsync(string nodeId) => _reservations.HasActiveForNodeAsync(nodeId);
    public async Task ExpireOverdueAsync(CancellationToken cancellationToken)
    {
        foreach (var reservation in await _reservations.GetApprovedEndedAsync(DateTime.UtcNow))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (reservation.Id != null) await _reservations.TransitionAsync(reservation.Id, "Approved", "Expired", "reservation-expiry-service");
        }
    }
    private async Task RestoreCapacityAsync(string slotId, double amount)
    {
        if (!MongoDB.Bson.ObjectId.TryParse(slotId, out _)) return;
        var update = Builders<EnergySlot>.Update.Inc(s => s.AvailableAmount, amount).Set(s => s.UpdatedAt, DateTime.UtcNow);
        await _db.EnergySlots.UpdateOneAsync(s => s.Id == slotId, update);
    }
    private static ReservationResponseDto Map(Reservation r) => new() { Id = r.Id ?? string.Empty, ProsumerId = r.ProsumerId, MicrogridNodeId = r.MicrogridNodeId, EnergySlotId = r.EnergySlotId, EnergyAmount = r.EnergyAmount, ReservationDate = r.ReservationDate, StartTime = r.StartTime, EndTime = r.EndTime, Status = r.Status, CreatedAt = r.CreatedAt, UpdatedAt = r.UpdatedAt, StatusHistory = r.StatusHistory };
}

public class ReservationExpiryService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ReservationExpiryService> _logger;
    public ReservationExpiryService(IServiceScopeFactory scopeFactory, ILogger<ReservationExpiryService> logger) { _scopeFactory = scopeFactory; _logger = logger; }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(3));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try { using var scope = _scopeFactory.CreateScope(); await scope.ServiceProvider.GetRequiredService<IReservationService>().ExpireOverdueAsync(stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex) { _logger.LogError(ex, "Reservation expiry sweep failed"); }
        }
    }
}
