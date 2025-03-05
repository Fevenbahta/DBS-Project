using DTO;
using System.Threading.Tasks;

namespace IRepository
{
    public interface IMpesaRepositoryAPI
    {
        Task<FinInsInsResponseDTO> CreateMpesaTransfer(decimal amount, string phoneNo);
    }
}
