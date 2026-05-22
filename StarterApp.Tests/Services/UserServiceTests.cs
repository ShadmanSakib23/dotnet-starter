using FluentAssertions;
using Moq;
using StarterApp.DTOs;
using StarterApp.Enums;
using StarterApp.Exceptions;
using StarterApp.Models;
using StarterApp.Repositories;
using StarterApp.Services;
using StarterApp.Tests.Helpers;

namespace StarterApp.Tests.Services;

public class UserServiceTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly UserService _sut;

    public UserServiceTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _sut = new UserService(_userRepositoryMock.Object);
    }

    [Fact]
    public async Task GetUserAsync_WhenNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var id = Guid.NewGuid();
        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(id))
            .ReturnsAsync((User?)null);

        // Act
        var act = async () => await _sut.GetUserAsync(id);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task GetUserAsync_WhenFound_ReturnsMappedResponse()
    {
        // Arrange
        var user = UserFactory.Create();
        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(user.Id))
            .ReturnsAsync(user);

        // Act
        var result = await _sut.GetUserAsync(user.Id);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(user.Id);
        result.Email.Should().Be(user.Email);
        result.FirstName.Should().Be(user.FirstName);
        result.LastName.Should().Be(user.LastName);
        result.Role.Should().Be(user.Role.ToString());
    }

    [Fact]
    public async Task UpdateUserAsync_UpdatesOnlyProvidedFields()
    {
        // Arrange
        var user = UserFactory.Create(firstName: "OldFirst", lastName: "OldLast", username: "olduser");
        var request = new UpdateUserRequest { FirstName = "NewFirst", LastName = null, Username = null };

        _userRepositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<Guid>(), It.IsAny<Action<User>>()))
            .ReturnsAsync((Guid id, Action<User> action) => { action(user); return user; });

        // Act
        var result = await _sut.UpdateUserAsync(user.Id, request);

        // Assert
        result.FirstName.Should().Be("NewFirst");
        result.LastName.Should().Be("OldLast");
        result.Username.Should().Be("olduser");
    }

    [Fact]
    public async Task GetAllUsersAsync_WithPageBelowOne_ClampsToOne()
    {
        // Arrange
        var pagedResult = new PagedResult<User>
        {
            Items = new List<User>(),
            Page = 1,
            PageSize = 10,
            TotalCount = 0,
            TotalPages = 0
        };
        _userRepositoryMock
            .Setup(r => r.GetAllAsync(1, 10))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _sut.GetAllUsersAsync(0, 10);

        // Assert
        _userRepositoryMock.Verify(r => r.GetAllAsync(1, 10), Times.Once());
        result.Page.Should().Be(1);
    }

    [Fact]
    public async Task GetAllUsersAsync_WithPageSizeBelowOne_ClampsToOne()
    {
        // Arrange
        var pagedUsers = new PagedResult<User>
        {
            Items = new List<User>(),
            Page = 1,
            PageSize = 1,
            TotalCount = 0,
            TotalPages = 0
        };
        _userRepositoryMock
            .Setup(r => r.GetAllAsync(1, 1))
            .ReturnsAsync(pagedUsers);

        // Act
        var result = await _sut.GetAllUsersAsync(1, 0);

        // Assert
        _userRepositoryMock.Verify(r => r.GetAllAsync(1, 1), Times.Once());
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetAllUsersAsync_WithPageSizeAboveMax_ClampsTo100()
    {
        // Arrange
        var pagedResult = new PagedResult<User>
        {
            Items = new List<User>(),
            Page = 1,
            PageSize = 100,
            TotalCount = 0,
            TotalPages = 0
        };
        _userRepositoryMock
            .Setup(r => r.GetAllAsync(1, 100))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _sut.GetAllUsersAsync(1, 200);

        // Assert
        _userRepositoryMock.Verify(r => r.GetAllAsync(1, 100), Times.Once());
    }

    [Fact]
    public async Task GetAllUsersAsync_ReturnsPagedResult()
    {
        // Arrange
        var users = new List<User>
        {
            UserFactory.Create(email: "a@example.com"),
            UserFactory.Create(email: "b@example.com")
        };
        var pagedResult = new PagedResult<User>
        {
            Items = users,
            Page = 1,
            PageSize = 10,
            TotalCount = 2,
            TotalPages = 1
        };
        _userRepositoryMock
            .Setup(r => r.GetAllAsync(1, 10))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _sut.GetAllUsersAsync(1, 10);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task DeleteUserAsync_WhenDeletingSelf_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        var act = async () => await _sut.DeleteUserAsync(id, id);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task DeleteUserAsync_WhenNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var requesterId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(targetId))
            .ReturnsAsync((User?)null);

        // Act
        var act = async () => await _sut.DeleteUserAsync(requesterId, targetId);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task DeleteUserAsync_WhenValid_CallsDeleteAsync()
    {
        // Arrange
        var requesterId = Guid.NewGuid();
        var user = UserFactory.Create();
        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(user.Id))
            .ReturnsAsync(user);
        _userRepositoryMock
            .Setup(r => r.DeleteAsync(user.Id))
            .Returns(Task.CompletedTask);

        // Act
        await _sut.DeleteUserAsync(requesterId, user.Id);

        // Assert
        _userRepositoryMock.Verify(r => r.DeleteAsync(user.Id), Times.Once());
    }

    [Fact]
    public async Task AssignRoleAsync_WhenAssigningSuperAdmin_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var requesterId = Guid.NewGuid();
        var targetId = Guid.NewGuid();

        // Act
        var act = async () => await _sut.AssignRoleAsync(requesterId, targetId, UserRole.SuperAdmin);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task AssignRoleAsync_WhenTargetNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var requesterId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(targetId))
            .ReturnsAsync((User?)null);

        // Act
        var act = async () => await _sut.AssignRoleAsync(requesterId, targetId, UserRole.Admin);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task AssignRoleAsync_WhenSameRole_ThrowsBadRequestException()
    {
        // Arrange
        var requesterId = Guid.NewGuid();
        // User already has Admin role (1); trying to assign Admin again (same role) should fail
        var user = UserFactory.Create(role: UserRole.Admin);
        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(user.Id))
            .ReturnsAsync(user);

        // Act
        var act = async () => await _sut.AssignRoleAsync(requesterId, user.Id, UserRole.Admin);

        // Assert
        await act.Should().ThrowAsync<BadRequestException>();
    }

    [Fact]
    public async Task AssignRoleAsync_WhenDemoting_ThrowsBadRequestException()
    {
        // Arrange
        var requesterId = Guid.NewGuid();
        // User already has Admin role (1); trying to assign User role (0) is a demotion
        var user = UserFactory.Create(role: UserRole.Admin);
        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(user.Id))
            .ReturnsAsync(user);

        // Act
        var act = async () => await _sut.AssignRoleAsync(requesterId, user.Id, UserRole.User);

        // Assert
        await act.Should().ThrowAsync<BadRequestException>();
    }

    [Fact]
    public async Task AssignRoleAsync_WithValidUpgrade_ReturnsUpdatedUser()
    {
        // Arrange
        var requesterId = Guid.NewGuid();
        var user = UserFactory.Create(role: UserRole.User);
        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(user.Id))
            .ReturnsAsync(user);
        _userRepositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<User>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.AssignRoleAsync(requesterId, user.Id, UserRole.Admin);

        // Assert
        result.Should().NotBeNull();
        result.Role.Should().Be(UserRole.Admin.ToString());
        _userRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Once());
    }
}
