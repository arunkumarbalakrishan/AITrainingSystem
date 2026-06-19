using AITrainingSystem.API.Extensions;
using QuestPDF.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
#if DEBUG
builder.Environment.EnvironmentName = "Development";
#endif
QuestPDF.Settings.License = LicenseType.Community;

var env = builder.Environment;

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularDev",
        policy => policy.SetIsOriginAllowed(origin => new Uri(origin).Host == "localhost")
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials());
});

builder.Configuration.SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true);

if (env.IsDevelopment())
{
    builder.Configuration.AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: true);
}

// Add Controllers
builder.Services.AddControllers();


// Database Configuration
builder.Services.AddDatabase(builder.Configuration);

//Add vaidation
builder.Services.AddValidation();

// JWT Authentication
builder.Services.AddJwtAuthentication(builder.Configuration);


// Authorization
builder.Services.AddAuthorization();


// Dependency Injection
builder.Services.AddApplicationServices();

// Swagger Configuration
builder.Services.AddSwaggerDocumentation();

var app = builder.Build();


// Configure Middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();

    app.UseSwaggerUI();
}

app.UseGlobalExceptionHandler();

app.UseHttpsRedirection();

app.UseCors("AllowAngularDev");

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();