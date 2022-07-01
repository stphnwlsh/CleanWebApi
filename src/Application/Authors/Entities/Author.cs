namespace CleanWebApi.Application.Authors.Entities;

using CleanWebApi.Application.Common.Entities;
using CleanWebApi.Application.Reviews.Entities;

public record Author : Entity
{
    public string FirstName { get; set; }

    public string LastName { get; set; }

    public ICollection<Review> Reviews { get; set; }
}
