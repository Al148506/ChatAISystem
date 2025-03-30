using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ChatAISystem.Models;

public partial class User
{
    public int Id { get; set; }

    public string Username { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;
    [Display(Name = "Creation Date")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public string Role { get; set; } = "User";

    public virtual ICollection<Character> Characters { get; set; } = new List<Character>();

    public virtual ICollection<Conversation> Conversations { get; set; } = new List<Conversation>();
}
