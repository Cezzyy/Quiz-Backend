# Online Quiz API - Supabase Setup Guide

## 🚀 Getting Started

### 1. Configure Environment Variables

Copy the `.env.example` file to `.env`:

```bash
cp .env.example .env
```

### 2. Get Your Supabase Credentials

1. Go to your [Supabase Dashboard](https://app.supabase.com)
2. Select your project
3. Go to **Settings** → **API**
4. Copy the following values:
   - **Project URL** → `SUPABASE_URL`
   - **anon/public key** → `SUPABASE_KEY`

### 3. Update Your `.env` File

Replace the placeholder values in `.env` with your actual Supabase credentials:

```env
# Supabase Configuration
SUPABASE_URL=https://your-project-id.supabase.co
SUPABASE_KEY=your-anon-key-here

# Application Settings
ASPNETCORE_ENVIRONMENT=Development
ASPNETCORE_URLS=http://localhost:5000;https://localhost:5001
```

> **Note**: You only need the Supabase URL and anon key. The Supabase C# SDK handles all database connections internally.

### 4. Run the Database Schema

Execute the `supabase_schema.sql` file in your Supabase SQL Editor:

1. Go to **SQL Editor** in your Supabase dashboard
2. Click **New Query**
3. Copy and paste the contents of `supabase_schema.sql`
4. Click **Run** to execute

This will create all tables, indexes, RLS policies, and helper functions.

## 📦 Running the Application

### Restore NuGet Packages

```bash
dotnet restore
```

### Run the Application

```bash
dotnet run
```

The API will start on:
- HTTP: `http://localhost:5000`
- HTTPS: `https://localhost:5001`

## 📚 API Documentation

Once the application is running, access the API documentation at:

**Scalar UI**: `https://localhost:5001/scalar/v1`

(Scalar has replaced Swagger for a better API documentation experience)

## 🔑 Key Features

- ✅ **Supabase C# SDK** - Direct integration with Supabase
- ✅ **Environment Variables** - Secure configuration with `.env`
- ✅ **Scalar API Docs** - Modern, interactive API documentation
- ✅ **AutoMapper** - Object-to-object mapping
- ✅ **Repository-Service Pattern** - Clean architecture

## 📁 Project Structure

```
OnlineQuiz/
├── Controllers/        # API endpoints
├── DTOs/              # Data Transfer Objects
├── Data/              # Database context (if needed)
├── IRepository/       # Repository interfaces
├── IServices/         # Service interfaces
├── Mappings/          # AutoMapper profiles
├── Models/            # Entity models (14 models)
├── Repository/        # Repository implementations
├── Services/          # Business logic
│   └── SupabaseService.cs  # Supabase client wrapper
├── Utilities/         # Helper classes
├── .env               # Environment variables (gitignored)
├── .env.example       # Environment template
└── supabase_schema.sql # Database schema
```

## 🔒 Security Notes

- **Never commit `.env`** to version control (already in `.gitignore`)
- Keep your Supabase credentials secure
- The anon key is safe for client-side use (it's protected by Row Level Security)
- For admin operations, use Supabase's built-in auth and RLS policies

## 🛠️ Next Steps

1. Create DTOs for your API requests/responses
2. Implement repositories for data access
3. Create services for business logic
4. Build controllers for API endpoints
5. Configure AutoMapper profiles for entity-DTO mapping

## 📖 Supabase C# SDK Documentation

For more information on using the Supabase C# SDK:
- [Supabase C# Documentation](https://supabase.com/docs/reference/csharp/introduction)
- [GitHub Repository](https://github.com/supabase-community/supabase-csharp)

## ⚠️ Important Notes

- The models use data annotations for validation but **NOT** for Entity Framework navigation properties
- Supabase handles relationships through its own query system
- Use the `SupabaseService` to access the Supabase client in your repositories and services
