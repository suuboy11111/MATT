using MaiAmTinhThuong.Data;
using MaiAmTinhThuong.Models;
using Microsoft.EntityFrameworkCore;

namespace MaiAmTinhThuong.Services
{
    public class SupporterService
    {
        private readonly ApplicationDbContext _context;

        public SupporterService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Supporter>> GetAllAsync()
        {
            return await _context.Supporters
                .Include(s => s.MaiAm)
                .Include(s => s.SupporterSupportTypes)
                    .ThenInclude(sst => sst.SupportType)
                .OrderByDescending(s => s.CreatedDate)
                .ToListAsync();
        }

        public async Task<Supporter?> GetByIdAsync(int id)
        {
            return await _context.Supporters
                .Include(s => s.MaiAm)
                .Include(s => s.SupporterSupportTypes)
                    .ThenInclude(sst => sst.SupportType)
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task<List<Supporter>> GetByMaiAmIdAsync(int maiAmId)
        {
            return await _context.Supporters
                .Where(s => s.MaiAmId == maiAmId)
                .Include(s => s.MaiAm)
                .OrderByDescending(s => s.CreatedDate)
                .ToListAsync();
        }

        public async Task<List<Supporter>> GetApprovedAsync()
        {
            return await _context.Supporters
                .Where(s => s.IsApproved)
                .Include(s => s.MaiAm)
                .OrderByDescending(s => s.CreatedDate)
                .ToListAsync();
        }

        public async Task<Supporter> CreateAsync(Supporter supporter)
        {
            supporter.CreatedDate = DateTime.Now;
            supporter.UpdatedDate = DateTime.Now;
            supporter.IsApproved = false;

            _context.Supporters.Add(supporter);
            await _context.SaveChangesAsync();
            return supporter;
        }

        public async Task<Supporter> UpdateAsync(Supporter supporter)
        {
            supporter.UpdatedDate = DateTime.Now;
            _context.Supporters.Update(supporter);
            await _context.SaveChangesAsync();
            return supporter;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var supporter = await GetByIdAsync(id);
            if (supporter == null)
                return false;

            _context.Supporters.Remove(supporter);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ApproveAsync(int id)
        {
            var supporter = await GetByIdAsync(id);
            if (supporter == null)
                return false;

            supporter.IsApproved = true;
            supporter.UpdatedDate = DateTime.Now;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<int> GetCountAsync()
        {
            return await _context.Supporters.CountAsync();
        }

        public async Task<int> GetApprovedCountAsync()
        {
            return await _context.Supporters.CountAsync(s => s.IsApproved);
        }
    }
}



