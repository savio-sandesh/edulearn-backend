# EduLearn Assessment / Quiz Service

EduLearn Assessment / Quiz Service manages quiz authoring and quiz-attempt lifecycle for courses and lessons. It supports quiz publishing, attempt limits, JSON-based answer payloads, and scoring with pass/fail evaluation.

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

This microservice provides assessment/quiz management:

- Create, update, publish, and delete quizzes
- Retrieve quizzes by ID, course, and lesson
- Start quiz attempts with max-attempt guard per student
- Submit attempt answers and evaluate score/pass-fail
- Retrieve attempt history, best attempt, and attempt count per quiz

## Features

- Layered architecture: Controller -> Service -> Repository
- EF Core entities for `Quiz` and `QuizAttempt`
- JWT authentication and role-based authorization
- JSON answer handling using `System.Text.Json`
- Cycle-safe JSON serialization (`ReferenceHandler.IgnoreCycles`)
- Score calculation as percentage of matched answers
- Pass/fail evaluation using `score >= PassingScore`
- Max-attempt enforcement before attempt creation

## Tech Stack

- .NET 10
- ASP.NET Core Web API
- Entity Framework Core + SQL Server
- Swagger / OpenAPI (Swashbuckle)
- NUnit + Moq for service-level tests

## Project Structure

- Controllers/QuizController.cs: quiz and attempt endpoints
- Services/IQuizService.cs: quiz service contract
- Services/QuizService.cs: quiz/attempt business logic
- Repositories/IQuizRepository.cs: data access contract
- Repositories/QuizRepository.cs: EF Core data access implementation
- Data/AssessmentDbContext.cs: DbContext and model constraints
- Models/Quiz.cs: quiz entity
- Models/QuizAttempt.cs: attempt entity
- DTOs/QuizAttemptResponseDto.cs: flat start/submit attempt response contract
- DTOs/SubmitAttemptRequestDto.cs: submit-attempt payload contract

## Data Model

`Quiz` fields:

- QuizId
- CourseId
- LessonId (nullable)
- Title
- Description
- TimeLimitMinutes
- PassingScore (0-100)
- MaxAttempts (> 0)
- IsPublished
- CreatedAt
- QuestionsJson (JSON map: `QuestionId -> CorrectAnswer`)

`QuizAttempt` fields:

- AttemptId
- QuizId
- StudentId
- Score (0-100)
- IsPassed
- StartedAt
- SubmittedAt (nullable)
- Answers (JSON map: `QuestionId -> StudentSelectedAnswer`)

## Business Rules

- `PassingScore` must be between 0 and 100.
- `MaxAttempts` must be greater than zero.
- `StartAttempt(studentId, quizId)` rules:
  - quiz must exist
  - quiz must be published
  - `CountAttempts(studentId, quizId) < MaxAttempts`
- If max attempts is reached, start attempt is blocked.
- `SubmitAttempt(attemptId, answers)` rules:
  - attempt must exist
  - attempt must not have already been submitted
  - quiz for that attempt must exist
- Student answers are serialized into `QuizAttempt.Answers` as JSON.
- `SubmitAttempt` sets `SubmittedAt` using UTC and persists serialized answers before save.
- Quiz correct answers are read from `Quiz.QuestionsJson` as JSON.
- Score is computed as:
  - `(matchedAnswers / totalCorrectAnswers) * 100`
  - rounded using midpoint-away-from-zero
- Pass flag is computed as:
  - `IsPassed = score >= Quiz.PassingScore`

## Configuration

Primary configuration file:

- `src/EduLearn.Assessment.API/appsettings.json`

Required values:

- `ConnectionStrings:DefaultConnection`
- `Jwt:Key`
- `Jwt:Issuer`
- `Jwt:Audience`

Default local DB connection currently points to SQL Express:

- `Server=localhost\\SQLEXPRESS;Database=EduLearn_Assessment_Db;Trusted_Connection=True;TrustServerCertificate=True`

JWT rule:

- `Jwt:Key`, `Jwt:Issuer`, and `Jwt:Audience` must match values issued by Auth API.

Example local setup with user-secrets:

