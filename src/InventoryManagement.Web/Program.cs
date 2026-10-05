using InventoryManagement.Core.Interfaces;
using InventoryManagement.Data;
using InventoryManagement.Data.Repositories;
using InventoryManagement.Services;
using Microsoft.EntityFrameworkCore;



var builder = WebApplication.CreateBuilder(args);

// 1. إعداد اتصال قاعدة البيانات SQLite
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=inventory.db"));

// 2. تسجيل المستودعات والخدمات مع مساراتها الصحيحة
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddScoped<IReorderService, ReorderService>();
builder.Services.AddScoped<IReportingService, ReportingService>();

builder.Services.AddControllersWithViews();

var app = builder.Build();

// 3. تهيئة قاعدة البيانات فقط بدون SeedData إذا لم تكن موجودة
//using (var scope = app.Services.CreateScope())
//{
//    var services = scope.ServiceProvider;
//    var context = services.GetRequiredService<AppDbContext>();
//    context.Database.EnsureCreated();
//}

// تهيئة وبذر قاعدة البيانات تلقائياً
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<AppDbContext>();
    context.Database.EnsureCreated();
    
    // ⭐ استدعاء دالة بذر البيانات لكي تظهر قطع الغيار في الجدول
    await DataSeeder.SeedAsync(context);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

// تعيين الصفحة الافتراضية لتعرض المنتجات مباشرة
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Products}/{action=Index}/{id?}");

app.Run();