using Haven.Domain.Entities;
using Haven.Domain.Enums;
using Haven.Domain.ValueObjects;
using Haven.Testing.Common;

using Microsoft.EntityFrameworkCore;

using Shouldly;

namespace Haven.Infrastructure.Tests.Persistence;

[Category("Unit")]
public sealed class NotificationQuietHoursPersistenceTests
{
    [Test]
    public async Task ChannelConfig_ShouldRoundTripQuietHours()
    {
        await using var context = TestDbContextFactory.CreateUnitDbContext();
        var quietHours = QuietHours.From(
            [DayOfWeek.Friday, DayOfWeek.Saturday], new TimeOnly(22, 30), new TimeOnly(7, 15), "Europe/Berlin");
        var channel = NotificationChannelConfig.Create("ops", NotificationChannel.Discord, "{}", enabled: true);
        channel.SetQuietHours(quietHours);
        context.NotificationChannelConfigs.Add(channel);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var loaded = await context.NotificationChannelConfigs.SingleAsync(c => c.Id == channel.Id);

        loaded.QuietHours.ShouldBe(quietHours);
    }

    [Test]
    public async Task ChannelConfig_WithoutQuietHours_ShouldLoadNull()
    {
        await using var context = TestDbContextFactory.CreateUnitDbContext();
        var channel = NotificationChannelConfig.Create("ops", NotificationChannel.Discord, "{}", enabled: true);
        context.NotificationChannelConfigs.Add(channel);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var loaded = await context.NotificationChannelConfigs.SingleAsync(c => c.Id == channel.Id);

        loaded.QuietHours.ShouldBeNull();
    }

    [Test]
    public async Task ChannelConfig_ClearingQuietHours_ShouldPersistNull()
    {
        await using var context = TestDbContextFactory.CreateUnitDbContext();
        var channel = NotificationChannelConfig.Create("ops", NotificationChannel.Discord, "{}", enabled: true);
        channel.SetQuietHours(QuietHours.From([DayOfWeek.Monday], new TimeOnly(0, 0), new TimeOnly(0, 0), "UTC"));
        context.NotificationChannelConfigs.Add(channel);
        await context.SaveChangesAsync();

        channel.SetQuietHours(null);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var loaded = await context.NotificationChannelConfigs.SingleAsync(c => c.Id == channel.Id);
        loaded.QuietHours.ShouldBeNull();
    }
}