using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LegalMinds.Backend.Database;
using LegalMinds.Backend.Models;

namespace LegalMinds.Backend.Controllers
{
    [ApiController]
    [Route("api")]
    public class CasesController : ControllerBase
    {
        private readonly LegalMindsDbContext _context;
        private readonly string _uploadDir;

        public CasesController(LegalMindsDbContext context)
        {
            _context = context;
            // Uploads folder in the parent (Legal-Minds) directory
            _uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "..", "uploads");
            if (!Directory.Exists(_uploadDir))
            {
                Directory.CreateDirectory(_uploadDir);
            }
        }

        // Helper: Format DB Case to Frontend CaseResponse
        private CaseResponse MapCaseToResponse(Case c)
        {
            List<string> proofsList = new List<string>();
            try
            {
                if (!string.IsNullOrEmpty(c.Proofs))
                    proofsList = JsonSerializer.Deserialize<List<string>>(c.Proofs) ?? new List<string>();
            }
            catch { }

            List<string> solutionFilesList = new List<string>();
            try
            {
                if (!string.IsNullOrEmpty(c.SolutionFiles))
                    solutionFilesList = JsonSerializer.Deserialize<List<string>>(c.SolutionFiles) ?? new List<string>();
            }
            catch { }

            return new CaseResponse
            {
                id = c.Id,
                title = c.Title,
                name = c.Name,
                mobile = c.Mobile,
                category = c.Category,
                description = c.Description,
                language = c.Language,
                location = c.Location,
                status = c.Status,
                acceptedBy = c.AcceptedBy,
                proofs = proofsList,
                voice = c.Voice,
                video = c.Video,
                createdAt = c.CreatedAt.ToString("o"),
                solution = new SolutionResponse
                {
                    status = c.SolutionStatus,
                    text = c.SolutionText,
                    docsNeeded = c.SolutionDocsNeeded,
                    files = solutionFilesList,
                    voice = c.SolutionVoice,
                    video = c.SolutionVideo,
                    studentName = c.SolutionStudentName,
                    submittedAt = c.SolutionSubmittedAt?.ToString("o"),
                    feedback = c.ReviewFeedback
                }
            };
        }

