using MaiAmTinhThuong.Data;
using MaiAmTinhThuong.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

public class BotRulesController : Controller
{
    private readonly ApplicationDbContext _db;

    public BotRulesController(ApplicationDbContext db)
    {
        _db = db;
    }

    // GET: BotRules
    public async Task<IActionResult> Index()
    {
        var rules = await _db.BotRules.OrderByDescending(r => r.Id).ToListAsync();
        return View(rules);
    }

    // GET: BotRules/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: BotRules/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BotRule rule)
    {
        if (ModelState.IsValid)
        {
            _db.BotRules.Add(rule);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(rule);
    }

    // GET: BotRules/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        var rule = await _db.BotRules.FindAsync(id);
        if (rule == null) return NotFound();
        return View(rule);
    }

    // POST: BotRules/Edit
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(BotRule rule)
    {
        if (ModelState.IsValid)
        {
            _db.BotRules.Update(rule);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(rule);
    }

    // GET: BotRules/Delete/5
    public async Task<IActionResult> Delete(int id)
    {
        var rule = await _db.BotRules.FindAsync(id);
        if (rule == null) return NotFound();

        _db.BotRules.Remove(rule);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
}
