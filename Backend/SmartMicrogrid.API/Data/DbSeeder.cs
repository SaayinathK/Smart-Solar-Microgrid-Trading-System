using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Driver;
using SmartMicrogrid.API.Helpers;
using SmartMicrogrid.API.Models.Common;
using SmartMicrogrid.API.Models.M1;
using SmartMicrogrid.API.Models.M2;

namespace SmartMicrogrid.API.Data
{
    public static class DbSeeder
    {
        public static async Task SeedDefaultUsersAsync(MongoDbContext context)
        {
            // ──────────────────────────────────────────────
            //  1. USERS  (12 records – at least 10 required)
            // ──────────────────────────────────────────────
            var userCount = await context.Users.CountDocumentsAsync(Builders<User>.Filter.Empty);
            if (userCount == 0)
            {
                var now = DateTime.UtcNow;
                var defaultUsers = new List<User>
                {
                    // ── Admins ──
                    new User
                    {
                        FirstName = "System", LastName = "Administrator",
                        Email = "admin@microgrid.com", PhoneNumber = "+94770000001",
                        Nic = "200012345678",
                        PasswordHash = PasswordHelper.HashPassword("Admin123!"),
                        Role = Role.Admin, IsActive = true,
                        CreatedAt = now, UpdatedAt = now
                    },
                    new User
                    {
                        FirstName = "Nimal", LastName = "Fernando",
                        Email = "nimal.admin@microgrid.com", PhoneNumber = "+94770000011",
                        Nic = "199812345679",
                        PasswordHash = PasswordHelper.HashPassword("Admin123!"),
                        Role = Role.Admin, IsActive = true,
                        CreatedAt = now, UpdatedAt = now
                    },

                    // ── Microgrid Operators ──
                    new User
                    {
                        FirstName = "Alex", LastName = "Perera",
                        Email = "operator@microgrid.com", PhoneNumber = "+94770000002",
                        Nic = "199512345680",
                        PasswordHash = PasswordHelper.HashPassword("Operator123!"),
                        Role = Role.MicrogridOperator, IsActive = true,
                        CreatedAt = now, UpdatedAt = now
                    },
                    new User
                    {
                        FirstName = "Kavindu", LastName = "Silva",
                        Email = "kavindu.op@microgrid.com", PhoneNumber = "+94770000012",
                        Nic = "199712345681",
                        PasswordHash = PasswordHelper.HashPassword("Operator123!"),
                        Role = Role.MicrogridOperator, IsActive = true,
                        CreatedAt = now, UpdatedAt = now
                    },
                    new User
                    {
                        FirstName = "Lakshmi", LastName = "Ratnayake",
                        Email = "lakshmi.op@microgrid.com", PhoneNumber = "+94770000013",
                        Nic = "199612345682",
                        PasswordHash = PasswordHelper.HashPassword("Operator123!"),
                        Role = Role.MicrogridOperator, IsActive = true,
                        CreatedAt = now, UpdatedAt = now
                    },

                    // ── Prosumers ──
                    new User
                    {
                        FirstName = "Sam", LastName = "Jayawardena",
                        Email = "prosumer@microgrid.com", PhoneNumber = "+94770000003",
                        Nic = "200112345683",
                        PasswordHash = PasswordHelper.HashPassword("Prosumer123!"),
                        Role = Role.Prosumer, IsActive = true,
                        CreatedAt = now, UpdatedAt = now
                    },
                    new User
                    {
                        FirstName = "Amaya", LastName = "De Silva",
                        Email = "amaya@microgrid.com", PhoneNumber = "+94770000014",
                        Nic = "200212345684",
                        PasswordHash = PasswordHelper.HashPassword("Prosumer123!"),
                        Role = Role.Prosumer, IsActive = true,
                        CreatedAt = now, UpdatedAt = now
                    },
                    new User
                    {
                        FirstName = "Ruwan", LastName = "Bandara",
                        Email = "ruwan@microgrid.com", PhoneNumber = "+94770000015",
                        Nic = "199912345685",
                        PasswordHash = PasswordHelper.HashPassword("Prosumer123!"),
                        Role = Role.Prosumer, IsActive = true,
                        CreatedAt = now, UpdatedAt = now
                    },
                    new User
                    {
                        FirstName = "Dilini", LastName = "Wickramasinghe",
                        Email = "dilini@microgrid.com", PhoneNumber = "+94770000016",
                        Nic = "200312345686",
                        PasswordHash = PasswordHelper.HashPassword("Prosumer123!"),
                        Role = Role.Prosumer, IsActive = false,  // deactivated account
                        CreatedAt = now, UpdatedAt = now
                    },
                    new User
                    {
                        FirstName = "Tharushi", LastName = "Kumari",
                        Email = "tharushi@microgrid.com", PhoneNumber = "+94770000017",
                        Nic = "200012345687",
                        PasswordHash = PasswordHelper.HashPassword("Prosumer123!"),
                        Role = Role.Prosumer, IsActive = true,
                        CreatedAt = now, UpdatedAt = now
                    },

                    // ── Transaction Verifiers ──
                    new User
                    {
                        FirstName = "Valerie", LastName = "Cooray",
                        Email = "verifier@microgrid.com", PhoneNumber = "+94770000004",
                        Nic = "199412345688",
                        PasswordHash = PasswordHelper.HashPassword("Verifier123!"),
                        Role = Role.TransactionVerifier, IsActive = true,
                        CreatedAt = now, UpdatedAt = now
                    },
                    new User
                    {
                        FirstName = "Ishara", LastName = "Gunasekara",
                        Email = "ishara.verifier@microgrid.com", PhoneNumber = "+94770000018",
                        Nic = "199312345689",
                        PasswordHash = PasswordHelper.HashPassword("Verifier123!"),
                        Role = Role.TransactionVerifier, IsActive = true,
                        CreatedAt = now, UpdatedAt = now
                    }
                };

                await context.Users.InsertManyAsync(defaultUsers);

                // Grab inserted operator IDs for microgrid assignment
                var opAlex  = defaultUsers[2].Id;
                var opKavindu = defaultUsers[3].Id;
                var opLakshmi = defaultUsers[4].Id;

                // ──────────────────────────────────────────────
                //  2. MICROGRID NODES  (10 records)
                // ──────────────────────────────────────────────
                var mg1  = MakeGrid("Colombo Solar Hub",          "Colombo",        "High-efficiency 500kW commercial solar array in Central Colombo.",                     6.9271,  79.8612, 500, 350, 100,  50, 200, 160, 80, "Active",      true,  opAlex,   now);
                var mg2  = MakeGrid("Kandy Highland Microgrid",   "Kandy",          "Highland solar and battery storage microgrid serving local tea estates.",               7.2906,  80.6337, 300, 200,  50,  50, 150, 105, 70, "Active",      true,  opKavindu,now);
                var mg3  = MakeGrid("Galle Coastal Solar Park",   "Galle",          "Coastal solar farm with lithium storage backup.",                                      6.0535,  80.2210, 450,   0,   0,   0, 180,  45, 25, "Maintenance", false, opLakshmi,now);
                var mg4  = MakeGrid("Jaffna Northern Grid",       "Jaffna",         "Northern peninsula solar micro-grid feeding rural electrification.",                    9.6615,  80.0255, 350, 280,  40,  30, 120,  96, 80, "Active",      true,  opAlex,   now);
                var mg5  = MakeGrid("Negombo Lagoon Solar Farm",  "Negombo",        "Floating solar panels on Negombo lagoon with smart inverter tech.",                     7.2083,  79.8358, 600, 450, 100,  50, 250, 200, 80, "Active",      true,  opKavindu,now);
                var mg6  = MakeGrid("Nuwara Eliya Green Energy",  "Nuwara Eliya",   "High-altitude hybrid wind and solar micro-grid.",                                      6.9497,  80.7891, 200, 140,  30,  30, 100,  60, 60, "Active",      true,  opLakshmi,now);
                var mg7  = MakeGrid("Trincomalee Eastern Hub",    "Trincomalee",    "Eastern coastal solar installation with EV charging integration.",                      8.5874,  81.2152, 400, 300,  60,  40, 160, 128, 80, "Active",      true,  opAlex,   now);
                var mg8  = MakeGrid("Matara Southern Solar",      "Matara",         "Southern district distributed solar panels across multiple rooftops.",                  5.9485,  80.5353, 250, 180,  40,  30, 100,  50, 50, "Active",      true,  opKavindu,now);
                var mg9  = MakeGrid("Batticaloa Solar Station",   "Batticaloa",     "Arid zone concentrated solar power station.",                                          7.7310,  81.6747, 380, 300,  50,  30, 140,  98, 70, "Active",      true,  opLakshmi,now);
                var mg10 = MakeGrid("Anuradhapura Heritage Grid", "Anuradhapura",   "Ancient city solar-powered heritage preservation micro-grid.",                          8.3114,  80.4037, 280,   0,   0,   0, 110,  22, 20, "Offline",     false, opAlex,   now);

                var microgrids = new[] { mg1, mg2, mg3, mg4, mg5, mg6, mg7, mg8, mg9, mg10 };
                await context.Microgrids.InsertManyAsync(microgrids);

                // ──────────────────────────────────────────────
                //  3. ENERGY SLOTS  (12 records)
                // ──────────────────────────────────────────────
                var slots = new List<EnergySlot>
                {
                    MakeSlot(mg1.Id!, 100, 100, 1,  5, 25.00m, "Available",        opAlex,   now),
                    MakeSlot(mg1.Id!, 80,   30, 2,  6, 28.00m, "PartiallyReserved", opAlex,   now),
                    MakeSlot(mg2.Id!, 50,   50, 1,  4, 22.50m, "Available",        opKavindu,now),
                    MakeSlot(mg2.Id!, 60,    0, 3,  7, 24.00m, "FullyReserved",    opKavindu,now),
                    MakeSlot(mg4.Id!, 70,   70, 2,  6, 20.00m, "Available",        opAlex,   now),
                    MakeSlot(mg5.Id!, 120, 100, 4,  8, 30.00m, "PartiallyReserved", opKavindu,now),
                    MakeSlot(mg5.Id!, 90,   90, 6, 10, 27.50m, "Available",        opKavindu,now),
                    MakeSlot(mg6.Id!, 40,   40, 3,  7, 18.00m, "Available",        opLakshmi,now),
                    MakeSlot(mg7.Id!, 110, 110, 1,  5, 26.00m, "Available",        opAlex,   now),
                    MakeSlot(mg8.Id!, 55,   55, 2,  6, 21.00m, "Available",        opKavindu,now),
                    MakeSlot(mg9.Id!, 75,   75, 4,  8, 23.00m, "Available",        opLakshmi,now),
                    MakeSlot(mg1.Id!, 60,   60, 8, 12, 32.00m, "Available",        opAlex,   now)
                };
                await context.EnergySlots.InsertManyAsync(slots);

                // Prosumer NICs
                var nicSam      = defaultUsers[5].Nic!;   // Sam
                var nicAmaya    = defaultUsers[6].Nic!;   // Amaya
                var nicRuwan    = defaultUsers[7].Nic!;   // Ruwan
                var nicTharushi = defaultUsers[9].Nic!;   // Tharushi

                // ──────────────────────────────────────────────
                //  4. RESERVATIONS  (12 records – mixed statuses)
                // ──────────────────────────────────────────────
                var reservations = new List<Reservation>
                {
                    MakeReservation(nicSam,      mg1.Id!, slots[0].Id!, 10.0, 1,  5, "Pending",   now, nicSam),
                    MakeReservation(nicSam,      mg2.Id!, slots[2].Id!, 8.0,  1,  4, "Approved",  now, nicSam,   opKavindu),
                    MakeReservation(nicAmaya,    mg1.Id!, slots[1].Id!, 15.0, 2,  6, "Approved",  now, nicAmaya, opAlex),
                    MakeReservation(nicAmaya,    mg5.Id!, slots[5].Id!, 20.0, 4,  8, "Pending",   now, nicAmaya),
                    MakeReservation(nicRuwan,    mg4.Id!, slots[4].Id!, 12.0, 2,  6, "Completed", now, nicRuwan, opAlex,  defaultUsers[10].Id),
                    MakeReservation(nicRuwan,    mg7.Id!, slots[8].Id!, 18.0, 1,  5, "Approved",  now, nicRuwan, opAlex),
                    MakeReservation(nicTharushi, mg8.Id!, slots[9].Id!, 7.5,  2,  6, "Pending",   now, nicTharushi),
                    MakeReservation(nicTharushi, mg9.Id!, slots[10].Id!, 10.0, 4,  8, "Rejected",  now, nicTharushi, opLakshmi, reason: "Slot capacity exceeded"),
                    MakeReservation(nicSam,      mg5.Id!, slots[6].Id!, 25.0, 6, 10, "Approved",  now, nicSam,   opKavindu),
                    MakeReservation(nicAmaya,    mg6.Id!, slots[7].Id!, 5.0,  3,  7, "Completed", now, nicAmaya, opLakshmi, defaultUsers[10].Id),
                    MakeReservation(nicRuwan,    mg1.Id!, slots[11].Id!, 15.0, 8, 12, "Pending",   now, nicRuwan),
                    MakeReservation(nicTharushi, mg2.Id!, slots[3].Id!, 12.0, 3,  7, "Cancelled", now, nicTharushi)
                };
                await context.Reservations.InsertManyAsync(reservations);
            }
        }

