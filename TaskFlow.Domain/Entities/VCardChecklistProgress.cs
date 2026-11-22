using System;
using System.Collections.Generic;

namespace TaskFlow.Domain.Entities;

public partial class VCardChecklistProgress
{
    public Guid? CardId { get; set; }

    public long? TotalItems { get; set; }

    public long? CheckedItems { get; set; }
}
