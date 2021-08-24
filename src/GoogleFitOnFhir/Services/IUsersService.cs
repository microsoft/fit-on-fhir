using System.Threading.Tasks;
using GoogleFitOnFhir.Models;

namespace GoogleFitOnFhir.Services
{
    public interface IUsersService
    {
        Task<User> Initiate(string accessCode);

        Task ImportFitnessData(string userId);

        void QueueFitnessImport(User user);
    }
}