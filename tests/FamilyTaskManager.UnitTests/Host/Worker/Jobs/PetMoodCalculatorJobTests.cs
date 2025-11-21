using Ardalis.Result;
using FamilyTaskManager.Core.PetAggregate;
using FamilyTaskManager.UseCases.Pets;
using FamilyTaskManager.Host.Modules.Worker.Jobs;
using Mediator;
using Microsoft.Extensions.Logging;
using Quartz;

namespace FamilyTaskManager.UnitTests.Host.Worker.Jobs;

public class PetMoodCalculatorJobTests
{
    private readonly IMediator _mediator;
    private readonly ILogger<PetMoodCalculatorJob> _logger;
    private readonly PetMoodCalculatorJob _job;
    private readonly IJobExecutionContext _context;

    public PetMoodCalculatorJobTests()
    {
        _mediator = Substitute.For<IMediator>();
        _logger = Substitute.For<ILogger<PetMoodCalculatorJob>>();
        _job = new PetMoodCalculatorJob(_mediator, _logger);
        _context = Substitute.For<IJobExecutionContext>();
    }

    [Fact]
    public async Task Execute_CalculatesMood_ForAllPets()
    {
        // Arrange
        var pets = new List<PetDto>
        {
            new PetDto(Guid.NewGuid(), "Мурзик", PetType.Cat, 50),
            new PetDto(Guid.NewGuid(), "Шарик", PetType.Dog, 75),
        };

        _mediator.Send(Arg.Any<GetAllPetsQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(pets));

        _mediator.Send(Arg.Any<CalculatePetMoodScoreCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(85));

        // Act
        await _job.Execute(_context);

        // Assert
        await _mediator.Received(1).Send(
            Arg.Any<GetAllPetsQuery>(),
            Arg.Any<CancellationToken>());
        
        await _mediator.Received(2).Send(
            Arg.Any<CalculatePetMoodScoreCommand>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Execute_DoesNotThrow_WhenNoPetsExist()
    {
        // Arrange
        _mediator.Send(Arg.Any<GetAllPetsQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(new List<PetDto>()));

        // Act & Assert
        await Should.NotThrowAsync(async () => await _job.Execute(_context));
    }

    [Fact]
    public async Task Execute_HandlesErrors_Gracefully()
    {
        // Arrange
        _mediator.Send(Arg.Any<GetAllPetsQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Error("Database error"));

        // Act & Assert
        await Should.NotThrowAsync(async () => await _job.Execute(_context));
    }

    [Fact]
    public async Task Execute_ContinuesProcessing_WhenOnePetFails()
    {
        // Arrange
        var pets = new List<PetDto>
        {
            new PetDto(Guid.NewGuid(), "Мурзик", PetType.Cat, 50),
            new PetDto(Guid.NewGuid(), "Шарик", PetType.Dog, 75),
            new PetDto(Guid.NewGuid(), "Хомяк", PetType.Hamster, 60),
        };

        _mediator.Send(Arg.Any<GetAllPetsQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(pets));

        // First pet succeeds, second fails, third succeeds
        _mediator.Send(Arg.Is<CalculatePetMoodScoreCommand>(c => c.PetId == pets[0].Id), Arg.Any<CancellationToken>())
            .Returns(Result.Success(85));
        _mediator.Send(Arg.Is<CalculatePetMoodScoreCommand>(c => c.PetId == pets[1].Id), Arg.Any<CancellationToken>())
            .Returns(Result.Error("Pet not found"));
        _mediator.Send(Arg.Is<CalculatePetMoodScoreCommand>(c => c.PetId == pets[2].Id), Arg.Any<CancellationToken>())
            .Returns(Result.Success(90));

        // Act
        await _job.Execute(_context);

        // Assert - all 3 pets should be processed
        await _mediator.Received(3).Send(
            Arg.Any<CalculatePetMoodScoreCommand>(),
            Arg.Any<CancellationToken>());
    }
}
