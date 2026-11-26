
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VERSUM.KitchenWareERP.Data;
using VERSUM.KitchenWareERP.Models;

namespace VERSUM.KitchenWareERP.Controllers
{
    [Authorize(Roles = "Admin,Staff")]
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReportsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // REPORTS AND ANALYTICS
        // ACCESS: ADMIN AND STAFF
        public async Task<IActionResult> Index(
            DateTime? fromDate, DateTime? toDate)
        {
            DateTime from = (fromDate ?? DateTime.Today.AddMonths(-5))
                .Date;
            DateTime to = (toDate ?? DateTime.Today).Date;

            if (from > to)
            {
                TempData["Error"] =
                    "From date cannot be later than To date.";

                return RedirectToAction(nameof(Index));
            }

            DateTime endExclusive = to.AddDays(1);

            var sales = await _context.Sales
                .AsNoTracking()
                .Include(s => s.Dealer)
                .Include(s => s.Items)
                    .ThenInclude(i => i.Product)
                .Where(s => s.SaleDate >= from &&
                            s.SaleDate < endExclusive)
                .ToListAsync();

            var purchases = await _context.Purchases
                .AsNoTracking()
                .Include(p => p.RawMaterial)
                .Where(p => p.PurchaseDate >= from &&
                            p.PurchaseDate < endExclusive)
                .ToListAsync();

            var products = await _context.Products
                .AsNoTracking()
                .ToListAsync();

            var materials = await _context.RawMaterials
                .AsNoTracking()
                .ToListAsync();

            var production = await _context.ProductionBatches
                .AsNoTracking()
                .Where(p => p.ProductionDate >= from &&
                            p.ProductionDate < endExclusive)
                .ToListAsync();

            var dealers = await _context.Dealers
                .AsNoTracking()
                .ToListAsync();

            var model = new ReportsViewModel
            {
                FromDate = from,
                ToDate = to,

                TotalSales = sales.Sum(s => s.GrandTotal),
                TotalPurchases = purchases.Sum(p => p.TotalAmount),

                TotalInvoices = sales.Count,
                TotalPurchaseOrders = purchases.Count,

                TotalProducts = products.Count,
                TotalDealers = dealers.Count,

                FinishedStockValue = products.Sum(
                    p => p.CurrentStock * p.CostPrice),

                RawMaterialStockValue = materials.Sum(
                    m => m.CurrentStock * m.CostPerUnit),

                LowStockProducts = products
                    .Where(p => p.IsActive &&
                        p.CurrentStock <= p.MinimumStock)
                    .OrderBy(p => p.CurrentStock)
                    .ToList(),

                LowStockMaterials = materials
                    .Where(m => m.IsActive &&
                        m.CurrentStock <= m.MinimumStock)
                    .OrderBy(m => m.CurrentStock)
                    .ToList(),

                TotalProductionBatches = production.Count,

                TotalUnitsProduced = production.Sum(
                    p => p.QuantityProduced),

                RecentSales = sales
                    .OrderByDescending(s => s.SaleDate)
                    .ThenByDescending(s => s.Id)
                    .Take(8)
                    .ToList(),

                TopProducts = sales
                    .SelectMany(s => s.Items)
                    .GroupBy(i => new
                    {
                        i.ProductId,
                        i.Product.Name
                    })
                    .Select(g => new ProductSalesReport
                    {
                        ProductName = g.Key.Name,
                        QuantitySold = g.Sum(i => i.Quantity),
                        Revenue = g.Sum(i => i.LineTotal)
                    })
                    .OrderByDescending(x => x.Revenue)
                    .Take(5)
                    .ToList(),

                MonthlySales = sales
                    .GroupBy(s => new
                    {
                        s.SaleDate.Year,
                        s.SaleDate.Month
                    })
                    .Select(g => new MonthlySalesReport
                    {
                        Month = new DateTime(
                            g.Key.Year, g.Key.Month, 1),
                        Amount = g.Sum(s => s.GrandTotal)
                    })
                    .OrderBy(x => x.Month)
                    .ToList(),

                DealerSales = sales
                    .GroupBy(s => new
                    {
                        s.DealerId,
                        s.Dealer!.Name
                    })
                    .Select(g => new DealerSalesReport
                    {
                        DealerName = g.Key.Name,
                        InvoiceCount = g.Count(),
                        TotalAmount = g.Sum(s => s.GrandTotal)
                    })
                    .OrderByDescending(x => x.TotalAmount)
                    .Take(5)
                    .ToList()
            };

            return View(model);
        }
    }

    public class ReportsViewModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }

        public decimal TotalSales { get; set; }
        public decimal TotalPurchases { get; set; }

        public int TotalInvoices { get; set; }
        public int TotalPurchaseOrders { get; set; }

        public int TotalProducts { get; set; }
        public int TotalDealers { get; set; }

        public decimal FinishedStockValue { get; set; }
        public decimal RawMaterialStockValue { get; set; }

        public int TotalProductionBatches { get; set; }
        public int TotalUnitsProduced { get; set; }

        public List<Product> LowStockProducts { get; set; } = new();
        public List<RawMaterial> LowStockMaterials { get; set; } = new();

        public List<Sale> RecentSales { get; set; } = new();
        public List<ProductSalesReport> TopProducts { get; set; } = new();
        public List<MonthlySalesReport> MonthlySales { get; set; } = new();
        public List<DealerSalesReport> DealerSales { get; set; } = new();
    }

    public class ProductSalesReport
    {
        public string ProductName { get; set; } = "";
        public int QuantitySold { get; set; }
        public decimal Revenue { get; set; }
    }

    public class MonthlySalesReport
    {
        public DateTime Month { get; set; }
        public decimal Amount { get; set; }
    }

    public class DealerSalesReport
    {
        public string DealerName { get; set; } = "";
        public int InvoiceCount { get; set; }
        public decimal TotalAmount { get; set; }
    }
}
