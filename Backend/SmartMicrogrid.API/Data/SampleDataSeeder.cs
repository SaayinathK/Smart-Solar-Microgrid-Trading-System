using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Driver;
using SmartMicrogrid.API.Helpers;
using SmartMicrogrid.API.Models.Common;
using SmartMicrogrid.API.Models.M1;
using SmartMicrogrid.API.Models.M2;
using SmartMicrogrid.API.Models.M4;
using SmartMicrogrid.API.Models.Transactions;

namespace SmartMicrogrid.API.Data
{
    /// <summary>
    /// Fills in demonstration data for the modules the base seeder does not touch.
    /// DbSeeder only creates M1/M2 records and a single user set, which leaves the
    /// M3 transaction ledger empty and every M4 lifecycle card showing zero, so the
    /// dashboard, activity log and reports cannot be reviewed without hand-editing
    /// the database.
    ///
    /// Every method is idempotent: it seeds only when its target collection is
    /// empty, so restarting the API never duplicates records and never overwrites
    /// real data. Pass --reseed to drop the database and rebuild from scratch.
    /// </summary>
    public static class SampleDataSeeder
    {
        public static async Task SeedAsync(MongoDbContext context)
        {
            await SeedLifecycleUsersAsync(context);
            await SeedTransactionsAsync(context);
            await SeedAuditTrailAsync(context);
        }

        // ──────────────────────────────────────────────
        //  USERS  (M4 lifecycle states)
        //
        //  DbSeeder only produces Active users plus one legacy
        //  isActive=false record, so Suspended, Inactive and Pending
        //  are never represented. Without them the dashboard status
        //  breakdown and the user-status report are untestable.
        // ──────────────────────────────────────────────
        private static async Task SeedLifecycleUsersAsync(MongoDbContext context)
        {
            // The lifecycle sample is additive: it runs even when base users exist,
            // because the point is to cover the statuses the base set does not have.
            var existing = await context.Users
                .Find(Builders<User>.Filter.Eq("email", "suspended.sample@microgrid.com"))
                .FirstOrDefaultAsync();

            if (existing != null)
            {
                return;
            }

            var now = DateTime.UtcNow;

            // The legacy record DbSeeder marks isActive=false without a matching
            // AccountStatus. It resolves to Inactive at read time through
            // DashboardService.ResolveStatus, so the API already reports it
            // correctly and no repair is needed here.
            var samples = new List<User>
            {
                MakeUser("Chamara", "Wickramasinghe", "suspended.sample@microgrid.com",
                    "+94770000101", "199012345701", Role.Prosumer,
                    AccountStatus.Suspended, "Admin", now.AddDays(-40), now.AddDays(-12)),

                MakeUser("Nadeesha", "Herath", "inactive.sample@microgrid.com",
                    "+94770000102", "199112345702", Role.Prosumer,
                    AccountStatus.Inactive, "Nimal Fernando", now.AddDays(-55), now.AddDays(-30)),

                MakeUser("Suresh", "Ranasinghe", "pending.sample@microgrid.com",
                    "+94770000103", "199212345703", Role.Prosumer,
                    AccountStatus.Pending, null, now.AddDays(-2), null),

                MakeUser("Menaka", "Dissanayake", "suspended.operator@microgrid.com",
                    "+94770000104", "199312345704", Role.MicrogridOperator,
                    AccountStatus.Suspended, "System Administrator", now.AddDays(-60), now.AddDays(-21)),

                MakeUser("Roshan", "Gamage", "inactive.operator@microgrid.com",
                    "+94770000105", "199412345705", Role.MicrogridOperator,
                    AccountStatus.Inactive, "System Administrator", now.AddDays(-70), now.AddDays(-45)),

                MakeUser("Ishara", "Weerasinghe", "pending.operator@microgrid.com",
                    "+94770000106", "199512345706", Role.MicrogridOperator,
                    AccountStatus.Pending, null, now.AddDays(-1), null),

                MakeUser("Kasun", "Abeysekara", "second.admin@microgrid.com",
                    "+94770000107", "199612345707", Role.Admin,
                    AccountStatus.Active, null, now.AddDays(-90), null),

                MakeUser("Deepika", "Senanayake", "second.admin.inactive@microgrid.com",
                    "+94770000108", "199712345708", Role.Admin,
                    AccountStatus.Inactive, "System Administrator", now.AddDays(-100), now.AddDays(-65))
            };

            await context.Users.InsertManyAsync(samples);
        }

