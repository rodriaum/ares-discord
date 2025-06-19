using Ares.Api.Filter;
using Ares.Api.HealthChecks;
using Ares.Api.Services.Core;
using Ares.Core.Constants;
using Ares.Core.Database.Postgres;
using Ares.Core.Database.Redis;
using Ares.Core.Manager;
using Ares.Core.Models.Database;
using Ares.Core.Repository;
using AspNetCoreRateLimit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;

namespace Ares.Api;

public class Startup
{
    public IConfiguration Configuration { get; }

    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    #region Main Service Configuration

    public void ConfigureServices(IServiceCollection services)
    {
        ConfigureProtectionServices(services);
        ConfigureInfrastructureServices(services);
        ConfigureApplicationServices(services);
        ConfigureSecurityServices(services);
        ConfigureLogging(services);
    }

    #endregion

    #region Application Services Configuration

    /// <summary>
    /// Configures application-specific services (Database, Repositories, Core Services)
    /// </summary>
    private void ConfigureApplicationServices(IServiceCollection services)
    {
        ConfigureDatabaseCredentials(services);
        ConfigureDatabaseConnections(services);
        ConfigureRepositories(services);
        ConfigureCoreServices(services);
    }

    /// <summary>
    /// Configures database credentials
    /// </summary>
    private void ConfigureDatabaseCredentials(IServiceCollection services)
    {
        // PostgreSQL Credentials
        services.AddSingleton(provider =>
        {
            IConfiguration configuration = provider.GetRequiredService<IConfiguration>();
            return new PostgresCredentials
            {
                Host = configuration["DatabaseSettings:Postgres:Host"],
                Port = int.Parse(configuration["DatabaseSettings:Postgres:Port"] ?? "5432"),
                User = configuration["DatabaseSettings:Postgres:User"],
                Password = configuration["DatabaseSettings:Postgres:Password"],
                Database = configuration["DatabaseSettings:Postgres:Database"]
            };
        });

        // Redis Credentials
        services.AddSingleton(provider =>
        {
            IConfiguration configuration = provider.GetRequiredService<IConfiguration>();
            return new RedisCredentials
            {
                Host = configuration["DatabaseSettings:Redis:Host"],
                Port = int.Parse(configuration["DatabaseSettings:Redis:Port"] ?? "6379"),
                Password = configuration["DatabaseSettings:Redis:Password"]
            };
        });
    }

    /// <summary>
    /// Configures database connections
    /// </summary>
    private void ConfigureDatabaseConnections(IServiceCollection services)
    {
        services.AddSingleton<PostgresDatabase>(provider =>
        {
            var credentials = provider.GetRequiredService<PostgresCredentials>();
            return new PostgresDatabase(credentials);
        });

        services.AddSingleton<RedisDatabase>(provider =>
        {
            var credentials = provider.GetRequiredService<RedisCredentials>();
            return new RedisDatabase(credentials);
        });
    }

    /// <summary>
    /// Configures repository services
    /// </summary>
    private void ConfigureRepositories(IServiceCollection services)
    {
        services.AddScoped<UserRepository>();
        services.AddScoped<GuildRepository>();
        services.AddScoped<ChatModelRepository>();
    }

    /// <summary>
    /// Configures core application services
    /// </summary>
    private void ConfigureCoreServices(IServiceCollection services)
    {
    services.AddSingleton<CoreService>();
    services.AddHostedService<CoreHostedService>();
    // Data Managers
    services.AddScoped<GuildDataManager>();
    services.AddScoped<UserDataManager>();
    services.AddScoped<ChatModelDataManager>();
    }

    #endregion

    #region Protection Services Configuration

    /// <summary>
    /// Configures rate limiting and protection services
    /// </summary>
    private void ConfigureProtectionServices(IServiceCollection services)
    {
        services.AddMemoryCache();
        services.Configure<IpRateLimitOptions>(Configuration.GetSection("IpRateLimiting"));
        services.Configure<IpRateLimitPolicies>(Configuration.GetSection("IpRateLimitPolicies"));
        services.AddInMemoryRateLimiting();
        services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
        services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
        services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>();
        services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
    }

    #endregion

    #region Infrastructure Services Configuration

    /// <summary>
    /// Configures infrastructure services (MVC, Swagger, HTTP clients, etc.)
    /// </summary>
    private void ConfigureInfrastructureServices(IServiceCollection services)
    {
        ConfigureResponseCompression(services);
        ConfigureControllers(services);
        ConfigureSwagger(services);
        ConfigureHttpClient(services);
    }

