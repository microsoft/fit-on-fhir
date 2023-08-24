// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.Extensions.Fhir.Service;
using Microsoft.Health.FitOnFhir.Common.Interfaces;
using Microsoft.Health.FitOnFhir.Common.Models;
using Microsoft.Health.FitOnFhir.Common.Repositories;
using NSubstitute;
using Xunit;
using Bundle = Hl7.Fhir.Model.Bundle;
using Identifier = Hl7.Fhir.Model.Identifier;
using Patient = Hl7.Fhir.Model.Patient;
using Received = NSubstitute.Received;
using Resource = Hl7.Fhir.Model.Resource;
using ResourceType = Hl7.Fhir.Model.ResourceType;

namespace Microsoft.Health.FitOnFhir.Common.Tests
{
    public abstract class UsersServiceBaseTests
    {
        private static IFhirService _fhirService;

        public UsersServiceBaseTests()
        {
            _fhirService = Substitute.For<IFhirService>();
            ResourceService = new ResourceManagementService(_fhirService);
            UsersTableRepository = Substitute.For<IUsersTableRepository>();
            QueueService = Substitute.For<IQueueService>();
            AuthStateService = Substitute.For<IAuthStateService>();

            // Default responses.
            _fhirService.SearchForResourceAsync(ResourceType.Patient, Arg.Any<string>()).Returns(Task.FromResult(GetBundle(EnsurePatient(null, true, true))));
            _fhirService.UpdateResourceAsync(Arg.Any<Patient>(), null, null, default).Returns(x => EnsurePatient(x.ArgAt<Patient>(0), false, false));
            AuthStateService.RetrieveAuthState(Arg.Is<string>(str => str == AuthorizationNonce), Arg.Any<CancellationToken>()).Returns(RetrieveAuthStateReturnFunc());
        }

        protected ResourceManagementService ResourceService { get; }

        protected IUsersTableRepository UsersTableRepository { get; }

        protected IQueueService QueueService { get; }

        protected IAuthStateService AuthStateService { get; }

        protected string AuthorizationNonce => "ABCDEFGHIJKLMNOPQRSTUVWX";

        protected abstract Func<AuthState> RetrieveAuthStateReturnFunc { get; }

        protected abstract Func<Task> ExecuteAuthorizationCallback { get; }

        protected abstract Func<Task> ExecuteRevokeAccess { get;  }

        protected abstract string ExpectedPatientId { get; }

        protected abstract string ExpectedPatientIdentifierSystem { get; }

        protected abstract string ExpectedPlatformUserId { get; }

        protected abstract string ExpectedPlatform { get; }

        protected abstract string ExpectedExternalPatientId { get; }

        protected abstract string ExpectedExternalSystem { get; }

        protected abstract string ExpectedAccessToken { get;  }

        [Fact]
        public async Task GivenPatientAndUserDoNotExist_WhenProcessAuthorizationCallbackCalled_NewPatientAndUserCreatedAndMessageQueued()
        {
            _fhirService.SearchForResourceAsync(ResourceType.Patient, Arg.Any<string>(), default, Arg.Any<CancellationToken>()).Returns(Task.FromResult(new Bundle()));
            UsersTableRepository.GetById(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<User>(null));

            await ExecuteAuthorizationCallback();

            await UsersTableRepository.DidNotReceive().Update(Arg.Any<User>(), Arg.Any<Func<User, User, User>>(), Arg.Any<CancellationToken>());

            Received.InOrder(async () =>
            {
                await _fhirService.Received(1).SearchForResourceAsync(ResourceType.Patient, $"identifier={ExpectedExternalSystem}|{ExpectedExternalPatientId}", default, Arg.Any<CancellationToken>());
                await _fhirService.Received(1).UpdateResourceAsync(Arg.Is<Patient>(x => IsExpected(x, true, false)), null, null, default);
                await _fhirService.Received(1).UpdateResourceAsync(Arg.Is<Patient>(x => IsExpected(x, true, true)), "W/\"1\"", null, default);
                await UsersTableRepository.Received(1).GetById(Arg.Any<string>(), Arg.Any<CancellationToken>());
                await UsersTableRepository.Received(1).Insert(Arg.Is<User>(x => IsExpected(x, DataImportState.ReadyToImport)), Arg.Any<CancellationToken>());
                await QueueService.Received(1).SendQueueMessage(Arg.Is<string>(x => x == ExpectedPatientId), Arg.Is<string>(x => x == ExpectedPlatformUserId), Arg.Is<string>(x => x == ExpectedPlatform), Arg.Any<CancellationToken>());
            });
        }

