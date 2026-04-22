# EduLearn API Gateway

EduLearn API Gateway is the reverse-proxy entry point for backend services in the EduLearn platform. It routes external requests to downstream microservices using YARP.

## Table of Contents

- [Overview](#overview)
- [Features](#features)
- [Tech Stack](#tech-stack)
- [Prerequisites](#prerequisites)
- [Project Structure](#project-structure)
- [Routing Configuration](#routing-configuration)
- [Run the Service](#run-the-service)
- [Gateway Endpoints](#gateway-endpoints)
- [Testing Guide](#testing-guide)
- [Troubleshooting](#troubleshooting)
- [Build Commands](#build-commands)

## Overview

This project acts as a single HTTP gateway for backend APIs.

- Incoming Auth requests are proxied to Auth API.
- Incoming Course requests are proxied to Course API.
- A health-style root endpoint is exposed for quick gateway availability checks.

## Features

- Reverse proxy powered by YARP
- Route and cluster definitions from configuration
- Path transform support (prefix removal and prefix add)
- Single entry-point URL for multiple APIs

## Tech Stack

- .NET 10
- ASP.NET Core Web API hosting model
- YARP (Yet Another Reverse Proxy)

## Prerequisites

- .NET SDK 10
- Auth API running locally at http://localhost:5206
- Course API running locally at http://localhost:5224

## Project Structure

- Program.cs: gateway pipeline and reverse-proxy registration
- appsettings.json: reverse proxy routes, transforms, and clusters
- Properties/launchSettings.json: local gateway URLs

## Routing Configuration

Configured routes in appsettings.json:

1. Auth route
   - Incoming path: /gateway/auth/{**remainder}
   - Transforms:
     - Remove prefix: /gateway/auth
     - Add prefix: /api
   - Destination cluster: auth-cluster -> http://localhost:5206/

2. Course route
   - Incoming path: /gateway/course/{**remainder}
   - Transforms:
     - Remove prefix: /gateway/course
     - Add prefix: /api
   - Destination cluster: course-cluster -> http://localhost:5224/

Example transformation:

- /gateway/auth/users/login -> /api/users/login (forwarded to Auth API)
- /gateway/course/courses/published -> /api/courses/published (forwarded to Course API)

## Run the Service

From repo root:

```powershell
dotnet run --project .\edulearn-backend\Edulearn.Gateway.API\Edulearn.Gateway.API.csproj
```

From service folder:

```powershell
cd .\edulearn-backend\Edulearn.Gateway.API
dotnet run
```

Default local URL from launch profile:

- http://localhost:5000

Optional HTTPS profile URL:

- https://localhost:7107

## Gateway Endpoints

Gateway status endpoint:

- GET /
  - Response: EduLearn API Gateway is Running!

Proxy entry paths:

- /gateway/auth/*
- /gateway/course/*

## Testing Guide

Recommended local test flow:

1. Start Auth API.
2. Start Course API.
3. Start Gateway API.
4. Verify gateway health endpoint.
5. Call downstream APIs through gateway-prefixed routes.

Sample test requests:

```http
GET http://localhost:5000/

GET http://localhost:5000/gateway/course/courses/published

POST http://localhost:5000/gateway/auth/users/login
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "your-password"
}
```

## Troubleshooting

- 502 Bad Gateway
  - verify downstream services are running on configured ports
  - check cluster destination addresses in appsettings.json

- 404 from downstream API
  - verify forwarded path after transforms starts with /api
  - confirm downstream endpoint exists

- Connection refused
  - check launch profile URL and request base URL
  - ensure no port conflict on 5000 or 7107

## Build Commands

```powershell
dotnet restore .\edulearn-backend\Edulearn.Gateway.API\Edulearn.Gateway.API.csproj
dotnet build .\edulearn-backend\Edulearn.Gateway.API\Edulearn.Gateway.API.csproj
```

