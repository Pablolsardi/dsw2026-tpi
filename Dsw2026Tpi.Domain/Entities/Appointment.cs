using System;
using System.Collections.Generic;
using System.Text;

namespace Dsw2026Tpi.Domain.Entities
{
    public class Appointment : EntityBase
    {
        public Guid AvailabilitySlotId { get; init; }
        public AvailabilitySlot? AvailabilitySlot { get; private set; }
        public Guid PatientId { get; init; }
        public Patient? Patient { get; private set; }
        public string Reason { get; init; }
        public AppointmentStatus Status { get; private set; }
        public DateTime? CancelledAt { get; private set; }
        public DateTime? AttendedAt { get; private set; }
        public byte[] RowVersion { get; private set; }
        #region Constructor for EF
#pragma warning disable CS8618
        private Appointment() { }
#pragma warning restore CS8618
        #endregion

        public Appointment(Guid availabilitySlotId, Guid patientId, string reason, Guid? id = null) : base(id)
        {
            if (availabilitySlotId == Guid.Empty)
                throw new ArgumentException("El identificador de AvailabilitySlot no puede ser vacío.", nameof(availabilitySlotId));
            if (patientId == Guid.Empty)
                throw new ArgumentException("El identificador de Patient no puede ser vacío.", nameof(patientId));
            if (string.IsNullOrWhiteSpace(reason))
                throw new ArgumentException("La razón es obligatoria.", nameof(reason));

            AvailabilitySlotId = availabilitySlotId;
            PatientId = patientId;
            Reason = reason.Trim();
            Status = AppointmentStatus.Booked;
            RowVersion = Array.Empty<byte>();
        }
        public void Cancel()
        {
            Status = AppointmentStatus.Cancelled;
            CancelledAt = DateTime.UtcNow;
        }

        public void MarkAttended()
        {
            Status = AppointmentStatus.Attended;
            AttendedAt = DateTime.UtcNow;
        }
    }
}