        [Fact]
        public async Task GivenPatientAndUserExistWithoutPlatform_WhenProcessAuthorizationCallbackCalled_PlatformAddedAndMessageQueued()
        {
            _fhirService.SearchForResourceAsync(ResourceType.Patient, Arg.Any<string>()).Returns(Task.FromResult(GetBundle(EnsurePatient(null, false, true))));
            UsersTableRepository.GetById(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(x => GetUser(x.ArgAt<string>(0), ExpectedPlatformUserId));

            await ExecuteAuthorizationCallback();

            await _fhirService.DidNotReceive().CreateResourceAsync(Arg.Any<Patient>());
            await UsersTableRepository.DidNotReceive().Insert(Arg.Any<User>(), Arg.Any<CancellationToken>());

            Received.InOrder(async () =>
            {
                await _fhirService.Received(1).SearchForResourceAsync(ResourceType.Patient, $"identifier={ExpectedExternalSystem}|{ExpectedExternalPatientId}");
                await _fhirService.Received(1).UpdateResourceAsync(Arg.Is<Patient>(x => IsExpected(x, true, true)), "W/\"1\"", null, Arg.Any<CancellationToken>());
                await UsersTableRepository.Received(1).GetById(Arg.Any<string>(), Arg.Any<CancellationToken>());
                await UsersTableRepository.Received(1).Update(Arg.Is<User>(x => IsExpected(x, DataImportState.ReadyToImport)), Arg.Any<Func<User, User, User>>(), Arg.Any<CancellationToken>());
                await QueueService.Received(1).SendQueueMessage(Arg.Is<string>(x => x == ExpectedPatientId), Arg.Is<string>(x => x == ExpectedPlatformUserId), Arg.Is<string>(x => x == ExpectedPlatform), Arg.Any<CancellationToken>());
            });
        }

        [Fact]
        public async Task GivenPatientHasPlatformAndUserDoesNot_WhenProcessAuthorizationCallbackCalled_PlatformAddedToUserAndMessageQueued()
        {
            _fhirService.SearchForResourceAsync(ResourceType.Patient, Arg.Any<string>()).Returns(Task.FromResult(GetBundle(EnsurePatient(null, true, true))));
            UsersTableRepository.GetById(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(x => GetUser(x.ArgAt<string>(0), ExpectedPlatformUserId, ("OtherPlatform", DataImportState.ReadyToImport)));

            await ExecuteAuthorizationCallback();

            await _fhirService.DidNotReceive().CreateResourceAsync(Arg.Any<Patient>());
            await _fhirService.DidNotReceive().UpdateResourceAsync(Arg.Any<Patient>(), "W/\"1\"", null, Arg.Any<CancellationToken>());
            await UsersTableRepository.DidNotReceive().Insert(Arg.Any<User>(), Arg.Any<CancellationToken>());

            Received.InOrder(async () =>
            {
                await _fhirService.Received(1).SearchForResourceAsync(ResourceType.Patient, $"identifier={ExpectedExternalSystem}|{ExpectedExternalPatientId}");
                await UsersTableRepository.Received(1).GetById(Arg.Any<string>(), Arg.Any<CancellationToken>());
                await UsersTableRepository.Received(1).Update(Arg.Is<User>(x => IsExpected(x, DataImportState.ReadyToImport)), Arg.Any<Func<User, User, User>>(), Arg.Any<CancellationToken>());
                await QueueService.Received(1).SendQueueMessage(Arg.Is<string>(x => x == ExpectedPatientId), Arg.Is<string>(x => x == ExpectedPlatformUserId), Arg.Is<string>(x => x == ExpectedPlatform), Arg.Any<CancellationToken>());
            });
        }

        [Fact]
        public async Task GivenPatientAndUserExistWithPlatform_WhenProcessAuthorizationCallbackCalled_MessageQueued()
        {
            _fhirService.SearchForResourceAsync(ResourceType.Patient, Arg.Any<string>()).Returns(Task.FromResult(GetBundle(EnsurePatient(null, true, true))));
            UsersTableRepository.GetById(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(x => GetUser(x.ArgAt<string>(0), ExpectedPlatformUserId, (ExpectedPlatform, DataImportState.ReadyToImport)));

            await ExecuteAuthorizationCallback();

            await _fhirService.DidNotReceive().CreateResourceAsync(Arg.Any<Patient>());
            await _fhirService.DidNotReceive().UpdateResourceAsync(Arg.Any<Patient>(), "W/\"1\"", null, Arg.Any<CancellationToken>());
            await UsersTableRepository.DidNotReceive().Insert(Arg.Any<User>(), Arg.Any<CancellationToken>());

            Received.InOrder(async () =>
            {
                await _fhirService.Received(1).SearchForResourceAsync(ResourceType.Patient, $"identifier={ExpectedExternalSystem}|{ExpectedExternalPatientId}");
                await UsersTableRepository.Received(1).GetById(Arg.Any<string>(), Arg.Any<CancellationToken>());
                await UsersTableRepository.Received(1).Update(Arg.Is<User>(x => IsExpected(x, DataImportState.ReadyToImport)), Arg.Any<Func<User, User, User>>(), Arg.Any<CancellationToken>());
                await QueueService.Received(1).SendQueueMessage(Arg.Is<string>(x => x == ExpectedPatientId), Arg.Is<string>(x => x == ExpectedPlatformUserId), Arg.Is<string>(x => x == ExpectedPlatform), Arg.Any<CancellationToken>());
            });
        }

        private Patient EnsurePatient(Patient patient, bool includePlatformIdentifier, bool includeExternalIdentifier)
        {
            patient = patient ?? new Patient();
            patient.Id = ExpectedPatientId;
            patient.Meta = new Hl7.Fhir.Model.Meta() { VersionId = "1" };

            if (includePlatformIdentifier)
            {
                var identifier = new Identifier(ExpectedPatientIdentifierSystem, ExpectedPlatformUserId);
                patient.Identifier.Add(identifier);
            }

            if (includeExternalIdentifier)
            {
                var identifier = new Identifier(ExpectedExternalSystem, ExpectedExternalPatientId);
                patient.Identifier.Add(identifier);
            }

            return patient;
        }

        private Bundle GetBundle(params Resource[] resources)
        {
            var entries = new List<Bundle.EntryComponent>();

            foreach (Resource resource in resources)
            {
                entries.Add(new Bundle.EntryComponent() { Resource = resource });
            }

            return new Bundle
            {
                Entry = entries,
            };
        }

        protected User GetUser(string userId, string platformUserId, params (string name, DataImportState state)[] platforms)
        {
            var user = new User(Guid.Parse(userId));

            foreach (var platform in platforms)
            {
                user.AddPlatformUserInfo(new PlatformUserInfo(platform.name, platformUserId, platform.state));
            }

            return user;
        }

        private bool IsExpected(Patient patient, bool verifyPlatformIdentifier, bool verifyExternalIdentifier)
        {
            bool hasPlatform = true;
            bool hasExternal = true;

            if (verifyPlatformIdentifier)
            {
                hasPlatform = patient.Identifier.Single(i => ExpectedPatientIdentifierSystem.Equals(i.System) && ExpectedPlatformUserId.Equals(i.Value)) != null;
            }

            if (verifyExternalIdentifier)
            {
                hasExternal = patient.Identifier.Single(i => ExpectedExternalSystem.Equals(i.System) && ExpectedExternalPatientId.Equals(i.Value)) != null;
            }

            return hasPlatform && hasExternal;
        }

        private bool IsExpected(User user, DataImportState expectedImportState)
        {
            return string.Equals(ExpectedPatientId, user.Id, StringComparison.OrdinalIgnoreCase) &&
                user.GetPlatformUserInfo().Any(x =>
                {
                    return string.Equals(ExpectedPlatform, x.PlatformName, StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(ExpectedPlatformUserId, x.UserId, StringComparison.OrdinalIgnoreCase) &&
                        expectedImportState == x.ImportState;
                });
        }
    }
}
