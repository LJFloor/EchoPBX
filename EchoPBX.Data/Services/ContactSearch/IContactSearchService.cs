namespace EchoPBX.Data.Services.ContactSearch;

public interface IContactSearchService
{
    /// <summary>
    /// Searches for a contact by phone number.
    /// </summary>
    /// <param name="phoneNumber">The phone number. Can be in any format.</param>
    public Task<Models.Contact?> Search(string phoneNumber);
}