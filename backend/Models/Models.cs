using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LegalMinds.Backend.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(255)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Role { get; set; } = string.Empty; // "student" or "lawyer"
    }

    public class Case
    {
        [Key]
        [MaxLength(50)]
        public string Id { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(255)]
        public string Name { get; set; } = string.Empty; // citizen name

        [MaxLength(50)]
        public string Mobile { get; set; } = string.Empty;

        [MaxLength(50)]
        public string Category { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        [MaxLength(50)]
        public string Language { get; set; } = string.Empty;

        [MaxLength(255)]
        public string Location { get; set; } = string.Empty;

        [MaxLength(50)]
        public string Status { get; set; } = "In Review"; // In Review, Accepted, Solved, Approved, Revision Needed

        [MaxLength(255)]
        public string? AcceptedBy { get; set; } // student name/email

        public string Proofs { get; set; } = "[]"; // Comma-separated paths or JSON list of proof files

        public string? Voice { get; set; } // Voice file path

        public string? Video { get; set; } // Video file path

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Solution Info
        [MaxLength(50)]
        public string SolutionStatus { get; set; } = "pending"; // pending, submitted

        public string SolutionText { get; set; } = string.Empty;

        public string SolutionDocsNeeded { get; set; } = string.Empty;

        public string SolutionFiles { get; set; } = "[]"; // Comma-separated or JSON list of solution files

        public string? SolutionVoice { get; set; }

        public string? SolutionVideo { get; set; }

        [MaxLength(255)]
        public string? SolutionStudentName { get; set; }

        public DateTime? SolutionSubmittedAt { get; set; }

        public string ReviewFeedback { get; set; } = string.Empty;
    }

    // DTOs
    public class UserCreate
    {
        [Required]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;

        [Required]
        public string Role { get; set; } = "student";
    }

    public class UserLogin
    {
        [Required]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }

    public class AcceptCaseRequest
    {
        [Required]
        public string Id { get; set; } = string.Empty;

        [Required]
        public string StudentName { get; set; } = string.Empty;
    }

    public class ReviewCaseRequest
    {
        [Required]
        public string Decision { get; set; } = string.Empty; // "approve" or "reject"

        [Required]
        public string Feedback { get; set; } = string.Empty;
    }
}
