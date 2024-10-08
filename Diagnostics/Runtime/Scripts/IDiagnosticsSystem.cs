
using Reflectis.SDK.Core;
using Reflectis.SDK.Utilities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Reflectis.SDK.Diagnostics
{
    public interface IDiagnosticsSystem : ISystem
    {
        public static Dictionary<EDiagnosticType, List<EDiagnosticVerb>> VerbsTypes =
            new Dictionary<EDiagnosticType, List<EDiagnosticVerb>>
            {
                {
                    EDiagnosticType.Experience,
                    new List<EDiagnosticVerb>
                    {
                        EDiagnosticVerb.ExpJoin,
                        EDiagnosticVerb.ExpStart,
                        EDiagnosticVerb.ExpComplete,
                        EDiagnosticVerb.StepStart,
                        EDiagnosticVerb.StepComplete,
                        EDiagnosticVerb.ExpTranscript
                    }
                }
            };

        public static Dictionary<EDiagnosticVerb, Type> VerbsDTOs =
            new Dictionary<EDiagnosticVerb, Type>
            {
                {
                    EDiagnosticVerb.ExpJoin,
                    typeof(ExperienceJoinDTO)
                },
                {
                    EDiagnosticVerb.ExpStart,
                    typeof(ExperienceStartDTO)
                },
                {
                    EDiagnosticVerb.ExpComplete,
                    typeof(ExperienceCompleteDTO)
                },
                {
                    EDiagnosticVerb.StepStart,
                    typeof(ExperienceStepStartDTO)
                },
                {
                    EDiagnosticVerb.StepComplete,
                    typeof(ExperienceStepCompleteDTO)
                },
                {
                    EDiagnosticVerb.ExpTranscript,
                    typeof(ExperienceTranscriptDTO)
                },
            };
        Task GenerateExperienceGUID(string key);

        void SendDiagnostic(EDiagnosticVerb verb, DiagnosticDTO diagnosticDTO, IEnumerable<Property> customProperties);
    }
}
