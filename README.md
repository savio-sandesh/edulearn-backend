# EduLearn Review API

EduLearn Review API is a dedicated microservice for course reviews and rating moderation.

## Table of Contents

- [Overview](#overview)
- [Features](#features)
- [Tech Stack](#tech-stack)
- [Project Structure](#project-structure)
- [Domain Model](#domain-model)
- [Database Constraints](#database-constraints)
- [Core Service Logic](#core-service-logic)
- [Authentication and Authorization](#authentication-and-authorization)
- [Configuration](#configuration)
- [Database Migrations](#database-migrations)
- [Run the Service](#run-the-service)
- [API Endpoints](#api-endpoints)
- [Local Testing Flow](#local-testing-flow)
- [Automated Tests](#automated-tests)
- [Build Commands](#build-commands)

## Overview

This service handles the review lifecycle for courses:

- students can submit ratings and comments for courses they are enrolled in
- admins can approve reviews for public visibility (endpoint still exists)
- reviews are currently auto-approved on submission (so they reflect immediately)
- public consumers can read approved reviews and course average ratings

## Features

- layered architecture: Controller -> Service -> Repository -> DbContext
- enrollment validation before review creation
- duplicate-review prevention at service and database level
- admin moderation with bulk update via Entity Framework ExecuteUpdateAsync
- approved-only public read endpoints
- average rating computation based only on approved reviews
- JWT bearer authentication with role-based authorization
- Swagger with bearer token support

## Tech Stack

- .NET 10
- ASP.NET Core Web API
- Entity Framework Core + SQL Server
- JWT Bearer Authentication
- Swagger / OpenAPI (Swashbuckle)

## Project Structure

- src/EduLearn.Review.API/Controllers/ReviewController.cs: API endpoints
- src/EduLearn.Review.API/Services/ReviewService.cs: review business logic
- src/EduLearn.Review.API/Services/EnrollmentServiceClient.cs: integration with Enrollment API
- src/EduLearn.Review.API/Repositories/ReviewRepository.cs: data access and query operations
- src/EduLearn.Review.API/Data/ReviewDbContext.cs: EF Core model configuration and constraints
- src/EduLearn.Review.API/Data/ReviewDbContextFactory.cs: design-time factory for EF migrations
- src/EduLearn.Review.API/Models/Review.cs: review entity
- src/EduLearn.Review.API/DTOs/: request and response contracts
- src/EduLearn.Review.API/Program.cs: dependency injection, auth, middleware, Swagger

## Domain Model

Review entity fields:

- ReviewId
- CourseId
- StudentId
- Rating (1..5)
- Comment
- IsApproved (default false in DB; service currently creates approved reviews)
- CreatedAt (UTC)

## Database Constraints

Configured in ReviewDbContext:

- unique index on (CourseId, StudentId)
- check constraint CK_Review_Rating enforcing Rating >= 1 AND Rating <= 5
- default value false for IsApproved

These constraints ensure data integrity even under concurrent requests.

## Core Service Logic

AddReviewAsync:

- validates enrollment via Enrollment API
- checks if student already reviewed the same course
- creates review with IsApproved = true and UTC CreatedAt (auto-approved)
- wraps creation in an EF execution strategy + transaction (commit/rollback)
- handles DbUpdateException as duplicate submission protection fallback

ApproveReviewAsync:

- admin moderation action
- uses ExecuteUpdateAsync to set IsApproved = true without loading entity

GetAverageRatingAsync:

- computes average only from approved reviews
- returns 0 when no approved reviews exist for a course

## Authentication and Authorization

JWT bearer authentication is required for protected routes.

Role policies:

- STUDENT: can create reviews
- ADMIN: can approve reviews
- anonymous: can read approved reviews and average rating

User identity for submission is read from ClaimTypes.NameIdentifier.

## Configuration

Primary config file:

- src/EduLearn.Review.API/appsettings.json

Required settings:

- ConnectionStrings:DefaultConnection
- Jwt:Key
- Jwt:Issuer
- Jwt:Audience
- EnrollmentApi:BaseUrl

Default local values:

- SQL Server database: EduLearn_Review_Db
- Enrollment API base URL: http://localhost:5114

Set secure JWT values for local development:

```powershell
cd .\edulearn-backend\src\EduLearn.Review.API
dotnet user-secrets init
dotnet user-secrets set "Jwt:Key" "your-32-plus-char-secret-key"
dotnet user-secrets set "Jwt:Issuer" "EduLearnAuthAPI"
dotnet user-secrets set "Jwt:Audience" "EduLearnAngularClient"
```

## Database Migrations

Create migration:

```powershell
cd .\edulearn-backend\src\EduLearn.Review.API
dotnet ef migrations add InitialReviewSchema
```

Apply migration:

```powershell
dotnet ef database update --project .\src\EduLearn.Review.API --startup-project .\src\EduLearn.Review.API
```

## Run the Service

From repository root:

```powershell
dotnet run --project .\edulearn-backend\src\EduLearn.Review.API\EduLearn.Review.API.csproj
```

From service folder:

```powershell
cd .\edulearn-backend\src\EduLearn.Review.API
dotnet run
```

Default launch URL:

- http://localhost:5144

## API Endpoints

Base route: api/reviews

- POST /api/reviews
	- role: STUDENT
	- body: courseId, rating, comment
	- behavior: creates review (auto-approved) after enrollment and duplicate checks

- GET /api/reviews/course/{id}
	- role: public
	- behavior: returns approved reviews for course id

- GET /api/reviews/course/{id}/average
	- role: public
	- behavior: returns average approved rating as double

- PUT /api/reviews/{id}/approve
	- role: ADMIN
	- behavior: marks review as approved

## Local Testing Flow

1. Start SQL Server.
2. Ensure Enrollment API is running on configured EnrollmentApi:BaseUrl.
3. Apply Review API migration.
4. Run Review API.
5. Use src/EduLearn.Review.API/EduLearn.Review.API.http for sample requests.
6. Submit review with STUDENT token.
7. Optional: Approve review with ADMIN token (if moderation is enabled).
8. Verify public endpoints show review and non-zero average.

## Automated Tests

Review API automated tests are in:

- tests/EduLearn.Review.Tests

Included coverage:

- service tests with Moq for IReviewRepository and IEnrollmentServiceClient
- AddReview failure when student is not enrolled
- AddReview failure when a duplicate review exists
- repository tests using EF Core InMemory provider
- GetAverageRating returning mean of approved reviews only
- ApproveReview setting IsApproved to true
- controller authorization-attribute tests for:
	- PUT /api/reviews/{id}/approve requires ADMIN
	- POST /api/reviews requires STUDENT

Run tests:

```powershell
dotnet test .\edulearn-backend\tests\EduLearn.Review.Tests\EduLearn.Review.Tests.csproj
```

## Build Commands

```powershell
dotnet restore .\edulearn-backend\src\EduLearn.Review.API\EduLearn.Review.API.csproj
dotnet build .\edulearn-backend\src\EduLearn.Review.API\EduLearn.Review.API.csproj
```
