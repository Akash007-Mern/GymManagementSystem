using System;
using System.Collections.Generic;

namespace GymManagementSystem.Models;

public partial class Attendance
{
    public int AttendanceId { get; set; }

    public int? MemberId { get; set; }

    public DateTime CheckInTime { get; set; }

    public DateTime? CheckOutTime { get; set; }

    public virtual Member? Member { get; set; }
}