        // ── Helper: MicrogridNode ──
        private static MicrogridNode MakeGrid(
            string name, string location, string desc,
            double lat, double lng,
            double cap, double avail, double reserved, double used,
            double batCap, double batLevel, double batPct,
            string status, bool isActive, string operatorId, DateTime now)
        {
            return new MicrogridNode
            {
                Name = name, Location = location, Description = desc,
                Latitude = lat, Longitude = lng,
                Capacity = cap, AvailableCapacity = avail,
                ReservedCapacity = reserved, UsedCapacity = used,
                BatteryCapacity = batCap, CurrentBatteryLevel = batLevel,
                BatteryPercentage = batPct,
                Status = status, IsActive = isActive,
                OperatorId = operatorId,
                CreatedAt = now, UpdatedAt = now
            };
        }

        // ── Helper: EnergySlot ──
        private static EnergySlot MakeSlot(
            string nodeId, double total, double available,
            int startHoursFromNow, int endHoursFromNow,
            decimal price, string status, string createdBy, DateTime now)
        {
            return new EnergySlot
            {
                MicrogridNodeId = nodeId,
                EnergyAmount = total,
                AvailableAmount = available,
                StartTime = now.AddHours(startHoursFromNow),
                EndTime = now.AddHours(endHoursFromNow),
                PricePerUnit = price,
                Status = status,
                CreatedBy = createdBy,
                CreatedAt = now, UpdatedAt = now
            };
        }