        // ──────────────────────────────────────────────
        //  TRANSACTIONS  (M3 ledger)
        //
        //  DbSeeder never creates transactions, so the whole M3
        //  transaction list, the M3 dashboard and the M4
        //  platform report render empty. These are linked to real
        //  reservations so the transaction detail and verification
        //  screens resolve their related entities.
        // ──────────────────────────────────────────────
        private static async Task SeedTransactionsAsync(MongoDbContext context)
        {
            if (await context.Transactions.CountDocumentsAsync(
                    Builders<Transaction>.Filter.Empty) > 0)
            {
                return;
            }

            var now = DateTime.UtcNow;

            // M3 enforces a unique index on Transaction.ReservationId, so the
            // transaction ledger can never hold more rows than there are
            // reservations. Seeding therefore creates its own reservations rather
            // than borrowing the M2 ones, which EnergySlotCleanupService may
            // already have expired.
            var reservations = await EnsureSampleReservationsAsync(context, now);

            if (reservations.Count == 0)
            {
                return;
            }

            var microgrids = await context.Microgrids
                .Find(Builders<MicrogridNode>.Filter.Empty)
                .ToListAsync();

            // VerifiedBy must be an operator who actually owns the transaction's
            // microgrid, because M3 verifies operator access against the node.
            var operators = await context.Users
                .Find(Builders<User>.Filter.Eq("role", Role.MicrogridOperator))
                .ToListAsync();

            var operatorById = operators
                .Where(o => o.Id != string.Empty)
                .GroupBy(o => o.Id)
                .ToDictionary(g => g.Key, g => g.First());

            var nodeOwner = microgrids
                .Where(m => m.Id != null && m.OperatorId != string.Empty)
                .GroupBy(m => m.Id!)
                .ToDictionary(g => g.Key, g => g.First().OperatorId);

            var transactions = new List<Transaction>();

            for (var i = 0; i < reservations.Count; i++)
            {
                var reservation = reservations[i];
                var status = TransactionStatusPlan[i % TransactionStatusPlan.Length];
                var createdAt = now.AddDays(-(reservations.Count - i) * 0.7);

                nodeOwner.TryGetValue(reservation.MicrogridNodeId, out var ownerId);
                operatorById.TryGetValue(ownerId ?? string.Empty, out var owner);

                var code = $"SMG-{now.Year}-{100000 + i * 37}";

                transactions.Add(new Transaction
                {
                    ReservationId = reservation.Id ?? string.Empty,
                    ProsumerId = reservation.ProsumerId,
                    MicrogridNodeId = reservation.MicrogridNodeId,
                    EnergySlotId = reservation.EnergySlotId,
                    EnergyAmount = reservation.EnergyAmount,
                    TransactionCode = HasCode(status) ? code : string.Empty,
                    QrCodeData = HasQr(status)
                        ? $"SMART-MICROGRID|TRANSACTION|pending|{code}"
                        : string.Empty,
                    VerifiedBy = IsVerifiedOrBeyond(status) ? owner?.Id : null,
                    VerificationTime = IsVerifiedOrBeyond(status)
                        ? createdAt.AddMinutes(35)
                        : null,
                    EnergyTransferTime = status == "Completed"
                        ? createdAt.AddMinutes(50)
                        : null,
                    Status = status,
                    CreatedAt = createdAt,
                    UpdatedAt = createdAt.AddMinutes(50)
                });
            }

            await context.Transactions.InsertManyAsync(transactions);
        }

