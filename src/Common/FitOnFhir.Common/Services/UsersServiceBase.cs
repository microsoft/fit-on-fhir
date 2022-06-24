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

        public async Task EnsurePatientAndUser(string platformName, string platformIdentifier, string platformSystem, AuthState state, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNullOrWhiteSpace(platformName, nameof(platformName));
            EnsureArg.IsNotNullOrWhiteSpace(platformIdentifier, nameof(platformIdentifier));
            EnsureArg.IsNotNullOrWhiteSpace(platformSystem, nameof(platformSystem));
            string externalPatientId = EnsureArg.IsNotNullOrWhiteSpace(state.PatientId, nameof(state.PatientId));
            string externalSystem = EnsureArg.IsNotNullOrWhiteSpace(state.System, nameof(state.System));

            // 1. Check if a Patient exists that contains the EXTERNAL Patient Identifier.
            Patient patient = await _resourceManagementService.GetResourceByIdentityAsync<Patient>(externalPatientId, externalSystem);

            Identifier identifierToAdd;

            if (patient == null)
            {
                // 2. There is no Patient that exists with the EXTERNAL Patient Identifier.
                // Create or get the Patient Resource using the PLATFORM user identifier.
                patient = await _resourceManagementService.EnsureResourceByIdentityAsync<Patient>(
                    platformIdentifier,
                    platformSystem,
                    (p, id) => p.Identifier = new List<Identifier> { id });

                // Set the identifierToAdd so the EXTERNAL Patient identifier is included in the Patient identifiers.
                identifierToAdd = new Identifier(externalSystem, externalPatientId);
            }
            else
            {
                // A Patient exists with the EXTERNAL Patient identifier.
                // Set the identifierToAdd to ensure the PLATFORM Patient identifier is included
                // in the Patient identifiers.
                identifierToAdd = new Identifier(platformSystem, platformIdentifier);
            }

            if (!patient.Identifier.Any(i =>
                {
                    return i.System.Equals(identifierToAdd.System, StringComparison.OrdinalIgnoreCase) &&
                    i.Value.Equals(identifierToAdd.Value, StringComparison.OrdinalIgnoreCase);
                }))
            {
                // 3. A required identifier is not included in the Patient, so the identifierToAdd must be added.
                // This ensures that both identifiers are included in the Patient Resource.
                patient.Identifier.Add(identifierToAdd);
                await _resourceManagementService.FhirService.UpdateResourceAsync(patient, cancellationToken: cancellationToken);
            }

            // Check to see if the user exists in the repository.
            User user = await _usersTableRepository.GetById(patient.Id, cancellationToken);

            if (user == null)
            {
                // If a user does not exist, create a new user and add it to the repository.
                user = new User(Guid.Parse(patient.Id));
                user.AddPlatformUserInfo(new PlatformUserInfo(platformName, platformIdentifier, DataImportState.ReadyToImport));
                await _usersTableRepository.Insert(user, cancellationToken);
            }
            else
            {
                user.UpdateImportState(platformName, DataImportState.ReadyToImport);
                await _usersTableRepository.Update(user, cancellationToken);
            }

            await QueueFitnessImport(user, cancellationToken);
        }

        public abstract Task QueueFitnessImport(User user, CancellationToken cancellationToken);
    }
}
