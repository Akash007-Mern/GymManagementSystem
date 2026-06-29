using System;
using System.Collections.Generic;

namespace GymManagementSystem.Models;

public partial class Payment
{
    public int PaymentId { get; set; }

    public int? MemberId { get; set; }

    public int? PlanId { get; set; }

    public decimal AmountPaid { get; set; }

    public DateOnly PaymentDate { get; set; }

    public DateOnly ValidUntil { get; set; }

    public string? Notes { get; set; }

    public virtual Member? Member { get; set; }

    public virtual MembershipPlan? Plan { get; set; }
}
