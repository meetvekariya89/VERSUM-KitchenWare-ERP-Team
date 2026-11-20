
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Data;
using VERSUM.KitchenWareERP.Data;
using VERSUM.KitchenWareERP.Models;

namespace VERSUM.KitchenWareERP.Controllers
{
    public class ProductionController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProductionController(ApplicationDbContext context)
        {
            _context = context;
        }

        private async Task LoadDropdowns()
        {
            var products = await _context.Products
                .AsNoTracking()
                .Where(p => p.IsActive)
                .OrderBy(p => p.Name)
                .ToListAsync();

            var materials = await _context.RawMaterials
                .AsNoTracking()
                .Where(m => m.IsActive)
                .OrderBy(m => m.Name)
                .ToListAsync();

            ViewBag.Products = new SelectList(
                products, "Id", "Name");

            ViewBag.Materials = new SelectList(
                materials, "Id", "Name");
        }

        // PRODUCTION HISTORY
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var batches = await _context.ProductionBatches
                .AsNoTracking()
                .OrderByDescending(b => b.ProductionDate)
                .ThenByDescending(b => b.Id)
                .ToListAsync();

            ViewBag.ProductNames = await _context.Products
                .AsNoTracking()
                .ToDictionaryAsync(p => p.Id, p => p.Name);

            ViewBag.MaterialNames = await _context.RawMaterials
                .AsNoTracking()
                .ToDictionaryAsync(m => m.Id, m => m.Name);

            return View(batches);
        }

        // NEW PRODUCTION PAGE
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await LoadDropdowns();

            return View(new ProductionBatch
            {
                BatchNumber = "BATCH-" +
                    DateTime.UtcNow.ToString("yyyyMMddHHmmss")
            });
        }

        // SAVE PRODUCTION
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("BatchNumber,ProductId,RawMaterialId,QuantityProduced,MaterialConsumed,Remarks")]
            ProductionBatch input)
        {
            if (string.IsNullOrWhiteSpace(input.BatchNumber))
            {
                ModelState.AddModelError(
                    nameof(input.BatchNumber),
                    "Batch number is required.");
            }

            if (input.QuantityProduced <= 0)
            {
                ModelState.AddModelError(
                    nameof(input.QuantityProduced),
                    "Produced quantity must be greater than zero.");
            }

            if (input.MaterialConsumed <= 0 ||
                input.MaterialConsumed !=
                    decimal.Round(input.MaterialConsumed, 3))
            {
                ModelState.AddModelError(
                    nameof(input.MaterialConsumed),
                    "Enter a positive quantity with up to 3 decimal places.");
            }

            if (input.ProductId <= 0)
            {
                ModelState.AddModelError(
                    nameof(input.ProductId),
                    "Select a finished product.");
            }

            if (input.RawMaterialId <= 0)
            {
                ModelState.AddModelError(
                    nameof(input.RawMaterialId),
                    "Select a raw material.");
            }

            if (!ModelState.IsValid)
            {
                await LoadDropdowns();
                return View(input);
            }

            await using var transaction =
                await _context.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable);

            if (await _context.ProductionBatches
                .AnyAsync(b => b.BatchNumber == input.BatchNumber))
            {
                ModelState.AddModelError(
                    nameof(input.BatchNumber),
                    "Batch number already exists.");

                await LoadDropdowns();
                return View(input);
            }

            var product = await _context.Products
                .FirstOrDefaultAsync(p =>
                    p.Id == input.ProductId && p.IsActive);

            var material = await _context.RawMaterials
                .FirstOrDefaultAsync(m =>
                    m.Id == input.RawMaterialId && m.IsActive);

            if (product == null || material == null)
            {
                ModelState.AddModelError(
                    "",
                    "Select valid active product and raw material.");

                await LoadDropdowns();
                return View(input);
            }

            if (material.CurrentStock < input.MaterialConsumed)
            {
                ModelState.AddModelError(
                    nameof(input.MaterialConsumed),
                    "Insufficient raw material stock.");

                await LoadDropdowns();
                return View(input);
            }

            if ((long)product.CurrentStock +
                input.QuantityProduced > int.MaxValue)
            {
                ModelState.AddModelError(
                    nameof(input.QuantityProduced),
                    "Finished product stock exceeds the limit.");

                await LoadDropdowns();
                return View(input);
            }

            decimal oldMaterialStock = material.CurrentStock;
            int oldProductStock = product.CurrentStock;

            material.CurrentStock -= input.MaterialConsumed;
            product.CurrentStock += input.QuantityProduced;

            input.ProductionDate = DateTime.UtcNow;

            _context.ProductionBatches.Add(input);

            _context.StockMovements.Add(new StockMovement
            {
                ItemType = "RawMaterial",
                ItemId = material.Id,
                MovementType = "OUT",
                Quantity = input.MaterialConsumed,
                PreviousStock = oldMaterialStock,
                NewStock = material.CurrentStock,
                Remarks = "Production " + input.BatchNumber,
                CreatedAt = DateTime.UtcNow
            });

            _context.StockMovements.Add(new StockMovement
            {
                ItemType = "Product",
                ItemId = product.Id,
                MovementType = "IN",
                Quantity = input.QuantityProduced,
                PreviousStock = oldProductStock,
                NewStock = product.CurrentStock,
                Remarks = "Production " + input.BatchNumber,
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            TempData["Success"] =
                "Production batch completed successfully.";

            return RedirectToAction(nameof(Index));
        }
    }
}
