namespace PicoPlus.Domain.Admin;

/// <summary>
/// HubSpot Owner model for admin panel
/// </summary>
public class HubSpotOwner
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}".Trim();
    public bool Archived { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    /// <summary>
    /// Teams this owner belongs to
    /// </summary>
    public List<string> Teams { get; set; } = new();
}

/// <summary>
/// Selected owner for admin context
/// </summary>
public class AdminOwnerContext
{
    public HubSpotOwner? SelectedOwner { get; set; }
    public DateTime SelectedAt { get; set; }
    public List<HubSpotOwner> RecentOwners { get; set; } = new();
}

/// <summary>
/// Dashboard statistics
/// </summary>
public class DashboardStatistics
{
    // Contact statistics
    public int TotalContacts { get; set; }
    public int NewContactsToday { get; set; }
    public int NewContactsThisWeek { get; set; }
    public int NewContactsThisMonth { get; set; }

    // Deal statistics
    public int TotalDeals { get; set; }
    public int OpenDeals { get; set; }
    public int ClosedWonDeals { get; set; }
    public int ClosedLostDeals { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal MonthlyRevenue { get; set; }
    public decimal AverageDealValue { get; set; }

    // Pipeline statistics
    public List<PipelineStageStats> PipelineStages { get; set; } = new();

    // Activity statistics
    public int TasksDueToday { get; set; }
    public int TasksOverdue { get; set; }
    public int EmailsSentToday { get; set; }
    public int CallsMadeToday { get; set; }

    // Performance metrics
    public decimal ConversionRate { get; set; }
    public int AverageDaysToClose { get; set; }
    public List<TopPerformer> TopPerformers { get; set; } = new();
}

/// <summary>
/// Pipeline stage statistics
/// </summary>
public class PipelineStageStats
{
    public string StageId { get; set; } = string.Empty;
    public string StageName { get; set; } = string.Empty;
    public int DealCount { get; set; }
    public decimal TotalValue { get; set; }
    public int Position { get; set; }
}

/// <summary>
/// Top performer data
/// </summary>
public class TopPerformer
{
    public string OwnerId { get; set; } = string.Empty;
    public string OwnerName { get; set; } = string.Empty;
    public int DealsWon { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal ConversionRate { get; set; }
}

/// <summary>
/// Kanban board card
/// </summary>
public class KanbanCard
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ContactName { get; set; }
    public string? CompanyName { get; set; }
    public decimal? Amount { get; set; }
    public DateTime? CloseDate { get; set; }
    public string Priority { get; set; } = "medium";
    public List<string> Tags { get; set; } = new();
    public string OwnerId { get; set; } = string.Empty;
    public string OwnerName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Kanban board column
/// </summary>
public class KanbanColumn
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = "#6c757d";
    public int Position { get; set; }
    public List<KanbanCard> Cards { get; set; } = new();
    public int TotalCards => Cards.Count;
    public decimal TotalValue => Cards.Sum(c => c.Amount ?? 0);
}

/// <summary>
/// Recent activity item
/// </summary>
public class RecentActivity
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // "deal_created", "contact_added", "deal_moved", etc.
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = "bi-circle";
    public string Color { get; set; } = "text-primary";
    public DateTime Timestamp { get; set; }
    public string RelativeTime { get; set; } = string.Empty;
}
