// ===========================================================================================================
// File: DbSeeder.cs
// Project: Smart Solar Microgrid Trading System
// Module: M1 – Microgrid & Energy Resource Management
// Section Owned: M1 – Microgrid & Energy Resource Management
// Author: K. Saayinath (IT23304338)
// Description: Comprehensive database seeder populating interconnected datasets across all models.
// ===========================================================================================================
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
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
    /// Coordinates seeding and reset of demonstration datasets across all 7 platform models:
    /// User, MicrogridNode, EnergySlot, Reservation, Transaction, SystemConfiguration, and SystemActivity.
    /// All seeded entities are inter-related by valid foreign keys, prosumer NICs, and lifecycle states.
    /// </summary>
    public static class DbSeeder
    {
        /// <summary>
        /// Performs migrate legacy roles async operation to map legacy verifier accounts.
        /// </summary>
        public static async Task MigrateLegacyRolesAsync(MongoDbContext context)
        {
            // Execute migrate legacy roles async operations
            var users = context.GetRawUsersCollection();
            await users.UpdateManyAsync(
                new BsonDocument("role", "TransactionVerifier"),
                new BsonDocument("$set", new BsonDocument("role", "MicrogridOperator")));
        }

        /// <summary>
        /// Seeds all models if the database is currently empty.
        /// </summary>
        public static async Task SeedDefaultUsersAsync(MongoDbContext context)
        {
            // Execute check and seed operations
            var userCount = await context.Users.CountDocumentsAsync(Builders<User>.Filter.Empty);
            if (userCount == 0)
            {
                await ReseedAllAsync(context);
            }
        }

        /// <summary>
        /// Drops all collections, regenerates database indexes, and seeds rich interrelated datasets across all models.
        /// </summary>
        public static async Task ReseedAllAsync(MongoDbContext context)
        {
            // Execute complete database drop and clean wipe of all collections
            var db = context.Database;
            await db.DropCollectionAsync(MongoCollections.Users);
            await db.DropCollectionAsync(MongoCollections.Microgrids);
            await db.DropCollectionAsync(MongoCollections.EnergySlots);
            await db.DropCollectionAsync(MongoCollections.Reservations);
            await db.DropCollectionAsync(MongoCollections.Transactions);
            await db.DropCollectionAsync(MongoCollections.SystemActivity);
            await db.DropCollectionAsync(MongoCollections.SystemConfiguration);

            // Recreate all database indexes cleanly
            context.EnsureIndexes();

            var now = DateTime.UtcNow;

            // ─────────────────────────────────────────────────────────────
            //  1. USERS (18 records across Admin, Operator, and Prosumer)
            // ─────────────────────────────────────────────────────────────
            var idAdmin1 = ObjectId.GenerateNewId().ToString();
            var idAdmin2 = ObjectId.GenerateNewId().ToString();
            var idAdmin3 = ObjectId.GenerateNewId().ToString();
            var idAdmin4 = ObjectId.GenerateNewId().ToString();

            var idOpAlex = ObjectId.GenerateNewId().ToString();
            var idOpKavindu = ObjectId.GenerateNewId().ToString();
            var idOpLakshmi = ObjectId.GenerateNewId().ToString();
            var idOpMenaka = ObjectId.GenerateNewId().ToString();
            var idOpRoshan = ObjectId.GenerateNewId().ToString();
            var idOpIshara = ObjectId.GenerateNewId().ToString();

            var idProSam = ObjectId.GenerateNewId().ToString();
            var idProAmaya = ObjectId.GenerateNewId().ToString();
            var idProRuwan = ObjectId.GenerateNewId().ToString();
            var idProTharushi = ObjectId.GenerateNewId().ToString();
            var idProChamara = ObjectId.GenerateNewId().ToString();
            var idProNadeesha = ObjectId.GenerateNewId().ToString();
            var idProSuresh = ObjectId.GenerateNewId().ToString();
            var idProDilini = ObjectId.GenerateNewId().ToString();

            var nicAdmin1 = "200012345678";
            var nicAdmin2 = "199812345679";
            var nicAdmin3 = "199612345707";
            var nicAdmin4 = "199712345708";

            var nicOpAlex = "199512345680";
            var nicOpKavindu = "199712345681";
            var nicOpLakshmi = "199612345682";
            var nicOpMenaka = "199312345704";
            var nicOpRoshan = "199412345705";
            var nicOpIshara = "199512345706";

            var nicProSam = "200112345683";
            var nicProAmaya = "200212345684";
            var nicProRuwan = "199912345685";
            var nicProTharushi = "200012345687";
            var nicProChamara = "199012345701";
            var nicProNadeesha = "199112345702";
            var nicProSuresh = "199212345703";
            var nicProDilini = "200312345686";

            var adminHash = PasswordHelper.HashPassword("Admin123!");
            var operatorHash = PasswordHelper.HashPassword("Operator123!");
            var prosumerHash = PasswordHelper.HashPassword("Prosumer123!");

            var users = new List<User>
            {
                // Admins
                new() { Id = idAdmin1, FirstName = "System", LastName = "Administrator", Email = "admin@microgrid.com", PhoneNumber = "+94770000001", Nic = nicAdmin1, PasswordHash = adminHash, Role = Role.Admin, AccountStatus = AccountStatus.Active, IsActive = true, CreatedAt = now.AddDays(-120), UpdatedAt = now },
                new() { Id = idAdmin2, FirstName = "Nimal", LastName = "Fernando", Email = "nimal.admin@microgrid.com", PhoneNumber = "+94770000011", Nic = nicAdmin2, PasswordHash = adminHash, Role = Role.Admin, AccountStatus = AccountStatus.Active, IsActive = true, CreatedAt = now.AddDays(-100), UpdatedAt = now },
                new() { Id = idAdmin3, FirstName = "Kasun", LastName = "Abeysekara", Email = "kasun.admin@microgrid.com", PhoneNumber = "+94770000107", Nic = nicAdmin3, PasswordHash = adminHash, Role = Role.Admin, AccountStatus = AccountStatus.Active, IsActive = true, CreatedAt = now.AddDays(-90), UpdatedAt = now },
                new() { Id = idAdmin4, FirstName = "Deepika", LastName = "Senanayake", Email = "deepika.admin@microgrid.com", PhoneNumber = "+94770000108", Nic = nicAdmin4, PasswordHash = adminHash, Role = Role.Admin, AccountStatus = AccountStatus.Inactive, IsActive = false, StatusChangedAt = now.AddDays(-30), StatusChangedBy = "System Administrator", CreatedAt = now.AddDays(-110), UpdatedAt = now.AddDays(-30) },

                // Microgrid Operators
                new() { Id = idOpAlex, FirstName = "Alex", LastName = "Perera", Email = "operator@microgrid.com", PhoneNumber = "+94770000002", Nic = nicOpAlex, PasswordHash = operatorHash, Role = Role.MicrogridOperator, AccountStatus = AccountStatus.Active, IsActive = true, CreatedAt = now.AddDays(-115), UpdatedAt = now },
                new() { Id = idOpKavindu, FirstName = "Kavindu", LastName = "Silva", Email = "kavindu.op@microgrid.com", PhoneNumber = "+94770000012", Nic = nicOpKavindu, PasswordHash = operatorHash, Role = Role.MicrogridOperator, AccountStatus = AccountStatus.Active, IsActive = true, CreatedAt = now.AddDays(-95), UpdatedAt = now },
                new() { Id = idOpLakshmi, FirstName = "Lakshmi", LastName = "Ratnayake", Email = "lakshmi.op@microgrid.com", PhoneNumber = "+94770000013", Nic = nicOpLakshmi, PasswordHash = operatorHash, Role = Role.MicrogridOperator, AccountStatus = AccountStatus.Active, IsActive = true, CreatedAt = now.AddDays(-80), UpdatedAt = now },
                new() { Id = idOpMenaka, FirstName = "Menaka", LastName = "Dissanayake", Email = "menaka.op@microgrid.com", PhoneNumber = "+94770000104", Nic = nicOpMenaka, PasswordHash = operatorHash, Role = Role.MicrogridOperator, AccountStatus = AccountStatus.Suspended, IsActive = false, StatusChangedAt = now.AddDays(-15), StatusChangedBy = "System Administrator", CreatedAt = now.AddDays(-75), UpdatedAt = now.AddDays(-15) },
                new() { Id = idOpRoshan, FirstName = "Roshan", LastName = "Gamage", Email = "roshan.op@microgrid.com", PhoneNumber = "+94770000105", Nic = nicOpRoshan, PasswordHash = operatorHash, Role = Role.MicrogridOperator, AccountStatus = AccountStatus.Inactive, IsActive = false, StatusChangedAt = now.AddDays(-20), StatusChangedBy = "Nimal Fernando", CreatedAt = now.AddDays(-70), UpdatedAt = now.AddDays(-20) },
                new() { Id = idOpIshara, FirstName = "Ishara", LastName = "Weerasinghe", Email = "ishara.op@microgrid.com", PhoneNumber = "+94770000106", Nic = nicOpIshara, PasswordHash = operatorHash, Role = Role.MicrogridOperator, AccountStatus = AccountStatus.Pending, IsActive = false, CreatedAt = now.AddDays(-2), UpdatedAt = now },

                // Prosumers (Sam is the primary demo prosumer for mobile app and web)
                new() { Id = idProSam, FirstName = "Sam", LastName = "Jayawardena", Email = "prosumer@microgrid.com", PhoneNumber = "+94770000003", Nic = nicProSam, PasswordHash = prosumerHash, Role = Role.Prosumer, AccountStatus = AccountStatus.Active, IsActive = true, CreatedAt = now.AddDays(-60), UpdatedAt = now },
                new() { Id = idProAmaya, FirstName = "Amaya", LastName = "De Silva", Email = "amaya@microgrid.com", PhoneNumber = "+94770000014", Nic = nicProAmaya, PasswordHash = prosumerHash, Role = Role.Prosumer, AccountStatus = AccountStatus.Active, IsActive = true, CreatedAt = now.AddDays(-50), UpdatedAt = now },
                new() { Id = idProRuwan, FirstName = "Ruwan", LastName = "Bandara", Email = "ruwan@microgrid.com", PhoneNumber = "+94770000015", Nic = nicProRuwan, PasswordHash = prosumerHash, Role = Role.Prosumer, AccountStatus = AccountStatus.Active, IsActive = true, CreatedAt = now.AddDays(-45), UpdatedAt = now },
                new() { Id = idProTharushi, FirstName = "Tharushi", LastName = "Kumari", Email = "tharushi@microgrid.com", PhoneNumber = "+94770000017", Nic = nicProTharushi, PasswordHash = prosumerHash, Role = Role.Prosumer, AccountStatus = AccountStatus.Active, IsActive = true, CreatedAt = now.AddDays(-40), UpdatedAt = now },
                new() { Id = idProChamara, FirstName = "Chamara", LastName = "Wickramasinghe", Email = "chamara@microgrid.com", PhoneNumber = "+94770000101", Nic = nicProChamara, PasswordHash = prosumerHash, Role = Role.Prosumer, AccountStatus = AccountStatus.Suspended, IsActive = false, StatusChangedAt = now.AddDays(-10), StatusChangedBy = "Nimal Fernando", CreatedAt = now.AddDays(-35), UpdatedAt = now.AddDays(-10) },
                new() { Id = idProNadeesha, FirstName = "Nadeesha", LastName = "Herath", Email = "nadeesha@microgrid.com", PhoneNumber = "+94770000102", Nic = nicProNadeesha, PasswordHash = prosumerHash, Role = Role.Prosumer, AccountStatus = AccountStatus.Inactive, IsActive = false, StatusChangedAt = now.AddDays(-12), StatusChangedBy = "System Administrator", CreatedAt = now.AddDays(-55), UpdatedAt = now.AddDays(-12) },
                new() { Id = idProSuresh, FirstName = "Suresh", LastName = "Ranasinghe", Email = "suresh@microgrid.com", PhoneNumber = "+94770000103", Nic = nicProSuresh, PasswordHash = prosumerHash, Role = Role.Prosumer, AccountStatus = AccountStatus.Pending, IsActive = false, CreatedAt = now.AddDays(-1), UpdatedAt = now },
                new() { Id = idProDilini, FirstName = "Dilini", LastName = "Wickramasinghe", Email = "dilini@microgrid.com", PhoneNumber = "+94770000016", Nic = nicProDilini, PasswordHash = prosumerHash, Role = Role.Prosumer, AccountStatus = AccountStatus.Inactive, IsActive = false, StatusChangedAt = now.AddDays(-25), StatusChangedBy = "System Administrator", CreatedAt = now.AddDays(-65), UpdatedAt = now.AddDays(-25) }
            };

            await context.Users.InsertManyAsync(users);

            // ─────────────────────────────────────────────────────────────
            //  2. MICROGRID NODES (10 distributed nodes across Sri Lanka)
            // ─────────────────────────────────────────────────────────────
            var idMg1 = ObjectId.GenerateNewId().ToString();
            var idMg2 = ObjectId.GenerateNewId().ToString();
            var idMg3 = ObjectId.GenerateNewId().ToString();
            var idMg4 = ObjectId.GenerateNewId().ToString();
            var idMg5 = ObjectId.GenerateNewId().ToString();
            var idMg6 = ObjectId.GenerateNewId().ToString();
            var idMg7 = ObjectId.GenerateNewId().ToString();
            var idMg8 = ObjectId.GenerateNewId().ToString();
            var idMg9 = ObjectId.GenerateNewId().ToString();
            var idMg10 = ObjectId.GenerateNewId().ToString();

            var microgrids = new List<MicrogridNode>
            {
                MakeGrid(idMg1, "Colombo Solar Hub", "Colombo", "High-efficiency 500kW commercial solar array in Central Colombo with smart inverter management.", 6.9271, 79.8612, 500, 320, 110, 70, 200, 165, 82.5, 24, "Active", true, idOpAlex, now),
                MakeGrid(idMg2, "Kandy Highland Microgrid", "Kandy", "Highland solar and battery storage microgrid serving local tea estates and valley residents.", 7.2906, 80.6337, 300, 180, 70, 50, 150, 112, 74.6, 18, "Active", true, idOpKavindu, now),
                MakeGrid(idMg3, "Galle Coastal Solar Park", "Galle", "Coastal solar farm with lithium storage backup and direct harbor feeder lines.", 6.0535, 80.2210, 450, 310, 90, 50, 180, 140, 77.8, 20, "Active", true, idOpLakshmi, now),
                MakeGrid(idMg4, "Jaffna Northern Grid", "Jaffna", "Northern peninsula solar micro-grid feeding clean energy to agricultural community.", 9.6615, 80.0255, 350, 240, 60, 50, 120, 96, 80.0, 16, "Active", true, idOpAlex, now),
                MakeGrid(idMg5, "Negombo Lagoon Solar Farm", "Negombo", "Floating solar panel arrays on Negombo lagoon with IoT telemetry and grid stabilization.", 7.2083, 79.8358, 600, 410, 120, 70, 250, 205, 82.0, 32, "Active", true, idOpKavindu, now),
                MakeGrid(idMg6, "Nuwara Eliya Green Energy", "Nuwara Eliya", "High-altitude hybrid wind and solar micro-grid engineered for highland microclimates.", 6.9497, 80.7891, 200, 130, 40, 30, 100, 68, 68.0, 12, "Active", true, idOpLakshmi, now),
                MakeGrid(idMg7, "Trincomalee Eastern Hub", "Trincomalee", "Eastern coastal deep-water harbor solar installation with rapid DC EV charging integration.", 8.5874, 81.2152, 400, 270, 80, 50, 160, 132, 82.5, 20, "Active", true, idOpAlex, now),
                MakeGrid(idMg8, "Matara Southern Solar", "Matara", "Southern district distributed solar panels across multiple academic and commercial rooftops.", 5.9485, 80.5353, 250, 160, 55, 35, 100, 72, 72.0, 14, "Active", true, idOpKavindu, now),
                MakeGrid(idMg9, "Batticaloa Solar Station", "Batticaloa", "Dry zone high-insolation concentrated solar generation station with tracking panels.", 7.7310, 81.6747, 380, 280, 60, 40, 140, 105, 75.0, 22, "Active", true, idOpLakshmi, now),
                MakeGrid(idMg10, "Anuradhapura Heritage Grid", "Anuradhapura", "Ancient heritage zone eco-solar array currently undergoing scheduled inverter overhaul.", 8.3114, 80.4037, 280, 0, 0, 0, 110, 22, 20.0, 14, "Maintenance", false, idOpAlex, now)
            };

            await context.Microgrids.InsertManyAsync(microgrids);

            // ─────────────────────────────────────────────────────────────
            //  3. ENERGY SLOTS (18 slots covering past, current, future)
            // ─────────────────────────────────────────────────────────────
            var idSlot1 = ObjectId.GenerateNewId().ToString();
            var idSlot2 = ObjectId.GenerateNewId().ToString();
            var idSlot3 = ObjectId.GenerateNewId().ToString();
            var idSlot4 = ObjectId.GenerateNewId().ToString();
            var idSlot5 = ObjectId.GenerateNewId().ToString();
            var idSlot6 = ObjectId.GenerateNewId().ToString();
            var idSlot7 = ObjectId.GenerateNewId().ToString();
            var idSlot8 = ObjectId.GenerateNewId().ToString();
            var idSlot9 = ObjectId.GenerateNewId().ToString();
            var idSlot10 = ObjectId.GenerateNewId().ToString();
            var idSlot11 = ObjectId.GenerateNewId().ToString();
            var idSlot12 = ObjectId.GenerateNewId().ToString();
            var idSlot13 = ObjectId.GenerateNewId().ToString();
            var idSlot14 = ObjectId.GenerateNewId().ToString();
            var idSlot15 = ObjectId.GenerateNewId().ToString();
            var idSlot16 = ObjectId.GenerateNewId().ToString();
            var idSlot17 = ObjectId.GenerateNewId().ToString();
            var idSlot18 = ObjectId.GenerateNewId().ToString();

            var slots = new List<EnergySlot>
            {
                // Slot 1 (Colombo - Upcoming morning slot, booked by Sam)
                MakeSlot(idSlot1, idMg1, 100, 85, now.AddHours(2), now.AddHours(6), 25.00m, "PartiallyReserved", idOpAlex, now),
                // Slot 2 (Colombo - Upcoming afternoon slot, booked by Amaya)
                MakeSlot(idSlot2, idMg1, 80, 60, now.AddHours(6), now.AddHours(10), 28.00m, "PartiallyReserved", idOpAlex, now),
                // Slot 3 (Kandy - Upcoming morning slot, booked by Sam)
                MakeSlot(idSlot3, idMg2, 60, 50, now.AddHours(3), now.AddHours(7), 22.50m, "PartiallyReserved", idOpKavindu, now),
                // Slot 4 (Galle - Past delivery slot, completed by Sam)
                MakeSlot(idSlot4, idMg3, 120, 100, now.AddDays(-1).AddHours(2), now.AddDays(-1).AddHours(6), 24.00m, "PartiallyReserved", idOpLakshmi, now),
                // Slot 5 (Jaffna - Past delivery slot, completed by Ruwan)
                MakeSlot(idSlot5, idMg4, 70, 52, now.AddDays(-2).AddHours(1), now.AddDays(-2).AddHours(5), 20.00m, "PartiallyReserved", idOpAlex, now),
                // Slot 6 (Negombo - Upcoming afternoon slot, pending for Sam)
                MakeSlot(idSlot6, idMg5, 120, 95, now.AddHours(4), now.AddHours(8), 30.00m, "PartiallyReserved", idOpKavindu, now),
                // Slot 7 (Negombo - Tomorrow morning slot, pending for Amaya)
                MakeSlot(idSlot7, idMg5, 90, 75, now.AddDays(1).AddHours(2), now.AddDays(1).AddHours(6), 27.50m, "PartiallyReserved", idOpKavindu, now),
                // Slot 8 (Nuwara Eliya - Available slot tomorrow)
                MakeSlot(idSlot8, idMg6, 50, 50, now.AddDays(1).AddHours(1), now.AddDays(1).AddHours(5), 18.00m, "Available", idOpLakshmi, now),
                // Slot 9 (Nuwara Eliya - Past completed slot for Amaya)
                MakeSlot(idSlot9, idMg6, 40, 28, now.AddDays(-3).AddHours(2), now.AddDays(-3).AddHours(6), 18.00m, "PartiallyReserved", idOpLakshmi, now),
                // Slot 10 (Trincomalee - Available slot tomorrow afternoon)
                MakeSlot(idSlot10, idMg7, 110, 110, now.AddDays(1).AddHours(4), now.AddDays(1).AddHours(8), 26.00m, "Available", idOpAlex, now),
                // Slot 11 (Trincomalee - Upcoming slot, approved for Amaya)
                MakeSlot(idSlot11, idMg7, 90, 60, now.AddHours(5), now.AddHours(9), 26.00m, "PartiallyReserved", idOpAlex, now),
                // Slot 12 (Trincomalee - Tomorrow morning, approved for Ruwan)
                MakeSlot(idSlot12, idMg7, 100, 78, now.AddDays(1).AddHours(1), now.AddDays(1).AddHours(5), 26.50m, "PartiallyReserved", idOpAlex, now),
                // Slot 13 (Colombo - Day after tomorrow, pending for Ruwan)
                MakeSlot(idSlot13, idMg1, 75, 61, now.AddDays(2).AddHours(3), now.AddDays(2).AddHours(7), 29.00m, "PartiallyReserved", idOpAlex, now),
                // Slot 14 (Matara - Rejected reservation slot)
                MakeSlot(idSlot14, idMg8, 55, 55, now.AddDays(1).AddHours(2), now.AddDays(1).AddHours(6), 21.00m, "Available", idOpKavindu, now),
                // Slot 15 (Matara - Pending slot for Tharushi)
                MakeSlot(idSlot15, idMg8, 60, 52, now.AddHours(3), now.AddHours(7), 21.50m, "PartiallyReserved", idOpKavindu, now),
                // Slot 16 (Batticaloa - Rejected reservation slot)
                MakeSlot(idSlot16, idMg9, 75, 75, now.AddDays(1).AddHours(4), now.AddDays(1).AddHours(8), 23.00m, "Available", idOpLakshmi, now),
                // Slot 17 (Kandy - Cancelled reservation slot)
                MakeSlot(idSlot17, idMg2, 50, 50, now.AddDays(2).AddHours(1), now.AddDays(2).AddHours(5), 22.00m, "Available", idOpKavindu, now),
                // Slot 18 (Colombo - Completed past slot for Tharushi)
                MakeSlot(idSlot18, idMg1, 85, 67, now.AddDays(-1).AddHours(4), now.AddDays(-1).AddHours(8), 27.00m, "PartiallyReserved", idOpAlex, now)
            };

            await context.EnergySlots.InsertManyAsync(slots);

            // ─────────────────────────────────────────────────────────────
            //  4. RESERVATIONS (16 reservations across lifecycle states)
            // ─────────────────────────────────────────────────────────────
            var idRes1 = ObjectId.GenerateNewId().ToString();
            var idRes2 = ObjectId.GenerateNewId().ToString();
            var idRes3 = ObjectId.GenerateNewId().ToString();
            var idRes4 = ObjectId.GenerateNewId().ToString();
            var idRes5 = ObjectId.GenerateNewId().ToString();
            var idRes6 = ObjectId.GenerateNewId().ToString();
            var idRes7 = ObjectId.GenerateNewId().ToString();
            var idRes8 = ObjectId.GenerateNewId().ToString();
            var idRes9 = ObjectId.GenerateNewId().ToString();
            var idRes10 = ObjectId.GenerateNewId().ToString();
            var idRes11 = ObjectId.GenerateNewId().ToString();
            var idRes12 = ObjectId.GenerateNewId().ToString();
            var idRes13 = ObjectId.GenerateNewId().ToString();
            var idRes14 = ObjectId.GenerateNewId().ToString();
            var idRes15 = ObjectId.GenerateNewId().ToString();
            var idRes16 = ObjectId.GenerateNewId().ToString();

            var reservations = new List<Reservation>
            {
                // Sam Jayawardena (nicProSam) - Primary Demo Prosumer
                // 1. Approved with QR generated, ready for operator scan
                MakeReservation(idRes1, nicProSam, idMg1, idSlot1, 15.0, now.AddHours(2), now.AddHours(6), "Approved", now.AddHours(-3), nicProSam, idOpAlex),
                // 2. Approved ready to generate QR
                MakeReservation(idRes2, nicProSam, idMg2, idSlot3, 10.0, now.AddHours(3), now.AddHours(7), "Approved", now.AddHours(-2), nicProSam, idOpKavindu),
                // 3. Pending reservation, testable for edit and cancel on Android
                MakeReservation(idRes3, nicProSam, idMg5, idSlot6, 25.0, now.AddHours(4), now.AddHours(8), "Pending", now.AddMinutes(-45), nicProSam),
                // 4. Completed delivery from yesterday
                MakeReservation(idRes4, nicProSam, idMg3, idSlot4, 20.0, now.AddDays(-1).AddHours(2), now.AddDays(-1).AddHours(6), "Completed", now.AddDays(-1).AddHours(-4), nicProSam, idOpLakshmi, idOpLakshmi),

                // Amaya De Silva (nicProAmaya)
                // 5. Approved & Verified at Colombo
                MakeReservation(idRes5, nicProAmaya, idMg1, idSlot2, 20.0, now.AddHours(6), now.AddHours(10), "Approved", now.AddHours(-5), nicProAmaya, idOpAlex),
                // 6. Pending at Negombo
                MakeReservation(idRes6, nicProAmaya, idMg5, idSlot7, 15.0, now.AddDays(1).AddHours(2), now.AddDays(1).AddHours(6), "Pending", now.AddHours(-1), nicProAmaya),
                // 7. Completed at Nuwara Eliya
                MakeReservation(idRes7, nicProAmaya, idMg6, idSlot9, 12.0, now.AddDays(-3).AddHours(2), now.AddDays(-3).AddHours(6), "Completed", now.AddDays(-3).AddHours(-3), nicProAmaya, idOpLakshmi, idOpLakshmi),
                // 8. Approved at Trincomalee
                MakeReservation(idRes8, nicProAmaya, idMg7, idSlot11, 30.0, now.AddHours(5), now.AddHours(9), "Approved", now.AddHours(-4), nicProAmaya, idOpAlex),

                // Ruwan Bandara (nicProRuwan)
                // 9. Completed at Jaffna
                MakeReservation(idRes9, nicProRuwan, idMg4, idSlot5, 18.0, now.AddDays(-2).AddHours(1), now.AddDays(-2).AddHours(5), "Completed", now.AddDays(-2).AddHours(-5), nicProRuwan, idOpAlex, idOpAlex),
                // 10. Approved at Trincomalee
                MakeReservation(idRes10, nicProRuwan, idMg7, idSlot12, 22.0, now.AddDays(1).AddHours(1), now.AddDays(1).AddHours(5), "Approved", now.AddHours(-6), nicProRuwan, idOpAlex),
                // 11. Pending at Colombo
                MakeReservation(idRes11, nicProRuwan, idMg1, idSlot13, 14.0, now.AddDays(2).AddHours(3), now.AddDays(2).AddHours(7), "Pending", now.AddMinutes(-30), nicProRuwan),
                // 12. Rejected at Matara
                MakeReservation(idRes12, nicProRuwan, idMg8, idSlot14, 10.0, now.AddDays(1).AddHours(2), now.AddDays(1).AddHours(6), "Rejected", now.AddHours(-10), nicProRuwan, idOpKavindu, reason: "Feeder line capacity constraints during grid maintenance"),

                // Tharushi Kumari (nicProTharushi)
                // 13. Pending at Matara
                MakeReservation(idRes13, nicProTharushi, idMg8, idSlot15, 8.0, now.AddHours(3), now.AddHours(7), "Pending", now.AddMinutes(-15), nicProTharushi),
                // 14. Rejected at Batticaloa
                MakeReservation(idRes14, nicProTharushi, idMg9, idSlot16, 16.0, now.AddDays(1).AddHours(4), now.AddDays(1).AddHours(8), "Rejected", now.AddHours(-8), nicProTharushi, idOpLakshmi, reason: "Requested amount exceeds peak slot cap"),
                // 15. Cancelled at Kandy
                MakeReservation(idRes15, nicProTharushi, idMg2, idSlot17, 12.0, now.AddDays(2).AddHours(1), now.AddDays(2).AddHours(5), "Cancelled", now.AddHours(-5), nicProTharushi, reason: "Prosumer schedule change"),
                // 16. Completed at Colombo
                MakeReservation(idRes16, nicProTharushi, idMg1, idSlot18, 18.0, now.AddDays(-1).AddHours(4), now.AddDays(-1).AddHours(8), "Completed", now.AddDays(-1).AddHours(-6), nicProTharushi, idOpAlex, idOpAlex)
            };

            await context.Reservations.InsertManyAsync(reservations);

            // ─────────────────────────────────────────────────────────────
            //  5. TRANSACTIONS (12 M3 transactions covering all statuses)
            // ─────────────────────────────────────────────────────────────
            var year = now.Year;
            var transactions = new List<Transaction>
            {
                // Tx 1: Sam Jayawardena - QR Generated (Colombo Solar Hub)
                new()
                {
                    Id = ObjectId.GenerateNewId().ToString(),
                    ReservationId = idRes1,
                    ProsumerId = nicProSam,
                    MicrogridNodeId = idMg1,
                    EnergySlotId = idSlot1,
                    EnergyAmount = 15.0,
                    TransactionCode = $"SMG-{year}-100101",
                    QrCodeData = $"SMART-MICROGRID|TRANSACTION|qrgenerated|SMG-{year}-100101",
                    Status = "QRGenerated",
                    CreatedAt = now.AddHours(-2),
                    UpdatedAt = now.AddHours(-1)
                },
                // Tx 2: Sam Jayawardena - Pending (Kandy Highland)
                new()
                {
                    Id = ObjectId.GenerateNewId().ToString(),
                    ReservationId = idRes2,
                    ProsumerId = nicProSam,
                    MicrogridNodeId = idMg2,
                    EnergySlotId = idSlot3,
                    EnergyAmount = 10.0,
                    TransactionCode = $"SMG-{year}-100102",
                    QrCodeData = string.Empty,
                    Status = "Pending",
                    CreatedAt = now.AddHours(-1).AddMinutes(-30),
                    UpdatedAt = now.AddHours(-1).AddMinutes(-30)
                },
                // Tx 3: Sam Jayawardena - Completed (Galle Coastal)
                new()
                {
                    Id = ObjectId.GenerateNewId().ToString(),
                    ReservationId = idRes4,
                    ProsumerId = nicProSam,
                    MicrogridNodeId = idMg3,
                    EnergySlotId = idSlot4,
                    EnergyAmount = 20.0,
                    TransactionCode = $"SMG-{year}-100103",
                    QrCodeData = $"SMART-MICROGRID|TRANSACTION|completed|SMG-{year}-100103",
                    VerifiedBy = idOpLakshmi,
                    VerificationTime = now.AddDays(-1).AddHours(2).AddMinutes(15),
                    EnergyTransferTime = now.AddDays(-1).AddHours(2).AddMinutes(45),
                    Status = "Completed",
                    CreatedAt = now.AddDays(-1).AddHours(1),
                    UpdatedAt = now.AddDays(-1).AddHours(3)
                },
                // Tx 4: Amaya De Silva - Verified (Colombo Solar Hub)
                new()
                {
                    Id = ObjectId.GenerateNewId().ToString(),
                    ReservationId = idRes5,
                    ProsumerId = nicProAmaya,
                    MicrogridNodeId = idMg1,
                    EnergySlotId = idSlot2,
                    EnergyAmount = 20.0,
                    TransactionCode = $"SMG-{year}-100104",
                    QrCodeData = $"SMART-MICROGRID|TRANSACTION|verified|SMG-{year}-100104",
                    VerifiedBy = idOpAlex,
                    VerificationTime = now.AddMinutes(-35),
                    Status = "Verified",
                    CreatedAt = now.AddHours(-4),
                    UpdatedAt = now.AddMinutes(-35)
                },
                // Tx 5: Amaya De Silva - Completed (Nuwara Eliya)
                new()
                {
                    Id = ObjectId.GenerateNewId().ToString(),
                    ReservationId = idRes7,
                    ProsumerId = nicProAmaya,
                    MicrogridNodeId = idMg6,
                    EnergySlotId = idSlot9,
                    EnergyAmount = 12.0,
                    TransactionCode = $"SMG-{year}-100105",
                    QrCodeData = $"SMART-MICROGRID|TRANSACTION|completed|SMG-{year}-100105",
                    VerifiedBy = idOpLakshmi,
                    VerificationTime = now.AddDays(-3).AddHours(2).AddMinutes(20),
                    EnergyTransferTime = now.AddDays(-3).AddHours(2).AddMinutes(50),
                    Status = "Completed",
                    CreatedAt = now.AddDays(-3).AddHours(1),
                    UpdatedAt = now.AddDays(-3).AddHours(3)
                },
                // Tx 6: Amaya De Silva - Verification Pending (Trincomalee)
                new()
                {
                    Id = ObjectId.GenerateNewId().ToString(),
                    ReservationId = idRes8,
                    ProsumerId = nicProAmaya,
                    MicrogridNodeId = idMg7,
                    EnergySlotId = idSlot11,
                    EnergyAmount = 30.0,
                    TransactionCode = $"SMG-{year}-100106",
                    QrCodeData = $"SMART-MICROGRID|TRANSACTION|verificationpending|SMG-{year}-100106",
                    Status = "VerificationPending",
                    CreatedAt = now.AddHours(-3),
                    UpdatedAt = now.AddMinutes(-20)
                },
                // Tx 7: Ruwan Bandara - Completed (Jaffna Northern Grid)
                new()
                {
                    Id = ObjectId.GenerateNewId().ToString(),
                    ReservationId = idRes9,
                    ProsumerId = nicProRuwan,
                    MicrogridNodeId = idMg4,
                    EnergySlotId = idSlot5,
                    EnergyAmount = 18.0,
                    TransactionCode = $"SMG-{year}-100107",
                    QrCodeData = $"SMART-MICROGRID|TRANSACTION|completed|SMG-{year}-100107",
                    VerifiedBy = idOpAlex,
                    VerificationTime = now.AddDays(-2).AddHours(1).AddMinutes(15),
                    EnergyTransferTime = now.AddDays(-2).AddHours(1).AddMinutes(45),
                    Status = "Completed",
                    CreatedAt = now.AddDays(-2).AddHours(1),
                    UpdatedAt = now.AddDays(-2).AddHours(2)
                },
                // Tx 8: Ruwan Bandara - QR Generated (Trincomalee Eastern Hub)
                new()
                {
                    Id = ObjectId.GenerateNewId().ToString(),
                    ReservationId = idRes10,
                    ProsumerId = nicProRuwan,
                    MicrogridNodeId = idMg7,
                    EnergySlotId = idSlot12,
                    EnergyAmount = 22.0,
                    TransactionCode = $"SMG-{year}-100108",
                    QrCodeData = $"SMART-MICROGRID|TRANSACTION|qrgenerated|SMG-{year}-100108",
                    Status = "QRGenerated",
                    CreatedAt = now.AddHours(-5),
                    UpdatedAt = now.AddHours(-2)
                },
                // Tx 9: Tharushi Kumari - Completed (Colombo Solar Hub)
                new()
                {
                    Id = ObjectId.GenerateNewId().ToString(),
                    ReservationId = idRes16,
                    ProsumerId = nicProTharushi,
                    MicrogridNodeId = idMg1,
                    EnergySlotId = idSlot18,
                    EnergyAmount = 18.0,
                    TransactionCode = $"SMG-{year}-100109",
                    QrCodeData = $"SMART-MICROGRID|TRANSACTION|completed|SMG-{year}-100109",
                    VerifiedBy = idOpAlex,
                    VerificationTime = now.AddDays(-1).AddHours(4).AddMinutes(20),
                    EnergyTransferTime = now.AddDays(-1).AddHours(4).AddMinutes(50),
                    Status = "Completed",
                    CreatedAt = now.AddDays(-1).AddHours(3),
                    UpdatedAt = now.AddDays(-1).AddHours(5)
                },
                // Tx 10: Cancelled Transaction
                new()
                {
                    Id = ObjectId.GenerateNewId().ToString(),
                    ReservationId = idRes15,
                    ProsumerId = nicProTharushi,
                    MicrogridNodeId = idMg2,
                    EnergySlotId = idSlot17,
                    EnergyAmount = 12.0,
                    TransactionCode = $"SMG-{year}-100110",
                    QrCodeData = string.Empty,
                    Status = "Cancelled",
                    CreatedAt = now.AddHours(-5),
                    UpdatedAt = now.AddHours(-4)
                },
                // Tx 11: Rejected Transaction
                new()
                {
                    Id = ObjectId.GenerateNewId().ToString(),
                    ReservationId = idRes14,
                    ProsumerId = nicProTharushi,
                    MicrogridNodeId = idMg9,
                    EnergySlotId = idSlot16,
                    EnergyAmount = 16.0,
                    TransactionCode = $"SMG-{year}-100111",
                    QrCodeData = string.Empty,
                    Status = "Rejected",
                    CreatedAt = now.AddHours(-8),
                    UpdatedAt = now.AddHours(-7)
                },
                // Tx 12: Rejected Transaction
                new()
                {
                    Id = ObjectId.GenerateNewId().ToString(),
                    ReservationId = idRes12,
                    ProsumerId = nicProRuwan,
                    MicrogridNodeId = idMg8,
                    EnergySlotId = idSlot14,
                    EnergyAmount = 10.0,
                    TransactionCode = $"SMG-{year}-100112",
                    QrCodeData = string.Empty,
                    Status = "Rejected",
                    CreatedAt = now.AddHours(-10),
                    UpdatedAt = now.AddHours(-9)
                }
            };

            await context.Transactions.InsertManyAsync(transactions);

            // ─────────────────────────────────────────────────────────────
            //  6. SYSTEM CONFIGURATION (Singleton document)
            // ─────────────────────────────────────────────────────────────
            var config = new SystemConfiguration
            {
                Id = SystemConfiguration.SingletonId,
                PlatformName = "Smart Microgrid Energy Management & Trading Platform",
                PlatformDescription = "Client-server platform for microgrid energy management, reservation and transaction administration.",
                MaintenanceMode = false,
                MaintenanceMessage = null,
                AllowRegistration = true,
                SessionTimeoutMinutes = 480,
                MaxLoginAttempts = 5,
                DefaultPageSize = 20,
                CreatedAt = now.AddDays(-120),
                UpdatedAt = now,
                UpdatedBy = "System Administrator"
            };

            await context.SystemConfiguration.InsertOneAsync(config);

            // ─────────────────────────────────────────────────────────────
            //  7. SYSTEM ACTIVITY (30+ rich audit trail entries)
            // ─────────────────────────────────────────────────────────────
            var activityLogs = new List<SystemActivity>
            {
                // Authentication
                MakeActivity(idAdmin1, "System Administrator", Role.Admin.ToString(), AuditAction.LoginSuccess, AuditModule.Authentication, "Admin signed in successfully from management workstation.", "User", idAdmin1, "192.168.1.10", now.AddMinutes(-12), AuditStatus.Success),
                MakeActivity(idAdmin2, "Nimal Fernando", Role.Admin.ToString(), AuditAction.LoginFailed, AuditModule.Authentication, "Failed sign-in attempt: invalid password supplied.", "User", idAdmin2, "192.168.1.15", now.AddMinutes(-48), AuditStatus.Failure),
                MakeActivity(idOpAlex, "Alex Perera", Role.MicrogridOperator.ToString(), AuditAction.LoginSuccess, AuditModule.Authentication, "Microgrid Operator signed in successfully.", "User", idOpAlex, "192.168.1.20", now.AddMinutes(-75), AuditStatus.Success),
                MakeActivity(idProSam, "Sam Jayawardena", Role.Prosumer.ToString(), AuditAction.LoginSuccess, AuditModule.Authentication, "Prosumer signed in via Android Mobile App.", "User", idProSam, "192.168.1.45", now.AddMinutes(-90), AuditStatus.Success),
                MakeActivity(idProChamara, "Chamara Wickramasinghe", Role.Prosumer.ToString(), AuditAction.LoginFailed, AuditModule.Authentication, "Sign-in rejected: Account is currently suspended.", "User", idProChamara, "192.168.1.80", now.AddHours(-6), AuditStatus.Failure),

                // User Management
                MakeActivity(idAdmin1, "System Administrator", Role.Admin.ToString(), AuditAction.UserCreated, AuditModule.UserManagement, $"Created new Prosumer account for Amaya De Silva ({nicProAmaya}).", "User", idProAmaya, "192.168.1.10", now.AddDays(-50), AuditStatus.Success),
                MakeActivity(idAdmin1, "System Administrator", Role.Admin.ToString(), AuditAction.RoleAssigned, AuditModule.UserManagement, "Assigned MicrogridOperator role to Alex Perera.", "User", idOpAlex, "192.168.1.10", now.AddDays(-115), AuditStatus.Success),
                MakeActivity(idAdmin2, "Nimal Fernando", Role.Admin.ToString(), AuditAction.UserSuspended, AuditModule.UserManagement, "Suspended prosumer Chamara Wickramasinghe pending identity verification.", "User", idProChamara, "192.168.1.15", now.AddDays(-10), AuditStatus.Success),
                MakeActivity(idAdmin1, "System Administrator", Role.Admin.ToString(), AuditAction.UserDeactivated, AuditModule.UserManagement, "Deactivated retired operator account Roshan Gamage.", "User", idOpRoshan, "192.168.1.10", now.AddDays(-20), AuditStatus.Success),
                MakeActivity(idAdmin2, "Nimal Fernando", Role.Admin.ToString(), AuditAction.UserStatusChanged, AuditModule.UserManagement, "Activated registered Prosumer Sam Jayawardena after NIC verification.", "User", idProSam, "192.168.1.15", now.AddDays(-60), AuditStatus.Success),
                MakeActivity(idAdmin1, "System Administrator", Role.Admin.ToString(), AuditAction.UserUpdated, AuditModule.UserManagement, "Updated contact phone number for operator Kavindu Silva.", "User", idOpKavindu, "192.168.1.10", now.AddDays(-5), AuditStatus.Success),

                // Microgrid & Slot Management
                MakeActivity(idAdmin1, "System Administrator", Role.Admin.ToString(), "MICROGRID_CREATED", "Microgrid Management", "Created Colombo Solar Hub (500 kW capacity) assigned to Alex Perera.", "Microgrid", idMg1, "192.168.1.10", now.AddDays(-110), AuditStatus.Success),
                MakeActivity(idAdmin1, "System Administrator", Role.Admin.ToString(), "MICROGRID_CREATED", "Microgrid Management", "Created Kandy Highland Microgrid (300 kW capacity) assigned to Kavindu Silva.", "Microgrid", idMg2, "192.168.1.10", now.AddDays(-90), AuditStatus.Success),
                MakeActivity(idOpAlex, "Alex Perera", Role.MicrogridOperator.ToString(), "SLOT_CREATED", "Energy Slot Management", "Created morning energy slot for Colombo Solar Hub (100 kWh @ 25.00 LKR).", "EnergySlot", idSlot1, "192.168.1.20", now.AddDays(-1), AuditStatus.Success),
                MakeActivity(idOpKavindu, "Kavindu Silva", Role.MicrogridOperator.ToString(), "SLOT_CREATED", "Energy Slot Management", "Created morning energy slot for Kandy Highland Microgrid (60 kWh @ 22.50 LKR).", "EnergySlot", idSlot3, "192.168.1.22", now.AddDays(-1), AuditStatus.Success),

                // Reservation & Trading
                MakeActivity(idProSam, "Sam Jayawardena", Role.Prosumer.ToString(), "RESERVATION_CREATED", "Reservation Management", "Created 15 kWh reservation for Colombo Solar Hub.", "Reservation", idRes1, "192.168.1.45", now.AddHours(-3), AuditStatus.Success),
                MakeActivity(idOpAlex, "Alex Perera", Role.MicrogridOperator.ToString(), "RESERVATION_APPROVED", "Reservation Management", "Approved 15 kWh reservation for prosumer Sam Jayawardena.", "Reservation", idRes1, "192.168.1.20", now.AddHours(-2).AddMinutes(-30), AuditStatus.Success),
                MakeActivity(idProAmaya, "Amaya De Silva", Role.Prosumer.ToString(), "RESERVATION_CREATED", "Reservation Management", "Created 20 kWh reservation for Colombo Solar Hub.", "Reservation", idRes5, "192.168.1.48", now.AddHours(-5), AuditStatus.Success),
                MakeActivity(idOpAlex, "Alex Perera", Role.MicrogridOperator.ToString(), "RESERVATION_APPROVED", "Reservation Management", "Approved 20 kWh reservation for Amaya De Silva.", "Reservation", idRes5, "192.168.1.20", now.AddHours(-4).AddMinutes(-30), AuditStatus.Success),
                MakeActivity(idOpKavindu, "Kavindu Silva", Role.MicrogridOperator.ToString(), "RESERVATION_REJECTED", "Reservation Management", "Rejected reservation for Ruwan Bandara due to feeder constraints.", "Reservation", idRes12, "192.168.1.22", now.AddHours(-10), AuditStatus.Success),

                // Transaction & Verification
                MakeActivity(idProSam, "Sam Jayawardena", Role.Prosumer.ToString(), "QR_GENERATED", "Transaction Verification", $"Generated digital QR token for transaction SMG-{year}-100101.", "Transaction", transactions[0].Id, "192.168.1.45", now.AddHours(-2), AuditStatus.Success),
                MakeActivity(idOpAlex, "Alex Perera", Role.MicrogridOperator.ToString(), "TRANSACTION_VERIFIED", "Transaction Verification", $"Scanned and verified prosumer QR code for transaction SMG-{year}-100104.", "Transaction", transactions[3].Id, "192.168.1.20", now.AddMinutes(-35), AuditStatus.Success),
                MakeActivity(idOpLakshmi, "Lakshmi Ratnayake", Role.MicrogridOperator.ToString(), "ENERGY_TRANSFERRED", "Transaction Verification", $"Finalized physical energy transfer (20 kWh) for transaction SMG-{year}-100103.", "Transaction", transactions[2].Id, "192.168.1.25", now.AddDays(-1).AddHours(3), AuditStatus.Success),

                // System Configuration & Operations
                MakeActivity(idAdmin1, "System Administrator", Role.Admin.ToString(), AuditAction.ConfigurationUpdated, AuditModule.SystemConfiguration, "Updated system session timeout to 480 minutes.", "SystemConfiguration", SystemConfiguration.SingletonId, "192.168.1.10", now.AddDays(-15), AuditStatus.Success),
                MakeActivity(idAdmin2, "Nimal Fernando", Role.Admin.ToString(), AuditAction.ConfigurationUpdated, AuditModule.SystemConfiguration, "Verified platform identity metadata and network endpoints.", "SystemConfiguration", SystemConfiguration.SingletonId, "192.168.1.15", now.AddDays(-8), AuditStatus.Success),

                // Reporting
                MakeActivity(idAdmin1, "System Administrator", Role.Admin.ToString(), AuditAction.ReportGenerated, AuditModule.Reporting, "Generated comprehensive platform user administration report.", "Report", "UserReport", "192.168.1.10", now.AddHours(-8), AuditStatus.Success),
                MakeActivity(idAdmin2, "Nimal Fernando", Role.Admin.ToString(), AuditAction.ReportGenerated, AuditModule.Reporting, "Exported energy trading and transaction ledger summary to CSV.", "Report", "TransactionLedger", "192.168.1.15", now.AddHours(-24), AuditStatus.Success),
                MakeActivity(idOpAlex, "Alex Perera", Role.MicrogridOperator.ToString(), AuditAction.ReportGenerated, AuditModule.Reporting, "Generated daily dispatch and capacity utilization report for Colombo Hub.", "Report", "MicrogridReport", "192.168.1.20", now.AddHours(-36), AuditStatus.Success)
            };

            await context.SystemActivity.InsertManyAsync(activityLogs);
        }

        /// <summary>
        /// Helper method constructing a validated MicrogridNode instance.
        /// </summary>
        private static MicrogridNode MakeGrid(
            string id, string name, string location, string desc,
            double lat, double lng,
            double cap, double avail, double reserved, double used,
            double batCap, double batLevel, double batPct, int batteryStorageSlots,
            string status, bool isActive, string operatorId, DateTime now)
        {
            // Execute make grid operations
            return new MicrogridNode
            {
                Id = id,
                Name = name,
                Location = location,
                Description = desc,
                Latitude = lat,
                Longitude = lng,
                Capacity = cap,
                AvailableCapacity = avail,
                ReservedCapacity = reserved,
                UsedCapacity = used,
                BatteryCapacity = batCap,
                CurrentBatteryLevel = batLevel,
                BatteryPercentage = batPct,
                BatteryStorageSlots = batteryStorageSlots,
                Status = status,
                IsActive = isActive,
                OperatorId = operatorId,
                CreatedAt = now.AddDays(-100),
                UpdatedAt = now
            };
        }

        /// <summary>
        /// Helper method constructing a validated EnergySlot instance.
        /// </summary>
        private static EnergySlot MakeSlot(
            string id, string nodeId, double total, double available,
            DateTime startTime, DateTime endTime,
            decimal price, string status, string createdBy, DateTime now)
        {
            // Execute make slot operations
            return new EnergySlot
            {
                Id = id,
                MicrogridNodeId = nodeId,
                EnergyAmount = total,
                AvailableAmount = available,
                StartTime = startTime,
                EndTime = endTime,
                PricePerUnit = price,
                Status = status,
                CreatedBy = createdBy,
                CreatedAt = now.AddDays(-2),
                UpdatedAt = now
            };
        }

        /// <summary>
        /// Helper method constructing a validated Reservation instance with audit status history.
        /// </summary>
        private static Reservation MakeReservation(
            string id, string prosumerNic, string nodeId, string slotId,
            double amount, DateTime startTime, DateTime endTime,
            string status, DateTime createdAt,
            string createdBy,
            string? approvedBy = null, string? completedBy = null,
            string? reason = null)
        {
            // Execute make reservation operations
            var history = new List<ReservationStatusEvent>
            {
                new() { From = null, To = "Pending", ChangedBy = createdBy, ChangedAt = createdAt }
            };

            if (status is "Approved" or "Completed" or "Rejected")
            {
                var actor = approvedBy ?? createdBy;
                var target = status == "Rejected" ? "Rejected" : "Approved";
                history.Add(new ReservationStatusEvent
                {
                    From = "Pending",
                    To = target,
                    ChangedBy = actor,
                    ChangedAt = createdAt.AddMinutes(30),
                    Reason = status == "Rejected" ? reason : null
                });
            }

            if (status == "Completed" && completedBy != null)
            {
                history.Add(new ReservationStatusEvent
                {
                    From = "Approved",
                    To = "Completed",
                    ChangedBy = completedBy,
                    ChangedAt = createdAt.AddHours(2)
                });
            }

            if (status == "Cancelled")
            {
                history.Add(new ReservationStatusEvent
                {
                    From = "Pending",
                    To = "Cancelled",
                    ChangedBy = createdBy,
                    ChangedAt = createdAt.AddMinutes(45),
                    Reason = reason
                });
            }

            return new Reservation
            {
                Id = id,
                ProsumerId = prosumerNic,
                MicrogridNodeId = nodeId,
                EnergySlotId = slotId,
                EnergyAmount = amount,
                ReservationDate = startTime.Date,
                StartTime = startTime,
                EndTime = endTime,
                Status = status,
                CreatedAt = createdAt,
                UpdatedAt = createdAt.AddHours(1),
                StatusHistory = history
            };
        }

        /// <summary>
        /// Helper method constructing a system activity audit log record.
        /// </summary>
        private static SystemActivity MakeActivity(
            string? userId, string userName, string? role,
            string action, string module, string description,
            string? entityType, string? entityId, string? ipAddress,
            DateTime timestamp, string status)
        {
            // Execute make activity operations
            return new SystemActivity
            {
                Id = ObjectId.GenerateNewId().ToString(),
                UserId = userId,
                UserName = userName,
                Role = role,
                Action = action,
                Module = module,
                Description = description,
                EntityType = entityType,
                EntityId = entityId,
                IpAddress = ipAddress,
                Timestamp = timestamp,
                Status = status
            };
        }
    }
}