```powershell
cd .\edulearn-backend\src\EduLearn.Assessment.API
dotnet user-secrets init
dotnet user-secrets set "Jwt:Key" "your-32-plus-char-secret-key"
dotnet user-secrets set "Jwt:Issuer" "EduLearnAuthAPI"
dotnet user-secrets set "Jwt:Audience" "EduLearnAngularClient"
```

## Database Migrations

Create a new migration (when schema changes):

```powershell
cd .\edulearn-backend\src\EduLearn.Assessment.API
dotnet ef migrations add <MigrationName>
```

Apply migrations to database:

```powershell
cd .\edulearn-backend\src\EduLearn.Assessment.API
dotnet ef database update
```

## Run the Service

From repo root:

```powershell
dotnet run --project .\edulearn-backend\src\EduLearn.Assessment.API\EduLearn.Assessment.API.csproj
```

From service folder:

```powershell
cd .\edulearn-backend\src\EduLearn.Assessment.API
dotnet run
```

Default local URL from launch profile:

- http://localhost:5012

## API Endpoints

Base route: `api/quizzes`

- `POST /api/quizzes`
- `GET /api/quizzes/{id}`
- `GET /api/quizzes/byCourse/{courseId}`
- `GET /api/quizzes/byLesson/{lessonId}`
- `PUT /api/quizzes/{id}`
- `DELETE /api/quizzes/{id}`
- `PUT /api/quizzes/publish/{id}`
- `POST /api/quizzes/{quizId}/startAttempt`
- `POST /api/quizzes/attempts/{attemptId}/submit`
- `GET /api/quizzes/{quizId}/attempts`
- `GET /api/quizzes/{quizId}/bestAttempt`
- `GET /api/quizzes/{quizId}/attemptCount`

## Gateway Integration

Gateway route for Assessment API is not configured yet in current gateway settings.

To add it in `src/Edulearn.Gateway.API/appsettings.json`:

- Incoming: `/gateway/assessment/{**remainder}`
- Forwarded path transform:
  - remove prefix `/gateway/assessment`
  - add prefix `/api/quizzes`
- Gateway destination: `http://localhost:5012/`

Example through gateway after route is added:

- `GET http://localhost:5000/gateway/assessment/byCourse/1`

Authorization matrix:

- `INSTRUCTOR, ADMIN`:
  - `POST /api/quizzes`
  - `PUT /api/quizzes/{id}`
  - `DELETE /api/quizzes/{id}`
  - `PUT /api/quizzes/publish/{id}`
- `STUDENT, ADMIN`:
  - `POST /api/quizzes/{quizId}/startAttempt`
  - `POST /api/quizzes/attempts/{attemptId}/submit`
  - `GET /api/quizzes/{quizId}/attempts`
  - `GET /api/quizzes/{quizId}/bestAttempt`
  - `GET /api/quizzes/{quizId}/attemptCount`
- No explicit `[Authorize]` on current implementation:
  - `GET /api/quizzes/{id}`
  - `GET /api/quizzes/byCourse/{courseId}`
  - `GET /api/quizzes/byLesson/{lessonId}`

## Testing Guide

Quick local flow:

1. Start SQL Server/LocalDB.
2. Start Assessment API.
3. Use Swagger at `/swagger` in development.
4. Create a quiz with `QuestionsJson` containing correct answers.
5. Publish quiz.
6. Start attempt as student.
7. Submit answers and verify score and pass/fail behavior.
8. Repeat attempts until max limit and verify guard blocks additional attempts.

Sample submit payload:

```json
{
  "answers": {
    "1": "A",
    "2": "B",
    "3": "C"
  }
}
```

Current automated tests:

- `tests/EduLearn.Assessment.Tests/QuizServiceTests.cs`
  - `StartAttempt_WhenWithinLimit_CreatesAttempt`
  - `StartAttempt_WhenMaxAttemptsReached_ThrowsInvalidOperationException`
  - `SubmitAttempt_WhenScoreMeetsPassingScore_SetsPassedTrueAndPersistsAnswers`
  - `SubmitAttempt_WhenScoreBelowPassingScore_SetsPassedFalse`

## Build Commands

```powershell
dotnet restore .\edulearn-backend\src\EduLearn.Assessment.API\EduLearn.Assessment.API.csproj
dotnet build .\edulearn-backend\src\EduLearn.Assessment.API\EduLearn.Assessment.API.csproj
```
