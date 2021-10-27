using System;
using System.Security.Cryptography;

namespace SenseNet.ContentRepository.Security.Clients
{
    public class RandomStringOptions
    {
        public int Length { get; set; } = 16;
        public bool RequireDigit { get; set; } = true;
        public bool RequireLowercase { get; set; } = true;
        public bool RequireUppercase { get; set; } = true;
    }
    
    public static class RandomStringGenerator
    {
        private const string LowerCase = "abcdefghijklmnopqursuvwxyz";
        private const string UpperCase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        private const string Numbers = "123456789";

        /// <summary>
        /// Generates a random string based on the default parameters.
        /// </summary>
        public static string New()
        {
            return New(new RandomStringOptions());
        }
        /// <summary>
        /// Generates a random string of the provided length.
        /// </summary>
        public static string New(int length)
        {
            return New(new RandomStringOptions{ Length = length });
        }
        /// <summary>
        /// Generates a random string based on the provided parameters.
        /// </summary>
        public static string New(RandomStringOptions options)
        {
            if (options.Length < 1)
                throw new InvalidOperationException($"Length {options.Length} is invalid.");

            var randomString = new char[options.Length];
            var charSet = string.Empty;

            // assemble the character set to choose from
            if (options.RequireLowercase) charSet += LowerCase;
            if (options.RequireUppercase) charSet += UpperCase;
            if (options.RequireDigit) charSet += Numbers;

            using (var randomGenerator = RandomNumberGenerator.Create())
            {
                for (var i = 0; i < options.Length; i++)
                {
                    var randomBytes = new byte[sizeof(int)];
                    randomGenerator.GetBytes(randomBytes);
                    var randomNumber = Math.Abs(BitConverter.ToInt32(randomBytes, 0));

                    // restrict the number to the size of the array
                    var randomIndex = randomNumber % charSet.Length;
                    randomString[i] = charSet[randomIndex];
                }
            }

            return string.Join(null, randomString);
        }
    }
}
