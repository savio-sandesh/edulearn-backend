# EduLearn Backend

This backend workspace contains the EduLearn microservices and shared contracts.

## Table of Contents

- [Overview](#overview)
- [Features](#features)
- [Tech Stack](#tech-stack)
- [Project Structure](#project-structure)
- [Data Model](#data-model)
- [Business Rules](#business-rules)
- [Configuration](#configuration)
- [Database Migrations](#database-migrations)
- [Run the Service](#run-the-service)
- [API Endpoints](#api-endpoints)
- [Authorization Matrix](#authorization-matrix)
- [Testing Guide](#testing-guide)
- [Build Commands](#build-commands)

## Overview

This microservice set supports enrollment workflows:

- Enroll a student into a course
- Prevent duplicate enrollment per student and course
- Track progress percentage
- Complete and drop enrollments
- Issue certificate for completed enrollments
- Query by student, by course, and enrollment count
- Publish and consume course completion events with RabbitMQ

## Features

- Layered architecture: Controller -> Service -> Repository
- JWT authentication and role-based authorization
- Student identity extracted from token claims, not request body
- Transactional enrollment flow with course count increment call
- Progress update via IProgressService and formula calculation
- Completion flow with quiz-pass gated certificate issuance
- Explicit repository update flow for status/progress/completion/certificate state changes
- Shared contract library for cross-service messaging
- MassTransit + RabbitMQ event flow for enrollment completion notifications

## Tech Stack

- .NET 10
- ASP.NET Core Web API
- Entity Framework Core + SQL Server
- JWT Bearer Authentication
- MassTransit + RabbitMQ
- Swagger / OpenAPI (Swashbuckle)

Compatibility note:

- `MassTransit.RabbitMQ` is pinned to `8.5.1` in Enrollment and Content services to avoid the v9 runtime license requirement in local dev.

## Project Structure

- src/EduLearn.Auth.API
- src/EduLearn.Content.API
- src/EduLearn.Course.API
- src/EduLearn.Enrollment.API
- src/EduLearn.Gateway.API
- src/EduLearn.Shared
- tests/EduLearn.Auth.Tests
- tests/EduLearn.Content.Tests
- tests/EduLearn.Course.Tests
- tests/EduLearn.Enrollment.Tests

## Data Model

Enrollment fields:

- EnrollmentId
- StudentId
- CourseId
- EnrolledAt
- CompletedAt (nullable)
- Status (ACTIVE, COMPLETED, DROPPED)
- ProgressPercent (0-100)
- LastAccessedAt (nullable)
- CertificateIssued
- PaymentId (nullable)

Database constraints:

- Unique index on (StudentId, CourseId)
- Index on CourseId
- Index on StudentId

## Business Rules

- Enroll first checks IsEnrolled(studentId, courseId).
- Duplicate active/completed enrollments are blocked.
- StudentId is always derived from token claim ClaimTypes.NameIdentifier.
- Enroll flow uses one transaction:
  - create enrollment record
  - call ICourseService.IncrementEnrollment(courseId)
  - commit only if both steps succeed
- UpdateProgress reads progress from IProgressService.GetCourseProgress and computes:

  ProgressPercent = (completedLessons / totalLessons) * 100

- CompleteEnrollment sets:
  - Status = COMPLETED
  - CompletedAt = now
  - ProgressPercent = 100
- If all quizzes are passed, certificate is issued.
- DropCourse only applies to an ACTIVE enrollment and updates status to DROPPED.
- Enrollment mutation updates are persisted through repository UpdateAsync + SaveChangesAsync.
- Enrollment completion publishes a course-completed event after persistence.

## Configuration

Primary file:

- src/EduLearn.Enrollment.API/appsettings.json

Required values:

- ConnectionStrings:DefaultConnection
- Jwt:Key
- Jwt:Issuer
- Jwt:Audience
- CourseApi:BaseUrl
- RabbitMQ host: localhost
- RabbitMQ username/password: guest / guest

Progress provider settings:

- Progress:UseMock
- Progress:MockCompletedLessons
- Progress:MockTotalLessons
- Progress:MockAllQuizzesPassed

Example user-secrets setup:

```powershell
cd .\edulearn-backend\src\EduLearn.Enrollment.API
dotnet user-secrets init
dotnet user-secrets set "Jwt:Key" "your-32-plus-char-secret-key"
dotnet user-secrets set "Jwt:Issuer" "EduLearnAuthAPI"
dotnet user-secrets set "Jwt:Audience" "EduLearnAngularClient"
```

## Database Migrations

From service folder:

```powershell
cd .\edulearn-backend\src\EduLearn.Enrollment.API
dotnet ef migrations add InitialEnrollmentSchema
dotnet ef database update
```

## Run the Service

From repo root:

```powershell
dotnet run --project .\edulearn-backend\src\EduLearn.Enrollment.API\EduLearn.Enrollment.API.csproj
```

From service folder:

```powershell
cd .\edulearn-backend\src\EduLearn.Enrollment.API
dotnet run
```

## API Endpoints

Base route: api/enrollments

- POST /api/enrollments/enroll/{courseId}
- GET /api/enrollments/byId/{id}
- GET /api/enrollments/byStudent/{studentId}
- GET /api/enrollments/byCourse/{courseId}
- GET /api/enrollments/isEnrolled/{courseId}
- PUT /api/enrollments/progress/{enrollmentId}
- POST /api/enrollments/complete/{courseId}
- PUT /api/enrollments/issueCert/{enrollmentId}
- POST /api/enrollments/drop/{courseId}
- GET /api/enrollments/completed/{studentId}
- GET /api/enrollments/inProgress/{studentId}
- GET /api/enrollments/count/{courseId}

## Authorization Matrix

- STUDENT:
  - complete, drop
- STUDENT, ADMIN:
  - enroll, update progress, student-specific query endpoints
- INSTRUCTOR, ADMIN:
  - byCourse, count
- ADMIN:
  - issue certificate

## Testing Guide

Quick local validation:

1. Configure JWT values to match Auth API.
2. Run Course API and Enrollment API.
3. Enroll with student token.
4. Re-enroll same course and verify duplicate rejection.
5. Update progress and verify ProgressPercent formula.
6. Complete enrollment and verify certificate issuance when quizzes are passed.

For Content test project:
- tests/EduLearn.Content.Tests

## Build Commands

```powershell
dotnet restore .\edulearn-backend\src\EduLearn.Enrollment.API\EduLearn.Enrollment.API.csproj
dotnet build .\edulearn-backend\src\EduLearn.Enrollment.API\EduLearn.Enrollment.API.csproj
```

