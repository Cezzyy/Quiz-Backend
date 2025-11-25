using DotNetEnv;
using Mapster;
using OnlineQuiz.Mappings;
using OnlineQuiz.Services;
using Scalar.AspNetCore;

// Load environment variables from .env file
Env.Load();

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Configure JSON serialization for consistent API responses
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase; // Use camelCase
        options.JsonSerializerOptions.WriteIndented = true; // Pretty print in development
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });

// Configure Supabase
var supabaseUrl = Environment.GetEnvironmentVariable("SUPABASE_URL") 
    ?? throw new InvalidOperationException("SUPABASE_URL is not set in environment variables");
var supabaseKey = Environment.GetEnvironmentVariable("SUPABASE_KEY") 
    ?? throw new InvalidOperationException("SUPABASE_KEY is not set in environment variables");

var supabaseService = new SupabaseService(supabaseUrl, supabaseKey);
await supabaseService.InitializeAsync();
builder.Services.AddSingleton(supabaseService);

// Configure Mapster mappings
MapsterConfig.RegisterMappings();


// Configure Mapster
builder.Services.AddMapster();

// Register Repository Layer
builder.Services.AddScoped<OnlineQuiz.IRepository.IUserRepository, OnlineQuiz.Repository.UserRepository>();
builder.Services.AddScoped<OnlineQuiz.IRepository.IStudentRepository, OnlineQuiz.Repository.StudentRepository>();
builder.Services.AddScoped<OnlineQuiz.IRepository.ITeacherRepository, OnlineQuiz.Repository.TeacherRepository>();
builder.Services.AddScoped<OnlineQuiz.IRepository.IUserRoleRepository, OnlineQuiz.Repository.UserRoleRepository>();
builder.Services.AddScoped<OnlineQuiz.IRepository.ICourseRepository, OnlineQuiz.Repository.CourseRepository>();
builder.Services.AddScoped<OnlineQuiz.IRepository.IQuizRepository, OnlineQuiz.Repository.QuizRepository>();
builder.Services.AddScoped<OnlineQuiz.IRepository.IEnrollmentRepository, OnlineQuiz.Repository.EnrollmentRepository>();

// Register Service Layer
builder.Services.AddScoped<OnlineQuiz.IServices.IUserService, OnlineQuiz.Services.UserService>();
builder.Services.AddScoped<OnlineQuiz.IServices.ICourseService, OnlineQuiz.Services.CourseService>();
builder.Services.AddScoped<OnlineQuiz.IServices.IQuizService, OnlineQuiz.Services.QuizService>();


// Configure CORS for Web (Vue) and Mobile (Flutter)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowWebAndMobile", policy =>
    {
        policy.SetIsOriginAllowed(origin => 
            {
                // Allow localhost for development
                if (string.IsNullOrEmpty(origin)) return true;
                if (origin.StartsWith("http://localhost") || origin.StartsWith("https://localhost")) return true;
                // Add your production domains here
                return false;
            })
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials()
            .WithExposedHeaders("Authorization", "Content-Type", "X-Total-Count");
    });
});

// Configure services for Scalar
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() 
    { 
        Title = "Online Quiz API", 
        Version = "v1",
        Description = "A comprehensive online quiz platform API powered by Supabase"
    });
    
    // Configure JWT Bearer authentication (for future use)
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });
    
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.MapScalarApiReference(options =>
    {
        options.OpenApiRoutePattern = "/swagger/{documentName}/swagger.json";
        options.WithTitle("Online Quiz API")
               .WithTheme(ScalarTheme.Purple)
               .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
    });
}

app.UseHttpsRedirection();

app.UseCors("AllowWebAndMobile");

// Add security headers
app.Use(async (context, next) =>
{
    // Security headers for web clients
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    
    // API-specific headers
    context.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
    context.Response.Headers["Pragma"] = "no-cache";
    context.Response.Headers["Expires"] = "0";
    
    await next();
});





app.UseAuthorization();

// Redirect root path to Scalar API documentation
app.MapGet("/", () => Results.Redirect("/scalar/v1"));

app.MapControllers();

app.Run();
