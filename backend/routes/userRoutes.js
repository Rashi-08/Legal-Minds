const express = require("express");
const router = express.Router();

const { users } = require("../models/User");

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

  users.push({ name, email });

  res.json({
    message: "User added successfully",
    users
  });
});

// get all users
router.get("/users", (req, res) => {
  res.json(users);
});

module.exports = router;
