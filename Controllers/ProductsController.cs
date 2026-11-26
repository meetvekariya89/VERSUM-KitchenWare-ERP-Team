
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using VERSUM.KitchenWareERP.Data;
using VERSUM.KitchenWareERP.Models;

namespace VERSUM.KitchenWareERP.Controllers
{
    [Authorize(Roles = "Admin,Staff")]
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProductsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // LOAD CATEGORIES
        private async Task LoadCategories(int? selectedId = null)
        {
            var categories = await _context.Categories
                .AsNoTracking()
                .Where(c => c.IsActive ||
                    (selectedId.HasValue && c.Id == selectedId.Value))
                .OrderBy(c => c.Name)
                .ToListAsync();

            ViewBag.Categories = new SelectList(
                categories, "Id", "Name", selectedId);
        }

        // PRODUCT LIST AND SEARCH
        // ACCESS: ADMIN AND STAFF
        [HttpGet]
        public async Task<IActionResult> Index(string? search)
        {
            var query = _context.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();

                query = query.Where(p =>
                    p.Name.Contains(search) ||
                    p.ProductCode.Contains(search));
            }

            ViewBag.Search = search;

            var products = await query
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return View(products);
        }

        // CREATE PRODUCT PAGE
        // ACCESS: ADMIN ONLY
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create()
        {
            bool hasCategories = await _context.Categories
                .AnyAsync(c => c.IsActive);

            if (!hasCategories)
            {
                TempData["Error"] =
                    "Please add an active category first.";

                return RedirectToAction("Create", "Categories");
            }

            await LoadCategories();

            return View(new Product
            {
                IsActive = true,
                CurrentStock = 0,
                MinimumStock = 10
            });
        }

        // SAVE NEW PRODUCT
        // ACCESS: ADMIN ONLY
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("ProductCode,Name,CategoryId,Description,SellingPrice,CostPrice,MinimumStock,CurrentStock,IsActive")]
            Product product)
        {
            ModelState.Remove(nameof(Product.Category));

            if (await _context.Products.AnyAsync(p =>
                p.ProductCode == product.ProductCode))
            {
                ModelState.AddModelError(
                    nameof(Product.ProductCode),
                    "Product code already exists.");
            }

            bool validCategory = await _context.Categories
                .AnyAsync(c =>
                    c.Id == product.CategoryId &&
                    c.IsActive);

            if (!validCategory)
            {
                ModelState.AddModelError(
                    nameof(Product.CategoryId),
                    "Please select a valid active category.");
            }

            ValidateProduct(product);

            if (!ModelState.IsValid)
            {
                await LoadCategories(product.CategoryId);
                return View(product);
            }

            product.CreatedAt = DateTime.UtcNow;

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Product added successfully.";

            return RedirectToAction(nameof(Index));
        }

        // EDIT PRODUCT PAGE
        // ACCESS: ADMIN ONLY
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var product = await _context.Products.FindAsync(id);

            if (product == null)
                return NotFound();

            await LoadCategories(product.CategoryId);

            return View(product);
        }

        // UPDATE PRODUCT AND STOCK
        // ACCESS: ADMIN ONLY
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("Id,ProductCode,Name,CategoryId,Description,SellingPrice,CostPrice,MinimumStock,CurrentStock,IsActive")]
            Product input)
        {
            if (id != input.Id)
                return BadRequest();

            ModelState.Remove(nameof(Product.Category));

            var product = await _context.Products.FindAsync(id);

            if (product == null)
                return NotFound();

            bool duplicateCode = await _context.Products
                .AnyAsync(p =>
                    p.ProductCode == input.ProductCode &&
                    p.Id != id);

            if (duplicateCode)
            {
                ModelState.AddModelError(
                    nameof(Product.ProductCode),
                    "Product code already exists.");
            }

            bool validCategory = await _context.Categories
                .AnyAsync(c =>
                    c.Id == input.CategoryId &&
                    (c.IsActive || c.Id == product.CategoryId));

            if (!validCategory)
            {
                ModelState.AddModelError(
                    nameof(Product.CategoryId),
                    "Please select a valid category.");
            }

            ValidateProduct(input);

            if (!ModelState.IsValid)
            {
                await LoadCategories(input.CategoryId);
                return View(input);
            }

            product.ProductCode = input.ProductCode;
            product.Name = input.Name;
            product.CategoryId = input.CategoryId;
            product.Description = input.Description;
            product.CostPrice = input.CostPrice;
            product.SellingPrice = input.SellingPrice;
            product.MinimumStock = input.MinimumStock;
            product.CurrentStock = input.CurrentStock;
            product.IsActive = input.IsActive;

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Product and stock updated successfully.";

            return RedirectToAction(nameof(Index));
        }

        // DELETE CONFIRMATION PAGE
        // ACCESS: ADMIN ONLY
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _context.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
                return NotFound();

            return View(product);
        }

        // DELETE PRODUCT
        // ACCESS: ADMIN ONLY
        [HttpPost]
        [ActionName("Delete")]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.Products.FindAsync(id);

            if (product == null)
                return NotFound();

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Product deleted successfully.";

            return RedirectToAction(nameof(Index));
        }

        // SHARED VALIDATION
        private void ValidateProduct(Product product)
        {
            if (product.CostPrice < 0)
            {
                ModelState.AddModelError(
                    nameof(Product.CostPrice),
                    "Cost price cannot be negative.");
            }

            if (product.SellingPrice < 0)
            {
                ModelState.AddModelError(
                    nameof(Product.SellingPrice),
                    "Selling price cannot be negative.");
            }

            if (product.MinimumStock < 0)
            {
                ModelState.AddModelError(
                    nameof(Product.MinimumStock),
                    "Minimum stock cannot be negative.");
            }

            if (product.CurrentStock < 0)
            {
                ModelState.AddModelError(
                    nameof(Product.CurrentStock),
                    "Current stock cannot be negative.");
            }
        }
    }
}
