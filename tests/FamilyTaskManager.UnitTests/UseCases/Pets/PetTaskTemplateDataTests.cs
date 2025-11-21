using FamilyTaskManager.Core.PetAggregate;
using FamilyTaskManager.UseCases.Pets;

namespace FamilyTaskManager.UnitTests.UseCases.Pets;

public class PetTaskTemplateDataTests
{
  [Theory]
  [InlineData(PetType.Cat, 8)]
  [InlineData(PetType.Dog, 6)]
  [InlineData(PetType.Hamster, 5)]
  public void GetDefaultTemplates_ReturnsCorrectCount(PetType petType, int expectedCount)
  {
    // Act
    var templates = PetTaskTemplateData.GetDefaultTemplates(petType);

    // Assert
    templates.Count.ShouldBe(expectedCount);
  }

  [Fact]
  public void GetDefaultTemplates_Cat_ReturnsCorrectTemplates()
  {
    // Act
    var templates = PetTaskTemplateData.GetDefaultTemplates(PetType.Cat);

    // Assert
    templates.ShouldContain(t => t.Title == "Убрать какахи из лотка кота" && t.Points == 4);
    templates.ShouldContain(t => t.Title == "Убрать комки из лотка кота" && t.Points == 3);
    templates.ShouldContain(t => t.Title == "Налить свежую воду коту" && t.Points == 3);
    templates.ShouldContain(t => t.Title == "Насыпать корм коту" && t.Points == 3);
    templates.ShouldContain(t => t.Title == "Полностью заменить наполнитель в лотке кота" && t.Points == 7);
    templates.ShouldContain(t => t.Title == "Помыть миски кота" && t.Points == 4);
    templates.ShouldContain(t => t.Title == "Почистить место для сна кота" && t.Points == 4);
    templates.ShouldContain(t => t.Title == "Поиграть с котом" && t.Points == 2);
  }

  [Fact]
  public void GetDefaultTemplates_Dog_ReturnsCorrectTemplates()
  {
    // Act
    var templates = PetTaskTemplateData.GetDefaultTemplates(PetType.Dog);

    // Assert
    templates.ShouldContain(t => t.Title == "Выгулять собаку утром" && t.Points == 5);
    templates.ShouldContain(t => t.Title == "Выгулять собаку вечером" && t.Points == 5);
    templates.ShouldContain(t => t.Title == "Накормить собаку" && t.Points == 4);
    templates.ShouldContain(t => t.Title == "Помыть миски собаки" && t.Points == 4);
    templates.ShouldContain(t => t.Title == "Расчесать собаку" && t.Points == 4);
    templates.ShouldContain(t => t.Title == "Искупать собаку" && t.Points == 8);
  }

  [Fact]
  public void GetDefaultTemplates_Hamster_ReturnsCorrectTemplates()
  {
    // Act
    var templates = PetTaskTemplateData.GetDefaultTemplates(PetType.Hamster);

    // Assert
    templates.ShouldContain(t => t.Title == "Насыпать корм хомяку" && t.Points == 2);
    templates.ShouldContain(t => t.Title == "Проверить и долить воду хомяку" && t.Points == 2);
    templates.ShouldContain(t => t.Title == "Убрать клетку хомяка" && t.Points == 5);
    templates.ShouldContain(t => t.Title == "Полностью помыть клетку хомяка" && t.Points == 7);
    templates.ShouldContain(t => t.Title == "Проверить игрушки и колесо хомяка" && t.Points == 3);
  }

  [Fact]
  public void GetDefaultTemplates_AllTemplates_HaveValidSchedules()
  {
    // Arrange
    var allPetTypes = new[] { PetType.Cat, PetType.Dog, PetType.Hamster };

    // Act & Assert
    foreach (var petType in allPetTypes)
    {
      var templates = PetTaskTemplateData.GetDefaultTemplates(petType);
      
      foreach (var template in templates)
      {
        template.Schedule.ShouldNotBeNullOrWhiteSpace();
        template.Title.ShouldNotBeNullOrWhiteSpace();
        template.Points.ShouldBeGreaterThan(0);
        template.Points.ShouldBeLessThanOrEqualTo(100);
      }
    }
  }
}
