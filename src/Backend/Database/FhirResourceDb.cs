using Microsoft.EntityFrameworkCore;
using Model;

namespace Backend.Database
{
    public class FhirResourceDb : DbContext
    {
        public FhirResourceDb(DbContextOptions<FhirResourceDb> options) : base(options) { }

        public DbSet<Patient> PatientResources => Set<Patient>();
    }
}
