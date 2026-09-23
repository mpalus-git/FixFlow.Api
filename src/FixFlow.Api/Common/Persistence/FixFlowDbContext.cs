using Microsoft.EntityFrameworkCore;

namespace FixFlow.Api.Common.Persistence;

public sealed class FixFlowDbContext(DbContextOptions<FixFlowDbContext> options) : DbContext(options);
