---
name: unit-tests
description: How unit tests are written with xUnit, Shouldly and NSubstitute - folder layout, naming, Arrange/Act/Assert, testing validators and handlers, and asserting the cancellation token. Use when testing a handler, validator, endpoint or any class in isolation.
metadata:
  scope: shared
---

# Unit tests

## The rule

xUnit for tests, Shouldly for assertions, NSubstitute for fakes. Test folders mirror the source folders. `test-design` says which tests a change needs; this skill says how to write them. Each test has `// Arrange`, `// Act` and `// Assert` comments and tests one thing.

| Project | Tests |
| --- | --- |
| `tests/Application.Tests.Unit` | Handlers (repositories faked with NSubstitute) and validators (`FluentValidation.TestHelper`) |
| `tests/Presentation.Tests.Unit` | Presentation classes called directly, with `ISender` faked. Only templates whose presentation layer has logic worth testing on its own have this project; the others test it through `presentation-integration-tests` |

Names read as `Method_Should<Outcome>_<Condition>`: `Handle_ShouldThrowException_ReviewDoesNotExist`, `GetReviews_ShouldReturn_Ok`, `Validator_ShouldHaveValidationErrorFor_Id`.

## Steps

1. Create `<Class>Tests.cs` in the matching folder, for example `tests/Application.Tests.Unit/Reviews/Commands/UpdateReview/UpdateReviewHandlerTests.cs`.
2. Handlers: one test for the happy path, one per failure (not found, and so on). Fake each repository with `Substitute.For<I...Repository>()` and set returns with `ReturnsForAnyArgs` or `Returns` on specific arguments.
3. Assert the result, then assert the calls: `Received(1)` with the exact arguments and token, and `DidNotReceiveWithAnyArgs()` for calls that mustn't happen.
4. Validators: a static validator instance, one valid case with `ShouldNotHaveAnyValidationErrors()`, and a `[Theory]` per rule with `ShouldHaveValidationErrorFor(...)`.
5. Use a real `CancellationTokenSource` so you can prove the token was passed on (`cancellation`).

## Example

```csharp
// tests/Application.Tests.Unit/Reviews/Commands/UpdateReview/UpdateReviewHandlerTests.cs
[Fact]
public async Task Handle_ShouldPassThrough_Command()
{
    // Arrange
    var command = new UpdateReviewCommand
    {
        Id = Guid.NewGuid(),
        Stars = 1
    };

    var reviewsRepository = Substitute.For<IReviewsRepository>();

    _ = reviewsRepository.ReviewExists(default, default).ReturnsForAnyArgs(true);
    _ = reviewsRepository.UpdateReview(default, default, default).ReturnsForAnyArgs(true);

    var handler = new UpdateReviewHandler(reviewsRepository);
    using var tokenSource = new CancellationTokenSource();
    var token = tokenSource.Token;

    // Act
    var result = await handler.Handle(command, token);

    // Assert
    result.ShouldBeTrue();

    _ = await reviewsRepository.Received(1).ReviewExists(command.Id, token);
    _ = await reviewsRepository.Received(1).UpdateReview(command.Id, command.Stars, token);
}
```

```csharp
// tests/Application.Tests.Unit/Reviews/Commands/UpdateReview/UpdateReviewValidatorTests.cs
var result = Validator.TestValidate(command);

_ = result.ShouldHaveValidationErrorFor(command => command.Id);
```

## Don't

- Use `Assert.Equal` or FluentAssertions. Shouldly only.
- Use Moq. NSubstitute only.
- Test private methods, or test through the database in a unit test.
- Share mutable state between tests through static fields.
- Write a test that only exists to raise coverage without asserting behaviour.

## Check

``` bash
dotnet test tests/Application.Tests.Unit
dotnet test tests/Presentation.Tests.Unit      # where the template has one
```
