using Microsoft.EntityFrameworkCore;
using SlotRace.Infrastructure.Persistence;
using SlotRace.Application.Interfaces;
using SlotRace.Infrastructure.Repositories;
using SlotRace.Application.Common.Options;
using SlotRace.API.Options;
using SlotRace.Application.Mapping;
using SlotRace.Infrastructure.Security;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));


// הגדרות: כל חלק בקונפיגורציה נקשר למחלקה מוקלדת, ומוזרק כ-IOptions<T>.
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.Configure<RegistrationOptions>(builder.Configuration.GetSection(RegistrationOptions.SectionName));
builder.Services.Configure<CorsOptions>(builder.Configuration.GetSection(CorsOptions.SectionName));

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IStudentRepository, StudentRepository>();
builder.Services.AddScoped<ISlotRepository, SlotRepository>();

// אבטחה: כל ממשק מ-Application מקבל את המימוש שלו מ-Infrastructure
builder.Services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();

// שעון המערכת. Singleton כי אין לו מצב. בבדיקות מחליפים אותו בשעון מזויף
builder.Services.AddSingleton(TimeProvider.System);

// AutoMapper: רישום ה-Profile, ו-IMapper יוזרק לשירותים
builder.Services.AddAutoMapper(c =>
{
    c.AddProfile<MappingProfile>();
});

var app = builder.Build();
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
