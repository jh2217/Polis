# Polis

## Testing conventions

- Test files map one to one to the classes they test: `Foo` in `Assets/Polis.Sim/Foo.cs` is tested by `FooTests` in `Assets/Tests/EditMode/FooTests.cs`.
- Do not put tests for one class in another class's test file.
