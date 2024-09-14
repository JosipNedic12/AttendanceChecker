Student Attendance Tracking System using RFID/NFC
This project is a web-based application for tracking student attendance at university lectures using RFID/NFC technology. 
The system allows professors to monitor student attendance in real-time through the scanning of student ID cards (IKSICA). 
The application stores data on attendance, including student names, course details, and attendance percentages, using the Supabase platform.

Features
Real-time student attendance tracking via RFID/NFC card scanning.
Professors can view attendance data for each lecture, including the percentage of attendance.
Web-based interface built with ASP.NET Razor for frontend and C# for backend logic.
RFID scanner connected to a Dasduino ESP32 ConnectPlus board for wireless data transmission.
Database interaction via Supabase for secure and scalable data storage.
Exportable attendance data in Excel format for administrative purposes.

Technologies Used
Frontend: ASP.NET Razor
Backend: C# (ASP.NET Core)
Database: Supabase (PostgreSQL)
Hardware: RFID/NFC Reader, Dasduino ESP32 ConnectPlus
Other Libraries: ClosedXML for Excel export, HttpClient for API communication

Requirements
.NET 8 SDK
RFID/NFC Reader
Dasduino ESP32 ConnectPlus
Supabase account and API keys for data storage
Visual Studio or compatible IDE
