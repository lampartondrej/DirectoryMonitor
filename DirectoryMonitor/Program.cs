using DirectoryMonitor.Infrastructure;
using DirectoryMonitor.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

var snapshotPath = Path.Combine(builder.Environment.ContentRootPath, "App_Data", "Snapshots");
builder.Services.AddInfrastructure(snapshotPath);

var app = builder.Build();

// Centralized exception handling must come first so it wraps everything below.
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

