using System;
using System.Collections.Generic;
using System.Text;

namespace Dsw2026Tpi.Domain.Entities
{
    // Estado de la cita (tabla APPOINTMENTS)
    public enum AppointmentStatus
    {
        Booked,
        Cancelled,
        Attended,
        NoShow
    }

}