    /// <summary>
    /// Configures response compression
    /// </summary>
    private void ConfigureResponseCompression(IServiceCollection services)
    {
        services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;
            options.Providers.Add<GzipCompressionProvider>();
            options.Providers.Add<BrotliCompressionProvider>();
        });
    }

    /// <summary>
    /// Configures MVC controllers and filters
    /// </summary>
    private void ConfigureControllers(IServiceCollection services)
    {
        services.AddControllers(options =>
        {
            options.Filters.Add<ValidateModelStateFilter>();
            options.Filters.Add<SanitizeInputFilter>();
            options.CacheProfiles.Add("Default", new CacheProfile
            {
                Duration = 60,
                Location = ResponseCacheLocation.Any
            });
        });

        services.AddResponseCaching();
    }

    /// <summary>
    /// Configures Swagger documentation
    /// </summary>
    private void ConfigureSwagger(IServiceCollection services)
    {
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = $"{AppConstants.AppName} API", Version = AppConstants.AppVersion });
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT Authorization header using the Bearer scheme",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer"
            });
            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });
    }

    /// <summary>
    /// Configures HTTP client services
    /// </summary>
    private void ConfigureHttpClient(IServiceCollection services)
    {
        services.AddHttpClient();
    }

    #endregion

    #region Security Services Configuration

    /// <summary>
    /// Configures security services (Authentication, CORS, Health Checks)
    /// </summary>
    private void ConfigureSecurityServices(IServiceCollection services)
    {
        ConfigureAuthentication(services);
        ConfigureCors(services);
        ConfigureHealthChecks(services);
    }

    /// <summary>
    /// Configures JWT authentication
    /// </summary>
    private void ConfigureAuthentication(IServiceCollection services)
    {
        services.AddAuthentication(options =>
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
                ClockSkew = TimeSpan.Zero
            };
        });
    }

    /// <summary>
    /// Configures CORS policies
    /// </summary>
    private void ConfigureCors(IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddDefaultPolicy(builder =>
            {
                builder.WithOrigins(Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>())
                       .WithMethods(Configuration.GetSection("Cors:AllowedMethods").Get<string[]>() ?? Array.Empty<string>())
                       .WithHeaders(Configuration.GetSection("Cors:AllowedHeaders").Get<string[]>() ?? Array.Empty<string>())
                       .WithExposedHeaders(Configuration.GetSection("Cors:ExposedHeaders").Get<string[]>() ?? Array.Empty<string>())
                       .SetPreflightMaxAge(TimeSpan.FromSeconds(Configuration.GetValue<int>("Cors:MaxAge", 3600)));
            });
        });
    }

    /// <summary>
    /// Configures health checks
    /// </summary>
    private void ConfigureHealthChecks(IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck<SecurityHealthCheck>("security")
            .AddCheck<RateLimitHealthCheck>("rate_limit")
            .AddCheck<AuthenticationHealthCheck>("auth");
    }

    #endregion

    #region Logging Configuration

    /// <summary>
    /// Configures application logging
    /// </summary>
    private void ConfigureLogging(IServiceCollection services)
    {
        services.AddLogging(builder =>
        {
            builder.AddSerilog(new LoggerConfiguration()
                .ReadFrom.Configuration(Configuration)
                .CreateLogger());
        });
    }

    #endregion

    #region Application Pipeline Configuration

    /// <summary>
    /// Configures the HTTP request pipeline
    /// </summary>
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IServiceProvider serviceProvider, ILogger<Startup> logger)
    {
        ConfigureExceptionHandling(app, env);
        ConfigureLoggingMiddleware(app);
        ConfigureSecurityHeaders(app);
        ConfigureSwaggerUI(app);
        ConfigureRateLimiting(app);
        ConfigureHttpsAndRouting(app);
        ConfigureCorsMiddleware(app);
        ConfigureAuthentication(app);
        ConfigureResponseOptimization(app);
        ConfigureEndpoints(app);
    }

    /// <summary>
    /// Configures exception handling based on environment
    /// </summary>
    private void ConfigureExceptionHandling(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }
        else
        {
            app.UseExceptionHandler("/error");
            app.UseHsts();
        }
    }

    /// <summary>
    /// Configures request logging middleware
    /// </summary>
    private void ConfigureLoggingMiddleware(IApplicationBuilder app)
    {
        app.UseSerilogRequestLogging();
    }

    /// <summary>
    /// Configures security headers middleware
    /// </summary>
    private void ConfigureSecurityHeaders(IApplicationBuilder app)
    {
        app.Use(async (context, next) =>
        {
            context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
            context.Response.Headers.Append("X-Frame-Options", "DENY");
            context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
            context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
            context.Response.Headers.Append("Content-Security-Policy", "default-src 'self'");
            context.Response.Headers.Append("Permissions-Policy", "geolocation=(), microphone=(), camera=()");
            context.Response.Headers.Append("X-Permitted-Cross-Domain-Policies", "none");

            context.Response.Headers.Remove("Server");
            context.Response.Headers.Remove("X-Powered-By");

            await next();
        });
    }

    /// <summary>
    /// Configures Swagger UI
    /// </summary>
    private void ConfigureSwaggerUI(IApplicationBuilder app)
    {
        app.UseSwagger();
        app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", $"{AppConstants.AppName} API {AppConstants.AppVersion}"));
    }

    /// <summary>
    /// Configures rate limiting middleware
    /// </summary>
    private void ConfigureRateLimiting(IApplicationBuilder app)
    {
        app.UseIpRateLimiting();
    }

    /// <summary>
    /// Configures HTTPS redirection and routing
    /// </summary>
    private void ConfigureHttpsAndRouting(IApplicationBuilder app)
    {
        app.UseHttpsRedirection();
        app.UseRouting();
    }

    /// <summary>
    /// Configures CORS middleware
    /// </summary>
    private void ConfigureCorsMiddleware(IApplicationBuilder app)
    {
        app.UseCors();
    }

    /// <summary>
    /// Configures authentication and authorization middleware
    /// </summary>
    private void ConfigureAuthentication(IApplicationBuilder app)
    {
        app.UseAuthentication();
        app.UseAuthorization();
    }

    /// <summary>
    /// Configures response optimization middleware
    /// </summary>
    private void ConfigureResponseOptimization(IApplicationBuilder app)
    {
        app.UseResponseCaching();
        app.UseResponseCompression();
    }

    /// <summary>
    /// Configures application endpoints
    /// </summary>
    private void ConfigureEndpoints(IApplicationBuilder app)
    {
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
            endpoints.MapHealthChecks("/health");
        });
    }

    #endregion
}