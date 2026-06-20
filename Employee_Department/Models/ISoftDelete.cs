namespace Employee_Department.Models
{
    public interface ISoftDelete
    {
        bool IsDeleted { get; set; }
        DateTime? DeletedAt {  get; set; }
    }
}
