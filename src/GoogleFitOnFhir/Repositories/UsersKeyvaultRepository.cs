using GoogleFitOnFhir.Persistence;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Extensions.Logging;

namespace GoogleFitOnFhir.Repositories
{
    public class UsersKeyvaultRepository : IUsersKeyvaultRepository
    {
        private readonly UsersKeyvaultContext keyvaultContext;

        private readonly IKeyVaultClient keyVaultClient;

        private readonly AzureServiceTokenProvider tokenProvider;

        private readonly ILogger<UsersKeyvaultRepository> logger;

        public UsersKeyvaultRepository(
            UsersKeyvaultContext keyvaultContext,
            ILogger<UsersKeyvaultRepository> logger)
        {
            this.keyvaultContext = keyvaultContext;
            this.logger = logger;

            this.tokenProvider = new AzureServiceTokenProvider();
            this.keyVaultClient = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(this.tokenProvider.KeyVaultTokenCallback));
        }

        public async void Upsert(string secretName, string value)
        {
            await this.keyVaultClient.SetSecretAsync(
                this.keyvaultContext.Uri,
                secretName,
                value);
        }
    }
}