using MongoDB.Driver;
using SmartMicrogrid.API.Helpers;
using SmartMicrogrid.API.Models.Common;

namespace SmartMicrogrid.API.Data
{
    public static class DbSeeder
    {
        public static async Task SeedDefaultUsersAsync(MongoDbContext context)
        {
            var userCount = await context.Users.CountDocumentsAsync(Builders<User>.Filter.Empty);
            if (userCount > 0)
            {
                return; // Database already populated
            }

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
    }
}
