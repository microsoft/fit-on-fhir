using System.Threading.Tasks;

namespace GoogleFitOnFhir.Repositories
{
    public interface IUsersKeyvaultRepository
    {
        Task Upsert(string secretName, string value);

        Task<string> GetByName(string secretName);
    }
}