# Nem_Mockery

A mocking library for .NET unit tests that does **not** require interfaces, virtual
members, or any changes to the code under test. It stubs and verifies **static
methods, sealed classes, records, structs, properties, and constructors** by
detouring the compiled method at runtime (via
[MonoMod.RuntimeDetour](https://github.com/MonoMod/MonoMod)), with a syntax modeled
on [Mockito](https://site.mockito.org/) for Java.

Where Moq and NSubstitute build proxy objects — and therefore need an interface or a
virtual member to intercept — Nem_Mockery rewrites the real method's entry point in
memory for the duration of a test, then puts it back.

## Requirements

- .NET 10
- Any test framework (developed and tested against xUnit v3)

## Usage

```csharp
using Nem_Mockery;
using static Nem_Mockery.Mock;   // optional: bare When(...) / Verify(...)

[Fact]
public void ReadsConfiguredData() {
  // Every test opens a MockContext; disposing it restores all real behavior.
  using MockContext context = new();

  // Static method on a static class — no interface, no virtual.
  When(() => FileReader.Read(Arg.Any<string>())).ThenReturn("data");

  // Sealed class instance method: the stub applies to THIS instance.
  SealedService service = new();
  When(() => service.Save(Arg.Is<Order>(o => o.Quantity > 0)))
    .ThenThrow(new IOException("disk full"));

  string result = FileReader.Read("a.txt");   // "data"
  FileReader.Read(42.ToString());             // also "data" (Arg.Any)

  Verify(() => FileReader.Read("a.txt"), Times.Once());
}
```

### Stubbing behaviors

```csharp
When(() => Parser.Parse("x"))
  .ThenReturn(1, 2, 3)              // consecutive calls: 1, 2, 3, 3, 3, ...
  .ThenThrow<InvalidOperationException>()  // ...then every later call throws
  ;

When(() => Clock.Now()).ThenAnswer(invocation => testStart.AddSeconds(1));

// Unmatched calls run the REAL implementation (partial-mock semantics):
When(() => Lookup.Find("known")).ThenReturn("cached");
Lookup.Find("other");               // executes the real Find
```

### Properties, constructors, out parameters

```csharp
// Property getter and setter.
When(() => Config.Timeout).ThenReturn(5);
WhenSet(() => Config.Timeout, () => Arg.Any<int>()).ThenDoNothing();

// Constructor: the object is still allocated, but the stubbed constructor body
// is skipped (fields stay default) or replaced by an answer.
WhenNew(() => new Widget(Arg.Any<string>())).ThenDoNothing();
WhenNew(() => new Widget("live")).ThenAnswer(inv => { /* init via inv.Instance */ });

// out / ref parameters via ThenAnswer + SetArgument.
When(() => int.TryParse(Arg.Any<string>(), out ignored))
  .ThenAnswer(inv => { inv.SetArgument(1, 42); return true; });
```

### Verification

```csharp
Verify(() => FileReader.Read("a.txt"));                  // exactly once (default)
Verify(() => FileReader.Read(Arg.Any<string>()), Times.AtLeast(2));
VerifySet(() => Config.Timeout, () => 5, Times.Never());
VerifyNew(() => new Widget(Arg.Any<string>()), Times.Once());
VerifyNoOtherCalls();   // every recorded call must have been verified
```

`Times` supports `Once()`, `Never()`, `Exactly(n)`, `AtLeast(n)`, `AtLeastOnce()`,
and `AtMost(n)`.

## Semantics worth knowing

- **Miss policy: call the original.** A stub only answers calls whose receiver and
  arguments match. Everything else — other arguments, other instances, unstubbed
  methods — runs real code. This is closer to Mockito's *spy* than its *mock*.
- **Instance matching.** Stubs written against a class instance match that instance
  by reference. Struct receivers are matched by value equality (every box of a
  struct is a copy, so identity is meaningless).
- **Later stubs win.** When two stubs match the same call, the most recently
  arranged one answers, like Mockito.
- **All calls are recorded** while a method is mocked — including calls that fell
  through to the real implementation — and `Verify` counts them.
- **Isolation and parallelism.** Detours are process-global, so the first
  `MockContext` to stub a method owns it; a parallel test stubbing the same method
  blocks (up to 30 s) until the owner disposes. Contexts nested in one test share
  ownership. **Always dispose contexts** — `using MockContext context = new();`.

## Limitations

- **Inlining.** The JIT may inline very small methods into an already-compiled
  caller before the detour is installed; such call sites bypass the mock. This is
  rare in test code (stubs are arranged before the calling code first runs) but can
  affect hot, trivial methods and code precompiled with R2R/AOT. JIT intrinsics
  (e.g. some `Math`/`DateTime` members) may not be hookable at all.
- **Generic methods** are mocked per closed instantiation; instantiations over
  reference types share compiled code, so a stub for `Find<string>` can intercept
  `Find<object>` calls too (matchers still filter by argument values).
- **Ref-returning methods, abstract methods, and open generics** are rejected with
  a descriptive `MockeryException`.
- **Constructors cannot substitute the allocated instance** — `new` still returns
  the real object; the stub only controls whether/how the constructor body runs.
- Mocking methods of non-public types may fail delegate binding; keep mocked test
  fixtures public.

## Building

```
dotnet build          # also produces the NuGet package (GeneratePackageOnBuild)
dotnet test           # runs the xUnit suite with the 80%/80% coverage gate
```

See [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) for how the interception pipeline
works.
