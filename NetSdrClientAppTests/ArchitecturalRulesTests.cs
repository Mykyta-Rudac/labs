using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace NetSdrClientAppTests
{
    /// <summary>
    /// Simple architectural rules enforcement tests. These tests scan the production assembly
    /// (`NetSdrClientApp`) and ensure that types in namespaces matching the "UI" pattern
    /// do not depend directly on forbidden namespaces such as "Infrastructure" or "DataAccess".
    ///
    /// The checks are intentionally conservative (they inspect field/property/method signatures
    /// and base types). If a violation is found, the test fails and will make the build red.
    /// </summary>
    public class ArchitecturalRulesTests
    {
        private Assembly _appAssembly;

        [SetUp]
        public void Setup()
        {
            // Load the production assembly that the tests reference
            _appAssembly = typeof(NetSdrClientApp.NetSdrClient).Assembly;
        }

        [Test]
        public void UI_ShouldNotDependOn_Infrastructure_Or_DataAccess()
        {
            var forbiddenFragments = new[] { ".Infrastructure", ".DataAccess" };
            var violations = new List<string>();

            var allTypes = _appAssembly.GetTypes();

            // Find candidate UI namespaces (types whose namespace contains .UI)
            var uiTypes = allTypes.Where(t => !string.IsNullOrEmpty(t.Namespace) && t.Namespace.Contains(".UI")).ToArray();

            // If there are no UI types, the rule is vacuously satisfied
            if (!uiTypes.Any())
            {
                Assert.Pass("No types in namespaces containing '.UI' found; rule is not applicable.");
                return;
            }

            foreach (var t in uiTypes)
            {
                // Check base type
                var baseType = t.BaseType;
                if (baseType != null && !string.IsNullOrEmpty(baseType.Namespace) && forbiddenFragments.Any(f => baseType.Namespace.Contains(f)))
                {
                    violations.Add($"Type {t.FullName} derives from forbidden base type {baseType.FullName}");
                }

                // Check implemented interfaces
                foreach (var i in t.GetInterfaces())
                {
                    if (!string.IsNullOrEmpty(i.Namespace) && forbiddenFragments.Any(f => i.Namespace.Contains(f)))
                        violations.Add($"Type {t.FullName} implements forbidden interface {i.FullName}");
                }

                // Fields
                foreach (var f in t.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static))
                {
                    var ns = f.FieldType?.Namespace;
                    if (!string.IsNullOrEmpty(ns) && forbiddenFragments.Any(frag => ns.Contains(frag)))
                        violations.Add($"Type {t.FullName} has field {f.Name} of forbidden type {f.FieldType?.FullName ?? "<unknown>"}");
                }

                // Properties
                foreach (var p in t.GetProperties(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static))
                {
                    var ns = p.PropertyType?.Namespace;
                    if (!string.IsNullOrEmpty(ns) && forbiddenFragments.Any(frag => ns.Contains(frag)))
                        violations.Add($"Type {t.FullName} has property {p.Name} of forbidden type {p.PropertyType?.FullName ?? "<unknown>"}");
                }

                // Methods: return type and parameters
                foreach (var m in t.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly))
                {
                    var ret = m.ReturnType?.Namespace;
                    if (!string.IsNullOrEmpty(ret) && forbiddenFragments.Any(frag => ret.Contains(frag)))
                        violations.Add($"Type {t.FullName} method {m.Name} returns forbidden type {m.ReturnType?.FullName ?? "<unknown>"}");

                    foreach (var param in m.GetParameters())
                    {
                        var pns = param.ParameterType?.Namespace;
                        if (!string.IsNullOrEmpty(pns) && forbiddenFragments.Any(frag => pns.Contains(frag)))
                            violations.Add($"Type {t.FullName} method {m.Name} has parameter {param.Name} of forbidden type {param.ParameterType?.FullName ?? "<unknown>"}");
                    }
                }
            }

            if (violations.Any())
            {
                var message = "Architectural rule violations detected:\n" + string.Join("\n", violations);
                Assert.Fail(message);
            }

            Assert.Pass("No architectural violations detected for UI -> Infrastructure/DataAccess rule.");
        }
    }
}
