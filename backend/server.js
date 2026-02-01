const express = require("express");
const cors = require("cors");
const fs = require("fs");
const path = require("path");
const multer = require("multer");

const app = express();
const PORT = 5000;

// Correct paths
const USERS_FILE = path.join(__dirname, "data/users.json");
const CASES_FILE = path.join(__dirname, "data/cases.json");
const UPLOAD_DIR = path.join(__dirname, "uploads");

// Ensure needed files/folders exist
if (!fs.existsSync(USERS_FILE)) fs.writeFileSync(USERS_FILE, "[]", "utf8");
if (!fs.existsSync(CASES_FILE)) fs.writeFileSync(CASES_FILE, "[]", "utf8");
if (!fs.existsSync(UPLOAD_DIR)) fs.mkdirSync(UPLOAD_DIR);

// Multer setup
const storage = multer.diskStorage({
  destination: (req, file, cb) => cb(null, UPLOAD_DIR),
  filename: (req, file, cb) => {
    const unique = `${Date.now()}-${Math.round(Math.random() * 1e9)}`;
    cb(null, unique + "-" + file.originalname.replace(/\s+/g, "_"));
  },
});
const upload = multer({ storage });

// JSON helpers
function readCases() {
  try {
    return JSON.parse(fs.readFileSync(CASES_FILE, "utf8"));
  } catch {
    return [];
  }
}
function writeCases(data) {
  fs.writeFileSync(CASES_FILE, JSON.stringify(data, null, 2));
}

app.use(cors());
app.use(express.json());
app.use(express.urlencoded({ extended: true }));
app.use("/uploads", express.static(UPLOAD_DIR));

/* ============================================================
   LOGIN — MATCHES YOUR FRONTEND (name + email ONLY)
   ============================================================ */
app.post("/api/login", (req, res) => {
  console.log("🔥 LOGIN BODY RECEIVED:", req.body);

  const { name, email } = req.body;

  if (!name || !email) {
    return res.status(400).json({ message: "Name & email required" });
  }

  const users = JSON.parse(fs.readFileSync(USERS_FILE, "utf8"));

  const user = users.find(
    (u) =>
      u.name.toLowerCase() === name.toLowerCase() &&
      u.email.toLowerCase() === email.toLowerCase()
  );

  if (!user) {
    return res.status(400).json({ message: "Invalid credentials" });
  }

  res.json({
    message: "Login success",
    role: user.role,
    name: user.name,
    email: user.email,
    mobile: user.mobile || "",
    stars: user.stars || 0,
  });
});

/* ============================================================
   SUBMIT CASE (USER)
   ============================================================ */
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

      if (!description)
        return res
          .status(400)
          .json({ success: false, message: "Description is required" });

      const cleanDesc = description.trim();
      const title =
        cleanDesc.length > 80 ? cleanDesc.slice(0, 77) + "..." : cleanDesc;

      const proofs = (req.files.proofs || []).map(
        (f) => "/uploads/" + f.filename
      );

      const newCase = {
        id: "CASE-LM-" + Math.floor(100000 + Math.random() * 900000),
        title,
        name: name || "",
        mobile: mobile || "",
        category,
        description,
        language,
        location,
        status: "In Review",
        acceptedBy: null,
        proofs,
        voice: req.files.voice
          ? "/uploads/" + req.files.voice[0].filename
          : null,
        video: req.files.video
          ? "/uploads/" + req.files.video[0].filename
          : null,
        solution: {
          status: "pending",
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

      res.json({ success: true, caseData: newCase });
    } catch (err) {
      console.error(err);
      res.status(500).json({ success: false, message: "Server error." });
    }
  }
);

/* ============================================================
   GET ALL CASES
   ============================================================ */
app.get("/api/get-cases", (req, res) => {
  res.json(readCases());
});

/* ============================================================
   GET CASE BY ID
   ============================================================ */
app.get("/api/get-case", (req, res) => {
  const id = req.query.id;
  const cases = readCases();
  const found = cases.find((c) => c.id === id);
  if (!found)
    return res
      .status(404)
      .json({ success: false, message: "Case not found" });
  res.json(found);
});

/* ============================================================
   STUDENT ACCEPT CASE
   ============================================================ */
app.post("/api/accept-case", (req, res) => {
  const { id, studentName } = req.body;

  const cases = readCases();
  const idx = cases.findIndex((c) => c.id === id);

  if (idx === -1)
    return res
      .status(404)
      .json({ success: false, message: "Case not found" });

  cases[idx].status = "Accepted";
  cases[idx].acceptedBy = studentName;

  writeCases(cases);

  res.json({ success: true, caseData: cases[idx] });
});

/* ============================================================
   SUBMIT SOLUTION
   ============================================================ */
app.post(
  "/api/submit-solution",
  upload.fields([
    { name: "solutionFiles", maxCount: 10 },
    { name: "solutionVoice", maxCount: 1 },
    { name: "solutionVideo", maxCount: 1 },
  ]),
  (req, res) => {
    const { id, studentName, solutionText, docsNeeded } = req.body;

    const cases = readCases();
    const idx = cases.findIndex((c) => c.id === id);

    if (idx === -1)
      return res
        .status(404)
        .json({ success: false, message: "Case not found" });

    cases[idx].status = "Solved";
    cases[idx].solution = {
      status: "submitted",
      text: solutionText,
      docsNeeded,
      files: (req.files.solutionFiles || []).map(
        (f) => "/uploads/" + f.filename
      ),
      voice: req.files.solutionVoice
        ? "/uploads/" + req.files.solutionVoice[0].filename
        : null,
      video: req.files.solutionVideo
        ? "/uploads/" + req.files.solutionVideo[0].filename
        : null,
      studentName,
      submittedAt: new Date().toISOString(),
    };

    writeCases(cases);
    res.json({ success: true, caseData: cases[idx] });
  }
);

/* ============================================================
   START SERVER
   ============================================================ */
app.listen(PORT, () => {
  console.log(`🔥 Unified backend running at http://localhost:${PORT}`);
});
/* ============================================================
   SIGNUP (User / Student / Recruiter)
   ============================================================ */
app.post("/api/signup", (req, res) => {
  try {
    const users = JSON.parse(fs.readFileSync(USERS_FILE, "utf8"));

    const { role, name, email, password, mobile, school, year, company, position } =
      req.body;

    // Validate required fields
    if (!role || !name || !email || !password) {
      return res.status(400).json({ success: false, message: "Required fields missing" });
    }

    // Duplicate check
    const exists = users.find(
      (u) => u.email.toLowerCase() === email.toLowerCase()
    );

    if (exists) {
      return res.status(400).json({ success: false, message: "User already exists" });
    }

    // Build user object
    const newUser = { role, name, email, password };

    if (role === "user") {
      newUser.mobile = mobile || "";
    }

    if (role === "student") {
      newUser.school = school;
      newUser.year = year;
      newUser.stars = 0;
    }

    if (role === "recruiter") {
      newUser.company = company;
      newUser.position = position;
    }

    // Save
    users.push(newUser);
    fs.writeFileSync(USERS_FILE, JSON.stringify(users, null, 2));

    res.json({ success: true, user: newUser });

  } catch (err) {
    console.error("SIGNUP ERROR:", err);
    res.status(500).json({ success: false, message: "Server error" });
  }
});
