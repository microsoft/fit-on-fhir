using System;
using System.Collections.Generic;
using GoogleFitOnFhir.Models;
using GoogleFitOnFhir.Repositories;
using Microsoft.Extensions.Logging;

namespace GoogleFitOnFhir.Services
{
    /// <summary>
    /// User Service.
    /// </summary>
    public class UsersService : IUsersService
    {
        private IUsersTableRepository usersTableRepository;

        private ILogger<UsersService> logger;

        public UsersService(IUsersTableRepository usersTableRepository, ILogger<UsersService> logger)
        {
            this.usersTableRepository = usersTableRepository;
            this.logger = logger;
        }

        public void Initiate(User user)
        {
            this.usersTableRepository.Upsert(user);
        }

        public void ImportFitnessData(User user)
        {
            // Update LastSync column
            user.LastSync = DateTime.Now;
            this.usersTableRepository.Update(user);
        }

        public void QueueFitnessImport(User user)
        {
        }
    }
}