        // ──────────────────────────────────────────────
        //  SAMPLE RESERVATIONS  (support data for M3)
        //
        //  Created on demand and tagged with a known marker so re-seeding is
        //  idempotent and never mixes with user-created reservations.
        // ──────────────────────────────────────────────
        private static async Task<List<Reservation>> EnsureSampleReservationsAsync(
            MongoDbContext context, DateTime now)
        {
            const string marker = "sample-data@microgrid.com";

            var existing = await context.Reservations
                .Find(Builders<Reservation>.Filter.Eq("prosumerId", marker))
                .ToListAsync();

            if (existing.Count > 0)
            {
                return existing;
            }

            var microgrids = await context.Microgrids
                .Find(Builders<MicrogridNode>.Filter.Eq("isActive", true))
                .ToListAsync();

            if (microgrids.Count == 0)
            {
                return existing;
            }

            var slots = await context.EnergySlots
                .Find(Builders<EnergySlot>.Filter.Empty)
                .ToListAsync();

            if (slots.Count == 0)
            {
                var newSlots = new List<EnergySlot>();
                foreach (var mg in microgrids)
                {
                    if (string.IsNullOrEmpty(mg.Id) || !MongoDB.Bson.ObjectId.TryParse(mg.Id, out _))
                        continue;

                    newSlots.Add(new EnergySlot
                    {
                        MicrogridNodeId = mg.Id,
                        CreatedBy = mg.OperatorId,
                        EnergyAmount = 100,
                        AvailableAmount = 80,
                        StartTime = now.AddHours(1),
                        EndTime = now.AddHours(5),
                        PricePerUnit = 25.00m,
                        Status = "Available",
                        CreatedAt = now,
                        UpdatedAt = now
                    });
                }

                if (newSlots.Count > 0)
                {
                    await context.EnergySlots.InsertManyAsync(newSlots);
                    slots = await context.EnergySlots
                        .Find(Builders<EnergySlot>.Filter.Empty)
                        .ToListAsync();
                }
            }

            var created = new List<Reservation>();

            // One reservation per microgrid, cycling if there are more
            // transactions wanted than grids available.
            for (var i = 0; i < TransactionStatusPlan.Length; i++)
            {
                var node = microgrids[i % microgrids.Count];
                var nodeSlots = slots
                    .Where(s => s.MicrogridNodeId == node.Id)
                    .ToList();

                var slot = nodeSlots.Count > 0
                    ? nodeSlots[i % nodeSlots.Count]
                    : (slots.Count > 0 ? slots[i % slots.Count] : null);

                var slotId = slot?.Id;
                if (string.IsNullOrWhiteSpace(slotId) || !MongoDB.Bson.ObjectId.TryParse(slotId, out _))
                {
                    slotId = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
                }

                var nodeId = !string.IsNullOrWhiteSpace(slot?.MicrogridNodeId) && MongoDB.Bson.ObjectId.TryParse(slot.MicrogridNodeId, out _)
                    ? slot.MicrogridNodeId
                    : (!string.IsNullOrWhiteSpace(node.Id) && MongoDB.Bson.ObjectId.TryParse(node.Id, out _) ? node.Id : MongoDB.Bson.ObjectId.GenerateNewId().ToString());

                var status = TransactionStatusPlan[i];
                var start = now.AddHours(2 + (i % 8));

                created.Add(new Reservation
                {
                    ProsumerId = marker,
                    MicrogridNodeId = nodeId,
                    EnergySlotId = slotId,
                    EnergyAmount = 5 + (i * 2.5),
                    ReservationDate = start.Date,
                    StartTime = start,
                    EndTime = start.AddHours(3),
                    Status = status == "Completed" ? "Completed" : "Approved",
                    CreatedAt = now.AddDays(-(12 - i) * 0.6),
                    UpdatedAt = now.AddDays(-(12 - i) * 0.6),
                    StatusHistory = new List<ReservationStatusEvent>
                    {
                        new()
                        {
                            From = null, To = "Pending",
                            ChangedBy = marker, ChangedAt = now.AddDays(-(12 - i) * 0.6)
                        },
                        new()
                        {
                            From = "Pending", To = "Approved",
                            ChangedBy = node.OperatorId,
                            ChangedAt = now.AddDays(-(12 - i) * 0.6).AddHours(2)
                        }
                    }
                });
            }

            await context.Reservations.InsertManyAsync(created);

            return await context.Reservations
                .Find(Builders<Reservation>.Filter.Eq("prosumerId", marker))
                .ToListAsync();
        }

