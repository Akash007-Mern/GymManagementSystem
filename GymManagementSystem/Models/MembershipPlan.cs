using System;
using System.Collections.Generic;

namespace GymManagementSystem.Models;

public partial class MembershipPlan
{
    public int PlanId { get; set; }

    public string PlanName { get; set; } = null!;

    public int DurationDays { get; set; }

    public decimal Price { get; set; }

    public string? Description { get; set; }

    public virtual ICollection<Member> Members { get; set; } = new List<Member>();

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
