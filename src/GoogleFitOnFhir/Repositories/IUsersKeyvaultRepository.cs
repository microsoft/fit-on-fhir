namespace GoogleFitOnFhir.Repositories
{
    public interface IUsersKeyvaultRepository
    {
        void Upsert(string secretName, string value);
    }
}