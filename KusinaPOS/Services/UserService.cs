using KusinaPOS.Helpers;
using KusinaPOS.Models;
using SQLite;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Threading.Tasks;

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
        /// Creates Users table and inserts default Admin/Cashier if DB is empty.
        /// </summary>
        public async Task InitializeAsync()
        {
            await _db.CreateTableAsync<User>();
            var hasUsers = await _db.Table<User>().CountAsync() > 0;
            if (!hasUsers)
            {
                await InsertDefaultUsersAsync();
            }
        }

        /// <summary>
        /// Inserts default Administrator and Cashier users.
        /// </summary>
        public async Task InsertDefaultUsersAsync()
        {
            const string defaultPin = "123456";

            // Admin
            var adminSalt = GenerateSalt();
            var admin = new User
            {
                Name = "Administrator",
                Role = "Administrator",
                Salt = adminSalt,
                PinHash = HashPin(defaultPin, adminSalt)
            };
            await _db.InsertAsync(admin);

            // Cashier
            var cashierSalt = GenerateSalt();
            var cashier = new User
            {
                Name = "Cashier",
                Role = "Cashier",
                Salt = cashierSalt,
                PinHash = HashPin(defaultPin, cashierSalt)
            };
            await _db.InsertAsync(cashier);
        }

        /// <summary>
        /// Resets Users table and reinserts default users.
        /// </summary>
        public async Task ResetUserTableAsync()
        {
            try
            {
                await _db.CreateTableAsync<User>();
                await _db.DeleteAllAsync<User>();
                await InsertDefaultUsersAsync();
            }
            catch (SQLiteException ex)
            {
                Debug.WriteLine($"ResetUserTableAsync Error: {ex.Message}");
            }
        }

        #region Basic Data Access

        public Task<List<User>> GetUsersAsync() => _db.Table<User>().ToListAsync();

        public Task<User?> GetUserByRoleAsync(string role)
            => _db.Table<User>().Where(u => u.Role == role).FirstOrDefaultAsync();

        public Task<int> InsertUserAsync(User user) => _db.InsertAsync(user);

        /// <summary>
        /// Updates user name and optionally PIN.
        /// </summary>
        public async Task SaveUserAsync(User user, string newPin = null)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(newPin))
                {
                    string newSalt = GenerateSalt();
                    user.Salt = newSalt;
                    user.PinHash = HashPin(newPin, newSalt);
                }

                await _db.UpdateAsync(user);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"SaveUserAsync Error: {ex.Message}");
                await PageHelper.DisplayAlertAsync(
                    "Error",
                    $"Failed to save user. {ex.Message}",
                    "OK"
                );
            }
        }

        #endregion

        #region Security Helpers

        public static string GenerateSalt()
        {
            var bytes = new byte[16];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes);
        }

        public static string HashPin(string pin, string salt)
        {
            using var pbkdf2 = new Rfc2898DeriveBytes(
                pin,
                Convert.FromBase64String(salt),
                10000,
                HashAlgorithmName.SHA256);

            return Convert.ToBase64String(pbkdf2.GetBytes(32));
        }

        #endregion

        #region Authentication

        public async Task<User?> LoginWithPinAsync(string pin, string role)
        {
            pin = pin.Trim();

            var users = await _db.Table<User>()
                                 .Where(u => u.Role == role)
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
