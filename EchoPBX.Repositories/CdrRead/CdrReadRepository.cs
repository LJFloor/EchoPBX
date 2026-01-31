using EchoPBX.Data;
using Microsoft.EntityFrameworkCore;

namespace EchoPBX.Repositories.CdrRead;

public class CdrReadRepository(EchoDbContext dbContext) : ICdrReadRepository
{
    public async Task<Models.CdrEntry[]> List(int n = 100)
    {
        return await dbContext.Cdr
            .OrderByDescending(x => x.Id)
            .Take(n)
            .Select(x => new Models.CdrEntry
            {
                Id = x.Id,
                Direction = x.Direction,
                Destination = x.Destination,
                Source = x.Source,
                Answer = x.Answer,
                Duration = x.Duration,
                End = x.End,
                Start = x.Start
            }).ToArrayAsync();
    }
}