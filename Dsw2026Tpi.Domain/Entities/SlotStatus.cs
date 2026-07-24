using System;
using System.Collections.Generic;
using System.Text;

namespace Dsw2026Tpi.Domain.Entities
{
    // Estado del slot de disponibilidad (tabla AVAILABILITYSLOTS)
    public enum SlotStatus
    {
        Available,
        Booked,
        Blocked
    }
}
