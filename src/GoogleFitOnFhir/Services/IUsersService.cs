using System.Threading.Tasks;
using GoogleFitOnFhir.Models;

namespace GoogleFitOnFhir.Services
{
    public interface IUsersService
    {
        Task<User> Initiate(string accessCode);

        void ImportFitnessData(User user);

        void QueueFitnessImport(User user);
    }
}