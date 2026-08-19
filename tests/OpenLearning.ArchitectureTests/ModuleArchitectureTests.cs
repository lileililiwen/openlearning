using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ArchUnitNET.Domain;
using ArchUnitNET.Fluent.Extensions;
using ArchUnitNET.Fluent.Syntax.Elements.Types;
using ArchUnitNET.Loader;
using ArchUnitNET.xUnit;
using Xunit;
using static ArchUnitNET.Fluent.ArchRuleDefinition;
using Assembly = System.Reflection.Assembly;

namespace OpenLearning.ArchitectureTests;

/// <summary>
/// Machine-checked modular-monolith rules from Agents.md §2.
///
/// FIXTURE MAINTENANCE: when a new module is added, add its assembly to
/// <see cref="_moduleAssemblyNames"/>, its allowed OpenLearning dependencies to
/// <see cref="_allowedModuleDependencies"/>, and its name to the Web
/// composition-root expectation. A failing architecture test is the signal
/// that the fixture (or the module graph) is out of date.
/// </summary>
public sealed class ModuleArchitectureTests
{
    private static readonly string[] _moduleAssemblyNames =
    {
        "OpenLearning.Auth",
        "OpenLearning.CourseManagement",
        "OpenLearning.Enrollment",
        "OpenLearning.Progress",
        "OpenLearning.Assessments",
        "OpenLearning.Ecommerce",
        "OpenLearning.Scorm",
        "OpenLearning.Chat",
        "OpenLearning.Ratings",
        "OpenLearning.Certificates",
        "OpenLearning.Notifications",
        "OpenLearning.UserManagement",
        "OpenLearning.Storage",
        "OpenLearning.Logging",
        "OpenLearning.SystemConfig",
        "OpenLearning.Memberships",
        "OpenLearning.Operations",
        "OpenLearning.Assignments",
        "OpenLearning.StudyTools",
        "OpenLearning.Settlement",
        "OpenLearning.QuestionIO",
        "OpenLearning.StudentIO",
        "OpenLearning.GradeExport",
        "OpenLearning.Exams",
        "OpenLearning.Navigation",
        "OpenLearning.Classes",
        "OpenLearning.Community",
        "OpenLearning.Data",
        "OpenLearning.Web",
    };

    /// <summary>Declared OpenLearning module dependencies (Agents.md §2, csproj graph).</summary>
    private static readonly IReadOnlyDictionary<string, string[]> _allowedModuleDependencies =
        new Dictionary<string, string[]>
        {
            ["OpenLearning.Auth"] = Array.Empty<string>(),
            ["OpenLearning.CourseManagement"] = new[] { "OpenLearning.Auth" },
            ["OpenLearning.Enrollment"] = new[] { "OpenLearning.Auth", "OpenLearning.CourseManagement" },
            ["OpenLearning.Progress"] = new[] { "OpenLearning.Auth", "OpenLearning.CourseManagement", "OpenLearning.Enrollment" },
            ["OpenLearning.Assessments"] = new[] { "OpenLearning.Auth", "OpenLearning.CourseManagement", "OpenLearning.Enrollment" },
            ["OpenLearning.Ecommerce"] = new[] { "OpenLearning.Auth", "OpenLearning.CourseManagement", "OpenLearning.Enrollment" },
            ["OpenLearning.Scorm"] = new[] { "OpenLearning.Auth", "OpenLearning.CourseManagement", "OpenLearning.Enrollment", "OpenLearning.Progress" },
            ["OpenLearning.Chat"] = new[] { "OpenLearning.Auth", "OpenLearning.CourseManagement", "OpenLearning.Enrollment" },
            ["OpenLearning.Ratings"] = new[] { "OpenLearning.Auth", "OpenLearning.CourseManagement", "OpenLearning.Enrollment" },
            ["OpenLearning.Certificates"] = new[] { "OpenLearning.Auth", "OpenLearning.CourseManagement", "OpenLearning.Enrollment", "OpenLearning.Progress" },
            ["OpenLearning.Notifications"] = new[] { "OpenLearning.Auth", "OpenLearning.CourseManagement", "OpenLearning.Enrollment" },
            ["OpenLearning.UserManagement"] = new[] { "OpenLearning.Auth", "OpenLearning.CourseManagement", "OpenLearning.Enrollment" },
            ["OpenLearning.Storage"] = Array.Empty<string>(),
            ["OpenLearning.Logging"] = Array.Empty<string>(),
            ["OpenLearning.SystemConfig"] = new[] { "OpenLearning.Notifications" },
            ["OpenLearning.Memberships"] = Array.Empty<string>(),
            ["OpenLearning.Operations"] = Array.Empty<string>(),
            ["OpenLearning.Assignments"] = Array.Empty<string>(),
            ["OpenLearning.StudyTools"] = new[] { "OpenLearning.Auth", "OpenLearning.CourseManagement", "OpenLearning.Enrollment", "OpenLearning.Progress" },
            ["OpenLearning.Settlement"] = new[] { "OpenLearning.CourseManagement" },
            ["OpenLearning.QuestionIO"] = new[]
            {
                "OpenLearning.Auth",
                "OpenLearning.Assessments",
                "OpenLearning.AsyncIO",
                "OpenLearning.Jobs",
                "OpenLearning.Notifications",
                "OpenLearning.Storage",
                "OpenLearning.SystemConfig",
            },
            ["OpenLearning.StudentIO"] = new[]
            {
                "OpenLearning.AsyncIO",
                "OpenLearning.Auth",
                "OpenLearning.Classes",
                "OpenLearning.CourseManagement",
                "OpenLearning.Ecommerce",
                "OpenLearning.Enrollment",
                "OpenLearning.Jobs",
                "OpenLearning.Logging",
                "OpenLearning.Notifications",
                "OpenLearning.Storage",
            },
            ["OpenLearning.GradeExport"] = new[]
            {
                "OpenLearning.AsyncIO",
                "OpenLearning.Assessments",
                "OpenLearning.Assignments",
                "OpenLearning.Auth",
                "OpenLearning.Certificates",
                "OpenLearning.Classes",
                "OpenLearning.CourseManagement",
                "OpenLearning.Enrollment",
                "OpenLearning.Exams",
                "OpenLearning.Jobs",
                "OpenLearning.Logging",
                "OpenLearning.Progress",
                "OpenLearning.Storage",
                "OpenLearning.SystemConfig",
            },
            ["OpenLearning.Exams"] = new[] { "OpenLearning.Auth", "OpenLearning.CourseManagement", "OpenLearning.Enrollment", "OpenLearning.Assessments" },
            ["OpenLearning.Navigation"] = new[] { "OpenLearning.Auth", "OpenLearning.SystemConfig" },
            ["OpenLearning.Classes"] = new[] { "OpenLearning.Auth", "OpenLearning.CourseManagement", "OpenLearning.Enrollment", "OpenLearning.Notifications" },
            ["OpenLearning.Community"] = new[] { "OpenLearning.Auth", "OpenLearning.CourseManagement", "OpenLearning.Enrollment" },
        };

