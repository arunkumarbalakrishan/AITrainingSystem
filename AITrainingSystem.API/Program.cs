using AITrainingSystem.API.Extensions;

var builder = WebApplication.CreateBuilder(args);


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

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();