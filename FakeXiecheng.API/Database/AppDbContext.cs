using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using FakeXiecheng.API.Models;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace FakeXiecheng.API.Database
{
    public class AppDbContext: IdentityDbContext<ApplicationUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<TouristRoute> TouristRoutes { get; set; }
        public DbSet<TouristRoutePicture> TouristRoutePictures { get; set; }
        public DbSet<ShoppingCart> ShoppingCarts { get; set; }
        public DbSet<LineItem> LineItems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var touristRouteJsonData =
                File.ReadAllText(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) +
                                 @"/Database/touristRoutesMockData.json");
            IList<TouristRoute> touristRoutes =
                JsonConvert.DeserializeObject<IList<TouristRoute>>(touristRouteJsonData);
            modelBuilder.Entity<TouristRoute>().HasData(touristRoutes);

            var touristRoutePictureJsonData =
                File.ReadAllText(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"/Database/touristRoutePicturesMockData.json");
            IList<TouristRoutePicture> touristRoutePictures =
                JsonConvert.DeserializeObject<IList<TouristRoutePicture>>(touristRoutePictureJsonData);
            modelBuilder.Entity<TouristRoutePicture>().HasData(touristRoutePictures);

            //1. update user role FK
            modelBuilder.Entity<ApplicationUser>(b =>
            {
                b.HasMany(x => x.UserRoles)
                    .WithOne().HasForeignKey(ur => ur.UserId)
                    .IsRequired();
            });
            //2. add admin 
            var adminRoleId = "8D0A8042-2D4E-487A-BE8B-48481E9F1DAB";
            modelBuilder.Entity<IdentityRole>().HasData(
                new IdentityRole
                {
                    Id = adminRoleId,
                    Name = "Admin",
                    NormalizedName = "Admin".ToUpper()
                }
            );
            //3. add user 
            var adminUserId = "2072ADFB-291D-4F3F-9664-4443D0D209FB";
            ApplicationUser adminUser = new ApplicationUser
            {
                Id = adminUserId,
                UserName = "admin@fakexiechang.com",
                NormalizedUserName = "admin@fakexiechang.com".ToUpper(),
                Email = "admin@fakexiechang.com",
                NormalizedEmail = "admin@fakexiechang.com".ToUpper(),
                TwoFactorEnabled = false,
                EmailConfirmed = true,
                PhoneNumber = "121231498",
                PhoneNumberConfirmed = false
            };
            PasswordHasher<ApplicationUser> ph = new PasswordHasher<ApplicationUser>();
            adminUser.PasswordHash = ph.HashPassword(adminUser, "Fake123$");
            modelBuilder.Entity<ApplicationUser>().HasData(adminUser);
            //4. add admin role to user, using linking table: identityUserRole
            modelBuilder.Entity<IdentityUserRole<string>>().HasData(
                new IdentityUserRole<string>()
                {
                    RoleId = adminRoleId,
                    UserId = adminUserId
                });
            base.OnModelCreating(modelBuilder);
        }

    }
}
 