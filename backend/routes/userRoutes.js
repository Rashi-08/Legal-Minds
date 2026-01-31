const fs = require("fs");
const path = require("path");

const USERS_FILE = path.join(__dirname, "../data/users.json");

function readUsers() {
  const data = fs.readFileSync(USERS_FILE, "utf-8");
  return JSON.parse(data);
}

function writeUsers(users) {
  fs.writeFileSync(USERS_FILE, JSON.stringify(users, null, 2));
}

const express = require("express");
const router = express.Router();

// test route
router.get("/test", (req, res) => {
  res.json({ message: "User routes working ✅" });
});

// signup route
router.post("/signup", (req, res) => {
  const { name, email } = req.body;

  if (!name || !email) {
    return res.status(400).json({ error: "Name and email required" });
  }

  const users = readUsers();   // read from JSON file

  users.push({ name, email }); // add new user

  writeUsers(users);           // save back to JSON file

  res.json({
    message: "User added successfully"
  });
});


// get all users
router.get("/users", (req, res) => {
  const users = readUsers();   // read from JSON file
  res.json(users);
});


module.exports = router;
