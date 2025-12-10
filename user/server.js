// server.js
const express = require("express");
const cors = require("cors");
const fs = require("fs");
const path = require("path");
const multer = require("multer");

const app = express();
const PORT = 3000;

// ===== PATHS =====
const DB_FILE = path.join(__dirname, "cases.json");
const UPLOAD_DIR = path.join(__dirname, "uploads");

// ===== ENSURE FILES/FOLDERS EXIST =====
try {
  if (!fs.existsSync(DB_FILE)) {
    fs.writeFileSync(DB_FILE, "[]", "utf8");
    console.log("✅ Created cases.json");
  }
} catch (e) {
  console.error("❌ Error creating cases.json:", e);
}

try {
  if (!fs.existsSync(UPLOAD_DIR)) {
    fs.mkdirSync(UPLOAD_DIR);
    console.log("✅ Created uploads/ folder");
  }
} catch (e) {
  console.error("❌ Error creating uploads folder:", e);
}

// ===== MULTER CONFIG (FILE STORAGE) =====
const storage = multer.diskStorage({
  destination: (req, file, cb) => cb(null, UPLOAD_DIR),
  filename: (req, file, cb) => {
    const unique = Date.now() + "-" + Math.round(Math.random() * 1e9);
    cb(null, unique + "-" + file.originalname.replace(/\s+/g, "_"));
  },
});
const upload = multer({ storage });

// ===== SAFE JSON HELPERS =====
function readCases() {
  try {
    const raw = fs.readFileSync(DB_FILE, "utf8");
    const data = JSON.parse(raw);
    return Array.isArray(data) ? data : [];
  } catch (e) {
    console.error("❌ Error reading cases.json:", e);
    return [];
  }
}

function writeCases(data) {
  try {
    fs.writeFileSync(DB_FILE, JSON.stringify(data, null, 2), "utf8");
  } catch (e) {
    console.error("❌ Error writing cases.json:", e);
  }
}

// ===== MIDDLEWARE =====
app.use(cors());
app.use(express.urlencoded({ extended: true }));
app.use(express.json());
app.use("/uploads", express.static(UPLOAD_DIR));

// ===== SERVE USER PORTAL =====
app.get("/", (req, res) => {
  const portalPath = path.join(__dirname, "portal.html");
  if (!fs.existsSync(portalPath)) {
    return res.status(500).send("portal.html not found in /user folder");
  }
  res.sendFile(portalPath);
});

// ===== SUBMIT CASE + FILE UPLOAD =====
// citizen side – creates new case with solution.status = "pending"
app.post(
  "/api/submit-case",
  upload.fields([
    { name: "proofs", maxCount: 10 },
    { name: "voice", maxCount: 1 },
    { name: "video", maxCount: 1 },
  ]),
  (req, res) => {
    try {
      const { category, description, language, location, name, mobile } =
        req.body;

      if (!description || description.trim() === "") {
        return res.status(400).json({
          success: false,
          message: "Description is required",
        });
      }

      const safeCategory =
        category && category.trim() ? category.trim().toLowerCase() : "other";

      // Mask mobile: first 2 digits + "xxxx" + last 2
      const digitsOnly = (mobile || "").replace(/\D/g, "");
      let maskedMobile = "";
      if (digitsOnly.length >= 4) {
        const first2 = digitsOnly.slice(0, 2);
        const last2 = digitsOnly.slice(-2);
        maskedMobile = first2 + "xxxx" + last2;
      } else {
        maskedMobile = digitsOnly || "";
      }

      // Title = short snippet from description
      const cleanDesc = description.trim();
      const title =
        cleanDesc.length > 80
          ? cleanDesc.slice(0, 77).trimEnd() + "..."
          : cleanDesc;

      // Files
      const proofFiles = (req.files && req.files.proofs) || [];
      const proofPaths = proofFiles.map((file) => "/uploads/" + file.filename);

      const voiceFile =
        req.files && req.files.voice && req.files.voice[0]
          ? req.files.voice[0]
          : null;
      const videoFile =
        req.files && req.files.video && req.files.video[0]
          ? req.files.video[0]
          : null;

      const newCase = {
        id: "CASE-LM-" + Math.floor(100000 + Math.random() * 900000),
        title,
        name: name || "",
        mobile: maskedMobile,
        category: safeCategory,
        description,
        language: language || "en",
        location: location || "",
        status: "In Review",     // In Review -> Accepted -> Solved
        acceptedBy: null,        // student name after accept
        proofs: proofPaths,      // ["/uploads/file..."]
        voice: voiceFile ? "/uploads/" + voiceFile.filename : null,
        video: videoFile ? "/uploads/" + videoFile.filename : null,
        solution: {
          status: "pending",     // pending / submitted
          text: "",
          docsNeeded: "",
          files: [],
          voice: null,
          video: null,
          studentName: null,
          submittedAt: null,
        },
        createdAt: new Date().toISOString(),
      };

      const cases = readCases();
      cases.push(newCase);
      writeCases(cases);

      console.log("✅ Saved new case:", newCase.id);

      res.status(201).json({
        success: true,
        caseData: newCase,
      });
    } catch (err) {
      console.error("❌ Error saving case:", err);
      res.status(500).json({
        success: false,
        message: "Server error while saving case",
      });
    }
  }
);

