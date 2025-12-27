const express = require("express");
const cors = require("cors");

const app = express();

// middleware
app.use(cors());
app.use(express.json());

// routes
const userRoutes = require("./routes/userRoutes");
app.use("/api", userRoutes);

// test root
app.get("/", (req, res) => {
  res.send("Backend is running 🚀");
});

// start server
const PORT = 5000;
app.listen(PORT, () => {
  console.log(`Server running on port ${PORT}`);
});
