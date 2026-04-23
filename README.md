# EduLearn Lesson / Content Service

EduLearn Lesson / Content Service manages the ordered lesson list within each course. It supports content-driven rendering metadata, lesson publishing, preview access, and atomic reordering.

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
- [Gateway Integration](#gateway-integration)
- [Testing Guide](#testing-guide)
- [Build Commands](#build-commands)

## Overview

This microservice provides lesson/content management for courses:

- Create, update, publish, and delete lessons
- Retrieve lesson lists by course, ordered sequence, and preview visibility
- Reorder lesson sequence with an ordered list of LessonIds
- Return lesson counts per course

## Features

- Layered architecture: Controller -> Service -> Repository
- EF Core entity for `Lesson`
- ContentType support: `VIDEO`, `ARTICLE`, `PDF`, `QUIZ_LINK`
- JWT authentication and role-based authorization
- ContentUrl support for Azure Blob URLs (served as temporary SAS URLs) or external URLs
- Atomic lesson reordering using transaction + `ExecuteUpdateAsync` loop
- Preview lesson retrieval (`IsPreview = true`) for discovery use cases

## Tech Stack

- .NET 10
- ASP.NET Core Web API
- Entity Framework Core + SQL Server
- Swagger / OpenAPI (Swashbuckle)
- Azure Storage Blobs SDK (for blob-oriented content workflows)

## Project Structure

- Controllers/LessonController.cs: lesson endpoints
- Services/ILessonService.cs: lesson service contract
- Services/LessonService.cs: lesson business logic
- Repositories/ILessonRepository.cs: data access contract
- Repositories/LessonRepository.cs: EF Core data access implementation
- Data/ContentDbContext.cs: DbContext and model configuration
- Models/Lesson.cs: lesson entity
- DTOs/: create/update/reorder/response contracts

## Data Model

`Lesson` fields:

- LessonId
- CourseId
- Title
- Description
- ContentType (`VIDEO` / `ARTICLE` / `PDF` / `QUIZ_LINK`)
- ContentUrl
- DurationMinutes
- DisplayOrder
- IsPreview
- IsPublished
- CreatedAt

## Business Rules

- `ContentType` is validated against the allowed set.
- `ContentUrl` must be an absolute URL.
- New lessons are appended at the end of the existing course order (`DisplayOrder = count + 1`).
- `ReorderLessons(courseId, IList<int>)` requires an exact, duplicate-free list of all lesson IDs for that course.
- Reordering is atomic:
  - begins database transaction
  - updates each lesson's `DisplayOrder` via EF Core `ExecuteUpdateAsync`
  - commits only after all updates succeed
- Preview endpoint returns published preview lessons (`IsPreview && IsPublished`) so course discovery can happen without enrollment.

## Configuration

Primary configuration file:

- `EduLearn.Content.API/appsettings.json`

Required values:

- `ConnectionStrings:DefaultConnection`
- `Jwt:Key`
- `Jwt:Issuer`
- `Jwt:Audience`
- `AzureStorage:ConnectionString`
- `AzureStorage:ContainerName`
- `AzureStorage:SasExpiryMinutes`

Default local DB connection currently points to SQL Express:

- `Server=localhost\\SQLEXPRESS;Database=EduLearn_Content_Db;Trusted_Connection=True;TrustServerCertificate=True`

JWT rule:

- `Jwt:Key`, `Jwt:Issuer`, and `Jwt:Audience` must match values issued by Auth API.

Example local setup with user-secrets:

```powershell
cd .\edulearn-backend\EduLearn.Content.API
dotnet user-secrets init
dotnet user-secrets set "Jwt:Key" "your-32-plus-char-secret-key"
dotnet user-secrets set "Jwt:Issuer" "EduLearnAuthAPI"
dotnet user-secrets set "Jwt:Audience" "EduLearnAngularClient"
dotnet user-secrets set "AzureStorage:ConnectionString" "UseDevelopmentStorage=true"
dotnet user-secrets set "AzureStorage:ContainerName" "lesson-content"
dotnet user-secrets set "AzureStorage:SasExpiryMinutes" "30"
```

SAS URL rule:

- For Azure Blob content URLs under the configured storage account, lesson fetch responses return a temporary read SAS URL.
- For non-blob external URLs (for example video platform links), the original URL is returned unchanged.

## Database Migrations

Create a new migration (when schema changes):

```powershell
cd .\edulearn-backend\EduLearn.Content.API
dotnet ef migrations add <MigrationName>
```

Apply migrations to database:

```powershell
cd .\edulearn-backend\EduLearn.Content.API
dotnet ef database update
```

## Run the Service

From repo root:

```powershell
dotnet run --project .\edulearn-backend\EduLearn.Content.API\EduLearn.Content.API.csproj
```

From service folder:

```powershell
cd .\edulearn-backend\EduLearn.Content.API
dotnet run
```

Default local URL from launch profile:

- http://localhost:5176

## API Endpoints

Base route: `api/lessons`

- `POST /api/lessons`
- `GET /api/lessons/byId/{lessonId}`
- `GET /api/lessons/byCourse/{courseId}`
- `GET /api/lessons/ordered/{courseId}`
- `GET /api/lessons/preview/{courseId}`
- `PUT /api/lessons/update/{lessonId}`
- `PUT /api/lessons/reorder/{courseId}`
- `PUT /api/lessons/publish/{lessonId}`
- `DELETE /api/lessons/lesson/{lessonId}`
- `DELETE /api/lessons/allForCourse/{courseId}`
- `GET /api/lessons/count/{courseId}`

## Gateway Integration

Gateway route for Content API:

- Incoming: `/gateway/content/{**remainder}`
- Forwarded path transform:
  - remove prefix `/gateway/content`
  - add prefix `/api/lessons`
- Gateway destination: `http://localhost:5176/`

Example through gateway:

- `GET http://localhost:5000/gateway/content/byCourse/1`

Authorization matrix:

- Anonymous:
  - `GET /api/lessons/preview/{courseId}`
- `INSTRUCTOR, ADMIN`:
  - `POST /api/lessons`
  - `PUT /api/lessons/update/{lessonId}`
  - `PUT /api/lessons/reorder/{courseId}`
  - `PUT /api/lessons/publish/{lessonId}`
  - `DELETE /api/lessons/lesson/{lessonId}`
  - `DELETE /api/lessons/allForCourse/{courseId}`
- `INSTRUCTOR, ADMIN, STUDENT`:
  - `GET /api/lessons/byId/{lessonId}`
  - `GET /api/lessons/byCourse/{courseId}`
  - `GET /api/lessons/ordered/{courseId}`
  - `GET /api/lessons/count/{courseId}`

## Testing Guide

Quick local flow:

1. Start SQL Server/LocalDB.
2. Start Content API.
3. Use Swagger at `/swagger` in development.
4. Create multiple lessons for a course.
5. Reorder using `orderedLessonIds` payload and verify `ordered` endpoint.
6. Publish selected lessons and verify `preview` endpoint only returns preview + published lessons.

Sample reorder payload:

```json
{
  "orderedLessonIds": [3, 1, 2]
}
```

## Build Commands

```powershell
dotnet restore .\edulearn-backend\EduLearn.Content.API\EduLearn.Content.API.csproj
dotnet build .\edulearn-backend\EduLearn.Content.API\EduLearn.Content.API.csproj
```