        // ──────────────────────────────────────────────
        //  AUDIT TRAIL  (M4 activity log)
        //
        //  Only records produced by real requests exist, which means the
        //  activity page cannot be reviewed for module, action or status
        //  coverage. These give every filter combination something to match.
        // ──────────────────────────────────────────────
        private static async Task SeedAuditTrailAsync(MongoDbContext context)
        {
            var users = await context.Users
                .Find(Builders<User>.Filter.Empty)
                .ToListAsync();

            if (users.Count == 0)
            {
                return;
            }

            // The activity log accumulates real request records, so a record
            // count cannot tell seeded rows from genuine ones. A sentinel entry
            // is used instead: once the trail exists, seeding stops.
            var alreadySeeded = await context.SystemActivity
                .Find(Builders<SystemActivity>.Filter.Regex(
                    x => x.Description, "^Sample audit trail"))
                .AnyAsync();

            if (alreadySeeded)
            {
                return;
            }

            var now = DateTime.UtcNow;
            var admins = users.Where(u => u.Role == Role.Admin).ToList();
            var operators = users.Where(u => u.Role == Role.MicrogridOperator).ToList();
            var prosumers = users.Where(u => u.Role == Role.Prosumer).ToList();

            var records = new List<SystemActivity>();
            var step = 0;

            void Log(User? actor, string action, string module,
                string description, string status, int minutesAgo)
            {
                step++;

                records.Add(new SystemActivity
                {
                    UserId = actor?.Id,
                    UserName = actor is null
                        ? "System"
                        : $"{actor.FirstName} {actor.LastName}".Trim(),
                    Role = actor?.Role.ToString(),
                    Action = action,
                    Module = module,
                    Description = description,
                    EntityType = module == AuditModule.UserManagement ? "User" : "Platform",
                    IpAddress = $"192.168.1.{20 + step % 60}",
                    Timestamp = now.AddMinutes(-minutesAgo),
                    Status = status
                });
            }

            var admin = admins.FirstOrDefault();
            var secondAdmin = admins.Skip(1).FirstOrDefault() ?? admin;
            var microgridOperator = operators.FirstOrDefault();
            var prosumer = prosumers.FirstOrDefault();

            // Authentication
            Log(admin, AuditAction.LoginSuccess, AuditModule.Authentication,
                "Sample audit trail: admin signed in successfully.", AuditStatus.Success, 8);
            Log(secondAdmin, AuditAction.LoginFailed, AuditModule.Authentication,
                "Sample audit trail: failed sign-in, incorrect password.", AuditStatus.Failure, 46);
            Log(microgridOperator, AuditAction.LoginSuccess, AuditModule.Authentication,
                "Sample audit trail: Microgrid operator signed in successfully.", AuditStatus.Success, 95);
            Log(prosumer, AuditAction.LoginSuccess, AuditModule.Authentication,
                "Sample audit trail: Prosumer signed in successfully.", AuditStatus.Success, 140);
            Log(prosumer, AuditAction.LoginFailed, AuditModule.Authentication,
                "Sample audit trail: Failed sign-in: account suspended.", AuditStatus.Failure, 288);

            // User management
            var created = prosumers.Skip(1).FirstOrDefault() ?? prosumer;
            Log(admin, AuditAction.UserCreated, AuditModule.UserManagement,
                $"Sample audit trail: created prosumer account for {created?.Email}.", AuditStatus.Success, 1320);
            Log(admin, AuditAction.UserStatusChanged, AuditModule.UserManagement,
                "Sample audit trail: Changed account status from Active to Suspended.", AuditStatus.Success, 1740);
            Log(secondAdmin, AuditAction.UserUpdated, AuditModule.UserManagement,
                "Sample audit trail: Updated prosumer contact details.", AuditStatus.Success, 2880);
            Log(admin, AuditAction.UserSuspended, AuditModule.UserManagement,
                "Sample audit trail: Suspended a prosumer account pending verification.", AuditStatus.Success, 4320);
            Log(secondAdmin, AuditAction.RoleAssigned, AuditModule.UserManagement,
                "Sample audit trail: Assigned MicrogridOperator role to a new account.", AuditStatus.Success, 5760);
            Log(admin, AuditAction.UserDeactivated, AuditModule.UserManagement,
                "Sample audit trail: Deactivated a retired prosumer account.", AuditStatus.Success, 7200);
            Log(admin, AuditAction.UserReactivated, AuditModule.UserManagement,
                "Sample audit trail: Reactivated a previously deactivated account.", AuditStatus.Success, 8640);
            Log(secondAdmin, AuditAction.UserDeleted, AuditModule.UserManagement,
                "Sample audit trail: Deleted a duplicate registration.", AuditStatus.Success, 10080);
            Log(admin, AuditAction.RoleChanged, AuditModule.UserManagement,
                "Sample audit trail: Changed role from Prosumer to MicrogridOperator.", AuditStatus.Failure, 11520);
            Log(admin, AuditAction.UserActivated, AuditModule.UserManagement,
                "Sample audit trail: Activated a pending registration.", AuditStatus.Success, 12960);

            // System configuration
            Log(admin, AuditAction.ConfigurationUpdated, AuditModule.SystemConfiguration,
                "Sample audit trail: Updated platform identity details.", AuditStatus.Success, 2100);
            Log(secondAdmin, AuditAction.ConfigurationUpdated, AuditModule.SystemConfiguration,
                "Sample audit trail: Enabled maintenance mode.", AuditStatus.Success, 6420);
            Log(secondAdmin, AuditAction.ConfigurationUpdated, AuditModule.SystemConfiguration,
                "Sample audit trail: Updated session timeout to 480 minutes.", AuditStatus.Failure, 9300);
            Log(admin, AuditAction.ConfigurationUpdated, AuditModule.SystemConfiguration,
                "Sample audit trail: Disabled public registration.", AuditStatus.Success, 15120);

            // Reporting
            Log(admin, AuditAction.ReportGenerated, AuditModule.Reporting,
                "Sample audit trail: Generated user administration report.", AuditStatus.Success, 320);
            Log(secondAdmin, AuditAction.ReportGenerated, AuditModule.Reporting,
                "Sample audit trail: Exported role distribution report to CSV.", AuditStatus.Success, 1560);
            Log(microgridOperator, AuditAction.ReportGenerated, AuditModule.Reporting,
                "Sample audit trail: Generated platform activity report.", AuditStatus.Failure, 5040);
            Log(admin, AuditAction.ReportGenerated, AuditModule.Reporting,
                "Sample audit trail: Generated transaction ledger report.", AuditStatus.Success, 11040);

            await context.SystemActivity.InsertManyAsync(records);
        }

