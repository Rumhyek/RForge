using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace RForge.Blazor.AutoApi.Generator
{
    /// <summary>
    /// Generates minimal API endpoints for classes marked [RForgeAutoApiHost] and HttpClient implementations
    /// for classes marked [RForgeAutoApiClient].
    /// </summary>
    [Generator(LanguageNames.CSharp)]
    public sealed class AutoApiGenerator : IIncrementalGenerator
    {
        /// <inheritdoc />
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            Register(context, ModelBuilder.HostAttribute, MarkerKind.Host);
            Register(context, ModelBuilder.ClientAttribute, MarkerKind.Client);
        }

        private static void Register(IncrementalGeneratorInitializationContext context, string attribute, MarkerKind kind)
        {
            var markers = context.SyntaxProvider.ForAttributeWithMetadataName(
                attribute,
                static (node, _) => node is ClassDeclarationSyntax,
                (ctx, ct) => ModelBuilder.GetMarkerInfo(ctx, kind, ct));

            // Interfaces usually live in referenced assemblies, so analysis must re-run when references change.
            var models = markers.Collect()
                .Combine(context.CompilationProvider)
                .Select(static (pair, ct) => pair.Left
                    .Select(marker => ModelBuilder.Analyze(pair.Right, marker, ct))
                    .ToImmutableArray()
                    .ToEquatableArray());

            context.RegisterSourceOutput(models, (spc, all) =>
            {
                for (var i = 0; i < all.Length; i++)
                {
                    var model = all[i];
                    if (i > 0)
                    {
                        spc.ReportDiagnostic(DiagnosticInfo.Create(Diagnostics.MultipleMarkers, model.Location,
                            kind == MarkerKind.Host ? "RForgeAutoApiHost" : "RForgeAutoApiClient", model.ClassName).ToDiagnostic());
                        continue;
                    }

                    foreach (var diagnostic in model.Diagnostics) spc.ReportDiagnostic(diagnostic.ToDiagnostic());

                    if (kind == MarkerKind.Host) ServerEmitter.Emit(spc, model);
                    else ClientEmitter.Emit(spc, model);
                }
            });
        }
    }
}
