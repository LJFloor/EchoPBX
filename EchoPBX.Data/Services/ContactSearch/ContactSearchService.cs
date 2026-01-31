using EchoPBX.Data.Helpers;
using Microsoft.EntityFrameworkCore;

namespace EchoPBX.Data.Services.ContactSearch;

public class ContactSearchService(EchoDbContext dbContext) : IContactSearchService
{
    public async Task<Models.Contact?> Search(string phoneNumber)
    {
        var cleanedNumber = StringHelper.CleanPhoneNumber(phoneNumber);
        if (cleanedNumber.Length > 7)
        {
            cleanedNumber = cleanedNumber[^7..];
        }

        var query = dbContext.Contacts.AsQueryable();

        var contact = await query
            .Where(x => x.PhoneNumber.EndsWith(cleanedNumber))
            .Select(x => new
            {
                x.Name,
                x.PhoneNumber,
            })
            .FirstOrDefaultAsync();

        return contact == null
            ? null
            : new Models.Contact(contact.Name, contact.PhoneNumber);
    }
}