using System;
using System.Collections.Generic;
using System.Text;

namespace Dsw2026Tpi.Domain.Entities
{
    public class AvailabilitySlot : EntityBase
    {
        public Guid AvailabilityRuleId { get; init; }
        public AvailabilityRule? AvailabilityRule { get; private set; }
        public Guid DoctorId { get; init; }
        public DateOnly SlotDate { get; init; }
        public TimeOnly StartTime { get; init; }
        public TimeOnly EndTime { get; init; }
        public SlotStatus Status { get; private set; }
        #region Constructor for EF
#pragma warning disable CS8618
        private AvailabilitySlot() { }
#pragma warning restore CS8618
        #endregion

        public AvailabilitySlot(Guid availabilityRuleId, Guid doctorId, DateOnly slotDate,
            TimeOnly startTime, TimeOnly endTime, Guid? id = null) : base(id)
        {
            AvailabilityRuleId = availabilityRuleId;
            DoctorId = doctorId;
            SlotDate = slotDate;
            StartTime = startTime;
            EndTime = endTime;
            Status = SlotStatus.Available;
        }
        public void Book() => Status = SlotStatus.Booked;

        public void Release() => Status = SlotStatus.Available;
    }
}
