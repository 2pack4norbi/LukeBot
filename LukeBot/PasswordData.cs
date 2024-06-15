using System;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;

[assembly: InternalsVisibleTo("LukeBot.Tests")]

namespace LukeBot
{
    internal class PasswordData
    {
        // with P - plaintextPassword, S - salt, H() - SHA-512 hasher:
        //   hash = H( H(P) | S )
        // that way remote client can generate and send us only H(P)
        // which we'll internally combine with S and get something
        // to compare with saved hash
        [JsonProperty]
        private byte[] hash = null; // result of H( H(P) | S)
        [JsonProperty]
        private byte[] salt = null; // also known as S

        internal const int SALT_SIZE = 32;

        [JsonIgnore]
        public byte[] Hash
        {
            get
            {
                return hash;
            }
        }

        private static byte[] ComputePasswordHash(string plainPassword)
        {
            // Hash plaintext password into SHA-512 (aka. get H(P) )
            SHA512 hasher = SHA512.Create();
            byte[] plaintextBuffer = Encoding.UTF8.GetBytes(plainPassword);
            byte[] passwordHash = hasher.ComputeHash(plaintextBuffer);

            // clear the plaintext password buffer
            Array.Clear(plaintextBuffer);

            return passwordHash;
        }

        private byte[] ComputeFinalHash(byte[] passwordHash)
        {
            // combine password hash and salt
            byte[] passwordAndSalt = new byte[passwordHash.Length + SALT_SIZE];
            Buffer.BlockCopy(passwordHash, 0, passwordAndSalt, 0, passwordHash.Length);
            Buffer.BlockCopy(salt, 0, passwordAndSalt, passwordHash.Length, SALT_SIZE);

            // generate final hash
            SHA512 hasher = SHA512.Create();
            byte[] finalHash = hasher.ComputeHash(passwordAndSalt);

            Array.Clear(passwordAndSalt);

            return finalHash;
        }

        public PasswordData()
        {
            // Generate random crypto-strong salt
            RandomNumberGenerator rng = RandomNumberGenerator.Create();
            this.salt = new byte[SALT_SIZE];
            rng.GetBytes(this.salt);
        }

        public PasswordData(byte[] salt)
        {
            this.hash = null;
            this.salt = salt;
        }

        public PasswordData(byte[] hash, byte[] salt)
        {
            this.hash = hash;
            this.salt = salt;
        }

        public static PasswordData Create(byte[] passwordHash)
        {
            PasswordData data = new();
            data.Load(passwordHash);
            return data;
        }

        public static PasswordData Create(string plainPassword)
        {
            // forward the process to other Create()
            return Create(ComputePasswordHash(plainPassword));
        }

        public void Load(byte[] passwordHash)
        {
            hash = ComputeFinalHash(passwordHash);
        }

        public void Load(string plainPassword)
        {
            // forward the process to other Load()
            Load(ComputePasswordHash(plainPassword));
        }

        public bool Equals(PasswordData other)
        {
            if (hash == null || other.hash == null)
                return false;

            return hash.SequenceEqual(other.hash);
        }

        public bool Equals(byte[] passwordHash)
        {
            return hash.SequenceEqual(ComputeFinalHash(passwordHash));
        }

        public bool Equals(string plainPassword)
        {
            return Equals(ComputePasswordHash(plainPassword));
        }
    }
}