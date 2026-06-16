using AITrainingSystem.API.Extensions;
using QuestPDF.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
QuestPDF.Settings.License = LicenseType.Community;

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularDev",
        policy => policy.WithOrigins("http://localhost:4200")
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials());
});

if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
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