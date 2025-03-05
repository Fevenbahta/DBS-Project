using System.Threading.Tasks;
using DTO;

namespace IRepository
{
    public interface IEthswichRepositoryAPI
    {
        Task<FinInsInsResponseDTO> CreateEthswichTransaction(decimal amount, string accountNo, string instId);
    }
}
