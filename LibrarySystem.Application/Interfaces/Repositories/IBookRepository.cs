using LibrarySystem.Domain.Entities;

namespace LibrarySystem.Application.Interfaces.Repositories;

public interface IBookRepository
{
    Task<List<Book>> GetAllAsync();

    Task<Book?> GetByIdAsync(int id);

    Task AddAsync(Book book);

    Task DeleteAsync(Book book);
}