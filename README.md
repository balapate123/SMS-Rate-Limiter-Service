Rate Limiter Monitoring
This project is a Rate Limiter Monitoring application consisting of a backend API built with .NET and a frontend UI built with React. The backend API provides rate limiting information for various accounts and phone numbers, while the frontend displays this data with options for filtering and real-time monitoring.

Features
Backend (API): Built with .NET, provides endpoints for rate limiting, account details, and phone number statistics.
Frontend (UI): Built with React, shows rate limiter stats per account and phone number, including messages sent per second and total messages. Auto-refresh every 60 seconds.
Redis Caching: Redis is used to store account and phone number statistics for performance and rate limiting.
Prerequisites
.NET SDK (for building the backend)
Redis (for caching)
Node.js and npm (for running the frontend)
Git
Getting Started
Step 1: Clone the Repository
bash
Copy code
git clone <repository-url>
cd <repository-folder>
Step 2: Setting Up Redis
Install Redis

Windows: Use WSL or download Memurai (a Redis-compatible Windows service).
macOS: Install via Homebrew:
bash
Copy code
brew install redis
Linux: Use the package manager for your distribution, e.g.:
bash
Copy code
sudo apt update
sudo apt install redis-server
Start Redis
Start the Redis server with:

bash
Copy code
redis-server
Redis should now be running on localhost:6379.

Test the Redis Connection
Open another terminal and run:

bash
Copy code
redis-cli
Inside the Redis CLI, type PING. You should receive PONG if Redis is running correctly.

Backend Setup
Navigate to Backend Directory

bash
Copy code
cd <repository-folder>/backend
Configure Environment Variables
In appsettings.json, configure Redis connection and rate limiting settings:

json
Copy code
{
  "Redis": {
    "ConnectionString": "localhost:6379"
  },
  "RateLimiter": {
    "MaxMessagesPerNumber": 5,
    "MaxMessagesPerAccount": 10,
    "ExpirationTimeInSeconds": 60
  }
}
Build and Run the Backend

bash
Copy code
dotnet build
dotnet run
The API will be available at http://localhost:5157/api/RateLimiter.

Backend API Endpoints
GET /api/RateLimiter/accounts: Retrieves a list of all accounts.
GET /api/RateLimiter/{accountId}: Retrieves details for a specific account.
GET /api/RateLimiter/{accountId}/{phoneNumber}: Retrieves details for a specific phone number within an account.
Frontend Setup
Navigate to the Frontend Directory

bash
Copy code
cd <repository-folder>/frontend
Install Dependencies

bash
Copy code
npm install
Configure API URL
In src/config.js, set the base URL for the API:

javascript
Copy code
const config = {
    apiUrl: "http://localhost:5157/api/RateLimiter"
};
export default config;
Start the Frontend

bash
Copy code
npm start
The application will be available at http://localhost:3000.

Folder Structure
php
Copy code
<repository-folder>
├── backend                 # .NET Backend API
│   ├── Controllers         # API Controllers
│   ├── Services            # Rate Limiter Service
│   ├── appsettings.json    # Configuration
│   └── Program.cs          # Application entry point
│
└── frontend                # React Frontend
    ├── src
    │   ├── components      # React Components
    │   ├── styles          # CSS files
    │   ├── config.js       # API Configuration
    │   └── App.js          # Main React app file
    └── public
Usage
View Accounts: The Accounts Overview section displays the accounts with message rate and sent messages.
Auto-Refresh: Data automatically refreshes every 2 seconds.
Troubleshooting
Common Errors
Redis Connection Issue

Ensure Redis is running on localhost:6379.
Check if the Redis server is accessible from your environment.
API Not Responding

Ensure the backend is running on http://localhost:5157.
Verify appsettings.json and config.js for correct URLs.
License
This project is licensed under the MIT License.
