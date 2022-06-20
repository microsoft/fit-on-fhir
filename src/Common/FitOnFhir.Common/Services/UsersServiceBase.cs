// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using FitOnFhir.Common.Models;
using FitOnFhir.Common.Repositories;
using Microsoft.Health.Extensions.Fhir.Service;
using Identifier = Hl7.Fhir.Model.Identifier;
using Patient = Hl7.Fhir.Model.Patient;

namespace FitOnFhir.Common.Services
{
    public abstract class UsersServiceBase
    {
        private readonly ResourceManagementService _resourceManagementService;
        private readonly IUsersTableRepository _usersTableRepository;

        public UsersServiceBase(ResourceManagementService resourceManagementService, IUsersTableRepository usersTableRepository)
        {
            _resourceManagementService = EnsureArg.IsNotNull(resourceManagementService, nameof(resourceManagementService));
            _usersTableRepository = EnsureArg.IsNotNull(usersTableRepository, nameof(usersTableRepository));
        }

        public async Task EnsurePatientAndUser(string platformName, string identifier, string system, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNullOrWhiteSpace(platformName, nameof(platformName));
            EnsureArg.IsNotNullOrWhiteSpace(identifier, nameof(identifier));
            EnsureArg.IsNotNullOrWhiteSpace(system, nameof(system));

            // Get or create a Patient resource.
            Patient patient = await _resourceManagementService.EnsureResourceByIdentityAsync<Patient>(
                identifier,
                system,
                (p, id) => p.Identifier = new List<Identifier> { id });

            // Check to see if the user exists in the repository.
            User user = await _usersTableRepository.GetById(patient.Id, cancellationToken);

            if (user == null)
            {
                // If a user does not exist, create a new user and add it to the repository.
                user = new User(Guid.Parse(patient.Id));
                user.AddPlatformUserInfo(new PlatformUserInfo(platformName, identifier, DataImportState.ReadyToImport));
                await _usersTableRepository.Insert(user, cancellationToken);
            }
        }
    }
}
