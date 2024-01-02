namespace Vapor
{
    public static class StringExtensions
    {
        /// <summary>
        /// A simple, but inefficient method to always generate a stable hashcode from a string. The normal <see cref="string.GetHashCode()"/> does not guarantee a stable value. 
        /// </summary>
        /// <param name="text">The string to hash</param>
        /// <returns>A unique hash code</returns>
        public static int GetKeyHashCode(this string text)
        {
            unchecked
            {
                int hash = 23;
                foreach (char c in text)
                {
                    hash = hash * 31 + c;
                }
                return hash;
            }
        }
    }
}
