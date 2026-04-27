# EduLearn Progress / Certificate Service

EduLearn Progress / Certificate Service handles lesson-progress records and certificate issuance. It consumes course completion events from RabbitMQ and generates PDF certificates using QuestPDF.

## Table of Contents

- [Overview](#overview)
- [Features](#features)
- [Tech Stack](#tech-stack)
- [Project Structure](#project-structure)
- [Data Model](#data-model)
- [Event Flow](#event-flow)
- [Configuration](#configuration)
- [Database Migrations](#database-migrations)
- [Run the Service](#run-the-service)
- [API Endpoints](#api-endpoints)
- [Testing Guide](#testing-guide)
- [Build Commands](#build-commands)

## Overview

This microservice provides progress/certificate management:

- stores lesson-level completion records
- stores issued certificate records
- consumes `ICourseCompletedEvent` messages via MassTransit
- generates PDF certificates and stores generated URL/path reference

## Features

- Layered architecture: Controller -> Service -> Consumer -> Data
- EF Core entities for `LessonProgress` and `Certificate`
- MassTransit + RabbitMQ consumer (`CourseCompletedConsumer`)
- QuestPDF-based certificate generation
- JWT authentication and role-based authorization
- debug endpoint for local certificate generation verification
- avatar image rendering from Azure Blob URL with `wwwroot/images/default-avatar.png` fallback
- static file mapping for generated certificates under `/certificates`
- certificate download endpoint that returns PDF as attachment

## Tech Stack

- .NET 10
- ASP.NET Core Web API
- Entity Framework Core + SQL Server
- MassTransit + RabbitMQ
- QuestPDF
- Swagger / OpenAPI (Swashbuckle)

## Project Structure

- Controllers/ProgressController.cs: read endpoints for lesson progress and certificates
- Controllers/CertificateDebugController.cs: local debug endpoint for generating PDF certificate and persisting certificate metadata
- Services/ICertificateService.cs: certificate generation contract
- Services/CertificateService.cs: QuestPDF implementation
- Consumers/CourseCompletedConsumer.cs: event consumer for `ICourseCompletedEvent`
- Data/ProgressDbContext.cs: DbContext and model constraints/indexes
- Data/ProgressDbContextFactory.cs: design-time factory for EF migrations
- Models/LessonProgress.cs: lesson progress entity
- Models/Certificate.cs: certificate entity

## Data Model

`LessonProgress` fields:

- Id
- StudentId
- CourseId
- LessonId
- IsCompleted
- CompletedAt (nullable)
- ProgressPercent

`Certificate` fields:

- Id
- StudentId
- CourseId
- CertificateUrl
- IssuedAt
- VerificationCode

Indexes/constraints:

- unique index on `LessonProgress(StudentId, CourseId, LessonId)`
- unique index on `Certificate(VerificationCode)`
- unique index on `Certificate(StudentId, CourseId)`

## Event Flow

1. Another service publishes `ICourseCompletedEvent`.
2. Progress API consumer receives the event.
3. Consumer checks if certificate already exists for that student/course.
4. If missing, service generates PDF certificate via QuestPDF.
5. Certificate metadata is saved to SQL Server.

## Configuration

Primary configuration file:

- `src/EduLearn.Progress.API/appsettings.json`

Required values:

- `ConnectionStrings:DefaultConnection`
- `Jwt:Key`
- `Jwt:Issuer`
- `Jwt:Audience`
- `RabbitMQ:Host`
- `RabbitMQ:VirtualHost`
- `RabbitMQ:Username`
- `RabbitMQ:Password`

Optional values:

- `Certificate:OutputDirectory` (default local usage is `C:\Temp`)
- `Avatar:BlobUrlTemplate` (optional; use `{studentId}` placeholder)

Default local DB connection:

- `Server=localhost\SQLEXPRESS;Database=EduLearn_Progress_Db;Trusted_Connection=True;TrustServerCertificate=True`

JWT rule:

- `Jwt:Key`, `Jwt:Issuer`, and `Jwt:Audience` must match values issued by Auth API.

Example local setup with user-secrets:

```powershell
cd .\edulearn-backend\src\EduLearn.Progress.API
dotnet user-secrets init
dotnet user-secrets set "Jwt:Key" "your-32-plus-char-secret-key"
dotnet user-secrets set "Jwt:Issuer" "EduLearnAuthAPI"
dotnet user-secrets set "Jwt:Audience" "EduLearnAngularClient"
```

QuestPDF license setup:

- startup sets `QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;`

## Database Migrations

Create a new migration (when schema changes):

```powershell
cd .\edulearn-backend\src\EduLearn.Progress.API
dotnet ef migrations add <MigrationName>
```

Apply migrations to database:

```powershell
dotnet ef database update --project .\src\EduLearn.Progress.API --startup-project .\src\EduLearn.Progress.API
```

## Run the Service

From repo root:

```powershell
dotnet run --project .\edulearn-backend\src\EduLearn.Progress.API\EduLearn.Progress.API.csproj
```

From service folder:

```powershell
cd .\edulearn-backend\src\EduLearn.Progress.API
dotnet run
```

Default local URL from launch profile:

- http://localhost:5218

## API Endpoints

Base route: `api/progress`

- `GET /api/progress/lesson-progress`
- `GET /api/progress/lesson-progress/{id}`
- `POST /api/progress/mark-complete`
- `GET /api/progress/certificates`
- `GET /api/progress/certificates/{id}`
- `GET /api/progress/certificates/verify/{code}`
- `GET /api/progress/certificates/download/{id}`

Debug route:

- `POST /api/progress/debug/generate-certificate`

Notes:

- read endpoints currently require `STUDENT,INSTRUCTOR,ADMIN`.
- `POST /api/progress/mark-complete` upserts lesson progress and recalculates course-level `ProgressPercent` for that student-course pair.
- `GET /api/progress/certificates/verify/{code}` allows anonymous verification and expects GUID-form verification code input.
- debug endpoint currently allows anonymous access for local testing.
- debug endpoint now generates the PDF and inserts/updates a `Certificate` row for the same student/course.

## Testing Guide

Quick local flow:

1. Start SQL Server/LocalDB.
2. Ensure RabbitMQ is running on localhost.
3. Run Progress API.
4. Call `POST /api/progress/debug/generate-certificate`.
5. Verify PDF file appears in configured output directory (`C:\Temp` by default).
6. Verify `Certificate` row is inserted/updated in SQL with `StudentId`, `CourseId`, `CertificateUrl`, and `IssuedAt`.
7. Call `GET /api/progress/certificates/download/{id}` and verify browser download works.
8. Publish/consume `ICourseCompletedEvent` and verify certificate persistence in DB.

Automated tests:

- `tests/EduLearn.Progress.Tests/ProgressApiCoreTests.cs`
	- Certificate persistence to DB (SQLite in-memory)
	- Avatar download + fallback behavior (Moq + mocked HttpClient)
	- Certificate URL format validation
	- Download endpoint returns `PhysicalFileResult`
	- Mark-complete upsert and `ProgressPercent` recalculation behavior
	- Certificate verification by GUID (invalid/missing/found cases)

## Build Commands

```powershell
dotnet restore .\edulearn-backend\src\EduLearn.Progress.API\EduLearn.Progress.API.csproj
dotnet build .\edulearn-backend\src\EduLearn.Progress.API\EduLearn.Progress.API.csproj
```
