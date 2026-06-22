
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using R3AIA.Models;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using R3AIA.Services;
using AutoMapper;
using R3AIA.Mapping;
using R3AIA.Repositories;
using R3AIA.Data;
using System.IdentityModel.Tokens.Jwt;

namespace R3AIA
{  
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // منع إعادة تسمية الـ Claims (sub, nameidentifier, role .. إلخ)
            JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

            // Add services to the container.

            builder.Services.AddControllers().AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
            });
            // OpenAPI document (built-in)
            builder.Services.AddOpenApi();
            // Swagger UI
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            // CORS for mobile / frontend
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                });
            });
            builder.Services.AddDbContext<AppDbContext>(options =>
            {
                options.UseNpgsql(builder.Configuration.GetConnectionString("R3AIA"));
            });

            // Identity setup
            builder.Services
                .AddIdentity<ApplicationUser, IdentityRole>(options => {
                    options.User.RequireUniqueEmail = true;
                    // Lower password requirements to match frontend simplicity
                    options.Password.RequireDigit = false;
                    options.Password.RequiredLength = 6;
                    options.Password.RequireNonAlphanumeric = false;
                    options.Password.RequireUppercase = false;
                    options.Password.RequireLowercase = false;
                })
                .AddEntityFrameworkStores<AppDbContext>()
                .AddDefaultTokenProviders();

            // JWT setup
            builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));
            var jwtSection = builder.Configuration.GetSection("Jwt");
            var key = Encoding.UTF8.GetBytes(jwtSection["Key"]!);

            builder.Services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = jwtSection["Issuer"],
                        ValidAudience = jwtSection["Audience"],
                        IssuerSigningKey = new SymmetricSecurityKey(key)
                    };
                });

            builder.Services.AddScoped<IJwtService, JwtService>();
            builder.Services.AddScoped<IFileService, FileService>();
            builder.Services.AddScoped<INotificationService, NotificationService>();
            builder.Services.AddScoped<ICloudinaryService, CloudinaryService>();
            builder.Services.AddScoped<IAccountRepository, AccountRepository>();
            builder.Services.AddScoped<IMedicineRepository, MedicineRepository>();
            builder.Services.AddScoped<IDeliveryRepository, DeliveryRepository>();
            builder.Services.AddScoped<IDonationRepository, DonationRepository>();
            builder.Services.AddScoped<ISupportRepository, SupportRepository>();
            builder.Services.AddScoped<IMedicalRequestRepository, MedicalRequestRepository>();
            builder.Services.AddScoped<IVolunteerRepository, VolunteerRepository>();
            builder.Services.AddScoped<IAdminRepository, AdminRepository>();
            builder.Services.AddScoped<IPatientCaseRepository, PatientCaseRepository>();
            builder.Services.AddHttpContextAccessor();

            // AutoMapper
            builder.Services.AddAutoMapper(typeof(MappingProfile));


            builder.WebHost.UseUrls("http://0.0.0.0:5129");

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                // OpenAPI JSON
                app.MapOpenApi();
                // Swagger UI
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            if (!app.Environment.IsDevelopment())
            {
                app.UseHttpsRedirection();
            }

            // تفعيل الـ CORS لتسمح لتطبيق الموبايل بالاتصال بالسيرفر
            app.UseCors(options =>
            {
                options.AllowAnyOrigin();
                options.AllowAnyMethod();
                options.AllowAnyHeader();
            });

            app.UseStaticFiles(new StaticFileOptions
            {
                OnPrepareResponse = ctx =>
                {
                    ctx.Context.Response.Headers.Append("Access-Control-Allow-Origin", "*");
                    ctx.Context.Response.Headers.Append("Access-Control-Allow-Headers", "Origin, X-Requested-With, Content-Type, Accept");
                }
            });

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            // Seed roles and default admin
            app.SeedAsync().GetAwaiter().GetResult();

            // Health check endpoint for discovery
            app.MapGet("/api/health", () => Results.Ok(new { status = "ok", app = "R3AIA-API" })).AllowAnonymous();

            app.Run();
        }
    }
}
