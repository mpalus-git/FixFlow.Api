using FixFlow.Api.Common.Persistence;
using FixFlow.Api.Common.Time;
using FixFlow.Api.Domain.WorkOrders;
using Microsoft.EntityFrameworkCore;

namespace FixFlow.Api.Features.WorkOrders;

public static class WorkOrderNumbering
{
    public static async Task AssignNextNumberAsync(this FixFlowDbContext dbContext, WorkOrder workOrder, CancellationToken cancellationToken)
    {
        var year = BusinessTime.From(workOrder.CreatedAt).Year;
        var sequences = await dbContext.Database
            .SqlQuery<int>($"""
                INSERT INTO work_order_number_counters (year, last_number) VALUES ({year}, 1)
                ON CONFLICT (year) DO UPDATE SET last_number = work_order_number_counters.last_number + 1
                RETURNING last_number AS "Value"
                """)
            .ToListAsync(cancellationToken);

        workOrder.AssignNumber(WorkOrderNumber.Format(year, sequences.Single()));
    }
}
