using MaiAmTinhThuong.Data;
using MaiAmTinhThuong.Models;
using Microsoft.EntityFrameworkCore;

namespace MaiAmTinhThuong.Services
{
    public class SupportRequestService
    {
        private readonly ApplicationDbContext _context;

        public SupportRequestService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<SupportRequest>> GetAllAsync()
        {
            return await _context.SupportRequests
                .Include(sr => sr.MaiAm)
                .OrderByDescending(sr => sr.CreatedDate)
                .ToListAsync();
        }

        public async Task<SupportRequest?> GetByIdAsync(int id)
        {
            return await _context.SupportRequests
                .Include(sr => sr.MaiAm)
                .FirstOrDefaultAsync(sr => sr.Id == id);
        }

        public async Task<List<SupportRequest>> GetByMaiAmIdAsync(int maiAmId)
        {
            return await _context.SupportRequests
                .Where(sr => sr.MaiAmId == maiAmId)
                .OrderByDescending(sr => sr.CreatedDate)
                .ToListAsync();
        }

        public async Task<List<SupportRequest>> GetPendingAsync()
        {
            return await _context.SupportRequests
                .Where(sr => !sr.IsApproved)
                .Include(sr => sr.MaiAm)
                .OrderByDescending(sr => sr.CreatedDate)
                .ToListAsync();
        }

        public async Task<SupportRequest> CreateAsync(SupportRequest supportRequest)
        {
            supportRequest.CreatedDate = DateTime.Now;
            supportRequest.UpdatedDate = DateTime.Now;
            supportRequest.IsApproved = false;

            _context.SupportRequests.Add(supportRequest);
            await _context.SaveChangesAsync();
            return supportRequest;
        }

        public async Task<SupportRequest> UpdateAsync(SupportRequest supportRequest)
        {
            supportRequest.UpdatedDate = DateTime.Now;
            _context.SupportRequests.Update(supportRequest);
            await _context.SaveChangesAsync();
            return supportRequest;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var supportRequest = await GetByIdAsync(id);
            if (supportRequest == null)
                return false;

            _context.SupportRequests.Remove(supportRequest);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ApproveAsync(int id)
        {
            var supportRequest = await GetByIdAsync(id);
            if (supportRequest == null)
                return false;

            supportRequest.IsApproved = true;
            supportRequest.UpdatedDate = DateTime.Now;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<int> GetCountAsync()
        {
            return await _context.SupportRequests.CountAsync();
        }

        public async Task<int> GetPendingCountAsync()
        {
            return await _context.SupportRequests.CountAsync(sr => !sr.IsApproved);
        }
    }
}



