
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VERSUM.KitchenWareERP.Data;
using VERSUM.KitchenWareERP.Models;

namespace VERSUM.KitchenWareERP.Controllers
{
    [Authorize(Roles = "Admin,Staff")]
    public class RawMaterialsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public RawMaterialsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // LIST AND SEARCH
        // ACCESS: ADMIN AND STAFF
        [HttpGet]
        public async Task<IActionResult> Index(string? search)
        {
            var query = _context.RawMaterials
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();

                query = query.Where(m =>
                    m.Name.Contains(search) ||
                    m.MaterialCode.Contains(search));
            }

            ViewBag.Search = search;

            return View(await query
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync());
        }

        // ADD PAGE
        // ACCESS: ADMIN ONLY
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View(new RawMaterial
            {
                Unit = "Kg",
                IsActive = true
            });
        }

        // SAVE MATERIAL
        // ACCESS: ADMIN ONLY
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("MaterialCode,Name,Unit,CurrentStock,MinimumStock,CostPerUnit,IsActive")]
            RawMaterial material)
        {
            if (await _context.RawMaterials.AnyAsync(m =>
                m.MaterialCode == material.MaterialCode))
            {
                ModelState.AddModelError(
                    nameof(RawMaterial.MaterialCode),
                    "Material code already exists.");
            }

            ValidateMaterial(material);

            if (!ModelState.IsValid)
                return View(material);

            material.CreatedAt = DateTime.UtcNow;

            _context.RawMaterials.Add(material);
            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Raw material added successfully.";

            return RedirectToAction(nameof(Index));
        }

        // EDIT PAGE
        // ACCESS: ADMIN ONLY
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var material = await _context.RawMaterials
                .FindAsync(id);

            if (material == null)
                return NotFound();

            return View(material);
        }

        // UPDATE MATERIAL
        // ACCESS: ADMIN ONLY
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("Id,MaterialCode,Name,Unit,CurrentStock,MinimumStock,CostPerUnit,IsActive")]
            RawMaterial input)
        {
            if (id != input.Id)
                return BadRequest();

            var material = await _context.RawMaterials
                .FindAsync(id);

            if (material == null)
                return NotFound();

            if (await _context.RawMaterials.AnyAsync(m =>
                m.MaterialCode == input.MaterialCode &&
                m.Id != id))
            {
                ModelState.AddModelError(
                    nameof(RawMaterial.MaterialCode),
                    "Material code already exists.");
            }

            ValidateMaterial(input);

            if (!ModelState.IsValid)
                return View(input);

            material.MaterialCode = input.MaterialCode;
            material.Name = input.Name;
            material.Unit = input.Unit;
            material.CurrentStock = input.CurrentStock;
            material.MinimumStock = input.MinimumStock;
            material.CostPerUnit = input.CostPerUnit;
            material.IsActive = input.IsActive;

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Raw material updated successfully.";

            return RedirectToAction(nameof(Index));
        }

        // DELETE CONFIRMATION
        // ACCESS: ADMIN ONLY
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var material = await _context.RawMaterials
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);

            if (material == null)
                return NotFound();

            return View(material);
        }

        // DELETE MATERIAL
        // ACCESS: ADMIN ONLY
        [HttpPost]
        [ActionName("Delete")]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var material = await _context.RawMaterials
                .FindAsync(id);

            if (material == null)
                return NotFound();

            _context.RawMaterials.Remove(material);
            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Raw material deleted successfully.";

            return RedirectToAction(nameof(Index));
        }

        // SHARED VALIDATION
        private void ValidateMaterial(RawMaterial material)
        {
            if (material.CurrentStock < 0)
            {
                ModelState.AddModelError(
                    nameof(RawMaterial.CurrentStock),
                    "Stock cannot be negative.");
            }

            if (material.MinimumStock < 0)
            {
                ModelState.AddModelError(
                    nameof(RawMaterial.MinimumStock),
                    "Minimum stock cannot be negative.");
            }

            if (material.CostPerUnit < 0)
            {
                ModelState.AddModelError(
                    nameof(RawMaterial.CostPerUnit),
                    "Cost cannot be negative.");
            }
        }
    }
}
