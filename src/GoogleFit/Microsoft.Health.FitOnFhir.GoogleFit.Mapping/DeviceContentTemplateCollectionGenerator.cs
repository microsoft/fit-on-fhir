// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Google.Apis.Fitness.v1.Data;
using Microsoft.Health.Fhir.Ingest.Template.Generator;
using Newtonsoft.Json.Linq;

namespace Microsoft.Health.FitOnFhir.GoogleFit.Mapping
{
    public class DeviceContentTemplateCollectionGenerator : TemplateCollectionGenerator<DataSource>
    {
        private readonly DeviceContentTemplateGenerator _generator;

        public DeviceContentTemplateCollectionGenerator(params string[] includedDataStreamExpressions)
        {
            _generator = new DeviceContentTemplateGenerator(includedDataStreamExpressions);
        }

        protected override bool RequireUniqueTemplateTypeNames => true;

        protected override TemplateCollectionType CollectionType => TemplateCollectionType.CollectionContent;

        public override async Task<JArray> GetTemplates(DataSource model, CancellationToken cancellationToken)
        {
            return await _generator.GenerateTemplates(model, cancellationToken);
        }
    }
}
