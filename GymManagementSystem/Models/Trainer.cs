using System;
using System.Collections.Generic;

namespace GymManagementSystem.Models;

public partial class Trainer
{
    public int TrainerId { get; set; }

    public string FullName { get; set; } = null!;

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public string? Specialty { get; set; }

    public DateOnly JoinDate { get; set; }

    public bool? IsActive { get; set; }

    public virtual ICollection<Member> Members { get; set; } = new List<Member>();
}
