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
        policy => policy.SetIsOriginAllowed(origin => {
                            var host = new Uri(origin).Host;
                            return host == "localhost" || host.EndsWith(".vercel.app") || host.EndsWith("somee.com");
                         })
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

builder.Configuration.AddEnvironmentVariables();

// Add Controllers
builder.Services.AddControllers();
builder.Services.AddSignalR();


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
app.MapHub<AITrainingSystem.API.Hubs.NotificationHub>("/hubs/notifications");

app.Run();