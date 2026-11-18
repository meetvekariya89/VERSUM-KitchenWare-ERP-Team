
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VERSUM.KitchenWareERP.Data;
using VERSUM.KitchenWareERP.Models;

namespace VERSUM.KitchenWareERP.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var today = DateTime.Today;
            var startMonth = new DateTime(today.Year, today.Month, 1);
            var chartStart = startMonth.AddMonths(-5);
            var chartEnd = startMonth.AddMonths(1);

            var products = await _context.Products
                .AsNoTracking()
                .ToListAsync();

            var materials = await _context.RawMaterials
                .AsNoTracking()
                .ToListAsync();

            var monthlyInvoices = await _context.Sales
                .AsNoTracking()
                .Where(s => s.SaleDate >= chartStart &&
                            s.SaleDate < chartEnd)
                .Select(s => new
                {
                    s.SaleDate,
                    s.GrandTotal
                })
                .ToListAsync();

            var alerts = products
                .Where(p => p.IsActive &&
                    p.CurrentStock <= p.MinimumStock)
                .Select(p => new DashboardLowStock
                {
                    Name = p.Name,
                    Code = p.ProductCode,
                    Type = "Finished Product",
                    CurrentStock = p.CurrentStock,
                    MinimumStock = p.MinimumStock
                })
                .Concat(materials
                    .Where(m => m.IsActive &&
                        m.CurrentStock <= m.MinimumStock)
                    .Select(m => new DashboardLowStock
                    {
                        Name = m.Name,
                        Code = m.MaterialCode,
                        Type = "Raw Material",
                        CurrentStock = m.CurrentStock,
                        MinimumStock = m.MinimumStock
                    }))
                .OrderBy(x => x.CurrentStock)
                .Take(6)
                .ToList();

            var model = new DashboardViewModel
            {
                TotalProducts = products.Count,
                TotalCategories = await _context.Categories.CountAsync(),
                TotalRawMaterials = materials.Count,
                TotalDealers = await _context.Dealers.CountAsync(),
                TotalProductionBatches =
                    await _context.ProductionBatches.CountAsync(),
                TotalSalesInvoices = await _context.Sales.CountAsync(),

                LowStockProducts = products.Count(p =>
                    p.IsActive && p.CurrentStock <= p.MinimumStock),

                LowStockRawMaterials = materials.Count(m =>
                    m.IsActive && m.CurrentStock <= m.MinimumStock),

                TotalSales = await _context.Sales
                    .SumAsync(s => (decimal?)s.GrandTotal) ?? 0m,

                TotalPurchases = await _context.Purchases
                    .SumAsync(p => (decimal?)p.TotalAmount) ?? 0m,

                FinishedInventoryValue = products.Sum(p =>
                    p.CurrentStock * p.CostPrice),

                RecentProducts = await _context.Products
                    .AsNoTracking()
                    .Include(p => p.Category)
                    .OrderByDescending(p => p.CreatedAt)
                    .Take(5)
                    .ToListAsync(),

                RecentSales = await _context.Sales
                    .AsNoTracking()
                    .Include(s => s.Dealer)
                    .OrderByDescending(s => s.SaleDate)
                    .ThenByDescending(s => s.Id)
                    .Take(5)
                    .ToListAsync(),

                RecentPurchases = await _context.Purchases
                    .AsNoTracking()
                    .OrderByDescending(p => p.PurchaseDate)
                    .ThenByDescending(p => p.Id)
                    .Take(5)
                    .ToListAsync(),

                StockAlerts = alerts
            };

            for (int i = 0; i < 6; i++)
            {
                var month = chartStart.AddMonths(i);

                model.MonthlySales.Add(new DashboardMonth
                {
                    Label = month.ToString("MMM"),
                    Sales = monthlyInvoices
                        .Where(s => s.SaleDate.Year == month.Year &&
                                    s.SaleDate.Month == month.Month)
                        .Sum(s => s.GrandTotal)
                });
            }

            return View(model);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult Error()
        {
            return View();
        }
    }
}
