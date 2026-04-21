using InvoiceFlow.Api.Features.Suppliers.CreateSupplier;
using InvoiceFlow.Api.Features.Suppliers.Idempotency;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
namespace InvoiceFlow.Api.Infrastructure.Persistence;

public sealed class InvoiceFlowDbContext : DbContext
{
    public InvoiceFlowDbContext(DbContextOptions<InvoiceFlowDbContext> options)
        : base(options)
    {
    }

    public DbSet<UploadedInvoiceEntity> UploadedInvoices => Set<UploadedInvoiceEntity>();
    public DbSet<ExactPostOutboxEntity> ExactPostOutbox => Set<ExactPostOutboxEntity>();
    public DbSet<SupplierCreateOutboxEntity> SupplierCreateOutbox => Set<SupplierCreateOutboxEntity>();
    public DbSet<SupplierMappingEntity> SupplierMappings => Set<SupplierMappingEntity>();
    public DbSet<BankAccountMappingEntity> BankAccountMappings => Set<BankAccountMappingEntity>();
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

        uploadedInvoice.Property(x => x.ExactPostingStatus)
            .HasMaxLength(32);

        uploadedInvoice.Property(x => x.ExactDocumentId)
            .HasMaxLength(128);

        uploadedInvoice.Property(x => x.ExactPostingError)
            .HasMaxLength(1024);

        uploadedInvoice.Property(x => x.MatchReasons)
     .HasConversion(
         value => JsonSerializer.Serialize(value, (JsonSerializerOptions?)null),
         value => JsonSerializer.Deserialize<List<string>>(value, (JsonSerializerOptions?)null) ?? new List<string>())
     .HasColumnType("nvarchar(max)");

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

        modelBuilder.Entity<SupplierCreateOutboxEntity>(entity =>
        {
            entity.ToTable("SupplierCreateOutbox");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.InvoiceId)
                .IsRequired();

            entity.Property(x => x.Status)
                .IsRequired();

            entity.Property(x => x.AttemptCount)
                .IsRequired();

            entity.Property(x => x.CreatedAtUtc)
                .IsRequired();

            entity.HasIndex(x => x.InvoiceId)
                .IsUnique();
        });
        modelBuilder.Entity<SupplierMappingEntity>(entity =>
        {
            entity.ToTable("SupplierMappings");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Fingerprint)
                .IsRequired();

            entity.Property(x => x.ExactSupplierId)
                .IsRequired();

            entity.Property(x => x.CreatedAtUtc)
                .IsRequired();

            entity.HasIndex(x => x.Fingerprint)
                .IsUnique();
        });

        modelBuilder.Entity<BankAccountMappingEntity>(entity =>
        {
            entity.ToTable("BankAccountMappings");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Fingerprint)
                .IsRequired();

            entity.Property(x => x.ExactSupplierId)
                .IsRequired();

            entity.Property(x => x.CreatedAtUtc)
                .IsRequired();

            entity.HasIndex(x => x.Fingerprint)
                .IsUnique();
        });
    }
}