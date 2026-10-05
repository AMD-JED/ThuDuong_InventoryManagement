using InventoryManagement.Core.Interfaces;
using InventoryManagement.Services;
using Microsoft.AspNetCore.Mvc;

namespace InventoryManagement.Web.Controllers
{
    public class ProductsController : Controller
    {
        private readonly IProductRepository _productRepository;
        private readonly IReportingService _reportingService;

        // قائمة لتخزين الأعمدة الإضافية المخصصة
        public static List<string> CustomColumns = new List<string>();

        public ProductsController(IProductRepository productRepository, IReportingService reportingService)
        {
            _productRepository = productRepository;
            _reportingService = reportingService;
        }

        // صفحة عرض قائمة المنتجات
        public async Task<IActionResult> Index()
        {
            var products = await _productRepository.GetAllAsync();
            ViewBag.CustomColumns = CustomColumns;
            return View(products);
        }

        // صفحة لوحة التحكم (KPI Dashboard)
        public async Task<IActionResult> Dashboard()
        {
            var totalValue = await _reportingService.GetTotalInventoryValueAsync();
            ViewBag.TotalValue = totalValue;
            return View();
        }

        // دالة إضافة عمود جديد
        [HttpPost]
        public IActionResult AddCustomColumn(string columnName)
        {
            if (!string.IsNullOrEmpty(columnName) && !CustomColumns.Contains(columnName))
            {
                CustomColumns.Add(columnName);
            }
            
            TempData["SuccessMessage"] = $"Le colonne '{columnName}' a été ajouté avec succès !";
            return RedirectToAction("Index");
        }
    }
}