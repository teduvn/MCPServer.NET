using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderManagement.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderManagement.Infrastructure.Persistence.Configurations
{
    public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
    {
        public void Configure(EntityTypeBuilder<AuditLog> builder)
        {
            builder.ToTable("AuditLogs");
            builder.HasKey(x => x.Id);


            builder.Property(x => x.CommandType).HasMaxLength(200).IsRequired();
            builder.Property(x => x.UserId).HasMaxLength(100);
            builder.Property(x => x.UserName).HasMaxLength(200);
            builder.Property(x => x.ActorType).HasMaxLength(50);
            builder.Property(x => x.ToolName).HasMaxLength(200);
            builder.Property(x => x.EntityType).HasMaxLength(100);
            builder.Property(x => x.EntityId).HasMaxLength(100);
            builder.Property(x => x.Parameters).HasColumnType("nvarchar(max)");
            builder.Property(x => x.ErrorMessage).HasColumnType("nvarchar(max)");
            builder.Property(x => x.IpAddress).HasMaxLength(50);


            // Index để query nhanh theo thời gian và actor
            builder.HasIndex(x => x.CreatedAt);
            builder.HasIndex(x => new { x.UserId, x.CreatedAt });
            builder.HasIndex(x => new { x.EntityType, x.EntityId });
            builder.HasIndex(x => x.CorrelationId);
        }
    }

}
