using buduns_server.Application.Common.Consts;
using buduns_server.Application.Repositories;
using buduns_server.Application.UnitOfWork;
using buduns_server.Domain.Entities;
using buduns_server.Persistence.MailTemplates;
using buduns_server.Persistence.Services;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace buduns_server.UnitTests.Services;

/// <summary>
/// Seeder'in sozu RoleSeeder'inkiyle ayni: eksigi ekler, var olana dokunmaz.
/// Ikinci kisim burada daha da onemli, cunku sablon icerigi duzenlenebilir bir
/// metin; her acilista koddaki surume donmesi duzenlemeyi anlamsiz kilardi.
/// </summary>
public class MailTemplateSeederTests
{
    [Fact]
    public async Task SeedAsync_TemplatesMissing_ShouldCreateEveryTemplate()
    {
        var (unitOfWork, utilityRepository) = CreateUnitOfWork();
        utilityRepository.GetByNameAsync(Arg.Any<string>()).Returns((Utility?)null);
        var added = new List<Utility>();
        utilityRepository.AddAsync(Arg.Do<Utility>(utility => added.Add(utility))).Returns(Task.CompletedTask);

        var result = await CreateSeeder(utilityRepository, unitOfWork).SeedAsync(CancellationToken.None);

        Assert.Equal(MailTemplateKeys.All, added.Select(utility => utility.Name).ToArray());
        Assert.Equal(MailTemplateKeys.All, result.CreatedKeys);
        Assert.Empty(result.DivergedKeys);
    }

    [Fact]
    public async Task SeedAsync_ShouldWriteTheBodyDeclaredInCode()
    {
        var (unitOfWork, utilityRepository) = CreateUnitOfWork();
        utilityRepository.GetByNameAsync(Arg.Any<string>()).Returns((Utility?)null);
        var added = new List<Utility>();
        utilityRepository.AddAsync(Arg.Do<Utility>(utility => added.Add(utility))).Returns(Task.CompletedTask);

        await CreateSeeder(utilityRepository, unitOfWork).SeedAsync(CancellationToken.None);

        foreach (var utility in added)
        {
            Assert.Equal(MailTemplateCatalog.GetBody(utility.Name), utility.Value);
        }
    }

    [Fact]
    public async Task SeedAsync_TemplatesAlreadyExist_ShouldNotWriteAnything()
    {
        var (unitOfWork, utilityRepository) = CreateUnitOfWork();
        utilityRepository.GetByNameAsync(Arg.Any<string>())
            .Returns(callInfo => Existing(callInfo.Arg<string>(), MailTemplateCatalog.GetBody(callInfo.Arg<string>())));

        var result = await CreateSeeder(utilityRepository, unitOfWork).SeedAsync(CancellationToken.None);

        await utilityRepository.DidNotReceiveWithAnyArgs().AddAsync(default!);
        await unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
        Assert.Empty(result.CreatedKeys);
        Assert.Empty(result.DivergedKeys);
    }

    [Fact]
    public async Task SeedAsync_SingleTemplateMissing_ShouldCreateOnlyThatOne()
    {
        var (unitOfWork, utilityRepository) = CreateUnitOfWork();
        utilityRepository.GetByNameAsync(Arg.Any<string>())
            .Returns(callInfo => Existing(callInfo.Arg<string>(), MailTemplateCatalog.GetBody(callInfo.Arg<string>())));
        utilityRepository.GetByNameAsync(MailTemplateKeys.ChangeEmail).Returns((Utility?)null);

        var result = await CreateSeeder(utilityRepository, unitOfWork).SeedAsync(CancellationToken.None);

        await utilityRepository.Received(1).AddAsync(Arg.Is<Utility>(utility => utility.Name == MailTemplateKeys.ChangeEmail));
        Assert.Equal(new[] { MailTemplateKeys.ChangeEmail }, result.CreatedKeys);
    }

    /// <summary>
    /// Elle duzenlenmis sablon korunur ama sessiz kalmaz; aksi halde "kodda
    /// degistirdim, mail eskisi gibi geliyor" sorusunun cevabi hicbir yerde
    /// gorunmezdi.
    /// </summary>
    [Fact]
    public async Task SeedAsync_ExistingTemplateDiffersFromCode_ShouldReportItWithoutOverwriting()
    {
        var (unitOfWork, utilityRepository) = CreateUnitOfWork();
        utilityRepository.GetByNameAsync(Arg.Any<string>())
            .Returns(callInfo => Existing(callInfo.Arg<string>(), MailTemplateCatalog.GetBody(callInfo.Arg<string>())));
        var edited = Existing(MailTemplateKeys.MailVerify, "<p>elle duzenlenmis</p>");
        utilityRepository.GetByNameAsync(MailTemplateKeys.MailVerify).Returns(edited);

        var result = await CreateSeeder(utilityRepository, unitOfWork).SeedAsync(CancellationToken.None);

        Assert.Equal(new[] { MailTemplateKeys.MailVerify }, result.DivergedKeys);
        Assert.Equal("<p>elle duzenlenmis</p>", edited.Value);
        await utilityRepository.DidNotReceiveWithAnyArgs().AddAsync(default!);
        utilityRepository.DidNotReceive().Update(Arg.Any<Utility>());
        await unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    private static MailTemplateSeeder CreateSeeder(IUtilityRepository utilityRepository, IUnitOfWork unitOfWork) =>
        new(utilityRepository, unitOfWork, NullLogger<MailTemplateSeeder>.Instance);

    private static (IUnitOfWork UnitOfWork, IUtilityRepository UtilityRepository) CreateUnitOfWork()
    {
        return (Substitute.For<IUnitOfWork>(), Substitute.For<IUtilityRepository>());
    }

    private static Utility Existing(string name, string value) => new() { Name = name, Value = value };
}
