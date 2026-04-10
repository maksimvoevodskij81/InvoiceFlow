using Microsoft.EntityFrameworkCore;

namespace InvoiceFlow.Api.Infrastructure.Persistence;

public sealed class InvoiceFlowDbContext : DbContext
{
    public InvoiceFlowDbContext(DbContextOptions<InvoiceFlowDbContext> options)
        : base(options)
    {
    }

    public DbSet<UploadedInvoiceEntity> UploadedInvoices => Set<UploadedInvoiceEntity>();
    public DbSet<ExactPostOutboxEntity> ExactPostOutbox => Set<ExactPostOutboxEntity>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        var uploadedInvoice = modelBuilder.Entity<UploadedInvoiceEntity>();

        uploadedInvoice.ToTable("UploadedInvoices");

        uploadedInvoice.HasKey(x => x.InvoiceId);

        uploadedInvoice.Property(x => x.InvoiceId)
            .HasMaxLength(64);

        uploadedInvoice.Property(x => x.OriginalFileName)
            .HasMaxLength(260);

        uploadedInvoice.Property(x => x.StoredFilePath)
            .HasMaxLength(1024);

        uploadedInvoice.Property(x => x.Status)
            .HasMaxLength(32);

        uploadedInvoice.Property(x => x.Message)
            .HasMaxLength(512);

        uploadedInvoice.Property(x => x.FileHash)
            .HasMaxLength(128);

        uploadedInvoice.Property(x => x.SupplierName)
            .HasMaxLength(256);

        uploadedInvoice.Property(x => x.InvoiceNumber)
            .HasMaxLength(128);

        uploadedInvoice.Property(x => x.Currency)
            .HasMaxLength(16);

        uploadedInvoice.Property(x => x.SupplierMatchedBy)
            .HasMaxLength(64);

        uploadedInvoice.Property(x => x.InternalSupplierId)
            .HasMaxLength(128);

        uploadedInvoice.Property(x => x.ExactSupplierId)
            .HasMaxLength(128);

        uploadedInvoice.Property(x => x.SupplierMatchMessage)
            .HasMaxLength(512);

        uploadedInvoice.HasIndex(x => x.FileHash)
            .IsUnique();

        var exactPostOutbox = modelBuilder.Entity<ExactPostOutboxEntity>();

        exactPostOutbox.ToTable("ExactPostOutbox");

        exactPostOutbox.HasKey(x => x.Id);

        exactPostOutbox.Property(x => x.InvoiceId)
            .HasMaxLength(64);

        exactPostOutbox.Property(x => x.Status)
            .HasMaxLength(32);

        exactPostOutbox.Property(x => x.ExternalDocumentId)
            .HasMaxLength(128);

        exactPostOutbox.Property(x => x.LastError)
            .HasMaxLength(1024);

        exactPostOutbox.HasIndex(x => x.InvoiceId)
            .IsUnique();
    }
}