using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Nodes;

namespace SelfService.Infrastructure.Persistence.Models
{
    [Table("metrics")]
    public class RequirementsMetric
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("name")]
        public string Name { get; set; } = null!;

        [Required]
        [Column("capability_root_id")]
        public string CapabilityRootId { get; set; } = null!;

        [Required]
        [Column("requirement_id")]
        public string RequirementId { get; set; } = null!;

        [Required]
        [Column("measurement")]
        public string Measurement { get; set; } = null!;

        [Column("help_url")]
        public string? HelpUrl { get; set; }

        [Column("owner")]
        public string? Owner { get; set; }

        [Column("description")]
        public string? Description { get; set; }

        [Column("cluster_name")]
        public string? ClusterName { get; set; }

        [Column("value")]
        public double Value { get; set; }

        [Column("help")]
        public string? Help { get; set; }

        [Column("type")]
        public string? Type { get; set; }

        [Column("date")]
        public long Date { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }

        [Column("labels")]
        public JsonObject? Labels { get; set; }
    }
}
