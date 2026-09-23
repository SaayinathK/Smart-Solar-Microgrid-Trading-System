using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Driver;
using SmartMicrogrid.API.Helpers;
using SmartMicrogrid.API.Models.Common;
using SmartMicrogrid.API.Models.M1;

namespace SmartMicrogrid.API.Data
{
    public static class DbSeeder
    {
        public static async Task SeedDefaultUsersAsync(MongoDbContext context)
        {
            var userCount = await context.Users.CountDocumentsAsync(Builders<User>.Filter.Empty);
            if (userCount == 0)
            {
                var defaultUsers = new List<User>
                {
                    new User
                    {
                        FirstName = "System",
                        LastName = "Administrator",
                        Email = "admin@microgrid.com",
                        PhoneNumber = "+10000000001",
                        PasswordHash = PasswordHelper.HashPassword("Admin123!"),
                        Role = Role.Admin,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    },
                    new User
                    {
                        FirstName = "Alex",
                        LastName = "Operator",
                        Email = "operator@microgrid.com",
                        PhoneNumber = "+10000000002",
                        PasswordHash = PasswordHelper.HashPassword("Operator123!"),
                        Role = Role.MicrogridOperator,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    },
                    new User
                    {
                        FirstName = "Sam",
                        LastName = "Prosumer",
                        Email = "prosumer@microgrid.com",
                        PhoneNumber = "+10000000003",
                        PasswordHash = PasswordHelper.HashPassword("Prosumer123!"),
                        Role = Role.Prosumer,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    },
                    new User
                    {
                        FirstName = "Valerie",
                        LastName = "Verifier",
                        Email = "verifier@microgrid.com",
                        PhoneNumber = "+10000000004",
                        PasswordHash = PasswordHelper.HashPassword("Verifier123!"),
                        Role = Role.TransactionVerifier,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    }
                };

                await context.Users.InsertManyAsync(defaultUsers);
            }

            // Seed Microgrids if empty
            var microgridCount = await context.Microgrids.CountDocumentsAsync(Builders<MicrogridNode>.Filter.Empty);
            if (microgridCount == 0)
            {
                var mg1 = new MicrogridNode
                {
                    Name = "Colombo Solar Hub",
                    Location = "Colombo",
                    Description = "High-efficiency 500kW commercial solar array located in Central Colombo.",
                    Latitude = 6.9271,
                    Longitude = 79.8612,
                    Capacity = 500.0,
                    AvailableCapacity = 350.0,
                    ReservedCapacity = 100.0,
                    UsedCapacity = 50.0,
                    BatteryCapacity = 200.0,
                    CurrentBatteryLevel = 160.0,
                    BatteryPercentage = 80.0,
                    Status = "Active",
                    IsActive = true,
                    OperatorId = "op-colombo-01",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                var mg2 = new MicrogridNode
                {
                    Name = "Kandy Highland Microgrid",
                    Location = "Kandy",
                    Description = "Highland solar and battery storage microgrid serving local tea estates.",
                    Latitude = 7.2906,
                    Longitude = 80.6337,
                    Capacity = 300.0,
                    AvailableCapacity = 200.0,
                    ReservedCapacity = 50.0,
                    UsedCapacity = 50.0,
                    BatteryCapacity = 150.0,
                    CurrentBatteryLevel = 105.0,
                    BatteryPercentage = 70.0,
                    Status = "Active",
                    IsActive = true,
                    OperatorId = "op-kandy-02",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                var mg3 = new MicrogridNode
                {
                    Name = "Galle Coastal Solar Park",
                    Location = "Galle",
                    Description = "Coastal solar farm with lithium storage backup.",
                    Latitude = 6.0535,
                    Longitude = 80.2210,
                    Capacity = 450.0,
                    AvailableCapacity = 0.0,
                    ReservedCapacity = 0.0,
                    UsedCapacity = 0.0,
                    BatteryCapacity = 180.0,
                    CurrentBatteryLevel = 45.0,
                    BatteryPercentage = 25.0,
                    Status = "Maintenance",
                    IsActive = false,
                    OperatorId = "op-galle-03",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await context.Microgrids.InsertManyAsync(new[] { mg1, mg2, mg3 });

                // Seed Energy Slots
                var slot1 = new EnergySlot
                {
                    MicrogridNodeId = mg1.Id!,
                    EnergyAmount = 100.0,
                    AvailableAmount = 100.0,
                    StartTime = DateTime.UtcNow.AddHours(1),
                    EndTime = DateTime.UtcNow.AddHours(5),
                    PricePerUnit = 25.00m,
                    Status = "Available",
                    CreatedBy = "op-colombo-01",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                var slot2 = new EnergySlot
                {
                    MicrogridNodeId = mg2.Id!,
                    EnergyAmount = 50.0,
                    AvailableAmount = 50.0,
                    StartTime = DateTime.UtcNow.AddHours(2),
                    EndTime = DateTime.UtcNow.AddHours(6),
                    PricePerUnit = 22.50m,
                    Status = "Available",
                    CreatedBy = "op-kandy-02",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await context.EnergySlots.InsertManyAsync(new[] { slot1, slot2 });
            }
        }
    }
}
