using AceJobAgencyPortal.Model;
using AceJobAgencyPortal.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();

builder.Services.AddDbContext<AuthDbContext>(options =>
{
	options.UseSqlServer(builder.Configuration.GetConnectionString("AuthConnectionString"));
});

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
	options.Password.RequiredLength = 12;
	options.Password.RequireUppercase = true;
	options.Password.RequireLowercase = true;
	options.Password.RequireDigit = true;
	options.Password.RequireNonAlphanumeric = true;

	options.User.RequireUniqueEmail = true;

	
	options.Lockout.AllowedForNewUsers = true;
	options.Lockout.MaxFailedAccessAttempts = 3;
	options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(2);

	options.SignIn.RequireConfirmedAccount = false;
	options.Tokens.AuthenticatorTokenProvider = TokenOptions.DefaultEmailProvider;
})
.AddEntityFrameworkStores<AuthDbContext>()
.AddDefaultTokenProviders(); 

builder.Services.AddScoped<IPasswordValidator<ApplicationUser>, StrongPasswordValidator>();

builder.Services.ConfigureApplicationCookie(options =>
{
	options.LoginPath = "/Login";
	options.AccessDeniedPath = "/Error?code=403";
	options.ExpireTimeSpan = TimeSpan.FromMinutes(1);
	options.SlidingExpiration = true;

	options.Events = new CookieAuthenticationEvents
	{
		OnRedirectToAccessDenied = ctx =>
		{
			ctx.Response.Redirect("/Error?code=403");
			return Task.CompletedTask;
		},

		OnValidatePrincipal = async ctx =>
		{
			var userManager = ctx.HttpContext.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
			var userId = ctx.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
			var sid = ctx.Principal?.FindFirst("sid")?.Value;

			if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(sid)) return;

			var user = await userManager.FindByIdAsync(userId);
			if (user == null || user.ActiveSessionId != sid)
			{
				ctx.RejectPrincipal();
				await ctx.HttpContext.SignOutAsync();
			}
		}
	};
});

builder.Services.AddHttpClient();
builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<NricProtector>();
builder.Services.AddScoped<AuditLogger>();
builder.Services.AddScoped<RecaptchaV3>();
builder.Services.AddScoped<PasswordPolicyService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
	var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
	if (!await roleManager.RoleExistsAsync("Admin"))
		await roleManager.CreateAsync(new IdentityRole("Admin"));
}

app.UseExceptionHandler("/Error?code=500");
if (!app.Environment.IsDevelopment())
{
	app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/Error", "?code={0}");

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();
app.Run();
