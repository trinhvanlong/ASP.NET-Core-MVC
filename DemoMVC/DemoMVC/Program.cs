using DemoMVC.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
    )
);
var app = builder.Build();

app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception exception)
    {
        if (!string.Equals(
                context.Request.Headers["X-Requested-With"],
                "XMLHttpRequest",
                StringComparison.OrdinalIgnoreCase))
        {
            throw;
        }

        var logger = context.RequestServices
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("AjaxExceptionHandler");
        logger.LogError(exception, "An AJAX request failed.");

        var databaseUnavailable =
            exception is SqlException ||
            exception is DbUpdateException { InnerException: SqlException };
        context.Response.Clear();
        context.Response.StatusCode = databaseUnavailable
            ? StatusCodes.Status503ServiceUnavailable
            : StatusCodes.Status500InternalServerError;
        await context.Response.WriteAsJsonAsync(new
        {
            success = false,
            message = databaseUnavailable
                ? "Không thể kết nối cơ sở dữ liệu. Vui lòng thử lại sau."
                : "Đã xảy ra lỗi khi xử lý yêu cầu. Vui lòng thử lại."
        });
    }
});

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
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
