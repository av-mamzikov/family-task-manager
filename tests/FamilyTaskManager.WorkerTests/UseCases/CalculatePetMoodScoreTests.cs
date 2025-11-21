using Ardalis.Result;
using Ardalis.SharedKernel;
using FamilyTaskManager.Core.PetAggregate;
using FamilyTaskManager.Core.TaskAggregate;
using FamilyTaskManager.UseCases.Pets;
using FamilyTaskManager.UseCases.Tasks.Specifications;

namespace FamilyTaskManager.WorkerTests.UseCases;

public class CalculatePetMoodScoreTests
{
    private readonly IRepository<Pet> _petRepository;
    private readonly IRepository<TaskInstance> _taskRepository;
    private readonly CalculatePetMoodScoreHandler _handler;

    public CalculatePetMoodScoreTests()
    {
        _petRepository = Substitute.For<IRepository<Pet>>();
        _taskRepository = Substitute.For<IRepository<TaskInstance>>();
        _handler = new CalculatePetMoodScoreHandler(_petRepository, _taskRepository);
    }

    [Fact]
    public async Task Handle_Returns100_WhenNoTasksDue()
    {
        // Arrange
        var petId = Guid.NewGuid();
        var pet = new Pet(Guid.NewGuid(), PetType.Cat, "Мурзик");
        
        _petRepository.GetByIdAsync(petId, Arg.Any<CancellationToken>())
            .Returns(pet);
        
        _taskRepository.ListAsync(Arg.Any<TasksByPetSpec>(), Arg.Any<CancellationToken>())
            .Returns(new List<TaskInstance>());

        // Act
        var result = await _handler.Handle(
            new CalculatePetMoodScoreCommand(petId),
            CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(100);
    }

    [Fact]
    public async Task Handle_ReturnsNotFound_WhenPetDoesNotExist()
    {
        // Arrange
        var petId = Guid.NewGuid();
        _petRepository.GetByIdAsync(petId, Arg.Any<CancellationToken>())
            .Returns((Pet?)null);

        // Act
        var result = await _handler.Handle(
            new CalculatePetMoodScoreCommand(petId),
            CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Status.ShouldBe(ResultStatus.NotFound);
    }

    [Fact]
    public async Task Handle_Returns100_WhenAllTasksCompletedOnTime()
    {
        // Arrange
        var petId = Guid.NewGuid();
        var familyId = Guid.NewGuid();
        var pet = new Pet(familyId, PetType.Cat, "Мурзик");
        
        var now = DateTime.UtcNow;
        var tasks = new List<TaskInstance>
        {
            CreateCompletedTask(familyId, petId, 10, now.AddHours(-2), now.AddHours(-1)), // Completed on time
            CreateCompletedTask(familyId, petId, 20, now.AddHours(-3), now.AddHours(-2)), // Completed on time
        };

        _petRepository.GetByIdAsync(petId, Arg.Any<CancellationToken>()).Returns(pet);
        _taskRepository.ListAsync(Arg.Any<TasksByPetSpec>(), Arg.Any<CancellationToken>()).Returns(tasks);

        // Act
        var result = await _handler.Handle(new CalculatePetMoodScoreCommand(petId), CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(100); // All tasks completed on time = 100% mood
    }

    [Fact]
    public async Task Handle_Returns50_WhenHalfTasksCompleted()
    {
        // Arrange
        var petId = Guid.NewGuid();
        var familyId = Guid.NewGuid();
        var pet = new Pet(familyId, PetType.Cat, "Мурзик");
        
        var now = DateTime.UtcNow;
        var tasks = new List<TaskInstance>
        {
            CreateCompletedTask(familyId, petId, 10, now.AddHours(-2), now.AddHours(-1)), // Completed
            CreateOverdueTask(familyId, petId, 10, now.AddHours(-2)), // Overdue, not completed
        };

        _petRepository.GetByIdAsync(petId, Arg.Any<CancellationToken>()).Returns(pet);
        _taskRepository.ListAsync(Arg.Any<TasksByPetSpec>(), Arg.Any<CancellationToken>()).Returns(tasks);

        // Act
        var result = await _handler.Handle(new CalculatePetMoodScoreCommand(petId), CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        // 10 points completed on time = +10
        // 10 points overdue (1 hour) = -10 * (1/24/7) ≈ -0.06
        // effectiveSum ≈ 9.94, maxPoints = 20
        // mood = 100 * (9.94 / 20) ≈ 50
        result.Value.ShouldBeGreaterThan(45);
        result.Value.ShouldBeLessThan(55);
    }

    [Fact]
    public async Task Handle_Returns0_WhenAllTasksOverdue()
    {
        // Arrange
        var petId = Guid.NewGuid();
        var familyId = Guid.NewGuid();
        var pet = new Pet(familyId, PetType.Cat, "Мурзик");
        
        var now = DateTime.UtcNow;
        var tasks = new List<TaskInstance>
        {
            CreateOverdueTask(familyId, petId, 10, now.AddDays(-10)), // Overdue 10 days
            CreateOverdueTask(familyId, petId, 20, now.AddDays(-8)),  // Overdue 8 days
        };

        _petRepository.GetByIdAsync(petId, Arg.Any<CancellationToken>()).Returns(pet);
        _taskRepository.ListAsync(Arg.Any<TasksByPetSpec>(), Arg.Any<CancellationToken>()).Returns(tasks);

        // Act
        var result = await _handler.Handle(new CalculatePetMoodScoreCommand(petId), CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(0); // All tasks overdue for > 7 days = 0% mood
    }

    [Fact]
    public async Task Handle_AppliesLateCompletionPenalty()
    {
        // Arrange
        var petId = Guid.NewGuid();
        var familyId = Guid.NewGuid();
        var pet = new Pet(familyId, PetType.Cat, "Мурзик");
        
        var now = DateTime.UtcNow;
        var dueAt = now.AddHours(-2);
        var completedAt = now.AddHours(-1); // Completed 1 hour late
        
        var tasks = new List<TaskInstance>
        {
            CreateCompletedTask(familyId, petId, 10, dueAt, completedAt), // Completed late
        };

        _petRepository.GetByIdAsync(petId, Arg.Any<CancellationToken>()).Returns(pet);
        _taskRepository.ListAsync(Arg.Any<TasksByPetSpec>(), Arg.Any<CancellationToken>()).Returns(tasks);

        // Act
        var result = await _handler.Handle(new CalculatePetMoodScoreCommand(petId), CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        // Late completion gives 50% of points (kLate = 0.5)
        // effectiveSum = 10 * 0.5 = 5, maxPoints = 10
        // mood = 100 * (5 / 10) = 50
        result.Value.ShouldBe(50);
    }

    [Fact]
    public async Task Handle_UpdatesPetMoodScore()
    {
        // Arrange
        var petId = Guid.NewGuid();
        var familyId = Guid.NewGuid();
        var pet = new Pet(familyId, PetType.Cat, "Мурзик");
        
        _petRepository.GetByIdAsync(petId, Arg.Any<CancellationToken>()).Returns(pet);
        _taskRepository.ListAsync(Arg.Any<TasksByPetSpec>(), Arg.Any<CancellationToken>())
            .Returns(new List<TaskInstance>());

        // Act
        await _handler.Handle(new CalculatePetMoodScoreCommand(petId), CancellationToken.None);

        // Assert
        await _petRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        pet.MoodScore.ShouldBe(100);
    }

    // Helper methods
    private TaskInstance CreateCompletedTask(Guid familyId, Guid petId, int points, DateTime dueAt, DateTime completedAt)
    {
        var task = new TaskInstance(familyId, petId, "Test Task", points, TaskType.OneTime, dueAt);
        task.Complete(Guid.NewGuid(), completedAt);
        return task;
    }

    private TaskInstance CreateOverdueTask(Guid familyId, Guid petId, int points, DateTime dueAt)
    {
        return new TaskInstance(familyId, petId, "Overdue Task", points, TaskType.OneTime, dueAt);
    }
}
