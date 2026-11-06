namespace CleanWebApi.Infrastructure.Databases.MovieReviews.Extensions;

using Bogus;
using Models;

internal static class MovieReviewsDbContextExtensions
{
    // A fixed seed and reference date give the same sample data on every run.
    private const int Seed = 20211014;
    private static readonly DateTime ReferenceDate = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    // Made-up titles, picked so that searches like "space", "love", "murder" and "dragon" find something.
    private static readonly string[] Titles =
    [
        "Deep Space Gardeners",
        "The Last Space Lighthouse",
        "Postcards from Outer Space",
        "Space Station Bake Sale",
        "Love Letters to a Lighthouse Keeper",
        "Love and Other Spreadsheets",
        "The Accidental Love Algorithm",
        "Love on the Night Ferry",
        "Murder at the Marmalade Factory",
        "A Quiet Murder in Wagga Wagga",
        "Murder on the Midnight Tram",
        "The Book Club Murder Rota",
        "The Dragon Who Hated Heights",
        "Dragon Tax Season",
        "Dragon of the Salt Flats",
        "Apprentice to a Retired Dragon",
        "The Long Drive to Coober Pedy",
        "Ghosts of the Hardware Aisle",
        "The Haunted Car Wash",
        "Seven Days in Saltwater Bay",
        "The Cartographer's Last Map",
        "Thunder Over the Wheat Belt",
        "The Beekeeper's Alibi",
        "Midnight at the Laundromat",
        "The Ninth Lifeguard",
        "Lawn Bowls at Dawn",
        "The Great Sourdough Heist",
        "Heist at the Lost Property Office",
        "Rust and Rain",
        "Tin Roof Summer",
        "The Clockmaker's Overdraft",
        "Wedding at the Caravan Park",
        "The Substitute Mayor",
        "Echoes Under the Ice Shelf",
        "The Robot Who Forgot Tuesday",
        "Night Shift on Mars Base Four",
        "Time Machines Repaired While You Wait",
        "The Pirate Accountant",
        "Treasure of the Tidal Flats",
        "My Neighbour the Werewolf Plumber",
        "Zombies of the Bowling Green",
        "Final Whistle at Shark Bay",
        "The Goalkeeper's Grandmother",
        "Wild Horses of the High Country",
        "The Mountain Does Not Care",
        "Letters from the Lost Valley",
        "The Last Train to Nowhere Junction",
        "The Violin in the Attic",
        "Two Tickets to the Moon Festival",
        "The Courtroom Cockatoo"
    ];

    // What reviewers say, one row per star rating.
    private static readonly string[][] ReviewTexts =
    [
        ["Two hours I will never get back.", "I asked for a refund and a hug.", "The popcorn had a better plot."],
        ["Some good ideas, lost in the second act.", "The trailer was better.", "Pretty to look at, hard to care about."],
        ["Fine for a rainy Sunday.", "Solid, if you've got nothing else on.", "Not bad, not great, mostly fine."],
        ["Great film, with one scene too many.", "Funny, warm and better than it needed to be.", "I'd happily watch it again."],
        ["An instant favourite.", "Great film. I cried at the ending.", "Perfect from the first scene to the last."]
    ];

    public static MovieReviewsDbContext AddData(this MovieReviewsDbContext context)
    {
        if (context.Authors.Any())
        {
            return context;
        }

        var authors = new Faker<Author>()
            .UseSeed(Seed)
            .RuleFor(a => a.Id, f => f.Random.Guid())
            .RuleFor(a => a.FirstName, f => f.Person.FirstName)
            .RuleFor(a => a.LastName, f => f.Person.LastName)
            .RuleFor(a => a.DateCreated, f => f.Date.Past(refDate: ReferenceDate))
            .RuleFor(a => a.DateModified, f => f.Date.Past(refDate: ReferenceDate))
            .Generate(15);

        context.AddRange(authors);

        var movies = new Faker<Movie>()
            .UseSeed(Seed)
            .RuleFor(m => m.Id, f => f.Random.Guid())
            .RuleFor(m => m.Title, f => Titles[f.IndexFaker])
            .RuleFor(m => m.DateCreated, f => f.Date.Past(refDate: ReferenceDate))
            .RuleFor(m => m.DateModified, f => f.Date.Past(refDate: ReferenceDate))
            .Generate(50);

        context.AddRange(movies);

        var reviews = new Faker<Review>()
            .UseSeed(Seed)
            .RuleFor(r => r.Id, f => f.Random.Guid())
            .RuleFor(r => r.ReviewedMovieId, f => f.PickRandom(movies).Id)
            .RuleFor(r => r.ReviewAuthorId, f => f.PickRandom(authors).Id)
            .RuleFor(r => r.Stars, f => f.Random.Number(1, 5))
            .RuleFor(r => r.Text, (f, r) => f.PickRandom(ReviewTexts[r.Stars - 1]))
            .RuleFor(r => r.DateCreated, f => f.Date.Past(refDate: ReferenceDate))
            .RuleFor(r => r.DateModified, f => f.Date.Past(refDate: ReferenceDate))
            .Generate(150);

        context.AddRange(reviews);

        _ = context.SaveChanges();

        return context;
    }
}
