using SQLite;
using KusinaPOS.Models;
using System.Security.Cryptography;

namespace KusinaPOS.Services
{
    public class UserService
    {
        private readonly SQLiteAsyncConnection _db;

        public UserService(IDatabaseService databaseService)
        {
            _db = databaseService.GetConnection();
        }

        /// <summary>
        /// Call once on app startup.
        /// Creates Users table and inserts default Admin if DB is empty.
        /// </summary>
        public async Task InitializeAsync()
        {
            await _db.CreateTableAsync<User>();
            bool hasUsers = await _db.Table<User>().CountAsync() > 0; // ✅

            if (!hasUsers)
            {
                await InsertDefaultAdminAsync();
            }
        }

        private async Task InsertDefaultAdminAsync()
        {
            const string defaultPin = "123456"; // owner must change later

            string salt = GenerateSalt();
            string pinHash = HashPin(defaultPin, salt);

            var admin = new User
            {
                Name = "Administrator",
                Role = "Administrator",
                PinHash = pinHash,
                Salt = salt,
                IsActive = true
            };

            await _db.InsertAsync(admin);
        }

        // BASIC data access methods (used by ViewModel)

        public Task<List<User>> GetActiveUsersAsync()
        {
            return _db.Table<User>()
                      .Where(u => u.IsActive)
                      .ToListAsync();
        }

        public Task<int> InsertUserAsync(User user)
        {
            return _db.InsertAsync(user);
        }

        public Task<int> UpdateUserAsync(User user)
        {
            return _db.UpdateAsync(user);
        }

        #region Security helpers (still OK in service)

        private static string GenerateSalt()
        {
            var bytes = new byte[16];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes);
        }

        private static string HashPin(string pin, string salt)
        {
            using var pbkdf2 = new Rfc2898DeriveBytes(
                pin,
                Convert.FromBase64String(salt),
                10000,
                HashAlgorithmName.SHA256);

            return Convert.ToBase64String(pbkdf2.GetBytes(32));
        }
        public async Task<User?> LoginWithPinAsync(string pin, string role)
        {
            var users = await _db.Table<User>()
                                 .Where(u => u.Role == role && u.IsActive)
                                 .ToListAsync();

            foreach (var user in users)
            {
                var hash = HashPin(pin, user.Salt);
                if (hash == user.PinHash)
                    return user;
            }

            return null;
        }

        #endregion
    }
}
