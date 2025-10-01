using LiveStream.API;
using LiveStream.APPLICATION;
using LiveStream.APPLICATION.Interfaces;
using LiveStream.INFRASTRUCTURE;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
// Add SignalR
builder.Services.AddSignalR();

//// Add Authentication
//builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
//    .AddJwtBearer(options =>
//    {
//        options.TokenValidationParameters = new TokenValidationParameters
//        {
//            ValidateIssuerSigningKey = true,
//            // Use a proper secret key from configuration
//            IssuerSigningKey = new SymmetricSecurityKey(
//                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SecretKey"] ??
//                throw new ArgumentNullException("Jwt:SecretKey"))),
//            ValidateIssuer = true,
//            ValidateAudience = true,
//            ValidIssuer = builder.Configuration["Jwt:Issuer"],
//            ValidAudience = builder.Configuration["Jwt:Audience"],
//            ClockSkew = TimeSpan.Zero
//        };

//        // SignalR JWT support for WebSockets
//        options.Events = new JwtBearerEvents
//        {
//            OnMessageReceived = context =>
//            {
//                var accessToken = context.Request.Query["access_token"];
//                var path = context.Request.Path;

//                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/streamhub"))
//                {
//                    context.Token = accessToken;
//                }
//                return Task.CompletedTask;
//            }
//        };
//    });

//// Add Authorization
//builder.Services.AddAuthorization(options =>
//{
//    options.AddPolicy("DeviceAccess", policy =>
//        policy.RequireAuthenticatedUser());

//    options.AddPolicy("AdminOnly", policy =>
//        policy.RequireRole("Admin"));

//    options.AddPolicy("ReporterOrAbove", policy =>
//        policy.RequireRole("Admin", "Reporter"));
//});



// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("SignalRPolicy", policy =>
    {
        policy.WithOrigins("http://localhost:4200", "https://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});
// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddApplicationServices().AddInfrastructureServices();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient("janus", c =>
{
    c.BaseAddress = new Uri("http://localhost:8088/janus/");
});

builder.Services.AddScoped<IJanusService, JanusService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseCors("SignalRPolicy");
//app.UseHttpsRedirection();

//app.UseAuthentication();
//app.UseAuthorization();


app.MapControllers();
app.MapHub<StreamHub>("/streamhub");

app.Run();
