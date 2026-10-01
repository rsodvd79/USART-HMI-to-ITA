using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Linq;
using System.Collections.Generic;

// args: input hmitype.dll, HmiTr.dll (net35), output
var m = ModuleDefinition.ReadModule(args[0]);
var src = ModuleDefinition.ReadModule(args[1]).GetType("HmiTr.T");
var md = m.GetType("hmitype.LanguageApp").Methods.Single(x => x.Name == "Language");
if (m.GetType("hmitype.HmiTrEmbedded") != null) throw new Exception("already patched");

var t = new TypeDefinition("hmitype", "HmiTrEmbedded", src.Attributes, m.ImportReference(src.BaseType));
m.Types.Add(t);
var fmap = new Dictionary<FieldDefinition, FieldDefinition>();
var mmap = new Dictionary<MethodDefinition, MethodDefinition>();
foreach (var f in src.Fields)
{
    var nf = new FieldDefinition(f.Name, f.Attributes, m.ImportReference(f.FieldType));
    if (f.HasConstant) nf.Constant = f.Constant;
    t.Fields.Add(nf); fmap[f] = nf;
}
foreach (var s in src.Methods)
{
    var nm = new MethodDefinition(s.Name, s.Attributes, m.ImportReference(s.ReturnType));
    nm.ImplAttributes = s.ImplAttributes;
    foreach (var p in s.Parameters) nm.Parameters.Add(new ParameterDefinition(p.Name, p.Attributes, m.ImportReference(p.ParameterType)));
    t.Methods.Add(nm); mmap[s] = nm;
}
foreach (var s in src.Methods)
{
    var nm = mmap[s];
    var b = nm.Body; b.InitLocals = s.Body.InitLocals;
    var vmap = new Dictionary<VariableDefinition, VariableDefinition>();
    foreach (var v in s.Body.Variables) { var nv = new VariableDefinition(m.ImportReference(v.VariableType)); b.Variables.Add(nv); vmap[v] = nv; }
    var imap = new Dictionary<Instruction, Instruction>();
    var il = b.GetILProcessor();
    foreach (var i in s.Body.Instructions)
    {
        var ni = Instruction.Create(OpCodes.Nop); ni.OpCode = i.OpCode; ni.Operand = i.Operand; imap[i] = ni;
    }
    foreach (var i in s.Body.Instructions)
    {
        var ni = imap[i];
        switch (i.Operand)
        {
            case Instruction x: ni.Operand = imap[x]; break;
            case Instruction[] xs: ni.Operand = xs.Select(y => imap[y]).ToArray(); break;
            case VariableDefinition v: ni.Operand = vmap[v]; break;
            case ParameterDefinition p: ni.Operand = nm.Parameters[p.Index]; break;
            case FieldDefinition f when fmap.ContainsKey(f): ni.Operand = fmap[f]; break;
            case MethodDefinition d when mmap.ContainsKey(d): ni.Operand = mmap[d]; break;
            case MethodReference r: ni.Operand = m.ImportReference(r); break;
            case FieldReference r: ni.Operand = m.ImportReference(r); break;
            case TypeReference r: ni.Operand = m.ImportReference(r); break;
        }
        b.Instructions.Add(ni);
    }
    foreach (var h in s.Body.ExceptionHandlers)
        b.ExceptionHandlers.Add(new ExceptionHandler(h.HandlerType)
        {
            TryStart = imap[h.TryStart], TryEnd = imap[h.TryEnd], HandlerStart = imap[h.HandlerStart],
            HandlerEnd = h.HandlerEnd == null ? null : imap[h.HandlerEnd],
            FilterStart = h.FilterStart == null ? null : imap[h.FilterStart],
            CatchType = h.CatchType == null ? null : m.ImportReference(h.CatchType)
        });
}

var trMethod = mmap[src.Methods.First(x => x.Name == "Tr")];
var ilp = md.Body.GetILProcessor();
var first = md.Body.Instructions[0];
var loc = new VariableDefinition(m.TypeSystem.String);
md.Body.Variables.Add(loc);
md.Body.InitLocals = true;
ilp.InsertBefore(first, ilp.Create(OpCodes.Ldarg_0));
ilp.InsertBefore(first, ilp.Create(OpCodes.Call, trMethod));
ilp.InsertBefore(first, ilp.Create(OpCodes.Stloc, loc));
ilp.InsertBefore(first, ilp.Create(OpCodes.Ldloc, loc));
ilp.InsertBefore(first, ilp.Create(OpCodes.Brfalse, first));
ilp.InsertBefore(first, ilp.Create(OpCodes.Ldloc, loc));
ilp.InsertBefore(first, ilp.Create(OpCodes.Ret));
m.Write(args[2]);
Console.WriteLine("ok");
