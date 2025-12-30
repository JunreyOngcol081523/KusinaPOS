using SQLite;
using System;
using System.Collections.Generic;
using System.Text;

namespace KusinaPOS.Models
{
    [Table("Users")]
    public class User
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [NotNull]
        public string Name { get; set; }          // Display name (Cashier Juan)

        [NotNull, Unique]
        public string PinHash { get; set; }       // Hashed PIN

        [NotNull]
        public string Salt { get; set; }          // Unique per user

        [NotNull]
        public string Role { get; set; }          // Admin, Cashier, Kitchen

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
