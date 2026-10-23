// Checks that every type and member an assembly references in Umbraco.* assemblies exists,
// on the same declaring type, in another set of Umbraco assemblies.
//
// The package is compiled against Umbraco 17 and run on 18 as-is. A member that moves between
// majors (e.g. IPublishedContent.Name moving to IPublishedElement in 18) still compiles on
// both, but the 17 build throws MissingMethodException on 18. Tests only catch that on the code
// paths they exercise; this checks every reference in the DLL.
//
// Usage: ReferenceCheck <assembly.dll> <probe-dir>...
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

if (args.Length < 2)
{
    Console.Error.WriteLine("Usage: ReferenceCheck <assembly.dll> <probe-dir>...");
    return 2;
}

var dll = args[0];
var probeDirs = args.Skip(1).ToArray();
var paths = probeDirs.SelectMany(d => Directory.GetFiles(d, "*.dll")).Concat(Directory.GetFiles(Path.GetDirectoryName(typeof(object).Assembly.Location)!, "*.dll"))
    .GroupBy(Path.GetFileName).Select(g => g.First());
using var mlc = new MetadataLoadContext(new PathAssemblyResolver(paths));

using var pe = new PEReader(File.OpenRead(dll));
var md = pe.GetMetadataReader();

string AsmOf(EntityHandle scope) => scope.Kind == HandleKind.AssemblyReference ? md.GetString(md.GetAssemblyReference((AssemblyReferenceHandle)scope).Name) : "";
(string asm, string name)? TypeRefName(TypeReferenceHandle h)
{
    var t = md.GetTypeReference(h);
    if (t.ResolutionScope.Kind == HandleKind.TypeReference)
    {
        var outer = TypeRefName((TypeReferenceHandle)t.ResolutionScope);
        return outer is null ? null : (outer.Value.asm, outer.Value.name + "+" + md.GetString(t.Name));
    }
    var ns = md.GetString(t.Namespace);
    return (AsmOf(t.ResolutionScope), (ns.Length > 0 ? ns + "." : "") + md.GetString(t.Name));
}
Type? Resolve(string asm, string name) { try { return mlc.LoadFromAssemblyName(asm).GetType(name); } catch { return null; } }

int checkedCount = 0, problems = 0;
foreach (var h in md.TypeReferences)
{
    if (TypeRefName(h) is not { } n || !n.asm.StartsWith("Umbraco")) continue;
    checkedCount++;
    if (Resolve(n.asm, n.name) is null) { problems++; Console.WriteLine($"MISSING TYPE {n.asm}: {n.name}"); }
}
foreach (var h in md.MemberReferences)
{
    var m = md.GetMemberReference(h);
    TypeReferenceHandle? parent = m.Parent.Kind switch
    {
        HandleKind.TypeReference => (TypeReferenceHandle)m.Parent,
        HandleKind.TypeSpecification => GenericBase(md.GetTypeSpecification((TypeSpecificationHandle)m.Parent)),
        _ => null,
    };
    if (parent is null || TypeRefName(parent.Value) is not { } n || !n.asm.StartsWith("Umbraco")) continue;
    var name = md.GetString(m.Name);
    var type = Resolve(n.asm, n.name);
    checkedCount++;
    if (type is null) continue; // reported above
    const BindingFlags all = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
    var found = m.GetKind() == MemberReferenceKind.Field
        ? type.GetField(name, all) is not null
        : type.GetMember(name, all).OfType<MethodBase>().Any(x => x.GetParameters().Length == ParamCount(m));
    if (!found) { problems++; Console.WriteLine($"MISSING MEMBER {n.name}::{name}"); }
}
Console.WriteLine($"Checked {checkedCount} Umbraco references, {problems} problem(s).");
return problems == 0 ? 0 : 1;

TypeReferenceHandle? GenericBase(TypeSpecification spec)
{
    var r = md.GetBlobReader(spec.Signature);
    if (r.ReadSignatureTypeCode() != SignatureTypeCode.GenericTypeInstance) return null;
    r.ReadSignatureTypeCode();
    var h = r.ReadTypeHandle();
    return h.Kind == HandleKind.TypeReference ? (TypeReferenceHandle)h : null;
}
int ParamCount(MemberReference m) { var r = md.GetBlobReader(m.Signature); var header = r.ReadSignatureHeader(); if (header.IsGeneric) r.ReadCompressedInteger(); return r.ReadCompressedInteger(); }
