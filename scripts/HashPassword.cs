using System;

class Program
{
    static void Main()
    {
        var password = "Admin@123";
        var hash = BCrypt.Net.BCrypt.HashPassword(password);

        Console.WriteLine($"Password: {password}");
        Console.WriteLine($"Hash: {hash}");

        Console.WriteLine();

        var password2 = "User@123";
        var hash2 = BCrypt.Net.BCrypt.HashPassword(password2);

        Console.WriteLine($"Password: {password2}");
        Console.WriteLine($"Hash: {hash2}");
    }
}