    private static readonly Architecture _architecture = new ArchLoader()
        .LoadAssemblies(_moduleAssemblyNames.Select(Assembly.Load).ToArray())
        .Build();

    private static readonly IObjectProvider<IType> _dataTypes = Types()
        .That().ResideInNamespaceMatching(@"^OpenLearning\.Data($|\.)");

    private static readonly IObjectProvider<IType> _applicationDbContextType = Types()
        .That().HaveFullName("OpenLearning.Data.ApplicationDbContext");


    private static GivenTypesConjunction TypesInNamespaces(params string[] namespaces)
    {
        // Partial-match: each alternative covers the namespace itself (via $)
        // or any sub-namespace (via the dot), without anchoring the end.
        var patterns = namespaces.Select(ns => $"{Regex.Escape(ns)}(\\.|$)");
        return Types().That().ResideInNamespaceMatching($"^({string.Join("|", patterns)})");
    }

    [Fact]
    public void Modules_do_not_depend_on_OpenLearning_Data()
    {
        Types().That().ResideInNamespaceMatching(@"^OpenLearning($|\.)")
            .And().DoNotResideInNamespaceMatching(@"^OpenLearning\.Data($|\.)")
            .And().DoNotResideInNamespaceMatching(@"^OpenLearning\.Web($|\.)")
            .Should().NotDependOnAny(_dataTypes)
            .Because("modules must never reference OpenLearning.Data; services depend on the base DbContext")
            .WithoutRequiringPositiveResults()
            .Check(_architecture);
    }

    [Fact]
    public void Modules_do_not_depend_on_the_concrete_ApplicationDbContext()
    {
        Types().That().ResideInNamespaceMatching(@"^OpenLearning($|\.)")
            .And().DoNotResideInNamespaceMatching(@"^OpenLearning\.Data($|\.)")
            .And().DoNotResideInNamespaceMatching(@"^OpenLearning\.Web($|\.)")
            .Should().NotDependOnAny(_applicationDbContextType)
            .Because("services inject the base Microsoft.EntityFrameworkCore.DbContext to avoid circular references")
            .WithoutRequiringPositiveResults()
            .Check(_architecture);
    }

    [Fact]
    public void Module_dependency_graph_matches_Agents_md()
    {
        foreach (var (module, allowedDependencies) in _allowedModuleDependencies)
        {
            var forbidden = _moduleAssemblyNames
                .Where(ns => ns != module && !allowedDependencies.Contains(ns))
                .ToArray();

            Types().That().ResideInNamespace(module)
                .Should().NotDependOnAny(TypesInNamespaces(forbidden))
                .Because($"'{module}' may only depend on: {(allowedDependencies.Length == 0 ? "no other OpenLearning module" : string.Join(", ", allowedDependencies))}")
                .Check(_architecture);
        }
    }

    [Fact]
    public void Modules_do_not_reference_Web()
    {
        Types().That().ResideInNamespaceMatching(@"^OpenLearning($|\.)")
            .And().DoNotResideInNamespaceMatching(@"^OpenLearning\.Web($|\.)")
            .Should().NotDependOnAny(Types().That().ResideInNamespaceMatching(@"^OpenLearning\.Web($|\.)"))
            .Because("the Web project is the composition root; no module may depend on it")
            .WithoutRequiringPositiveResults()
            .Check(_architecture);
    }

    [Fact]
    public void Web_is_the_composition_root_and_references_every_module()
    {
        var webAssembly = Assembly.Load("OpenLearning.Web");
        var referenced = webAssembly.GetReferencedAssemblies().Select(a => a.Name).ToHashSet();

        foreach (var module in _moduleAssemblyNames.Where(ns => ns != "OpenLearning.Web"))
        {
            Assert.True(
                referenced.Contains(module),
                $"OpenLearning.Web (composition root) must reference '{module}'.");
        }
    }
}

