using Microsoft.EntityFrameworkCore;
using BackgroundService.Data;
using BackgroundService.Hubs;
using BackgroundService.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<BackgroundServiceContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("BackgroundServiceContext") ?? throw new InvalidOperationException("Connection string 'BackgroundServiceContext' not found.")));

// Permet d'obtenir des erreurs de BD plus claires et même d'appliquer des migrations manquantes
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<IdentityUser>()
    .AddEntityFrameworkStores<BackgroundServiceContext>();

// Add services to the container.
builder.Services.AddControllersWithViews();

//builder.Services.AddHostedService<Game>();
builder.Services.AddSingleton<Game>();
builder.Services.AddHostedService<Game>(p => p.GetService<Game>());

builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", builder => builder
        .WithOrigins("http://localhost:4200", "https://localhost:4200")
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials());
});

string? urls = builder.Configuration["ASPNETCORE_URLS"];
if (urls != null)
{
    string[] serverAdresses = urls.Split(";");

    SymmetricSecurityKey signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("C'est tellement la meilleure cle qui a jamais ete cree dans l'histoire de l'humanite (doit etre longue)"));

    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
    }).AddJwtBearer(options =>
    {
        options.SaveToken = true;
        // TODO: Seulement lors du developement
        options.RequireHttpsMetadata = false;
        options.TokenValidationParameters = new TokenValidationParameters()
        {
            ValidateAudience = false,
            ValidateIssuer = true,
            ValidIssuers = serverAdresses,
            ValidAudience = null,
            IssuerSigningKey = signingKey
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];

                // If the request is for our hub...
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) &&
                    (path.StartsWithSegments("/game")))
                {
                    // Read the token out of the query string
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });
}

builder.Services.AddSignalR();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseCors("CorsPolicy");

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

app.MapHub<GameHub>("/game");

app.Run();