// ===== GET ALL CASES (for student dashboard) =====
app.get("/api/get-cases", (req, res) => {
  const cases = readCases();
  res.json(cases);
});

// ===== GET ONE CASE BY ID (for user portal to refresh status/acceptedBy/solution) =====
app.get("/api/get-case", (req, res) => {
  const id = req.query.id;
  if (!id) {
    return res.status(400).json({ success: false, message: "id is required" });
  }
  const cases = readCases();
  const found = cases.find((c) => c.id === id);
  if (!found) {
    return res.status(404).json({ success: false, message: "Case not found" });
  }
  res.json(found);
});

// ===== STUDENT ACCEPT CASE =====
app.post("/api/accept-case", (req, res) => {
  try {
    const { id, studentName } = req.body;

    if (!id || !studentName) {
      return res.status(400).json({
        success: false,
        message: "id and studentName are required",
      });
    }

    const cases = readCases();
    const idx = cases.findIndex((c) => c.id === id);
    if (idx === -1) {
      return res.status(404).json({
        success: false,
        message: "Case not found",
      });
    }

    cases[idx].status = "Accepted";
    cases[idx].acceptedBy = studentName;
    // keep solution.status = "pending"
    writeCases(cases);

    console.log(`✅ Case ${id} accepted by ${studentName}`);

    res.json({
      success: true,
      caseData: cases[idx],
    });
  } catch (err) {
    console.error("❌ Error accepting case:", err);
    res.status(500).json({
      success: false,
      message: "Server error while accepting case",
    });
  }
});

// ===== STUDENT SUBMIT SOLUTION =====
// student uploads solution text + docsNeeded + files/audio/video
app.post(
  "/api/submit-solution",
  upload.fields([
    { name: "solutionFiles", maxCount: 10 },
    { name: "solutionVoice", maxCount: 1 },
    { name: "solutionVideo", maxCount: 1 },
  ]),
  (req, res) => {
    try {
      const { id, studentName, solutionText, docsNeeded } = req.body;

      if (!id || !studentName || !solutionText) {
        return res.status(400).json({
          success: false,
          message: "id, studentName and solutionText are required",
        });
      }

      const cases = readCases();
      const idx = cases.findIndex((c) => c.id === id);
      if (idx === -1) {
        return res.status(404).json({
          success: false,
          message: "Case not found",
        });
      }

      const sFiles = (req.files && req.files.solutionFiles) || [];
      const sFilePaths = sFiles.map((f) => "/uploads/" + f.filename);

      const sVoice =
        req.files && req.files.solutionVoice && req.files.solutionVoice[0]
          ? "/uploads/" + req.files.solutionVoice[0].filename
          : null;

      const sVideo =
        req.files && req.files.solutionVideo && req.files.solutionVideo[0]
          ? "/uploads/" + req.files.solutionVideo[0].filename
          : null;

      cases[idx].solution = {
        status: "submitted",
        text: solutionText,
        docsNeeded: docsNeeded || "",
        files: sFilePaths,
        voice: sVoice,
        video: sVideo,
        studentName,
        submittedAt: new Date().toISOString(),
      };

      cases[idx].status = "Solved";

      writeCases(cases);

      console.log(`✅ Solution submitted for ${id} by ${studentName}`);

      res.json({
        success: true,
        caseData: cases[idx],
      });
    } catch (err) {
      console.error("❌ Error submitting solution:", err);
      res.status(500).json({
        success: false,
        message: "Server error while submitting solution",
      });
    }
  }
);

// ===== START SERVER =====
app.listen(PORT, () => {
  console.log(`✅ Server running at http://localhost:${PORT}`);
});
