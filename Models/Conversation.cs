using System;
using System.Collections.Generic;

namespace ChatAISystem.Models;

public partial class Conversation
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int CharacterId { get; set; }

    public string Role { get; set; } = null!;

    public string MessageText { get; set; } = null!;

    public DateTime Timestamp { get; set; }

    public virtual Character Character { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
