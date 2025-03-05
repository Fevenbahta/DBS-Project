using DTO;
using System.Threading.Tasks;

namespace IRepository
{
    public interface IAwachRepositoryAPI
    {
        Task<FinInsInsResponseDTO> CreateAwachTransfer(decimal amount, string accountNo);
    }
}
