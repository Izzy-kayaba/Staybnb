// Protects the Admin approval boundary for both new and existing Hosts.
using Staybnb.Web.Models;

namespace Staybnb.Tests;

public class PropertyActivationTests
{
    [Fact]
    public void PendingApplication_CannotBeActivated()
    {
        var property = new HostProperty
        {
            Application = new HostApplication { Status = ApplicationStatus.Pending }
        };

        Assert.False(property.CanActivate);
    }

    [Fact]
    public void ApprovedApplication_CanBeActivated()
    {
        var property = new HostProperty
        {
            Application = new HostApplication { Status = ApplicationStatus.Approved }
        };

        Assert.True(property.CanActivate);
    }

    [Fact]
    public void SeededPropertyWithoutApplication_CanBeActivated()
    {
        var property = new HostProperty();

        Assert.True(property.CanActivate);
    }
}
