using Microsoft.AspNetCore.Mvc.Rendering;

namespace MaiAmTinhThuong.Helpers
{
    public static class PaginationHelper
    {
        public static List<SelectListItem> GetPageSizeOptions()
        {
            return new List<SelectListItem>
            {
                new SelectListItem { Text = "10", Value = "10" },
                new SelectListItem { Text = "25", Value = "25" },
                new SelectListItem { Text = "50", Value = "50" },
                new SelectListItem { Text = "100", Value = "100" }
            };
        }

        public static int GetPageNumber(int? page, int defaultPage = 1)
        {
            return page.HasValue && page.Value > 0 ? page.Value : defaultPage;
        }

        public static int GetPageSize(int? pageSize, int defaultPageSize = 10)
        {
            var validSizes = new[] { 10, 25, 50, 100 };
            if (pageSize.HasValue && validSizes.Contains(pageSize.Value))
                return pageSize.Value;
            return defaultPageSize;
        }
    }
}







