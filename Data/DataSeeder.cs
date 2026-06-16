using Microsoft.EntityFrameworkCore;
using R3AIA.Models;
using System.Collections.Generic;
using System.Linq;

namespace R3AIA.Data
{
    public static class DataSeeder
    {
        public static void SeedLocationData(AppDbContext context)
        {
            if (!context.Governorates.Any())
            {
                var governorates = new List<Governorate>
                {
                    new Governorate { Id = 1, Name = "القاهرة" },
                    new Governorate { Id = 2, Name = "الجيزة" },
                    new Governorate { Id = 3, Name = "الإسكندرية" },
                    new Governorate { Id = 4, Name = "القليوبية" },
                    new Governorate { Id = 5, Name = "الشرقية" },
                    new Governorate { Id = 6, Name = "الغربية" },
                    new Governorate { Id = 7, Name = "المنوفية" },
                    new Governorate { Id = 8, Name = "الدقهلية" },
                    new Governorate { Id = 9, Name = "كفر الشيخ" },
                    new Governorate { Id = 10, Name = "البحيرة" },
                    new Governorate { Id = 11, Name = "الإسماعيلية" },
                    new Governorate { Id = 12, Name = "السويس" },
                    new Governorate { Id = 13, Name = "بورسعيد" },
                    new Governorate { Id = 14, Name = "دمياط" },
                    new Governorate { Id = 15, Name = "المنيا" },
                    new Governorate { Id = 16, Name = "أسيوط" },
                    new Governorate { Id = 17, Name = "سوهاج" },
                    new Governorate { Id = 18, Name = "قنا" },
                    new Governorate { Id = 19, Name = "الأقصر" },
                    new Governorate { Id = 20, Name = "أسوان" },
                    new Governorate { Id = 21, Name = "البحر الأحمر" },
                    new Governorate { Id = 22, Name = "مطروح" },
                    new Governorate { Id = 23, Name = "شمال سيناء" },
                    new Governorate { Id = 24, Name = "جنوب سيناء" },
                    new Governorate { Id = 25, Name = "الفيوم" },
                    new Governorate { Id = 26, Name = "بني سويف" },
                    new Governorate { Id = 27, Name = "الوادي الجديد" }
                };

                // Enable identity insert if needed, but since we are seeding, we can just save them.
                // It's safer to use IDENTITY_INSERT ON for raw SQL or just insert without IDs.
                // However, our flutter app relies on exact IDs.
            }
        }
    }
}
