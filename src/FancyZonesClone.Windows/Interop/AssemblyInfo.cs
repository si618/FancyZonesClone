using System.Runtime.CompilerServices;

// Every interop type is blittable, including the InlineArray character buffers, so LibraryImport passes them as
// raw memory. Disabling runtime marshalling makes that a compile-time guarantee, which Native AOT relies on.
[assembly: DisableRuntimeMarshalling]
