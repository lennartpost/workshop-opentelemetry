using Backend.Model;
using Microsoft.EntityFrameworkCore;

namespace Backend.Database
{
    public class FhirResourceDb : DbContext
    {
        public FhirResourceDb(DbContextOptions<FhirResourceDb> options) : base(options) { }

        public DbSet<FhirResource> FhirResources => Set<FhirResource>();
    }
}
