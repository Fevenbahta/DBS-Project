using DTO;
using System.Threading.Tasks;

namespace IRepository
{
    public interface ITelebirrRepositoryAPI
    {
        Task<FinInsInsResponseDTO> CreateTelebirrTransaction(decimal amount, string phoneNo);
    }
}
