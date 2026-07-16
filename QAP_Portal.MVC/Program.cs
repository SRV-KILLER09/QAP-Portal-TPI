using QAP_Portal.MVC.Services;

var builder = WebApplication.CreateBuilder(args);

// MVC + Razor views
builder.Services.AddControllersWithViews();

// Session is used by HomeController/QapController/QapApprovalController
// to remember the fake "logged in" role + email (see HomeController.SetRole).
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(4);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Typed HttpClient that talks to QAP.Portal.API.
// Base URL comes from appsettings.json ("ApiBaseUrl"), e.g. https://localhost:7290/api/
builder.Services.AddHttpClient<IQapApiService, QapApiService>((sp, client) =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var apiBaseUrl = config["ApiBaseUrl"]
        ?? throw new InvalidOperationException("ApiBaseUrl is not configured in appsettings.json.");
    client.BaseAddress = new Uri(apiBaseUrl);
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();