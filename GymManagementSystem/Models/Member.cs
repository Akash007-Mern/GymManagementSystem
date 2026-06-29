using System;
using System.Collections.Generic;

namespace GymManagementSystem.Models;

public partial class Member
{
    public int MemberId { get; set; }

    public string FullName { get; set; } = null!;

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public string? Address { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    public string? Gender { get; set; }

    public DateOnly JoinDate { get; set; }

    public int? PlanId { get; set; }

    public int? TrainerId { get; set; }

    public bool? IsActive { get; set; }

    public virtual ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual MembershipPlan? Plan { get; set; }

    public virtual Trainer? Trainer { get; set; }
}