        [HttpPost("submit-case")]
        public async Task<IActionResult> SubmitCase(
            [FromForm] string? category,
            [FromForm] string description,
            [FromForm] string? language,
            [FromForm] string? location,
            [FromForm] string? name,
            [FromForm] string? mobile,
            [FromForm] List<IFormFile> proofs,
            [FromForm] IFormFile? voice,
            [FromForm] IFormFile? video)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(description))
                {
                    return BadRequest(new { success = false, message = "Description is required" });
                }

                var safeCategory = (category ?? "other").Trim().ToLower();

                // Mask Mobile
                var digitsOnly = Regex.Replace(mobile ?? "", @"\D", "");
                string maskedMobile = "";
                if (digitsOnly.Length >= 4)
                {
                    maskedMobile = digitsOnly.Substring(0, 2) + "xxxx" + digitsOnly.Substring(digitsOnly.Length - 2);
                }
                else
                {
                    maskedMobile = digitsOnly;
                }

                // Title from description
                string cleanDesc = description.Trim();
                string title = cleanDesc.Length > 80 ? cleanDesc.Substring(0, 77).TrimEnd() + "..." : cleanDesc;

                // Save proofs
                List<string> proofPaths = new List<string>();
                foreach (var file in proofs)
                {
                    var filename = await SaveFileAsync(file);
                    proofPaths.Add("/uploads/" + filename);
                }

                // Save voice & video
                string? voicePath = null;
                if (voice != null)
                {
                    var voiceFilename = await SaveFileAsync(voice);
                    voicePath = "/uploads/" + voiceFilename;
                }

                string? videoPath = null;
                if (video != null)
                {
                    var videoFilename = await SaveFileAsync(video);
                    videoPath = "/uploads/" + videoFilename;
                }

                var random = new Random();
                var newCase = new Case
                {
                    Id = "CASE-LM-" + random.Next(100000, 999999),
                    Title = title,
                    Name = name ?? "",
                    Mobile = maskedMobile,
                    Category = safeCategory,
                    Description = description,
                    Language = language ?? "en",
                    Location = location ?? "",
                    Status = "In Review",
                    AcceptedBy = null,
                    Proofs = JsonSerializer.Serialize(proofPaths),
                    Voice = voicePath,
                    Video = videoPath,
                    CreatedAt = DateTime.UtcNow,
                    SolutionStatus = "pending",
                    SolutionText = ""
                };

                _context.Cases.Add(newCase);
                await _context.SaveChangesAsync();

                return Created("", new { success = true, caseData = MapCaseToResponse(newCase) });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Server error: " + ex.Message });
            }
        }

        [HttpGet("get-cases")]
        public async Task<IActionResult> GetCases()
        {
            var cases = await _context.Cases.OrderByDescending(c => c.CreatedAt).ToListAsync();
            var responses = cases.Select(MapCaseToResponse).ToList();
            return Ok(responses);
        }

        [HttpGet("get-case")]
        public async Task<IActionResult> GetCase([FromQuery] string id)
        {
            if (string.IsNullOrEmpty(id))
                return BadRequest(new { success = false, message = "id is required" });

            var c = await _context.Cases.FirstOrDefaultAsync(x => x.Id == id);
            if (c == null)
                return NotFound(new { success = false, message = "Case not found" });

            return Ok(MapCaseToResponse(c));
        }

        [HttpPost("accept-case")]
        public async Task<IActionResult> AcceptCase([FromBody] AcceptCaseRequest model)
        {
            var caseId = Request.Query["id"].ToString();
            if (string.IsNullOrEmpty(caseId))
            {
                // Fallback to body read if needed
                // But wait, the express server takes { id, studentName } from body!
                // Let's support reading id from body or query.
            }

            // Let's read from body
            var bodyString = "";
            using (var reader = new StreamReader(Request.Body))
            {
                bodyString = await reader.ReadToEndAsync();
            }

            string? id = null;
            string? studentName = null;
            try
            {
                using var doc = JsonDocument.Parse(bodyString);
                if (doc.RootElement.TryGetProperty("id", out var idProp)) id = idProp.GetString();
                if (doc.RootElement.TryGetProperty("studentName", out var nameProp)) studentName = nameProp.GetString();
            }
            catch { }

            // If not found in body, check query
            if (string.IsNullOrEmpty(id)) id = Request.Query["id"].ToString();
            if (string.IsNullOrEmpty(studentName)) studentName = model?.StudentName;

            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(studentName))
            {
                return BadRequest(new { success = false, message = "id and studentName are required" });
            }

            var c = await _context.Cases.FirstOrDefaultAsync(x => x.Id == id);
            if (c == null)
                return NotFound(new { success = false, message = "Case not found" });

            c.Status = "Accepted";
            c.AcceptedBy = studentName;
            await _context.SaveChangesAsync();

            return Ok(new { success = true, caseData = MapCaseToResponse(c) });
        }

        [HttpPost("submit-solution")]
        public async Task<IActionResult> SubmitSolution(
            [FromForm] string id,
            [FromForm] string studentName,
            [FromForm] string solutionText,
            [FromForm] string? docsNeeded,
            [FromForm] List<IFormFile> solutionFiles,
            [FromForm] IFormFile? solutionVoice,
            [FromForm] IFormFile? solutionVideo)
        {
            try
            {
                if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(studentName) || string.IsNullOrEmpty(solutionText))
                {
                    return BadRequest(new { success = false, message = "id, studentName, and solutionText are required" });
                }

                var c = await _context.Cases.FirstOrDefaultAsync(x => x.Id == id);
                if (c == null)
                    return NotFound(new { success = false, message = "Case not found" });

                // Save solution files
                List<string> sFilesList = new List<string>();
                foreach (var file in solutionFiles)
                {
                    var filename = await SaveFileAsync(file);
                    sFilesList.Add("/uploads/" + filename);
                }

                // Save media
                string? sVoicePath = null;
                if (solutionVoice != null)
                {
                    var voiceFilename = await SaveFileAsync(solutionVoice);
                    sVoicePath = "/uploads/" + voiceFilename;
                }

                string? sVideoPath = null;
                if (solutionVideo != null)
                {
                    var videoFilename = await SaveFileAsync(solutionVideo);
                    sVideoPath = "/uploads/" + videoFilename;
                }

                c.SolutionStatus = "submitted";
                c.SolutionText = solutionText;
                c.SolutionDocsNeeded = docsNeeded ?? "";
                c.SolutionFiles = JsonSerializer.Serialize(sFilesList);
                c.SolutionVoice = sVoicePath;
                c.SolutionVideo = sVideoPath;
                c.SolutionStudentName = studentName;
                c.SolutionSubmittedAt = DateTime.UtcNow;
                c.Status = "Solved";

                await _context.SaveChangesAsync();

                return Ok(new { success = true, caseData = MapCaseToResponse(c) });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Server error: " + ex.Message });
            }
        }

        [HttpPost("review-case")]
        public async Task<IActionResult> ReviewCase([FromQuery] string id, [FromBody] ReviewCaseRequest model)
        {
            // Support reading id from body or query
            if (string.IsNullOrEmpty(id))
            {
                // check query path
            }

            var c = await _context.Cases.FirstOrDefaultAsync(x => x.Id == id);
            if (c == null)
                return NotFound(new { success = false, message = "Case not found" });

            if (model.Decision.ToLower() == "approve")
            {
                c.Status = "Approved";
            }
            else
            {
                c.Status = "Revision Needed";
            }
            c.ReviewFeedback = model.Feedback;

            await _context.SaveChangesAsync();

            return Ok(new { success = true, caseData = MapCaseToResponse(c) });
        }

        // Helper: save file locally
        private async Task<string> SaveFileAsync(IFormFile file)
        {
            var unique = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + "-" + new Random().Next(1000, 9999);
            var safeName = Regex.Replace(file.FileName, @"\s+", "_");
            var filename = unique + "-" + safeName;
            var path = Path.Combine(_uploadDir, filename);

            using (var stream = new FileStream(path, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }
            return filename;
        }
    }

    // Response DTOs
    public class CaseResponse
    {
        public string id { get; set; } = string.Empty;
        public string title { get; set; } = string.Empty;
        public string name { get; set; } = string.Empty;
        public string mobile { get; set; } = string.Empty;
        public string category { get; set; } = string.Empty;
        public string description { get; set; } = string.Empty;
        public string language { get; set; } = string.Empty;
        public string location { get; set; } = string.Empty;
        public string status { get; set; } = string.Empty;
        public string? acceptedBy { get; set; }
        public List<string> proofs { get; set; } = new List<string>();
        public string? voice { get; set; }
        public string? video { get; set; }
        public string createdAt { get; set; } = string.Empty;
        public SolutionResponse solution { get; set; } = new SolutionResponse();
    }

    public class SolutionResponse
    {
        public string status { get; set; } = "pending";
        public string text { get; set; } = string.Empty;
        public string docsNeeded { get; set; } = string.Empty;
        public List<string> files { get; set; } = new List<string>();
        public string? voice { get; set; }
        public string? video { get; set; }
        public string? studentName { get; set; }
        public string? submittedAt { get; set; }
        public string? feedback { get; set; }
    }
}
