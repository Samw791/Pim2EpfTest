using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PimTestProject
{
    public class CredentialsReader
    {
        public static bool ReadCredentials(out string username, out string password)
        {
            username = null;
            password = null;

            try
            {
                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string filePath = Path.Combine(desktopPath, "credentials.txt");

                if (!File.Exists(filePath))
                {
                    Console.WriteLine("Credentials file not found on the desktop.");
                    return false;
                }

                string[] lines = File.ReadAllLines(filePath);

                if (lines.Length < 2)
                {
                    Console.WriteLine("Credentials file does not contain both username and password.");
                    return false;
                }

                username = lines[0];
                password = lines[1];

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while reading credentials: {ex.Message}");
                return false;
            }
        }
    }
}
