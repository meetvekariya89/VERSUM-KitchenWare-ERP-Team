
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using VERSUM.KitchenWareERP.Data;
using VERSUM.KitchenWareERP.Models;

namespace VERSUM.KitchenWareERP.Controllers
{
    public class PurchasesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PurchasesController(ApplicationDbContext context)
        {
            _context = context;
        }

        private async Task LoadRawMaterials()
        {
            var materials = await _context.RawMaterials
                .AsNoTracking()
                .Where(r => r.IsActive)
                .OrderBy(r => r.Name)
                .ToListAsync();

            ViewBag.RawMaterials = new SelectList(
                materials, "Id", "Name");
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? search)
        {
            var query = _context.Purchases
                .AsNoTracking()
                .Include(p => p.RawMaterial)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();

                query = query.Where(p =>
                    p.PurchaseNumber.Contains(search) ||
                    p.SupplierName.Contains(search) ||
                    (p.InvoiceNumber != null &&
                     p.InvoiceNumber.Contains(search)) ||
                    (p.RawMaterial != null &&
                     p.RawMaterial.Name.Contains(search)));
            }

            ViewBag.Search = search;

            var purchases = await query
                .OrderByDescending(p => p.PurchaseDate)
                .ThenByDescending(p => p.Id)
                .ToListAsync();

            return View(purchases);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            if (!await _context.RawMaterials
                .AnyAsync(r => r.IsActive))
            {
                TempData["Error"] =
                    "Please add an active raw material first.";

                return RedirectToAction(
                    "Create", "RawMaterials");
            }

            await LoadRawMaterials();

            return View(new Purchase
            {
                PurchaseNumber =
                    "PUR-" + DateTime.UtcNow.ToString("yyyyMMddHHmmssfff"),

                PurchaseDate = DateTime.Today,
                Quantity = 1
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("PurchaseNumber,SupplierName,InvoiceNumber,RawMaterialId,Quantity,UnitPrice,PurchaseDate,Remarks")]
            Purchase purchase)
        {
            if (await _context.Purchases.AnyAsync(p =>
                p.PurchaseNumber == purchase.PurchaseNumber))
            {
                ModelState.AddModelError(
                    nameof(Purchase.PurchaseNumber),
                    "Purchase number already exists.");
            }

            bool validMaterial = await _context.RawMaterials
                .AnyAsync(r =>
                    r.Id == purchase.RawMaterialId &&
                    r.IsActive);

            if (!validMaterial)
            {
                ModelState.AddModelError(
                    nameof(Purchase.RawMaterialId),
                    "Please select an active raw material.");
            }

            if (purchase.PurchaseDate == default)
            {
                ModelState.AddModelError(
                    nameof(Purchase.PurchaseDate),
                    "Please enter a valid purchase date.");
            }

            if (purchase.Quantity <= 0)
            {
                ModelState.AddModelError(
                    nameof(Purchase.Quantity),
                    "Quantity must be greater than zero.");
            }

            if (purchase.UnitPrice < 0)
            {
                ModelState.AddModelError(
                    nameof(Purchase.UnitPrice),
                    "Unit price cannot be negative.");
            }

            if (!ModelState.IsValid)
            {
                await LoadRawMaterials();
                return View(purchase);
            }

            purchase.TotalAmount = Math.Round(
                purchase.Quantity * purchase.UnitPrice, 2);

            purchase.CreatedAt = DateTime.UtcNow;

            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                var material = await _context.RawMaterials
                    .FirstOrDefaultAsync(r =>
                        r.Id == purchase.RawMaterialId &&
                        r.IsActive);

                if (material == null)
                {
                    ModelState.AddModelError(
                        nameof(Purchase.RawMaterialId),
                        "Raw material is unavailable.");

                    await LoadRawMaterials();
                    return View(purchase);
                }

                decimal previousStock = material.CurrentStock;

                decimal newStock =
                    previousStock + purchase.Quantity;

                material.CurrentStock = newStock;

                _context.Purchases.Add(purchase);

                _context.StockMovements.Add(new StockMovement
                {
                    ItemType = "RawMaterial",
                    ItemId = material.Id,
                    MovementType = "IN",
                    Quantity = purchase.Quantity,
                    PreviousStock = previousStock,
                    NewStock = newStock,
                    Remarks = "Purchase " +
                        purchase.PurchaseNumber +
                        " from " + purchase.SupplierName,
                    CreatedAt = DateTime.UtcNow
                });

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["Success"] =
                    "Purchase saved and raw material stock updated.";

                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException)
            {
                await transaction.RollbackAsync();

                ModelState.AddModelError(
                    string.Empty,
                    "Could not save the purchase. Check the purchase number and try again.");

                await LoadRawMaterials();
                return View(purchase);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var purchase = await _context.Purchases
                .AsNoTracking()
                .Include(p => p.RawMaterial)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (purchase == null)
                return NotFound();

            return View(purchase);
        }
    }
}