        // ── Helpers ──

        private static User MakeUser(
            string firstName, string lastName, string email,
            string phone, string nic, Role role,
            AccountStatus status, string? changedBy,
            DateTime createdAt, DateTime? statusChangedAt)
        {
            return new User
            {
                FirstName = firstName,
                LastName = lastName,
                Email = email,
                PhoneNumber = phone,
                Nic = nic,
                PasswordHash = PasswordHelper.HashPassword("Sample123!"),
                Role = role,
                AccountStatus = status,

                // IsActive stays the authentication gate: only Active may sign in.
                IsActive = status == AccountStatus.Active,
                StatusChangedAt = statusChangedAt,
                StatusChangedBy = changedBy,
                CreatedAt = createdAt,
                UpdatedAt = statusChangedAt ?? createdAt
            };
        }

        /// <summary>
        /// M3 lifecycle states to demonstrate, in the order the verification
        /// pipeline reaches them. Shared by the reservation and transaction
        /// seeders so the two stay aligned.
        /// </summary>
        private static readonly string[] TransactionStatusPlan =
        {
            "Completed", "Completed", "Completed", "Completed",
            "Verified", "VerificationPending", "QRGenerated", "QRGenerated",
            "Pending", "Pending", "Rejected", "Cancelled"
        };

        private static bool HasQr(string status) =>
            status is "QRGenerated" or "VerificationPending" or "Verified"
                or "Completed";

        private static bool IsVerifiedOrBeyond(string status) =>
            status is "Verified" or "Completed";

        private static bool HasCode(string status) =>
            status is not "Pending" and not "Rejected" and not "Cancelled";
    }
}