        // ── Helper: Reservation ──
        private static Reservation MakeReservation(
            string prosumerNic, string nodeId, string slotId,
            double amount, int startHoursFromNow, int endHoursFromNow,
            string status, DateTime now,
            string createdBy,
            string? approvedBy = null, string? completedBy = null,
            string? reason = null)
        {
            var history = new List<ReservationStatusEvent>
            {
                new() { From = null, To = "Pending", ChangedBy = createdBy, ChangedAt = now }
            };

            if (status is "Approved" or "Completed" or "Rejected")
            {
                var actor = approvedBy ?? createdBy;
                var target = status == "Rejected" ? "Rejected" : "Approved";
                history.Add(new ReservationStatusEvent
                {
                    From = "Pending", To = target,
                    ChangedBy = actor, ChangedAt = now.AddMinutes(30),
                    Reason = status == "Rejected" ? reason : null
                });
            }

            if (status == "Completed" && completedBy != null)
            {
                history.Add(new ReservationStatusEvent
                {
                    From = "Approved", To = "Completed",
                    ChangedBy = completedBy, ChangedAt = now.AddHours(1)
                });
            }

            if (status == "Cancelled")
            {
                history.Add(new ReservationStatusEvent
                {
                    From = "Pending", To = "Cancelled",
                    ChangedBy = createdBy, ChangedAt = now.AddMinutes(15)
                });
            }

            return new Reservation
            {
                ProsumerId = prosumerNic,
                MicrogridNodeId = nodeId,
                EnergySlotId = slotId,
                EnergyAmount = amount,
                ReservationDate = now.Date,
                StartTime = now.AddHours(startHoursFromNow),
                EndTime = now.AddHours(endHoursFromNow),
                Status = status,
                CreatedAt = now, UpdatedAt = now,
                StatusHistory = history
            };
        }
    }
}
