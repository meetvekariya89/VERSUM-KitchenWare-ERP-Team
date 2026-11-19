
using System.ComponentModel.DataAnnotations;
using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using VERSUM.KitchenWareERP.Data;
using VERSUM.KitchenWareERP.Models;

namespace VERSUM.KitchenWareERP.Controllers
{
    public class SaleLineInput
    {
        public int ProductId { get; set; }

        [Range(1, 999999999)]
        public int Quantity { get; set; } = 1;
    }

    public class SaleInput
    {
        [Required]
        public int DealerId { get; set; }

        [DataType(DataType.Date)]
        public DateTime SaleDate { get; set; } = DateTime.Today;

        [StringLength(500)]
        public string? Remarks { get; set; }

        public List<SaleLineInput> Items { get; set; } = new()
        {
            new SaleLineInput()
        };
    }

    public class SalesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SalesController(ApplicationDbContext context)
        {
            _context = context;
        }

        private async Task LoadDropdowns()
        {
            var dealers = await _context.Dealers
                .AsNoTracking()
                .Where(d => d.IsActive)
                .OrderBy(d => d.Name)
                .ToListAsync();

            var products = await _context.Products
                .AsNoTracking()
                .Where(p => p.IsActive)
                .OrderBy(p => p.Name)
                .ToListAsync();

            ViewBag.Dealers = new SelectList(
                dealers, "Id", "Name");

            ViewBag.Products = products;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? search)
        {
            var query = _context.Sales
                .AsNoTracking()
                .Include(s => s.Dealer)
                .Include(s => s.Items)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();

                query = query.Where(s =>
                    s.InvoiceNumber.Contains(search) ||
                    (s.Dealer != null &&
                     s.Dealer.Name.Contains(search)));
            }

            ViewBag.Search = search;

            var sales = await query
                .OrderByDescending(s => s.SaleDate)
                .ThenByDescending(s => s.Id)
                .ToListAsync();

            return View(sales);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            bool hasDealer = await _context.Dealers
                .AnyAsync(d => d.IsActive);

            if (!hasDealer)
            {
                TempData["Error"] =
                    "Please add an active dealer first.";

                return RedirectToAction("Create", "Dealers");
            }

            bool hasProduct = await _context.Products
                .AnyAsync(p => p.IsActive);

            if (!hasProduct)
            {
                TempData["Error"] =
                    "Please add an active product first.";

                return RedirectToAction("Create", "Products");
            }

            await LoadDropdowns();

            return View(new SaleInput());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SaleInput input)
        {
            bool validDealer = await _context.Dealers
                .AnyAsync(d =>
                    d.Id == input.DealerId &&
                    d.IsActive);

            if (!validDealer)
            {
                ModelState.AddModelError(
                    nameof(input.DealerId),
                    "Please select an active dealer.");
            }

            if (input.SaleDate == default)
            {
                ModelState.AddModelError(
                    nameof(input.SaleDate),
                    "Please enter a valid sale date.");
            }

            if (input.Items == null ||
                input.Items.Count == 0 ||
                input.Items.Count > 30)
            {
                ModelState.AddModelError(
                    "",
                    "Enter between 1 and 30 products.");
            }
            else
            {
                var productIds = input.Items
                    .Select(i => i.ProductId)
                    .ToList();

                if (productIds.Distinct().Count() != productIds.Count)
                {
                    ModelState.AddModelError(
                        "",
                        "Select each product only once.");
                }

                foreach (var item in input.Items)
                {
                    if (item.ProductId <= 0 || item.Quantity <= 0)
                    {
                        ModelState.AddModelError(
                            "",
                            "Select a product and enter a valid quantity.");
                    }
                }
            }

            if (!ModelState.IsValid)
            {
                await LoadDropdowns();
                return View(input);
            }

            await using var transaction =
                await _context.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable);

            try
            {
                var sale = new Sale
                {
                    InvoiceNumber = "INV-" +
                        DateTime.UtcNow.ToString("yyyyMMddHHmmssfff") +
                        "-" + Guid.NewGuid().ToString("N")[..6],

                    DealerId = input.DealerId,
                    SaleDate = input.SaleDate,
                    Remarks = input.Remarks,
                    CreatedAt = DateTime.UtcNow
                };

                foreach (var line in input.Items)
                {
                    var product = await _context.Products
                        .FirstOrDefaultAsync(p =>
                            p.Id == line.ProductId &&
                            p.IsActive);

                    if (product == null)
                    {
                        ModelState.AddModelError(
                            "",
                            "One or more selected products are unavailable.");

                        await LoadDropdowns();
                        return View(input);
                    }

                    if (product.CurrentStock < line.Quantity)
                    {
                        ModelState.AddModelError(
                            "",
                            "Insufficient stock for " + product.Name +
                            ". Available: " + product.CurrentStock);

                        await LoadDropdowns();
                        return View(input);
                    }

                    decimal lineTotal =
                        Math.Round(
                            line.Quantity * product.SellingPrice,
                            2,
                            MidpointRounding.AwayFromZero);

                    sale.Items.Add(new SaleItem
                    {
                        ProductId = product.Id,
                        Quantity = line.Quantity,
                        UnitPrice = product.SellingPrice,
                        LineTotal = lineTotal
                    });

                    sale.Subtotal += lineTotal;

                    int previousStock = product.CurrentStock;

                    product.CurrentStock -= line.Quantity;

                    _context.StockMovements.Add(new StockMovement
                    {
                        ItemType = "Product",
                        ItemId = product.Id,
                        MovementType = "OUT",
                        Quantity = line.Quantity,
                        PreviousStock = previousStock,
                        NewStock = product.CurrentStock,
                        Remarks = "Sales Invoice " + sale.InvoiceNumber,
                        CreatedAt = DateTime.UtcNow
                    });
                }

                sale.GrandTotal = sale.Subtotal;

                _context.Sales.Add(sale);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["Success"] =
                    "Sales invoice created and stock updated.";

                return RedirectToAction(
                    nameof(Details),
                    new { id = sale.Id });
            }
            catch (DbUpdateException)
            {
                await transaction.RollbackAsync();

                ModelState.AddModelError(
                    "",
                    "Unable to save the invoice. Please try again.");

                await LoadDropdowns();
                return View(input);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var sale = await _context.Sales
                .AsNoTracking()
                .Include(s => s.Dealer)
                .Include(s => s.Items)
                    .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (sale == null)
                return NotFound();

            return View(sale);
        }

        [HttpGet]
        public async Task<IActionResult> Print(int id)
        {
            var sale = await _context.Sales
                .AsNoTracking()
                .Include(s => s.Dealer)
                .Include(s => s.Items)
                    .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (sale == null)
                return NotFound();

            return View(sale);
        }
    }
}
