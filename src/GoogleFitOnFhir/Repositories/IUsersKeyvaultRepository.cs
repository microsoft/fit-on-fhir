using System.Threading.Tasks;

namespace GoogleFitOnFhir.Repositories
{
    public interface IUsersKeyvaultRepository
    {
        void Upsert(string secretName, string value);

        Task<string> GetByName(string secretName);
    }
}