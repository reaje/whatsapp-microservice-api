using Microsoft.EntityFrameworkCore;
using WhatsApp.Core.Entities;
using WhatsApp.Core.Enums;

namespace WhatsApp.Infrastructure.Data;

public class SupabaseContext : DbContext
{
    public SupabaseContext(DbContextOptions<SupabaseContext> options) : base(options)
    {
    }

    public DbSet<Tenant> Tenants { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<WhatsAppSession> WhatsAppSessions { get; set; }
    public DbSet<Message> Messages { get; set; }
    public DbSet<AIAgent> AIAgents { get; set; }
    public DbSet<AIConversation> AIConversations { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure schema
        modelBuilder.HasDefaultSchema("whatsapp_service");

        // Configure Tenant
        modelBuilder.Entity<Tenant>(entity =>
        {
            entity.ToTable("tenants");

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");

            entity.Property(e => e.ClientId)
                .HasColumnName("client_id")
                .HasMaxLength(100)
                .IsRequired();
            entity.HasIndex(e => e.ClientId).IsUnique();

            entity.Property(e => e.Name)
                .HasColumnName("name")
                .HasMaxLength(255)
                .IsRequired();

            entity.Property(e => e.Settings)
                .HasColumnName("settings")
                .HasColumnType("jsonb");

            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("NOW()");

            entity.Property(e => e.UpdatedAt)
                .HasColumnName("updated_at")
                .HasDefaultValueSql("NOW()");
        });

        // Configure User
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");

            entity.Property(e => e.TenantId).HasColumnName("tenant_id").IsRequired();

            entity.Property(e => e.Email)
                .HasColumnName("email")
                .HasMaxLength(255)
                .IsRequired();

            entity.Property(e => e.PasswordHash)
                .HasColumnName("password_hash")
                .IsRequired();

            entity.Property(e => e.FullName)
                .HasColumnName("full_name")
                .HasMaxLength(255)
                .IsRequired();

            entity.Property(e => e.Role)
                .HasColumnName("role")
                .HasMaxLength(50)
                .HasDefaultValue("User");

            entity.Property(e => e.IsActive)
                .HasColumnName("is_active")
                .HasDefaultValue(true);

            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("NOW()");

            entity.Property(e => e.UpdatedAt)
                .HasColumnName("updated_at")
                .HasDefaultValueSql("NOW()");

            entity.Property(e => e.LastLoginAt)
                .HasColumnName("last_login_at");

            entity.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.TenantId, e.Email }).IsUnique();
            entity.HasIndex(e => e.TenantId);
        });

        // Configure WhatsAppSession
        modelBuilder.Entity<WhatsAppSession>(entity =>
        {
            entity.ToTable("whatsapp_sessions");

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");

            entity.Property(e => e.TenantId).HasColumnName("tenant_id").IsRequired();

            entity.Property(e => e.PhoneNumber)
                .HasColumnName("phone_number")
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(e => e.ProviderType)
                .HasColumnName("provider_type")
                .HasMaxLength(50)
                .HasConversion(
                    v => v.ToString().ToLower(),
                    v => Enum.Parse<ProviderType>(v, true))
                .IsRequired();

            entity.Property(e => e.SessionData)
                .HasColumnName("session_data")
                .HasColumnType("jsonb");

            entity.Property(e => e.IsActive)
                .HasColumnName("is_active")
                .HasDefaultValue(true);

            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("NOW()");

            entity.Property(e => e.UpdatedAt)
                .HasColumnName("updated_at")
                .HasDefaultValueSql("NOW()");

            entity.HasOne(e => e.Tenant)
                .WithMany(t => t.Sessions)
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.TenantId, e.PhoneNumber }).IsUnique();
            entity.HasIndex(e => e.TenantId);
        });

        // Configure Message
        modelBuilder.Entity<Message>(entity =>
        {
            entity.ToTable("messages");

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");

            entity.Property(e => e.TenantId).HasColumnName("tenant_id").IsRequired();
            entity.Property(e => e.SessionId).HasColumnName("session_id").IsRequired();

            entity.Property(e => e.MessageId)
                .HasColumnName("message_id")
                .HasMaxLength(255);
            entity.HasIndex(e => e.MessageId).IsUnique();

            entity.Property(e => e.FromNumber)
                .HasColumnName("from_number")
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(e => e.ToNumber)
                .HasColumnName("to_number")
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(e => e.MessageType)
                .HasColumnName("message_type")
                .HasMaxLength(50)
                .HasConversion(
                    v => v.ToString().ToLower(),
                    v => Enum.Parse<MessageType>(v, true))
                .IsRequired();

            entity.Property(e => e.Content)
                .HasColumnName("content")
                .HasColumnType("jsonb");

            entity.Property(e => e.Status)
                .HasColumnName("status")
                .HasMaxLength(50)
                .HasConversion(
                    v => v.ToString().ToLower(),
                    v => Enum.Parse<MessageStatus>(v, true))
                .IsRequired();

            entity.Property(e => e.AiProcessed)
                .HasColumnName("ai_processed")
                .HasDefaultValue(false);

            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("NOW()");

            entity.Property(e => e.UpdatedAt)
                .HasColumnName("updated_at")
                .HasDefaultValueSql("NOW()");

            entity.HasOne(e => e.Tenant)
                .WithMany(t => t.Messages)
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Session)
                .WithMany(s => s.Messages)
                .HasForeignKey(e => e.SessionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => e.SessionId);
            entity.HasIndex(e => e.CreatedAt).IsDescending();
        });

        // Configure AIAgent
        modelBuilder.Entity<AIAgent>(entity =>
        {
            entity.ToTable("ai_agents");

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");

            entity.Property(e => e.TenantId).HasColumnName("tenant_id").IsRequired();

            entity.Property(e => e.Name)
                .HasColumnName("name")
                .HasMaxLength(255)
                .IsRequired();

            entity.Property(e => e.Type)
                .HasColumnName("type")
                .HasMaxLength(100);

            entity.Property(e => e.Configuration)
                .HasColumnName("configuration")
                .HasColumnType("jsonb");

            entity.Property(e => e.IsActive)
                .HasColumnName("is_active")
                .HasDefaultValue(true);

            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("NOW()");

            entity.Property(e => e.UpdatedAt)
                .HasColumnName("updated_at")
                .HasDefaultValueSql("NOW()");

            entity.HasOne(e => e.Tenant)
                .WithMany(t => t.AIAgents)
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure AIConversation
        modelBuilder.Entity<AIConversation>(entity =>
        {
            entity.ToTable("ai_conversations");

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");

            entity.Property(e => e.TenantId).HasColumnName("tenant_id").IsRequired();
            entity.Property(e => e.AgentId).HasColumnName("agent_id").IsRequired();
            entity.Property(e => e.SessionId).HasColumnName("session_id").IsRequired();

            entity.Property(e => e.Context)
                .HasColumnName("context")
                .HasColumnType("jsonb");

            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("NOW()");

            entity.Property(e => e.UpdatedAt)
                .HasColumnName("updated_at")
                .HasDefaultValueSql("NOW()");

            entity.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Agent)
                .WithMany(a => a.Conversations)
                .HasForeignKey(e => e.AgentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Session)
                .WithMany(s => s.AIConversations)
                .HasForeignKey(e => e.SessionId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}