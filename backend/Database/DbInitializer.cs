using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using LegalMinds.Backend.Models;

namespace LegalMinds.Backend.Database
{
    public static class DbInitializer
    {
        public static async Task InitializeAsync(LegalMindsDbContext context)
        {
            // Create database tables if they do not exist
            await context.Database.EnsureCreatedAsync();

            var hasher = new PasswordHasher<string>();

            // 1. Seed Students from students.json
            var studentsJsonPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "students.json");
            if (File.Exists(studentsJsonPath))
            {
                try
                {
                    var jsonString = await File.ReadAllTextAsync(studentsJsonPath);
                    using var doc = JsonDocument.Parse(jsonString);
                    if (doc.RootElement.TryGetProperty("students", out var studentsProp) && studentsProp.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var student in studentsProp.EnumerateArray())
                        {
                            string? username = null;
                            string? password = null;

                            if (student.TryGetProperty("username", out var userProp)) username = userProp.GetString();
                            if (student.TryGetProperty("password", out var passProp)) password = passProp.GetString();

                            if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
                            {
                                var existing = await context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == username.ToLower());
                                if (existing == null)
                                {
                                    var newUser = new User
                                    {
                                        Email = username,
                                        Role = "student"
                                    };
                                    newUser.PasswordHash = hasher.HashPassword(newUser.Email, password);
                                    context.Users.Add(newUser);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error seeding students: " + ex.Message);
                }
            }

            // 2. Seed Lawyer Jonathan Pierce
            var lawyerEmail = "jonathan";
            var existingLawyer = await context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == lawyerEmail.ToLower());
            if (existingLawyer == null)
            {
                var newLawyer = new User
                {
                    Email = lawyerEmail,
                    Role = "lawyer"
                };
                newLawyer.PasswordHash = hasher.HashPassword(newLawyer.Email, "jonathan123");
                context.Users.Add(newLawyer);
            }

            // 3. Seed Lawyer Sarah Rodriguez (in case she acts as reviewer or lawyer)
            var reviewerEmail = "sarah_lawyer";
            var existingReviewer = await context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == reviewerEmail.ToLower());
            if (existingReviewer == null)
            {
                var newReviewer = new User
                {
                    Email = reviewerEmail,
                    Role = "lawyer"
                };
                newReviewer.PasswordHash = hasher.HashPassword(newReviewer.Email, "sarah123");
                context.Users.Add(newReviewer);
            }

            await context.SaveChangesAsync();
        }
    }
}
