Legal Minds

Legal Minds is a multi-role legal case management and learning platform designed to support Students, Lawyers, Recruiters, and general Users. The current codebase contains a complete front-end built with HTML, CSS, and JavaScript, along with JSON files used for representing sample data. This project is structured as a modular foundation intended for extension into a fully functional application once backend and database layers are added.

Overview

The platform contains separate portals for each role:

Lawyer Portal
Provides access to case details, client information, and a basic workflow structure.

Student Portal
Displays case listings, resources, and pages oriented toward legal learning and research.

Recruiter Portal
Shows student profiles and information relevant to screening and evaluation.

User Portal
Offers general information and serves as an entry point to explore legal content.

Each module is organized within its own directory to maintain separation of concerns and support scalable development.

Technology Stack

Frontend:

HTML5

CSS3

JavaScript (vanilla)

Data:

Static JSON files such as cases.json and students.json

There is currently no backend, authentication system, or database integration.

Repository Structure

index.html
signup.html
cases.json
students.json

lawyer/
student/
recruiter/
user/

Each directory contains the UI and assets associated with that specific portal.

Current Features

Modular multi-role interface

Static JSON-based data simulation

Simple and clean UI suitable for demonstrations

Front-end architecture prepared for later expansion

Recommended Future Enhancements

Backend Development
Introduce a backend using Firebase (Authentication + Firestore), Node.js with Express.js, or Python with FastAPI.

Persistent Database
Replace JSON files with a proper database such as Firebase Firestore, MongoDB, PostgreSQL, or MySQL.

Authentication and Role-Based Access
Secure login, user roles, and protected pages.

API Development
Create endpoints for managing cases, profiles, submissions, and recruiter actions.

Administrative Dashboard
Add tools for case approvals, user verification, and platform analytics.

Optional Legal-NLP Integration
Case summarization, clause extraction, and legal document analysis using modern NLP models.

Contribution Guidelines

Fork the repository.

Create a new branch for your feature.

Commit your changes with descriptive messages.

Push your branch and open a pull request.

Licensing

No license is currently included. An MIT or Apache 2.0 license may be added depending on requirements.

Contact

For queries or collaboration, please open an issue in the repository or reach out to the maintainers.
