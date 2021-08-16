using GoogleFitOnFhir.Models;

namespace GoogleFitOnFhir.Services
{
    public interface IUsersService
    {
        void Initiate(User user);

        void ImportFitnessData(User user);

        void QueueFitnessImport(User user);
    }
}