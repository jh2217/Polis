# Polis

## Testing conventions

- Test files map one to one to the classes they test: `Foo` in `Assets/Polis.Sim/Foo.cs` is tested by `FooTests` in `Assets/Tests/EditMode/FooTests.cs`.
- Do not put tests for one class in another class's test file.

## Code comments

- Do not leave comments about things that were removed or moved (e.g. "X now lives on Y; edit it there"). Comments describe the code as it is; put change history and pointers in the commit message or your reply, not the source.
