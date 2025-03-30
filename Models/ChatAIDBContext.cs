using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace ChatAISystem.Models;

public partial class ChatAIDBContext : DbContext
{
    public ChatAIDBContext(DbContextOptions<ChatAIDBContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Character> Characters { get; set; }

    public virtual DbSet<Conversation> Conversations { get; set; }

    public virtual DbSet<User> Users { get; set; }

    [DbFunction(Name = "SOUNDEX", IsBuiltIn = true)]
    public static string GetSoundsLike(string query) => throw new NotImplementedException();



    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Character>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Characte__3214EC070690E599");

            entity.Property(e => e.AvatarUrl).HasMaxLength(255);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Name).HasMaxLength(100);

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.Characters)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("FK__Character__Creat__3D5E1FD2");
        });

        modelBuilder.Entity<Conversation>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Conversa__3214EC07F1B7E19D");

            entity.Property(e => e.Role).HasMaxLength(50);
            entity.Property(e => e.Timestamp)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.Character).WithMany(p => p.Conversations)
                .HasForeignKey(d => d.CharacterId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Conversat__Chara__4BAC3F29");

            entity.HasOne(d => d.User).WithMany(p => p.Conversations)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Conversat__UserI__4AB81AF0");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Users__3214EC07172A7739");

            entity.HasIndex(e => e.Username, "UQ__Users__536C85E4E34C925D").IsUnique();

            entity.HasIndex(e => e.Email, "UQ__Users__A9D1053486B47FD5").IsUnique();

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.PasswordHash).HasMaxLength(255);
            entity.Property(e => e.Username).HasMaxLength(100);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
