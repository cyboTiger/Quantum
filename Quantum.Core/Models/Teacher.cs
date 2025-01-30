using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace Quantum.Core.Models;

[Index(nameof(Name))]
public class Teacher
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string College { get; set; } = string.Empty;
    public decimal Rating { get; set; }
    public string Courses
    {
        get => string.Join('$', CoursesList);
        set => CoursesList = value.Split('$').ToList();
    }
    public DateTime LastUpdatedOn { get; set; }

    [NotMapped]
    public List<string> CoursesList { get; private set; } = [];
}