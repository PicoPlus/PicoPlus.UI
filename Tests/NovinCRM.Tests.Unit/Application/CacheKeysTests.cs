using NovinCRM.Application.Common;
using Xunit;
using FluentAssertions;

namespace NovinCRM.Tests.Unit.Application;

/// <summary>
/// Unit tests for <see cref="CacheKeys"/> centralised key factory.
/// </summary>
public class CacheKeysTests
{
    [Theory]
    [InlineData("c1",  "userpanel:c1")]
    [InlineData("abc", "userpanel:abc")]
    public void UserPanel_ProducesExpectedKey(string contactId, string expected)
        => CacheKeys.UserPanel(contactId).Should().Be(expected);

    [Fact]
    public void Otp_ProducesExpectedKey()
        => CacheKeys.Otp("09120000000").Should().Be("otp:09120000000");

    [Theory]
    [InlineData("contact", "42", "sync:ver:contact:42")]
    [InlineData("deal",    "7",  "sync:ver:deal:7")]
    public void SyncVersion_ProducesExpectedKey(string objectType, string objectId, string expected)
        => CacheKeys.SyncVersion(objectType, objectId).Should().Be(expected);

    [Theory]
    [InlineData("contact", "42", "sync:del:contact:42")]
    public void SyncDeleted_ProducesExpectedKey(string objectType, string objectId, string expected)
        => CacheKeys.SyncDeleted(objectType, objectId).Should().Be(expected);

    [Fact]
    public void SyncEvent_ProducesExpectedKey()
        => CacheKeys.SyncEvent("evt-99").Should().Be("sync:evt:evt-99");

    [Fact]
    public void KanbanBoard_ReturnsStaticKey()
        => CacheKeys.KanbanBoard.Should().Be("kanban:board");

    [Theory]
    [InlineData("contact", "1", "crm:contact:1")]
    [InlineData("deal",    "2", "crm:deal:2")]
    public void CrmObject_ProducesExpectedKey(string type, string id, string expected)
        => CacheKeys.CrmObject(type, id).Should().Be(expected);
}
