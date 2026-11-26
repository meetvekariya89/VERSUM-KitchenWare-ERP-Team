
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VERSUM.KitchenWareERP.Data;
using VERSUM.KitchenWareERP.Models;

namespace VERSUM.KitchenWareERP.Controllers
{
    [Authorize(Roles = "Admin,Staff")]
    public class DealersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DealersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // LIST AND SEARCH
        // ACCESS: ADMIN AND STAFF
        [HttpGet]
        public async Task<IActionResult> Index(string? search)
        {
            var query = _context.Dealers.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();
                query = query.Where(d =>
                    d.Name.Contains(search) ||
                    d.DealerCode.Contains(search) ||
                    d.City.Contains(search) ||
                    d.Phone.Contains(search));
            }

            ViewBag.Search = search;

            return View(await query
                .OrderBy(d => d.Name)
                .ToListAsync());
        }

        // CREATE DEALER PAGE
        // ACCESS: ADMIN ONLY
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View(new Dealer
            {
                State = "Gujarat",
                IsActive = true
            });
        }

        // SAVE NEW DEALER
        // ACCESS: ADMIN ONLY
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("DealerCode,Name,ContactPerson,Phone,Email,Address,City,State,Pincode,GSTNumber,IsActive")]
            Dealer dealer)
        {
            if (await _context.Dealers.AnyAsync(d =>
                d.DealerCode == dealer.DealerCode))
            {
                ModelState.AddModelError(
                    nameof(Dealer.DealerCode),
                    "Dealer code already exists.");
            }

            if (!ModelState.IsValid)
                return View(dealer);

            dealer.CreatedAt = DateTime.UtcNow;
            _context.Dealers.Add(dealer);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Dealer added successfully.";
            return RedirectToAction(nameof(Index));
        }

        // EDIT DEALER PAGE
        // ACCESS: ADMIN ONLY
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var dealer = await _context.Dealers.FindAsync(id);

            if (dealer == null)
                return NotFound();

            return View(dealer);
        }

        // UPDATE DEALER
        // ACCESS: ADMIN ONLY
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("Id,DealerCode,Name,ContactPerson,Phone,Email,Address,City,State,Pincode,GSTNumber,IsActive")]
            Dealer input)
        {
            if (id != input.Id)
                return BadRequest();

            var dealer = await _context.Dealers.FindAsync(id);

            if (dealer == null)
                return NotFound();

            if (await _context.Dealers.AnyAsync(d =>
                d.DealerCode == input.DealerCode && d.Id != id))
            {
                ModelState.AddModelError(
                    nameof(Dealer.DealerCode),
                    "Dealer code already exists.");
            }

            if (!ModelState.IsValid)
                return View(input);

            dealer.DealerCode = input.DealerCode;
            dealer.Name = input.Name;
            dealer.ContactPerson = input.ContactPerson;
            dealer.Phone = input.Phone;
            dealer.Email = input.Email;
            dealer.Address = input.Address;
            dealer.City = input.City;
            dealer.State = input.State;
            dealer.Pincode = input.Pincode;
            dealer.GSTNumber = input.GSTNumber;
            dealer.IsActive = input.IsActive;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Dealer updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        // DELETE CONFIRMATION PAGE
        // ACCESS: ADMIN ONLY
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var dealer = await _context.Dealers
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == id);

            if (dealer == null)
                return NotFound();

            return View(dealer);
        }

        // DELETE DEALER
        // ACCESS: ADMIN ONLY
        [HttpPost]
        [ActionName("Delete")]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var dealer = await _context.Dealers.FindAsync(id);

            if (dealer == null)
                return NotFound();

            _context.Dealers.Remove(dealer);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Dealer deleted successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
}
