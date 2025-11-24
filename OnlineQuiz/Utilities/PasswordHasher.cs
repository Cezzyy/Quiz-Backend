namespace OnlineQuiz.Utilities
{
    public static class PasswordHasher
    {
        /// <summary>
        /// Hashes a password using BCrypt
        /// </summary>
        /// <param name="password">Plain text password</param>
        /// <returns>Hashed password</returns>
        public static string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        /// <summary>
        /// Verifies a password against a hash
        /// </summary>
        /// <param name="password">Plain text password</param>
        /// <param name="hash">Password hash to verify against</param>
        /// <returns>True if password matches, false otherwise</returns>
        public static bool VerifyPassword(string password, string hash)
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
    }
}
