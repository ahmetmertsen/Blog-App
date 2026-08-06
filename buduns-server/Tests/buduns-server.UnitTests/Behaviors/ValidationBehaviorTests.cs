using buduns_server.Application.Common.Behaviors;
using FluentValidation;

namespace buduns_server.UnitTests.Behaviors;

public class ValidationBehaviorTests
{
    [Fact]
    public async Task Handle_WithoutValidators_ShouldCallNext()
    {
        var behavior = new ValidationBehavior<TestRequest, string>(Array.Empty<IValidator<TestRequest>>());
        var nextCalled = false;

        var result = await behavior.Handle(new TestRequest(), _ =>
        {
            nextCalled = true;
            return Task.FromResult("ok");
        }, CancellationToken.None);

        Assert.True(nextCalled);
        Assert.Equal("ok", result);
    }

    [Fact]
    public async Task Handle_ValidRequest_ShouldCallNext()
    {
        var validator = new InlineValidator<TestRequest>();
        validator.RuleFor(request => request.Value).NotEmpty();
        var behavior = new ValidationBehavior<TestRequest, string>(new[] { validator });

        var result = await behavior.Handle(new TestRequest { Value = "valid" }, _ => Task.FromResult("handled"), CancellationToken.None);

        Assert.Equal("handled", result);
    }

    [Fact]
    public async Task Handle_InvalidRequest_ShouldThrowValidationExceptionAndNotCallNext()
    {
        var validator = new InlineValidator<TestRequest>();
        validator.RuleFor(request => request.Value).NotEmpty();
        var behavior = new ValidationBehavior<TestRequest, string>(new[] { validator });
        var nextCalled = false;

        var exception = await Assert.ThrowsAsync<ValidationException>(() => behavior.Handle(new TestRequest(), _ =>
        {
            nextCalled = true;
            return Task.FromResult("handled");
        }, CancellationToken.None));

        Assert.False(nextCalled);
        Assert.Contains(exception.Errors, error => error.PropertyName == nameof(TestRequest.Value));
    }

    [Fact]
    public async Task Handle_MultipleValidators_ShouldCombineErrors()
    {
        var first = new InlineValidator<TestRequest>();
        first.RuleFor(request => request.Value).NotEmpty();
        var second = new InlineValidator<TestRequest>();
        second.RuleFor(request => request.OtherValue).GreaterThan(0);
        var behavior = new ValidationBehavior<TestRequest, string>(new IValidator<TestRequest>[] { first, second });

        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            behavior.Handle(new TestRequest(), _ => Task.FromResult("handled"), CancellationToken.None));

        Assert.Contains(exception.Errors, error => error.PropertyName == nameof(TestRequest.Value));
        Assert.Contains(exception.Errors, error => error.PropertyName == nameof(TestRequest.OtherValue));
    }

    private sealed class TestRequest
    {
        public string? Value { get; set; }
        public int OtherValue { get; set; }
    }
}
