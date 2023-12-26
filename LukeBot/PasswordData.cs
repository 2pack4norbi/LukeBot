using System;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("LukeBot.Tests")]

namespace LukeBot
{
    internal class PasswordData
    {
        // with P = plaintextPassword, S - salt, H() - SHA-512 hasher:
        //   hash = H( H(P) | S )
        // that way remote client can generate and send us only H(P)
        // which we'll internally combine with S and get something
        // to compare with saved hash
        private byte[] hash = null;
        private byte[] salt = null;
        private const int SALT_SIZE = 64;

        public byte[] Hash
        {
            get
            {
                return hash;
            }
        }

        public static PasswordData Create(byte[] passwordHash)
        {
            PasswordData data = new();

            // Generate random crypto-strong salt
            RandomNumberGenerator rng = RandomNumberGenerator.Create();
            data.salt = new byte[SALT_SIZE];
            rng.GetBytes(data.salt);

            // combine password hash and salt
            byte[] passwordAndSalt = new byte[passwordHash.Length + SALT_SIZE];
            Buffer.BlockCopy(passwordHash, 0, passwordAndSalt, 0, passwordHash.Length);
            Buffer.BlockCopy(data.salt, 0, passwordAndSalt, passwordHash.Length, SALT_SIZE);

            Array.Clear(passwordHash);

            // generate final hash
            SHA512 hasher = SHA512.Create();
            data.hash = hasher.ComputeHash(passwordAndSalt);

            Array.Clear(passwordAndSalt);

            return data;
        }

        public static PasswordData Create(string plainPassword)
        {
            // Hash plaintext password into SHA-512 (aka. get H(P) )
            SHA512 hasher = SHA512.Create();
            byte[] plaintextBuffer = Encoding.UTF8.GetBytes(plainPassword);
            byte[] passwordHash = hasher.ComputeHash(plaintextBuffer);

            // clear the plaintext password buffer
            Array.Clear(plaintextBuffer);

            // forward the process to other Create()
            return Create(passwordHash);
        }

        public bool Equals(PasswordData other)
        {
            return hash.SequenceEqual(other.hash);
        }

        public bool Equals(byte[] passwordHash)
        {
            byte[] passwordAndSalt = new byte[passwordHash.Length + SALT_SIZE];
            Buffer.BlockCopy(passwordHash, 0, passwordAndSalt, 0, passwordHash.Length);
            Buffer.BlockCopy(salt, 0, passwordAndSalt, passwordHash.Length, SALT_SIZE);

            SHA512 hasher = SHA512.Create();
            byte[] finalHash = hasher.ComputeHash(passwordAndSalt);

            return hash.SequenceEqual(finalHash);
        }

        public bool Equals(string plainPassword)
        {
            SHA512 hasher = SHA512.Create();
            byte[] plaintextBuffer = Encoding.UTF8.GetBytes(plainPassword);
            byte[] passwordHash = hasher.ComputeHash(plaintextBuffer);

            return Equals(passwordHash);
        }
    }
}