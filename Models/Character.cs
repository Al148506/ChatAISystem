using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ChatAISystem.Models;

public partial class Character
{
    public int Id { get; set; }
    [MaxLength(100)]
    public string Name { get; set; } = null!;
    [MaxLength(1000)]
    public string? Description { get; set; }
    [MaxLength(255)]
    public string? AvatarUrl { get; set; }

    public int CreatedBy { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<Conversation> Conversations { get; set; } = new List<Conversation>();

    public virtual User CreatedByNavigation { get; set; } = null!;
}
