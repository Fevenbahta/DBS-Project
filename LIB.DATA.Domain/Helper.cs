using System.Text;
using System;

namespace DTO
{
    public static class Helper
    {
        public static string generateRandomID(int length, string type)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            StringBuilder randomString = new StringBuilder();

            Random random = new Random();
            for (int i = 0; i < length; i++)
            {
                randomString.Append(chars[random.Next(chars.Length)]);
            }
            string cleanTimeStamp = DateTimeOffset.Now.ToUnixTimeSeconds().ToString();
            //string cleanTimeStamp = timeStamp.Replace(" ", "").Replace("-", "").Replace(":", "").Replace("T", "");
            string lastTimeDigits = cleanTimeStamp.Substring(2);
            string combinedString = type + lastTimeDigits + randomString.ToString();

            return combinedString.Replace(" ", "").Substring(0, Math.Min(length, combinedString.Length));

        }
    }
}
