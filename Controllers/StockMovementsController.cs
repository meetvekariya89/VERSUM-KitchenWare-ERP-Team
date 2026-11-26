
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using VERSUM.KitchenWareERP.Data;
using VERSUM.KitchenWareERP.Models;

namespace VERSUM.KitchenWareERP.Controllers
{
    [Authorize(Roles = "Admin,Staff")]
    public class StockMovementsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public StockMovementsController(ApplicationDbContext context)
        {
            _context = context;
        }

        private async Task LoadItems()
        {
            ViewBag.Products = new SelectList(
                await _context.Products
                    .AsNoTracking()
                    .Where(p => p.IsActive)
                    .OrderBy(p => p.Name)
                    .ToListAsync(),
                "Id", "Name");

            ViewBag.RawMaterials = new SelectList(
                await _context.RawMaterials
                    .AsNoTracking()
                    .Where(m => m.IsActive)
                    .OrderBy(m => m.Name)
                    .ToListAsync(),
                "Id", "Name");
        }

        // TRANSACTION HISTORY
        // ACCESS: ADMIN AND STAFF
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var movements = await _context.StockMovements
                .AsNoTracking()
                .OrderByDescending(m => m.CreatedAt)
                .ThenByDescending(m => m.Id)
                .ToListAsync();

            var productNames = await _context.Products
                .AsNoTracking()
                .ToDictionaryAsync(p => p.Id, p => p.Name);

            var materialNames = await _context.RawMaterials
                .AsNoTracking()
                .ToDictionaryAsync(m => m.Id, m => m.Name);

            ViewBag.ProductNames = productNames;
            ViewBag.MaterialNames = materialNames;

            return View(movements);
        }

        // STOCK ENTRY PAGE
        // ACCESS: ADMIN AND STAFF
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await LoadItems();

            return View(new StockMovement
            {
                ItemType = "RawMaterial",
                MovementType = "IN"
            });
        }

        // SAVE STOCK MOVEMENT
        // ACCESS: ADMIN AND STAFF
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("ItemType,ItemId,MovementType,Quantity,Remarks")]
            StockMovement input)
        {
            if (input.ItemType != "Product" &&
                input.ItemType != "RawMaterial")
            {
                ModelState.AddModelError(
                    nameof(input.ItemType),
                    "Select a valid item type.");
            }

            if (input.MovementType != "IN" &&
                input.MovementType != "OUT")
            {
                ModelState.AddModelError(
                    nameof(input.MovementType),
                    "Select Stock In or Stock Out.");
            }

            if (input.Quantity <= 0)
            {
                ModelState.AddModelError(
                    nameof(input.Quantity),
                    "Quantity must be greater than zero.");
            }

            if (input.ItemId <= 0)
            {
                ModelState.AddModelError(
                    nameof(input.ItemId),
                    "Select an item.");
            }

            // Only user-entered fields are validated here.
            ModelState.Remove(nameof(input.PreviousStock));
            ModelState.Remove(nameof(input.NewStock));

            if (!ModelState.IsValid)
            {
                await LoadItems();
                return View(input);
            }

            // SERIALIZABLE prevents concurrent stock transactions
            // from reading and updating the same old balance.
            await using var transaction =
                await _context.Database.BeginTransactionAsync(
                    System.Data.IsolationLevel.Serializable);

            if (input.ItemType == "Product")
            {
                var product = await _context.Products
                    .FirstOrDefaultAsync(p =>
                        p.Id == input.ItemId && p.IsActive);

                if (product == null)
                {
                    ModelState.AddModelError(
                        nameof(input.ItemId),
                        "Product not found or inactive.");

                    await LoadItems();
                    return View(input);
                }

                if (input.Quantity != decimal.Truncate(input.Quantity) ||
                    input.Quantity > int.MaxValue)
                {
                    ModelState.AddModelError(
                        nameof(input.Quantity),
                        "Product quantity must be a whole number.");

                    await LoadItems();
                    return View(input);
                }

                int quantity = (int)input.Quantity;
                int previousStock = product.CurrentStock;

                if (input.MovementType == "OUT" &&
                    previousStock < quantity)
                {
                    ModelState.AddModelError(
                        nameof(input.Quantity),
                        "Insufficient product stock.");

                    await LoadItems();
                    return View(input);
                }

                if (input.MovementType == "IN" &&
                    (long)previousStock + quantity > int.MaxValue)
                {
                    ModelState.AddModelError(
                        nameof(input.Quantity),
                        "Product stock exceeds the supported limit.");

                    await LoadItems();
                    return View(input);
                }

                product.CurrentStock = input.MovementType == "IN"
                    ? previousStock + quantity
                    : previousStock - quantity;

                input.PreviousStock = previousStock;
                input.NewStock = product.CurrentStock;
            }
            else
            {
                var material = await _context.RawMaterials
                    .FirstOrDefaultAsync(m =>
                        m.Id == input.ItemId && m.IsActive);

                if (material == null)
                {
                    ModelState.AddModelError(
                        nameof(input.ItemId),
                        "Raw material not found or inactive.");

                    await LoadItems();
                    return View(input);
                }

                decimal previousStock = material.CurrentStock;

                if (input.Quantity !=
                    decimal.Round(input.Quantity, 3))
                {
                    ModelState.AddModelError(
                        nameof(input.Quantity),
                        "Use at most three decimal places.");

                    await LoadItems();
                    return View(input);
                }

                if (input.MovementType == "OUT" &&
                    previousStock < input.Quantity)
                {
                    ModelState.AddModelError(
                        nameof(input.Quantity),
                        "Insufficient raw material stock.");

                    await LoadItems();
                    return View(input);
                }

                decimal newStock = input.MovementType == "IN"
                    ? previousStock + input.Quantity
                    : previousStock - input.Quantity;

                if (newStock > 999999999999999m)
                {
                    ModelState.AddModelError(
                        nameof(input.Quantity),
                        "Stock exceeds the supported limit.");

                    await LoadItems();
                    return View(input);
                }

                material.CurrentStock = newStock;

                input.PreviousStock = previousStock;
                input.NewStock = newStock;
            }

            input.CreatedAt = DateTime.UtcNow;

            _context.StockMovements.Add(input);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            TempData["Success"] =
                "Stock transaction recorded successfully.";

            return RedirectToAction(nameof(Index));
        }
    }
}
